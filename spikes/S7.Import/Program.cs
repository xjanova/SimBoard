using SimBoard.Document;
using SimBoard.Spice;

// ─────────────────────────────────────────────────────────────────────────────
// S7 — import, measured against real files.
//
// The corpus is the ngspice example set: netlists written by other people and
// other tools, not by us. A parser only tested on its own output tells you
// nothing, so the number that matters is what fraction of those files come in.
// ─────────────────────────────────────────────────────────────────────────────

var root = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tools", "Spice64", "examples");
root = Path.GetFullPath(root);

if (!Directory.Exists(root))
{
    Console.WriteLine($"corpus not found at {root} — run tools/fetch-ngspice.ps1");
    return 1;
}

var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
    .Where(f => f.EndsWith(".cir", StringComparison.OrdinalIgnoreCase)
             || f.EndsWith(".net", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f, StringComparer.Ordinal)
    .ToList();

Console.WriteLine($"corpus: {files.Count} files under {root}");
Console.WriteLine();

int clean = 0, partial = 0, empty = 0, crashed = 0;
long totalElements = 0, totalRecognised = 0;
var failures = new List<(string File, string Why)>();
var bigWins = new List<(string File, int Parts, int Nets)>();

foreach (var file in files)
{
    string text;
    try { text = File.ReadAllText(file); }
    catch (IOException) { continue; }

    try
    {
        var result = SpiceNetlistImporter.Import(text, Path.GetFileNameWithoutExtension(file));
        totalElements += result.Stats.Elements;
        totalRecognised += result.Stats.Recognised;

        if (result.Stats.Recognised == 0) { empty++; failures.Add((Rel(file), "no placeable elements")); }
        else if (result.Stats.Skipped == 0) clean++;
        else partial++;

        if (result.Stats.Recognised >= 8)
        {
            var nets = result.Document.ExtractNets();
            bigWins.Add((Rel(file), result.Stats.Recognised, nets.Count));
        }
    }
    catch (Exception e)
    {
        crashed++;
        failures.Add((Rel(file), e.GetType().Name + ": " + e.Message));
    }
}

Section("what came in");
Console.WriteLine($"  fully parsed      {clean,4}  ({clean * 100.0 / files.Count:0.0} %)");
Console.WriteLine($"  partly parsed     {partial,4}  ({partial * 100.0 / files.Count:0.0} %)");
Console.WriteLine($"  nothing placeable {empty,4}  ({empty * 100.0 / files.Count:0.0} %)");
Console.WriteLine($"  threw             {crashed,4}");
Console.WriteLine($"  elements: {totalRecognised:N0} of {totalElements:N0} recognised " +
                  $"({(totalElements == 0 ? 0 : totalRecognised * 100.0 / totalElements):0.0} %)");

Section("largest circuits that came in");
foreach (var (f, parts, nets) in bigWins.OrderByDescending(x => x.Parts).Take(8))
    Console.WriteLine($"  {parts,4} parts  {nets,4} nets   {f}");

Section("a real file, imported and re-emitted");
{
    // The proof that import is not just counting lines: take a file, bring it in, and
    // generate a netlist back out. If the node count survives, the connectivity did.
    // Prefer a small, readable circuit — a 5,000-node benchmark proves parsing but shows
    // nothing a person can check by eye.
    var smallest = bigWins.OrderBy(x => x.Parts).FirstOrDefault();
    var candidate = files.FirstOrDefault(f => f.EndsWith("rc.cir", StringComparison.OrdinalIgnoreCase))
                 ?? (smallest.File is { } name ? files.FirstOrDefault(f => Rel(f) == name) : null)
                 ?? files[0];

    var imported = SpiceNetlistImporter.Import(File.ReadAllText(candidate), "roundtrip");
    Console.WriteLine($"  source: {Rel(candidate)}");
    Console.WriteLine($"  {imported.Stats.Recognised} parts · {imported.Stats.Nodes} nodes in the file");

    var nets = imported.Document.ExtractNets();
    Console.WriteLine($"  {nets.Count} nets after extraction: {string.Join(", ", nets.Take(8).Select(n => n.SpiceName))}");

    var rebuilt = NetlistBuilder.Build(imported.Document, Analysis.OperatingPoint());
    Console.WriteLine();
    Console.WriteLine("  ── re-emitted deck ──");
    foreach (var line in rebuilt.Deck.TrimEnd().Split('\n').Take(16))
        Console.WriteLine($"  │ {line.TrimEnd()}");

    foreach (var w in imported.Warnings) Console.WriteLine($"  ⚠ {w}");
}

Section("why the rest did not come in");
foreach (var g in failures.GroupBy(f => Bucket(f.Why)).OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"  {g.Count(),4}  {g.Key}");
    foreach (var f in g.Take(3)) Console.WriteLine($"        {f.File}");
}

return 0;

static string Rel(string path)
{
    int i = path.IndexOf("examples", StringComparison.OrdinalIgnoreCase);
    return i >= 0 ? path[i..].Replace('\\', '/') : Path.GetFileName(path);
}

static string Bucket(string why) => why.StartsWith("no placeable")
    ? "no top-level R/C/L/D/Q/M/V — usually a pure .subckt library or an XSPICE deck"
    : why;

static void Section(string name)
{
    Console.WriteLine();
    Console.WriteLine($"── {name} ".PadRight(78, '─'));
}
