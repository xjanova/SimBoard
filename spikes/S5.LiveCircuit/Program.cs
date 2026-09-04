using SimBoard.Document;
using SimBoard.Spice;

// ─────────────────────────────────────────────────────────────────────────────
// S5 — the whole chain, for real.
//
// Nothing here hand-writes a netlist. Parts are PLACED on a grid and WIRED
// together exactly as the editor will do it; the nets are then discovered from
// the geometry, the deck is generated from those nets, and ngspice solves it.
// If the numbers match theory, then placing and wiring genuinely drives the
// simulator — which is the difference between a working tool and a mock.
// ─────────────────────────────────────────────────────────────────────────────

int failures = 0;

Section("1 · place and wire an RC low-pass, then simulate what was drawn");
{
    var doc = new CircuitDocument { Title = "rc-lowpass" };

    // Place. Coordinates are grid steps; the parts land where they are put.
    var v1 = doc.Place(PartCatalog.Require("VDC"), new GridPoint(0, 4));
    var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(6, 2));
    var c1 = doc.Place(PartCatalog.Require("C"), new GridPoint(16, 6), Rotation.R90);
    var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(6, 12));

    v1.Value = "5";
    r1.Value = "10k";
    c1.Value = "10n";

    // Wire pin to pin. The wires are what create the nets — nothing is implied.
    Connect(doc, v1, "1", r1, "1");     // V+ → R left
    Connect(doc, r1, "2", c1, "1");     // R right → C top    (this is v(out))
    Connect(doc, c1, "2", gnd, "1");    // C bottom → GND
    Connect(doc, v1, "2", gnd, "1");    // V- → GND

    var nets = doc.ExtractNets();
    Console.WriteLine($"  {doc.Parts.Count} parts placed · {doc.Wires.Count} wires drawn · {nets.Count} nets discovered");
    foreach (var n in nets)
        Console.WriteLine($"    {n.SpiceName,-6} {string.Join(", ", n.Connections.Select(c => $"{c.Part.Designator}.{c.Pin.Name}"))}");

    // tau = R*C = 10k * 10n = 100 us
    const double tau = 10e3 * 10e-9;
    var built = NetlistBuilder.Build(doc, Analysis.Transient(tau / 500, tau * 10));

    Console.WriteLine();
    Console.WriteLine("  ── generated deck ──");
    foreach (var line in built.Deck.TrimEnd().Split('\n')) Console.WriteLine($"  │ {line.TrimEnd()}");
    Console.WriteLine();

    Report(built.CanSimulate, "deck is simulatable" +
        (built.Blockers.Count > 0 ? $" — blocked: {string.Join("; ", built.Blockers)}" : ""));

    if (built.CanSimulate)
    {
        var result = await new NgspiceRunner().RunAsync(built.Deck);
        var outNet = nets.First(n => n.Connections.Any(c => c.Part.Designator == "C1" && c.Pin.Name == "A"));
        var t = result.Require("time");
        var vout = result.Require(outNet.SpiceName);

        // A step into an RC reaches 1 - 1/e of the supply at t = tau.
        double expected = 5.0 * (1.0 - 1.0 / Math.E);
        double actual = SampleAt(t, vout, tau);
        Console.WriteLine($"  {result.PointCount:N0} points · {result.Elapsed.TotalMilliseconds:F0} ms");
        Check($"v({outNet.SpiceName}) at t=RC", actual, expected, 0.02, "V");
    }
}

Section("2 · move a part — the netlist must follow the geometry");
{
    var doc = new CircuitDocument { Title = "moved" };
    var v1 = doc.Place(PartCatalog.Require("VDC"), new GridPoint(0, 4));
    var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(6, 2));
    var r2 = doc.Place(PartCatalog.Require("R"), new GridPoint(14, 2));
    var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(6, 12));
    v1.Value = "10"; r1.Value = "1k"; r2.Value = "1k";

    Connect(doc, v1, "1", r1, "1");
    Connect(doc, r1, "2", r2, "1");
    Connect(doc, r2, "2", gnd, "1");
    Connect(doc, v1, "2", gnd, "1");

    int before = doc.ExtractNets().Count;

    // Drag R2 away so its wire no longer lands on R1's pin.
    r2.Position = new GridPoint(30, 20);
    int after = doc.ExtractNets().Count;

    Report(after != before,
        $"moving a part changed the netlist ({before} nets → {after}) — connectivity comes from geometry, not from a stored list");
}

Section("3 · a divider solved by ngspice, checked against Ohm's law");
{
    var doc = new CircuitDocument { Title = "divider" };
    var v1 = doc.Place(PartCatalog.Require("VDC"), new GridPoint(0, 4));
    var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(6, 2));
    var r2 = doc.Place(PartCatalog.Require("R"), new GridPoint(14, 2));
    var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(6, 12));
    v1.Value = "9"; r1.Value = "2k"; r2.Value = "1k";

    Connect(doc, v1, "1", r1, "1");
    Connect(doc, r1, "2", r2, "1");
    Connect(doc, r2, "2", gnd, "1");
    Connect(doc, v1, "2", gnd, "1");

    var built = NetlistBuilder.Build(doc, Analysis.Transient(1e-6, 1e-3));
    var result = await new NgspiceRunner().RunAsync(built.Deck);

    var mid = built.Nets.First(n =>
        n.Connections.Any(c => c.Part.Designator == "R1" && c.Pin.Name == "B") &&
        n.Connections.Any(c => c.Part.Designator == "R2" && c.Pin.Name == "A"));

    var v = result.Require(mid.SpiceName);
    double expected = 9.0 * 1000.0 / (2000.0 + 1000.0);      // 3.000 V
    Check("mid-node voltage", v.Values[^1], expected, 0.01, "V");
}

Section("4 · the rule check catches what actually kills hardware");
{
    // A 5 V ultrasonic sensor driving an ESP32 pin straight — the classic dead board.
    var doc = new CircuitDocument { Title = "erc" };
    // Everything sits to the RIGHT of the ESP32 and wires into its right-hand pins.
    // Routing to a left-hand pin from over here would drag the wire straight across the
    // chip and pick up every right-hand pin on the way — which the extractor is correct
    // to treat as a connection, and which is exactly the mistake this test must not make.
    var esp = doc.Place(PartCatalog.Require("ESP32-DEVKIT"), new GridPoint(0, 0));
    var sr04 = doc.Place(PartCatalog.Require("HC-SR04"), new GridPoint(40, 0));
    var ds = doc.Place(PartCatalog.Require("DS18B20"), new GridPoint(40, 20));
    var led = doc.Place(PartCatalog.Require("LED"), new GridPoint(40, 40));
    var v5 = doc.Place(PartCatalog.Require("VDC"), new GridPoint(70, 0));
    var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(70, 40));
    v5.Value = "5";

    Connect(doc, sr04, "3", esp, "21");    // ECHO — 5 V output straight into GPIO4
    Connect(doc, v5, "1", sr04, "1");      // 5 V → sensor VCC
    Connect(doc, v5, "2", gnd, "1");
    Connect(doc, ds, "2", esp, "22");      // DS18B20 DQ → GPIO16, 1-Wire with no pull-up
    Connect(doc, ds, "3", esp, "16");      // sensor VDD → 3V3
    Connect(doc, ds, "1", gnd, "1");
    Connect(doc, led, "1", esp, "24");     // LED straight off GPIO5, no series resistor
    Connect(doc, led, "2", gnd, "1");

    var violations = ElectricalRuleCheck.Run(doc);
    foreach (var x in violations.Where(x => x.Severity != RuleSeverity.Warning || x.Code.StartsWith("ERC02")))
        Console.WriteLine($"    [{x.Severity,-7}] {x.Code}  {x.Message}");

    Report(violations.Any(x => x.Code == "ERC010"), "caught the 5 V output driving a 3.3 V-only ESP32 pin");
    Report(violations.Any(x => x.Code == "ERC020"), "caught the I2C bus with no pull-up resistors");
    Report(violations.Any(x => x.Code == "ERC030"), "caught the LED with no series resistor");
}

Section("5 · the catalog knows its parts");
{
    Console.WriteLine($"  {PartCatalog.All.Count} parts · {PartCatalog.Simulatable.Count()} simulate in SPICE · " +
                      $"{PartCatalog.Digital.Count()} carry a digital envelope");

    var esp = PartCatalog.Require("ESP32-DEVKIT");
    Report(esp.Pins.Count == 30, $"ESP32 DevKit has {esp.Pins.Count} pins");
    Report(esp.PinByName("GPIO21")?.Description?.Contains("SDA") == true, "GPIO21 is known to be the default I2C SDA");
    Report(esp.Digital!.VccMax < 3.7, $"ESP32 I/O ceiling recorded as {esp.Digital.VccMax} V");

    var ds = PartCatalog.Require("DS18B20");
    Report(ds.PinByName("DQ")?.Kind == PinKind.OpenDrain, "DS18B20 DQ is open-drain, so the pull-up rule applies to it");
    Report(ds.NoteTh?.Contains("4.7k") == true, "DS18B20 carries its 4.7k pull-up requirement");

    var ne555 = PartCatalog.Require("NE555");
    Report(ne555.PinByNumber("5")?.Name == "CTRL", "NE555 pin 5 is CTRL — the one that was miswired in S1");
}

Section("6 · undo and redo put the circuit back exactly");
{
    var doc = new CircuitDocument { Title = "history" };
    var history = new EditHistory(doc);

    var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(4, 4));
    history.Record(new PlacePart(r1));
    var c1 = doc.Place(PartCatalog.Require("C"), new GridPoint(12, 4));
    history.Record(new PlacePart(c1));
    history.Do(new SetValue(r1.Id, r1.Value, "4k7"));

    Report(doc.Parts.Count == 2 && r1.Value == "4k7", "placed two parts and changed a value");

    history.Undo();
    Report(r1.Value == "10k", $"undo restored the value ({r1.Value})");
    history.Undo();
    Report(doc.Parts.Count == 1, $"undo removed the second part ({doc.Parts.Count} left)");
    history.Redo();
    Report(doc.Parts.Count == 2, "redo put it back");
    history.Redo();
    Report(r1.Value == "4k7", "redo reapplied the value");

    // A drag arrives as many small moves; undo must take back the gesture, not a frame.
    var start = r1.Position;
    for (int i = 1; i <= 8; i++)
    {
        var from = r1.Position;
        r1.Position = new GridPoint(start.X + i, start.Y);
        history.Record(new MovePart(r1.Id, from, r1.Position));
    }
    history.Undo();
    Report(r1.Position == start, $"one undo took back the whole drag (back at {r1.Position})");
}

Section("7 · a project survives a round trip through disk");
{
    var doc = new CircuitDocument { Title = "roundtrip" };
    var v1 = doc.Place(PartCatalog.Require("VDC"), new GridPoint(0, 4));
    var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(6, 2), Rotation.R90);
    var esp = doc.Place(PartCatalog.Require("ESP32-DEVKIT"), new GridPoint(20, 0));
    var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(6, 12));
    v1.Value = "3.3";
    r1.Value = "4k7";
    doc.Connect(new GridPoint(1, 1), new GridPoint(9, 1));

    var path = Path.Combine(Path.GetTempPath(), "simboard-roundtrip.sbp");
    ProjectFile.Save(doc, path);
    var (loaded, warnings) = ProjectFile.Load(path);

    Report(warnings.Count == 0, $"loaded with no warnings ({warnings.Count})");
    Report(loaded.Parts.Count == doc.Parts.Count, $"{loaded.Parts.Count} parts survived");
    Report(loaded.Wires.Count == doc.Wires.Count, $"{loaded.Wires.Count} wires survived");

    var lr = loaded.Parts.First(x => x.Designator == r1.Designator);
    Report(lr.Value == "4k7" && lr.Rotation == Rotation.R90, "value and rotation survived");
    Report(loaded.Parts.Any(x => x.Definition.Pins.Count == 30), "the ESP32 came back with all 30 pins");

    // The round trip has to preserve the circuit, not just the objects — compare before
    // touching the loaded document, since placing anything adds nets of its own.
    Report(loaded.ExtractNets().Count == doc.ExtractNets().Count,
        $"the same nets come back ({loaded.ExtractNets().Count} vs {doc.ExtractNets().Count})");

    // The id counter must move past what was loaded, or a new part collides with an
    // existing one and undo starts acting on the wrong thing.
    var fresh = loaded.Place(PartCatalog.Require("R"), new GridPoint(40, 40));
    Report(loaded.Parts.Count(x => x.Id == fresh.Id) == 1, $"a part placed after loading got a free id ({fresh.Id})");

    File.Delete(path);
}

Console.WriteLine();
Console.WriteLine(failures == 0
    ? "S5 PASSED — placing and wiring parts drives the simulator for real."
    : $"S5 FAILED — {failures} check(s) did not pass.");
return failures == 0 ? 0 : 1;

// ── helpers ────────────────────────────────────────────────────────────────

/// <summary>
/// Routes a wire the way the tool will have to: leave each pin straight out from its own
/// side first, then travel.
///
/// The naive version — one corner at (bx, ay) — shorted a capacitor on the first run,
/// because the horizontal leg ran along the row both of its pins sit on and the net
/// extractor correctly picked up both as T-junctions. A wire crossing a pin IS a
/// connection on a real schematic, so the router has to leave the pin's own row before
/// it turns.
/// </summary>
static void Connect(CircuitDocument doc, PartInstance a, string aPin, PartInstance b, string bPin)
{
    var pinA = a.Definition.PinByNumber(aPin)!;
    var pinB = b.Definition.PinByNumber(bPin)!;
    var pa = a.PinAt(pinA);
    var pb = b.PinAt(pinB);

    var ea = Escape(pa, pinA.Side);
    var eb = Escape(pb, pinB.Side);

    doc.Connect(pa, ea);
    // Turn on the axis the first pin escaped along, so neither leg re-enters a pin row.
    var corner = pinA.Side is PinSide.Left or PinSide.Right
        ? new GridPoint(ea.X, eb.Y)
        : new GridPoint(eb.X, ea.Y);
    doc.Connect(ea, corner);
    doc.Connect(corner, eb);
    doc.Connect(eb, pb);

    static GridPoint Escape(GridPoint p, PinSide side) => side switch
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

void Check(string label, double actual, double expected, double tolerance, string unit)
{
    double err = Math.Abs(actual - expected);
    Report(!double.IsNaN(actual) && err <= tolerance,
        $"{label}: {actual:G6} {unit} vs theory {expected:G6} {unit} (Δ {err:G3})");
}

void Report(bool ok, string message)
{
    if (!ok) failures++;
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {message}");
}

static double SampleAt(SpiceVector time, SpiceVector v, double at)
{
    for (int i = 1; i < time.Count; i++)
    {
        if (time.Values[i] < at) continue;
        double dt = time.Values[i] - time.Values[i - 1];
        double f = dt <= 0 ? 0 : (at - time.Values[i - 1]) / dt;
        return v.Values[i - 1] + f * (v.Values[i] - v.Values[i - 1]);
    }
    return v.Values[^1];
}
