using SimBoard.Document;
using SimBoard.Spice;

// ─────────────────────────────────────────────────────────────────────────────
// S6 — audit of the calculation path.
//
// Written to look for specific things believed to be WRONG, not to confirm that
// what already passed still passes. Each section states the trap it is hunting.
// A section that fails here is the point of the exercise.
// ─────────────────────────────────────────────────────────────────────────────

int failures = 0;

Section("A · SPICE reads 'M' as milli, not mega");
{
    // The trap: everywhere else in electronics 1M means one megohm. In SPICE it means
    // one milliohm — a factor of a billion, silently, in the direction of a short.
    var doc = Divider("1M", "1k");
    var deck = NetlistBuilder.Build(doc, Analysis.Transient(1e-6, 1e-3));
    var result = await new NgspiceRunner().RunAsync(deck.Deck);

    var mid = deck.Nets.First(n => n.Connections.Any(c => c.Part.Designator == "R2" && c.Pin.Name == "A"));
    double v = result.Require(mid.SpiceName).Values[^1];

    // If 1M were read as 1 megohm the divider would give 9 * 1k/1001k = 8.99 mV.
    // If it is read as 1 milliohm we get essentially the whole 9 V.
    double asMegohm = 9.0 * 1000 / (1_000_000 + 1000);
    Report(Math.Abs(v - asMegohm) < 0.01,
        $"R = \"1M\" behaves as {(Math.Abs(v - asMegohm) < 0.01 ? "1 megohm" : $"{v:0.###} V at the midpoint — NOT a megohm")}");
}

Section("B · a switch must not take an element name that belongs to a resistor");
{
    var doc = new CircuitDocument { Title = "collision" };
    var v1 = doc.Place(PartCatalog.Require("VDC"), new GridPoint(0, 0));
    var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(10, 0));
    var sw = doc.Place(PartCatalog.Require("SW-PUSH"), new GridPoint(20, 0));
    var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(10, 20));

    var deck = NetlistBuilder.Build(doc, Analysis.OperatingPoint()).Deck;
    var elements = deck.Split('\n')
        .Select(l => l.Trim())
        .Where(l => l.Length > 0 && char.IsLetter(l[0]) && !l.StartsWith('.') && !l.StartsWith('*'))
        .Select(l => l.Split(' ')[0])
        .ToList();

    Report(elements.Distinct().Count() == elements.Count,
        $"every element name in the deck is unique: {string.Join(", ", elements)}");
}

Section("C · a potentiometer has three terminals, and the wiper is the point of it");
{
    var pot = PartCatalog.Require("POT");
    var doc = new CircuitDocument { Title = "pot" };
    var p = doc.Place(pot, new GridPoint(0, 0));
    var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(0, 20));
    doc.Connect(p.PinAt(pot.PinByNumber("3")!), gnd.PinAt(PartCatalog.Require("GND").PinByNumber("1")!));

    var deck = NetlistBuilder.Build(doc, Analysis.OperatingPoint()).Deck;
    var wiperNode = deck.Contains("RV1", StringComparison.OrdinalIgnoreCase);

    // A pot emitted as a single two-terminal resistor throws the wiper away, which makes
    // every divider, volume control and sensor bias built from one silently wrong.
    int resistorLines = deck.Split('\n').Count(l => l.TrimStart().StartsWith("R", StringComparison.OrdinalIgnoreCase));
    Report(resistorLines >= 2,
        $"a potentiometer emits two resistances around the wiper (found {resistorLines} resistor line(s))");
}

Section("D · net names must not change between runs of the same circuit");
{
    var a = Divider("2k", "1k");
    var b = Divider("2k", "1k");

    var namesA = a.ExtractNets().Select(n => $"{n.Name}:{string.Join(",", n.Connections.Select(c => c.Part.Designator + "." + c.Pin.Name).OrderBy(x => x))}").OrderBy(x => x).ToList();
    var namesB = b.ExtractNets().Select(n => $"{n.Name}:{string.Join(",", n.Connections.Select(c => c.Part.Designator + "." + c.Pin.Name).OrderBy(x => x))}").OrderBy(x => x).ToList();

    Report(namesA.SequenceEqual(namesB),
        "the same circuit produces the same net names twice");

    // And repeated extraction of ONE document must be stable too, or a scope trace
    // labelled N002 stops meaning the same wire after any edit elsewhere.
    var first = a.ExtractNets().Select(n => n.Name).ToList();
    var second = a.ExtractNets().Select(n => n.Name).ToList();
    Report(first.SequenceEqual(second), "extracting twice from one document is stable");
}

Section("E · rotation has to put pins where the symbol actually is");
{
    var r = PartCatalog.Require("R");        // 4x2 body, pins left and right
    foreach (var rot in new[] { Rotation.R0, Rotation.R90, Rotation.R180, Rotation.R270 })
    {
        var doc = new CircuitDocument();
        var part = doc.Place(r, new GridPoint(10, 10), rot);
        var pins = part.PinPositions().ToList();

        var (w, h) = CircuitDocument.Footprint(part);
        var body = (X: 10, Y: 10, W: w, H: h);

        bool allOutside = pins.All(p =>
            p.At.X < body.X || p.At.X >= body.X + body.W ||
            p.At.Y < body.Y || p.At.Y >= body.Y + body.H);

        double distance = Math.Sqrt(
            Math.Pow(pins[0].At.X - pins[1].At.X, 2) + Math.Pow(pins[0].At.Y - pins[1].At.Y, 2));

        Report(allOutside && distance > 1,
            $"{rot,-5} pins at {pins[0].At} and {pins[1].At}, body {w}x{h} — outside the body and {distance:0.#} apart");
    }
}

Section("F · engineering values survive being shown and typed back");
{
    // What matters is the MAGNITUDE that reaches SPICE, not the spelling. Values are
    // deliberately normalised to a plain number so no suffix convention can be misread —
    // this check was originally written to assert the text passed through unchanged,
    // which stopped being the right behaviour once "1M" was found to mean milliohm.
    foreach (var (typed, expected, meaning) in new[]
             {
                 ("4k7", 4700.0, "the infix style people write by hand"),
                 ("100n", 100e-9, "nano"),
                 ("10u", 10e-6, "micro, ASCII"),
                 ("2.2µ", 2.2e-6, "micro sign, as the UI prints it"),
                 ("1meg", 1e6, "mega, the SPICE spelling"),
                 ("1M", 1e6, "mega, the way every engineer writes it"),
                 ("1m", 1e-3, "milli, lower case"),
                 ("1R2", 1.2, "1.2 ohms"),
             })
    {
        var parsed = SpiceValue.Parse(typed);
        bool right = parsed is { } v && Math.Abs(v - expected) <= Math.Abs(expected) * 1e-9;
        Report(right, $"\"{typed}\" ({meaning}) = {(parsed?.ToString("G6") ?? "unparsed")}, expected {expected:G6}");
    }

    // And a waveform spec must pass through untouched — normalising it would destroy it.
    var pulse = "PULSE(0 5 0 1u 1u 500u 1m)";
    Report(SpiceValue.ForSpice(pulse) == pulse, "a PULSE spec reaches SPICE unchanged");
}

Section("G · what the import screen actually supports");
{
    // The mock lists Gerber, Excellon, KiCad, Eagle, Altium and image tracing. None of
    // that is implemented. Saying so here keeps the gap visible instead of letting a
    // dialog full of formats imply otherwise.
    string[] promised = ["Gerber RS-274X", "Excellon", "KiCad", "Eagle .brd", "Altium .PcbDoc", "image trace"];
    var implemented = new List<string>();
    Report(implemented.Count == 0 || implemented.Count == promised.Length,
        $"import formats implemented: {implemented.Count}/{promised.Length} — " +
        $"still only .sbp round-trips ({string.Join(", ", promised)} are dialog text only)");
}

Console.WriteLine();
Console.WriteLine(failures == 0
    ? "S6 — no defects found in the audited paths."
    : $"S6 — {failures} defect(s) found. That is the point of the pass.");
return 0;   // an audit reports; it does not fail the build

// ── helpers ────────────────────────────────────────────────────────────────

static CircuitDocument Divider(string r1Value, string r2Value)
{
    var doc = new CircuitDocument { Title = "divider" };
    var v1 = doc.Place(PartCatalog.Require("VDC"), new GridPoint(0, 4));
    var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(8, 2));
    var r2 = doc.Place(PartCatalog.Require("R"), new GridPoint(18, 2));
    var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(8, 14));
    v1.Value = "9";
    r1.Value = r1Value;
    r2.Value = r2Value;

    Wire(doc, v1, "1", r1, "1");
    Wire(doc, r1, "2", r2, "1");
    Wire(doc, r2, "2", gnd, "1");
    Wire(doc, v1, "2", gnd, "1");
    return doc;
}

static void Wire(CircuitDocument doc, PartInstance a, string aPin, PartInstance b, string bPin)
{
    var pinA = a.Definition.PinByNumber(aPin)!;
    var pinB = b.Definition.PinByNumber(bPin)!;
    var (pa, pb) = (a.PinAt(pinA), b.PinAt(pinB));
    var ea = Escape(pa, pinA.Side);
    var eb = Escape(pb, pinB.Side);
    var corner = pinA.Side is PinSide.Left or PinSide.Right
        ? new GridPoint(ea.X, eb.Y) : new GridPoint(eb.X, ea.Y);

    doc.Connect(pa, ea);
    doc.Connect(ea, corner);
    doc.Connect(corner, eb);
    doc.Connect(eb, pb);

    static GridPoint Escape(GridPoint p, PinSide s) => s switch
    {
        PinSide.Left => p.Offset(-2, 0),
        PinSide.Right => p.Offset(2, 0),
        PinSide.Top => p.Offset(0, -2),
        _ => p.Offset(0, 2),
    };
}

void Section(string name)
{
    Console.WriteLine();
    Console.WriteLine($"── {name} ".PadRight(78, '─'));
}

void Report(bool ok, string message)
{
    if (!ok) failures++;
    Console.WriteLine($"  [{(ok ? "ok  " : "GAP ")}] {message}");
}
