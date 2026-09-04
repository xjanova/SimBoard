using System.Text.RegularExpressions;

namespace SimBoard.Document;

/// <summary>
/// Imports a SPICE netlist — the interchange format ngspice, LTspice, PSpice and KiCad
/// all speak — into an editable document.
///
/// A netlist says what connects to what and nothing about where anything sat. There is no
/// geometry to recover, so this places parts on a grid and joins them with net labels
/// rather than inventing wire routes that were never drawn. That is what every schematic
/// tool does with a netlist import, and it is honest: the connectivity is exactly the
/// file's, the layout is admittedly ours.
/// </summary>
public static partial class SpiceNetlistImporter
{
    public sealed record Stats(
        int Elements,
        int Recognised,
        int Skipped,
        int Nodes,
        int SubcircuitsSkipped);

    public sealed record Result(
        CircuitDocument Document,
        Stats Stats,
        IReadOnlyList<string> Warnings);

    public static Result Import(string text, string title = "imported")
    {
        var doc = new CircuitDocument { Title = title };
        var warnings = new List<string>();
        var models = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var lines = Unfold(text);

        // First pass: .model cards tell us whether a Q is NPN or PNP, which decides the
        // symbol and cannot be guessed from the element line alone.
        foreach (var line in lines)
        {
            var m = ModelCard().Match(line);
            if (m.Success) models[m.Groups["name"].Value] = m.Groups["type"].Value.ToUpperInvariant();
        }

        // Subcircuit definitions first: an X call cannot be placed until its pin list
        // is known, and the definition may appear after the call in the file.
        var subcircuits = CollectSubcircuits(lines);

        int elements = 0, recognised = 0, skipped = 0, subckts = subcircuits.Count;
        int column = 0, row = 0;
        var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool insideSubckt = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '*') continue;

            if (line.StartsWith(".subckt", StringComparison.OrdinalIgnoreCase)) { insideSubckt = true; subckts++; continue; }
            if (line.StartsWith(".ends", StringComparison.OrdinalIgnoreCase)) { insideSubckt = false; continue; }
            if (insideSubckt) continue;          // only the top level is placed
            if (line[0] == '.') continue;        // analysis and option cards carry no parts

            elements++;
            var tokens = Tokenise(line);
            if (tokens.Count < 3) { skipped++; continue; }

            var (def, pinNodes, value) = Interpret(tokens, models, subcircuits, warnings);
            if (def is null) { skipped++; continue; }

            // Lay parts out in a readable grid. Nothing about this is the original layout,
            // and pretending otherwise would be worse than admitting it.
            var at = new GridPoint(2 + column * 14, 2 + row * 12);
            if (++column >= 8) { column = 0; row++; }

            var part = doc.Place(def, at);
            part.Designator = tokens[0].ToUpperInvariant();
            part.Value = value;
            recognised++;

            // A label per pin carries the file's connectivity with no invented geometry.
            foreach (var (pin, node) in def.Pins.Zip(pinNodes))
            {
                nodes.Add(node);
                doc.Labels.Add(new NetLabel
                {
                    Id = $"l{doc.Labels.Count + 1}",
                    Name = NormaliseNode(node),
                    At = part.PinAt(pin),
                });
            }
        }

        // Ground is a symbol on a sheet and a node number in a file. Placing one and
        // labelling it "0" is what turns the file's node 0 into a real ground net.
        if (nodes.Contains("0"))
        {
            var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(2, 4 + (row + 1) * 12));
            doc.Labels.Add(new NetLabel
            {
                Id = $"l{doc.Labels.Count + 1}",
                Name = "0",
                At = gnd.PinAt(PartCatalog.Require("GND").Pins[0]),
            });
        }
        else
        {
            warnings.Add("ไฟล์นี้ไม่มีโหนด 0 — วงจรจะไม่มีกราวด์ ต้องวางสัญลักษณ์ GND เองก่อนซิม");
        }

        if (subckts > 0)
            warnings.Add($"พบ .subckt {subckts} บล็อก — แต่ละบล็อกกลายเป็นอุปกรณ์หนึ่งตัวตามรายการขาของมัน " +
                         "วงจรข้างในยังไม่ได้กางออกมา");

        if (skipped > 0)
            warnings.Add($"มี {skipped} บรรทัดที่ยังแปลงไม่ได้ จากทั้งหมด {elements} อิลิเมนต์");

        doc.ReseedIds();
        return new Result(doc, new Stats(elements, recognised, skipped, nodes.Count, subckts), warnings);
    }

    /// <summary>
    /// Joins continuation lines. SPICE wraps a long card with a leading '+', and a parser
    /// that reads line by line silently truncates every multi-line model in the file.
    /// </summary>
    private static List<string> Unfold(string text)
    {
        var result = new List<string>();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = StripInlineComment(raw);
            if (line.TrimStart().StartsWith('+') && result.Count > 0)
                result[^1] += " " + line.TrimStart()[1..].Trim();
            else
                result.Add(line);
        }
        return result;
    }

    private static string StripInlineComment(string line)
    {
        int i = line.IndexOf(';');
        return i >= 0 ? line[..i] : line;
    }

    private static List<string> Tokenise(string line) =>
        [.. line.Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries)];

    /// <summary>
    /// Node names are case-insensitive in SPICE and often numeric. Normalising keeps
    /// "OUT", "out" and "Out" from becoming three separate nets on import.
    /// </summary>
    private static string NormaliseNode(string node) => node.ToUpperInvariant();

    /// <summary>
    /// Reads every .subckt header and turns it into a placeable part.
    ///
    /// The header carries the name and the pin list in order, which is exactly what an X
    /// call needs. Nothing about the inner circuit is required to place the call — and not
    /// flattening it is deliberate: a subcircuit is a black box on the sheet, the same way
    /// it is in the file.
    /// </summary>
    private static Dictionary<string, PartDefinition> CollectSubcircuits(List<string> lines)
    {
        var found = new Dictionary<string, PartDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (!line.TrimStart().StartsWith(".subckt", StringComparison.OrdinalIgnoreCase)) continue;

            var t = Tokenise(line);
            if (t.Count < 2) continue;

            var name = t[1];
            // Trailing name=value parameters are defaults, not pins.
            var pinNames = t.Skip(2).TakeWhile(x => !x.Contains('=')).ToList();
            if (pinNames.Count == 0 || found.ContainsKey(name)) continue;

            var pins = new List<Pin>();
            for (int i = 0; i < pinNames.Count; i++)
            {
                bool left = i < (pinNames.Count + 1) / 2;
                pins.Add(new Pin(
                    (i + 1).ToString(),
                    pinNames[i],
                    PinKind.Passive,          // a header says nothing about direction
                    left ? PinSide.Left : PinSide.Right,
                    left ? i : pinNames.Count - 1 - i));
            }

            found[name] = new PartDefinition
            {
                Key = "X-" + name,
                Prefix = "X",
                Name = name,
                NameTh = $"บล็อกย่อย {name}",
                Symbol = SymbolShape.IcBody,
                Spice = SpiceKind.Subcircuit,
                SpiceModel = name,
                Provenance = Provenance.Unverified,
                BodyWidth = 8,
                BodyHeight = Math.Max(4, pinNames.Count),
                Pins = pins,
                NoteTh = "มาจาก .subckt ในไฟล์ที่นำเข้า — ทิศทางของขายังไม่รู้ เพราะหัว .subckt ไม่ได้บอก",
            };
        }

        return found;
    }

    /// <summary>
    /// Turns one element line into a catalogue part, its node list and its value.
    /// Returns null when the element type is not something the editor can place yet.
    /// </summary>
    private static (PartDefinition? Def, string[] Nodes, string? Value) Interpret(
        List<string> t, Dictionary<string, string> models,
        Dictionary<string, PartDefinition> subcircuits, List<string> warnings)
    {
        char kind = char.ToUpperInvariant(t[0][0]);

        switch (kind)
        {
            case 'R' when t.Count >= 4:
                return (PartCatalog.Find("R"), [t[1], t[2]], t[3]);

            case 'C' when t.Count >= 4:
                return (PartCatalog.Find("C"), [t[1], t[2]], t[3]);

            case 'L' when t.Count >= 4:
                return (PartCatalog.Find("L"), [t[1], t[2]], t[3]);

            case 'D' when t.Count >= 4:
                // Match the file's model to a catalogue part when we have it, so an
                // imported 1N4007 stays a 1N4007 rather than becoming a generic diode.
                var diode = FindByModel(t[3], SymbolShape.Diode) ?? PartCatalog.Find("D-1N4148");
                return (diode, [t[1], t[2]], t[3]);

            case 'Q' when t.Count >= 5:
            {
                // SPICE order is collector base emitter; the catalogue's pins are B C E.
                var model = t[^1];
                bool pnp = models.GetValueOrDefault(model, "NPN").Contains("PNP");
                var q = FindByModel(model, pnp ? SymbolShape.BjtPnp : SymbolShape.BjtNpn)
                        ?? PartCatalog.Find(pnp ? "Q-2N3906" : "Q-2N3904");
                return (q, [t[2], t[1], t[3]], model);
            }

            case 'M' when t.Count >= 6:
            {
                var model = t[^1];
                bool pch = models.GetValueOrDefault(model, "NMOS").Contains("PMOS");
                var fet = FindByModel(model, pch ? SymbolShape.MosfetP : SymbolShape.MosfetN)
                          ?? PartCatalog.Find("Q-IRFZ44N");
                // drain gate source -> catalogue G D S
                return (fet, [t[2], t[1], t[3]], model);
            }

            case 'V' when t.Count >= 4:
            {
                var spec = string.Join(' ', t.Skip(3));
                bool timeVarying = spec.Contains("PULSE", StringComparison.OrdinalIgnoreCase)
                                || spec.Contains("SIN", StringComparison.OrdinalIgnoreCase)
                                || spec.Contains("PWL", StringComparison.OrdinalIgnoreCase);
                var src = PartCatalog.Find(timeVarying ? "VPULSE" : "VDC");
                var value = timeVarying ? spec : spec.Replace("DC", "", StringComparison.OrdinalIgnoreCase).Trim();
                return (src, [t[1], t[2]], value);
            }

            case 'X' when t.Count >= 3:
            {
                // The last token is the subcircuit name; everything between is its nodes.
                var name = t[^1];
                if (!subcircuits.TryGetValue(name, out var sub)) return (null, [], null);

                var callNodes = t.Skip(1).Take(t.Count - 2).ToArray();
                if (callNodes.Length != sub.Pins.Count)
                {
                    // A call whose node count disagrees with the definition is a real error
                    // in the file. Placing it anyway would attach nets to the wrong pins.
                    warnings.Add($"{t[0]} เรียก {name} ด้วย {callNodes.Length} โหนด " +
                                 $"แต่ .subckt ประกาศไว้ {sub.Pins.Count} ขา — ข้ามไป");
                    return (null, [], null);
                }
                return (sub, callNodes, name);
            }

            default:
                return (null, [], null);
        }
    }

    /// <summary>Finds a catalogue part whose SPICE model or part number matches.</summary>
    private static PartDefinition? FindByModel(string model, SymbolShape shape) =>
        PartCatalog.All.FirstOrDefault(p =>
            p.Symbol == shape &&
            (string.Equals(p.SpiceModel, model, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(p.Mpn, model, StringComparison.OrdinalIgnoreCase) ||
             (p.Mpn is not null && model.Contains(p.Mpn, StringComparison.OrdinalIgnoreCase))));

    [GeneratedRegex(@"^\s*\.model\s+(?<name>\S+)\s+(?<type>\w+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ModelCard();
}
