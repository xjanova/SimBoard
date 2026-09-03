namespace SimBoard.Document;

public enum RuleSeverity
{
    /// <summary>The circuit cannot be simulated, or hardware built from it will be damaged.</summary>
    Error,
    /// <summary>It will run, but it is very likely not what was meant.</summary>
    Warning,
    /// <summary>Worth knowing before ordering a board.</summary>
    Info,
}

public sealed record RuleViolation(
    RuleSeverity Severity,
    string Code,
    string Message,
    string? Designator = null,
    string? Net = null);

/// <summary>
/// Electrical rule check.
///
/// The generic rules — no ground, floating input, two drivers on one net — are what any
/// schematic tool owes you. The rest are the mistakes that actually destroy the boards
/// people build at this bench: a 5 V sensor output wired straight into a 3.3 V-only
/// ESP32 pin, an I²C bus with no pull-ups, an LED with no series resistor. Those cost
/// real hardware, and a tool that knows the parts should be the thing that catches them.
/// </summary>
public static class ElectricalRuleCheck
{
    public static IReadOnlyList<RuleViolation> Run(CircuitDocument doc)
    {
        var nets = doc.ExtractNets();
        var v = new List<RuleViolation>();

        CheckGround(nets, v);
        CheckFloating(doc, nets, v);
        CheckDriverConflicts(nets, v);
        CheckLogicLevels(nets, v);
        CheckI2CPullups(doc, nets, v);
        CheckLedSeriesResistor(doc, nets, v);
        CheckSupplyRange(doc, nets, v);

        return [.. v.OrderBy(x => x.Severity)];
    }

    private static void CheckGround(IReadOnlyList<Net> nets, List<RuleViolation> v)
    {
        if (!nets.Any(n => n.IsGround))
            v.Add(new RuleViolation(RuleSeverity.Error, "ERC001",
                "วงจรไม่มีกราวด์ — ต้องมีจุดอ้างอิงอย่างน้อยหนึ่งจุด ไม่งั้นซิมูเลเตอร์แก้สมการไม่ได้"));
    }

    private static void CheckFloating(CircuitDocument doc, IReadOnlyList<Net> nets, List<RuleViolation> v)
    {
        foreach (var net in nets.Where(n => n.PinCount == 1 && !n.IsGround))
        {
            var (part, pin) = net.Connections[0];
            if (pin.Kind is PinKind.NotConnected) continue;

            v.Add(new RuleViolation(RuleSeverity.Warning, "ERC002",
                $"ขา {pin.Name} ของ {part.Designator} ต่ออยู่ตัวเดียว ไม่ได้เชื่อมกับอะไร",
                part.Designator, net.Name));
        }

        // A pin with no net at all never reached the extractor.
        var connected = nets.SelectMany(n => n.Connections).Select(c => (c.Part.Id, c.Pin.Number)).ToHashSet();
        foreach (var part in doc.Parts)
            foreach (var pin in part.Definition.Pins)
            {
                if (connected.Contains((part.Id, pin.Number))) continue;
                if (pin.Kind is PinKind.NotConnected) continue;

                var severity = pin.Kind is PinKind.Power or PinKind.Ground
                    ? RuleSeverity.Error
                    : RuleSeverity.Warning;
                v.Add(new RuleViolation(severity, "ERC003",
                    $"ขา {pin.Name} ({pin.Kind}) ของ {part.Designator} ยังไม่ได้ต่อ" +
                    (severity == RuleSeverity.Error ? " — ขาไฟต้องต่อเสมอ" : ""),
                    part.Designator));
            }
    }

    private static void CheckDriverConflicts(IReadOnlyList<Net> nets, List<RuleViolation> v)
    {
        foreach (var net in nets)
        {
            var drivers = net.Connections.Where(c => c.Pin.Kind == PinKind.Output).ToList();
            if (drivers.Count > 1)
                v.Add(new RuleViolation(RuleSeverity.Error, "ERC004",
                    $"เนต {net.Name} มีขาเอาต์พุตขับพร้อมกัน {drivers.Count} ขา " +
                    $"({string.Join(", ", drivers.Select(d => $"{d.Part.Designator}.{d.Pin.Name}"))}) — ชนกันแล้วพัง",
                    Net: net.Name));

            var powers = net.Connections.Where(c => c.Pin.Kind == PinKind.Power).ToList();
            if (drivers.Count > 0 && powers.Count > 0)
                v.Add(new RuleViolation(RuleSeverity.Error, "ERC005",
                    $"เนต {net.Name} มีทั้งขาเอาต์พุตและขาไฟเลี้ยง — เอาต์พุตกำลังลัดกับแหล่งจ่าย",
                    Net: net.Name));
        }
    }

    /// <summary>
    /// The one that kills ESP32 boards: a 5 V part driving a 3.3 V-only input. The parts
    /// know their own supply range, so the tool can see it before the smoke does.
    /// </summary>
    private static void CheckLogicLevels(IReadOnlyList<Net> nets, List<RuleViolation> v)
    {
        foreach (var net in nets)
        {
            var drivers = net.Connections
                .Where(c => c.Pin.Kind is PinKind.Output or PinKind.Bidirectional)
                .Where(c => c.Part.Definition.Digital is not null)
                .ToList();

            var receivers = net.Connections
                .Where(c => c.Pin.Kind is PinKind.Input or PinKind.Bidirectional or PinKind.Analog)
                .Where(c => c.Part.Definition.Digital is not null)
                .ToList();

            foreach (var d in drivers)
            {
                double driveV = d.Part.Definition.Digital!.VccTypical;
                foreach (var r in receivers)
                {
                    if (ReferenceEquals(r.Part, d.Part)) continue;
                    double maxV = r.Part.Definition.Digital!.VccMax;
                    if (driveV <= maxV + 0.3) continue;

                    v.Add(new RuleViolation(RuleSeverity.Error, "ERC010",
                        $"{d.Part.Designator}.{d.Pin.Name} ส่งออก {driveV:0.#} V เข้า {r.Part.Designator}.{r.Pin.Name} " +
                        $"ซึ่งรับได้สูงสุด {maxV:0.#} V — ต้องมีตัวแบ่งแรงดันหรือตัวแปลงระดับคั่น ไม่งั้นขาพัง",
                        d.Part.Designator, net.Name));
                }
            }
        }
    }

    /// <summary>An I²C bus with no pull-up never communicates, and nothing about it looks wrong.</summary>
    private static void CheckI2CPullups(CircuitDocument doc, IReadOnlyList<Net> nets, List<RuleViolation> v)
    {
        foreach (var net in nets)
        {
            var openDrain = net.Connections.Where(c => c.Pin.Kind == PinKind.OpenDrain).ToList();
            if (openDrain.Count == 0) continue;

            bool hasPullup = net.Connections.Any(c =>
                c.Part.Definition.Symbol == SymbolShape.Box &&
                c.Part.Definition.Prefix == "R");

            // A breakout that already carries pull-ups is the usual reason this is fine —
            // but that has to be declared by the part, not guessed from its package name.
            bool moduleHasThem = openDrain.Any(c =>
                c.Part.Definition.Digital?.HasIntegratedPullups == true);

            if (!hasPullup && !moduleHasThem)
                v.Add(new RuleViolation(RuleSeverity.Warning, "ERC020",
                    $"เนต {net.Name} เป็นบัสแบบ open-drain " +
                    $"({string.Join(", ", openDrain.Select(c => $"{c.Part.Designator}.{c.Pin.Name}"))}) " +
                    "แต่ไม่มีตัวต้านทานพูลอัป — บัสจะไม่ขึ้นสูงเลยและสื่อสารไม่ได้ ปกติใช้ 4.7k",
                    Net: net.Name));
        }
    }

    /// <summary>An LED straight across a supply is the most common way a first circuit dies.</summary>
    private static void CheckLedSeriesResistor(CircuitDocument doc, IReadOnlyList<Net> nets, List<RuleViolation> v)
    {
        foreach (var led in doc.Parts.Where(p => p.Definition.Symbol == SymbolShape.Led))
        {
            bool protectedByResistor = false;

            foreach (var pin in led.Definition.Pins)
            {
                var net = nets.FirstOrDefault(n => n.Connections.Any(c => c.Part.Id == led.Id && c.Pin.Number == pin.Number));
                if (net is null) continue;

                if (net.Connections.Any(c => c.Part.Definition.Prefix == "R" && c.Part.Id != led.Id))
                    protectedByResistor = true;
            }

            if (!protectedByResistor)
                v.Add(new RuleViolation(RuleSeverity.Error, "ERC030",
                    $"{led.Designator} ไม่มีตัวต้านทานอนุกรม — ต่อตรงเข้าแหล่งจ่ายแล้ว LED พังทันที",
                    led.Designator));
        }
    }

    /// <summary>Checks each part against the supply it is actually wired to.</summary>
    private static void CheckSupplyRange(CircuitDocument doc, IReadOnlyList<Net> nets, List<RuleViolation> v)
    {
        // The DC sources on the sheet and what they produce.
        var supplyVoltage = new Dictionary<string, double>();
        foreach (var src in doc.Parts.Where(p => p.Definition.Symbol == SymbolShape.VoltageSource))
        {
            if (!double.TryParse(src.Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var volts)) continue;

            var plus = src.Definition.Pins.FirstOrDefault(p => p.Name == "+");
            if (plus is null) continue;

            var net = nets.FirstOrDefault(n => n.Connections.Any(c => c.Part.Id == src.Id && c.Pin.Number == plus.Number));
            if (net is not null) supplyVoltage[net.Name] = volts;
        }

        foreach (var part in doc.Parts)
        {
            var spec = part.Definition.Digital;
            if (spec is null) continue;

            foreach (var pin in part.Definition.Pins.Where(p => p.Kind == PinKind.Power))
            {
                var net = nets.FirstOrDefault(n => n.Connections.Any(c => c.Part.Id == part.Id && c.Pin.Number == pin.Number));
                if (net is null || !supplyVoltage.TryGetValue(net.Name, out var volts)) continue;

                if (volts > spec.VccMax)
                    v.Add(new RuleViolation(RuleSeverity.Error, "ERC040",
                        $"{part.Designator} รับไฟได้ {spec.VccMin:0.#}–{spec.VccMax:0.#} V แต่ต่ออยู่กับ {volts:0.#} V",
                        part.Designator, net.Name));
                else if (volts < spec.VccMin)
                    v.Add(new RuleViolation(RuleSeverity.Warning, "ERC041",
                        $"{part.Designator} ต้องการอย่างน้อย {spec.VccMin:0.#} V แต่ได้ {volts:0.#} V — อาจทำงานไม่เสถียร",
                        part.Designator, net.Name));
            }
        }
    }
}
