using System.Globalization;
using System.Text;

namespace SimBoard.Document;

/// <summary>What analysis the transport bar asked for.</summary>
public sealed record Analysis(string Card, string Description)
{
    public static Analysis Transient(double stepSeconds, double stopSeconds) =>
        new($".tran {Eng(stepSeconds)} {Eng(stopSeconds)} uic",
            $"transient 0–{Eng(stopSeconds)}s");

    public static Analysis OperatingPoint() => new(".op", "operating point");

    public static Analysis Ac(int pointsPerDecade, double startHz, double stopHz) =>
        new($".ac dec {pointsPerDecade} {Eng(startHz)} {Eng(stopHz)}",
            $"AC {Eng(startHz)}–{Eng(stopHz)}Hz");

    private static string Eng(double v) => v.ToString("G6", CultureInfo.InvariantCulture);
}

/// <summary>A netlist plus everything the UI needs to explain what it did.</summary>
public sealed record NetlistResult(
    string Deck,
    IReadOnlyList<Net> Nets,
    IReadOnlyList<string> Approximations,
    IReadOnlyList<string> Blockers)
{
    /// <summary>False when the deck cannot be simulated as it stands.</summary>
    public bool CanSimulate => Blockers.Count == 0;
}

/// <summary>
/// Turns the placed circuit into a SPICE deck.
///
/// Only what is on the sheet goes in. If pressing Play produced anything other than the
/// drawn circuit, every number the instruments show would be a lie — so this walks the
/// real nets from <see cref="CircuitDocument.ExtractNets"/> and nothing else.
///
/// Digital parts cannot be modelled from physics: SPICE has no way to run an ESP32's
/// firmware. They are emitted as their electrical envelope — supply draw, output drive,
/// input impedance — so the analog circuit around them still solves for real, and every
/// such substitution is reported in <see cref="NetlistResult.Approximations"/> rather
/// than passed off as exact.
/// </summary>
public static class NetlistBuilder
{
    public static NetlistResult Build(CircuitDocument doc, Analysis analysis)
    {
        var nets = doc.ExtractNets();
        var approximations = new List<string>();
        var blockers = new List<string>();
        var body = new StringBuilder();
        var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!nets.Any(n => n.IsGround))
            blockers.Add("วงจรยังไม่มีกราวด์ — ซิมูเลเตอร์ต้องมีจุดอ้างอิงถึงจะแก้สมการได้ วางสัญลักษณ์ GND อย่างน้อยหนึ่งจุด");

        // Net name per pin, so each element can look its own nodes up.
        var nodeOf = new Dictionary<(string PartId, string PinNumber), string>();
        foreach (var net in nets)
            foreach (var (part, pin) in net.Connections)
                nodeOf[(part.Id, pin.Number)] = net.SpiceName;

        // Local functions cannot be overloaded, so callers pass the pin number.
        string Node(PartInstance p, string pinNumber) =>
            nodeOf.TryGetValue((p.Id, pinNumber), out var n) ? n : $"nc_{p.Designator}_{pinNumber}";

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in doc.Parts.OrderBy(p => p.Designator, StringComparer.Ordinal))
        {
            var def = part.Definition;
            var value = part.Value ?? def.DefaultValue ?? "";

            switch (def.Spice)
            {
                case SpiceKind.None:
                    break;   // ground symbols and connectors carry no element of their own

                case SpiceKind.Primitive:
                    EmitPrimitive(body, part, def, value, Node, models, approximations, usedNames);
                    break;

                case SpiceKind.Subcircuit:
                    var pins = string.Join(' ', def.Pins.Select(p => Node(part, p.Number)));
                    body.AppendLine($"X{part.Designator} {pins} {def.SpiceModel}");
                    if (def.SpiceLibrary is { } lib) models.Add($".include {lib}");
                    break;

                case SpiceKind.Behavioural:
                    EmitBehavioural(body, part, def, Node, approximations);
                    break;
            }
        }

        if (doc.Parts.Count == 0)
            blockers.Add("ยังไม่มีอุปกรณ์บนผัง");

        var deck = new StringBuilder();
        deck.AppendLine($"* SimBoard — {doc.Title}");
        deck.AppendLine($"* {doc.Parts.Count} parts · {nets.Count} nets · {analysis.Description}");
        deck.AppendLine();
        foreach (var m in models.OrderBy(m => m, StringComparer.Ordinal)) deck.AppendLine(m);
        if (models.Count > 0) deck.AppendLine();
        deck.Append(body);
        deck.AppendLine();
        deck.AppendLine(analysis.Card);
        deck.AppendLine(".end");

        return new NetlistResult(deck.ToString(), nets, approximations, blockers);
    }

    private static void EmitPrimitive(
        StringBuilder b, PartInstance part, PartDefinition def, string value,
        Func<PartInstance, string, string> node, HashSet<string> models, List<string> approx,
        HashSet<string> usedNames)
    {
        string N(string pin) => node(part, pin);

        // SPICE identifies an element by its first letter, so the name has to start with
        // the right one AND be unique across the whole deck. A push button emitted as a
        // resistor took "R1" from an actual R1, and SPICE kept only one of them without
        // complaining.
        string Name(string spiceLetter)
        {
            var d = part.Designator;
            var candidate = d.StartsWith(spiceLetter, StringComparison.OrdinalIgnoreCase)
                ? d
                : spiceLetter + d;

            var unique = candidate;
            for (int i = 2; !usedNames.Add(unique); i++) unique = candidate + "_" + i;
            return unique;
        }

        // Magnitudes are normalised so SPICE cannot read "1M" as a milliohm.
        var spiceValue = SpiceValue.ForSpice(value);

        switch (def.Symbol)
        {
            case SymbolShape.Box when def.Pins.Count == 3:
            {
                // A potentiometer is two resistances meeting at the wiper. Emitting it as
                // one two-terminal resistor throws the wiper away, and with it every
                // divider, volume control and sensor bias anyone builds from it.
                double total = SpiceValue.Parse(value) ?? 10e3;
                double position = SpiceValue.Parse(part.Value) is not null ? 0.5 : 0.5;
                double upper = Math.Max(total * position, 1e-3);
                double lower = Math.Max(total - upper, 1e-3);
                var stem = Name("R");
                b.AppendLine($"{stem} {N("1")} {N("2")} {upper.ToString("G6", CultureInfo.InvariantCulture)}");
                b.AppendLine($"{stem}_B {N("2")} {N("3")} {lower.ToString("G6", CultureInfo.InvariantCulture)}");
                approx.Add($"{part.Designator} ตั้งตำแหน่งตัวปรับไว้กลางทาง (50%) — ยังปรับในโปรแกรมไม่ได้");
                break;
            }

            case SymbolShape.Box:
                b.AppendLine($"{Name("R")} {N("1")} {N("2")} {spiceValue}");
                break;

            case SymbolShape.CapacitorNonPolar:
            case SymbolShape.CapacitorPolarised:
                b.AppendLine($"{Name("C")} {N("1")} {N("2")} {spiceValue}");
                break;

            case SymbolShape.Inductor:
                b.AppendLine($"{Name("L")} {N("1")} {N("2")} {spiceValue}");
                break;

            case SymbolShape.Diode:
            case SymbolShape.Led:
            case SymbolShape.Zener:
                b.AppendLine($"{Name("D")} {N("1")} {N("2")} {def.SpiceModel}");
                if (def.SpiceModel is { } dm) models.Add(DiodeModelCard(dm));
                break;

            case SymbolShape.BjtNpn:
            case SymbolShape.BjtPnp:
                // SPICE order is collector base emitter.
                b.AppendLine($"{Name("Q")} {N("2")} {N("1")} {N("3")} {def.SpiceModel}");
                if (def.SpiceModel is { } qm)
                    models.Add(BjtModelCard(qm, def.Symbol == SymbolShape.BjtNpn));
                break;

            case SymbolShape.MosfetN:
            case SymbolShape.MosfetP:
                // drain gate source bulk — bulk tied to source, as a discrete part is.
                b.AppendLine($"{Name("M")} {N("2")} {N("1")} {N("3")} {N("3")} {def.SpiceModel}");
                if (def.SpiceModel is { } mm)
                    models.Add(MosfetModelCard(mm, def.Symbol == SymbolShape.MosfetN));
                break;

            case SymbolShape.VoltageSource:
                var spec = SpiceValue.IsMagnitude(value) ? $"DC {spiceValue}" : value;
                b.AppendLine($"{Name("V")} {N("1")} {N("2")} {spec}");
                break;

            case SymbolShape.CurrentSource:
                b.AppendLine($"{Name("I")} {N("1")} {N("2")} DC {spiceValue}");
                break;

            case SymbolShape.Switch:
                // A push button is a resistor: 1 GΩ open, 10 mΩ closed. Modelling it as a
                // real SPICE switch needs a control node the schematic does not have.
                bool closed = string.Equals(part.Value, "CLOSED", StringComparison.OrdinalIgnoreCase);
                b.AppendLine($"{Name("R")} {N("1")} {N("2")} {(closed ? "0.01" : "1e9")}");
                approx.Add($"{part.Designator} สวิตช์จำลองเป็นตัวต้านทาน {(closed ? "10 mΩ (ปิด)" : "1 GΩ (เปิด)")}");
                break;

            default:
                approx.Add($"{part.Designator} ({def.Key}) ไม่มีวิธีแปลงเป็น SPICE — ข้ามไป");
                break;
        }
    }

    /// <summary>
    /// A digital part becomes its electrical envelope: what it draws from the supply,
    /// what it drives, and what it loads. That is enough for the analog circuit around
    /// it to solve correctly, which is the honest limit of what SPICE can offer here.
    /// </summary>
    private static void EmitBehavioural(
        StringBuilder b, PartInstance part, PartDefinition def,
        Func<PartInstance, string, string> node, List<string> approx)
    {
        var spec = def.Digital;
        if (spec is null) return;

        var supply = def.Pins.FirstOrDefault(p => p.Kind == PinKind.Power);
        var ground = def.Pins.FirstOrDefault(p => p.Kind == PinKind.Ground);

        if (supply is not null && ground is not null && spec.Icc > 0)
        {
            double rEquivalent = spec.VccTypical / spec.Icc;
            b.AppendLine($"R{part.Designator}_ICC {node(part, supply.Number)} {node(part, ground.Number)} " +
                         $"{rEquivalent.ToString("G4", CultureInfo.InvariantCulture)}");
        }

        foreach (var pin in def.Pins)
        {
            switch (pin.Kind)
            {
                case PinKind.Output:
                    // Driven high through a realistic source resistance.
                    double rOut = spec.IoMax is > 0 ? spec.VccTypical / (spec.IoMax.Value * 4) : 50;
                    b.AppendLine($"V{part.Designator}_{Safe(pin.Name)} {node(part, pin.Number)}_drv 0 " +
                                 $"DC {spec.VccTypical.ToString("G4", CultureInfo.InvariantCulture)}");
                    b.AppendLine($"R{part.Designator}_{Safe(pin.Name)} {node(part, pin.Number)}_drv " +
                                 $"{node(part, pin.Number)} {rOut.ToString("G4", CultureInfo.InvariantCulture)}");
                    break;

                case PinKind.Input:
                case PinKind.Bidirectional:
                case PinKind.Analog:
                    // High-impedance input, so the net still has a DC path and solves.
                    b.AppendLine($"R{part.Designator}_{Safe(pin.Name)}_in {node(part, pin.Number)} 0 100Meg");
                    break;

                case PinKind.OpenDrain:
                    // Cannot drive high by definition — leave it to the pull-up on the net.
                    b.AppendLine($"R{part.Designator}_{Safe(pin.Name)}_od {node(part, pin.Number)} 0 1G");
                    break;
            }
        }

        approx.Add($"{part.Designator} ({def.Name}) เป็นอุปกรณ์ดิจิทัล — จำลองด้วยคุณสมบัติทางไฟฟ้า " +
                   $"(กิน {spec.Icc * 1000:G3} mA ที่ {spec.VccTypical} V) ไม่ใช่การรันเฟิร์มแวร์จริง");
    }

    private static string Suffix(PartInstance part, string prefix) =>
        part.Designator.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? part.Designator[prefix.Length..]
            : part.Designator;

    private static string Safe(string s) =>
        new([.. s.Select(c => char.IsLetterOrDigit(c) ? c : '_')]);

    // Model cards for the discrete parts the catalog ships. Kept here so a deck is
    // self-contained and does not depend on a library file being present.
    private static string DiodeModelCard(string name) => name switch
    {
        "D1N4148" => ".model D1N4148 D(IS=2.52n RS=0.568 N=1.752 CJO=4p M=0.4 TT=20n BV=100)",
        "D1N4007" => ".model D1N4007 D(IS=7.02n RS=0.0341 N=1.8 CJO=26.7p M=0.333 TT=4.32u BV=1000)",
        "D1N5819" => ".model D1N5819 D(IS=31.7u RS=0.051 N=1.37 CJO=170p M=0.38 TT=4.32n BV=40)",
        "LED_RED" => ".model LED_RED D(IS=1e-19 RS=2.0 N=1.9 CJO=30p BV=5)",
        _ => $".model {name} D(IS=1e-14 RS=0.1 N=1.0)",
    };

    private static string BjtModelCard(string name, bool npn) => name switch
    {
        "Q2N3904" => ".model Q2N3904 NPN(IS=6.734f BF=416.4 VAF=74.03 IKF=66.78m RB=10 RC=1 CJE=4.493p CJC=3.638p TF=301.2p TR=239.5n)",
        "Q2N3906" => ".model Q2N3906 PNP(IS=1.41f BF=180.7 VAF=18.7 IKF=80m RB=10 RC=2.5 CJE=8.063p CJC=9.728p TF=179.3p TR=33.42n)",
        "QBC547" => ".model QBC547 NPN(IS=7.049f BF=374.6 VAF=62.79 IKF=81.97m RB=10 RC=1 CJE=11.5p CJC=5.25p TF=410p TR=32n)",
        _ => $".model {name} {(npn ? "NPN" : "PNP")}(IS=1e-14 BF=100 VAF=100)",
    };

    private static string MosfetModelCard(string name, bool nch) => name switch
    {
        "IRFZ44N" => ".model IRFZ44N NMOS(VTO=3.7 KP=20 LAMBDA=0.001 RD=0.0175 RS=0.001 CGSO=2.5n CGDO=0.4n)",
        "IRLZ44N" => ".model IRLZ44N NMOS(VTO=1.6 KP=22 LAMBDA=0.001 RD=0.022 RS=0.001 CGSO=2.4n CGDO=0.4n)",
        _ => $".model {name} {(nch ? "NMOS" : "PMOS")}(VTO={(nch ? "2" : "-2")} KP=20)",
    };
}
