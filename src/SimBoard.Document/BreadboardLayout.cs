namespace SimBoard.Document;

/// <summary>
/// A hole on a solderless breadboard.
///
/// Columns are numbered 1-63 left to right. Rows A-E sit above the centre channel and
/// F-J below it; within one column those two groups are each connected, and the channel
/// separates them — which is the whole reason a DIP package straddles it.
/// </summary>
public readonly record struct TiePoint(int Column, char Row)
{
    public bool IsRail => Row is '+' or '-';
    public bool IsUpperBank => Row is >= 'A' and <= 'E';

    /// <summary>
    /// The group of holes electrically joined to this one. Two tie-points share a node
    /// when this is equal, which is what makes the breadboard model a real net model
    /// rather than a drawing.
    /// </summary>
    public string Node => IsRail
        ? $"rail{Row}"                                  // rails run the length of the board
        : $"col{Column}{(IsUpperBank ? "u" : "l")}";

    public override string ToString() => IsRail ? $"{Row}{Column}" : $"{Row}{Column}";
}

/// <summary>One part sitting on the board, with the hole each of its pins occupies.</summary>
public sealed record PlacedOnBoard(
    PartInstance Part,
    IReadOnlyList<(Pin Pin, TiePoint At)> Pins);

/// <summary>A jumper wire the layout had to add to join two tie-point groups.</summary>
public sealed record Jumper(TiePoint From, TiePoint To, string Net, JumperColour Colour);

/// <summary>
/// Jumper colours follow bench convention, because on a real board the colour is how you
/// read the circuit without tracing every wire.
/// </summary>
public enum JumperColour { Power, Ground, Signal, SignalAlt, SignalThird }

/// <summary>
/// Lays a <see cref="CircuitDocument"/> out on a solderless breadboard.
///
/// This is the same circuit as the schematic, not a second drawing of it: parts, nets and
/// values all come from the document, so a change on the sheet is a change here. The spec
/// is explicit that the four views share one netlist, and the only way to keep that true
/// is for none of them to hold their own copy.
///
/// The placement is deliberately simple — parts in document order, left to right, power
/// and ground on the rails. A breadboard layout that minimises jumpers is the same
/// combinatorial problem as PCB placement, and pretending to solve it here would produce
/// something that looks authoritative and is not. What this does produce is a layout that
/// is electrically correct and that a person can actually follow.
/// </summary>
public static class BreadboardLayout
{
    public const int Columns = 63;

    /// <summary>Rows A-E above the channel, F-J below.</summary>
    public static readonly char[] UpperRows = ['A', 'B', 'C', 'D', 'E'];
    public static readonly char[] LowerRows = ['F', 'G', 'H', 'I', 'J'];

    public sealed record Result(
        IReadOnlyList<PlacedOnBoard> Parts,
        IReadOnlyList<Jumper> Jumpers,
        IReadOnlyList<Net> Nets,
        IReadOnlyList<string> Notes);

    public static Result Build(CircuitDocument doc)
    {
        var nets = doc.ExtractNets();
        var notes = new List<string>();
        var placed = new List<PlacedOnBoard>();
        var jumpers = new List<Jumper>();

        // Which net each pin belongs to, so placement can reason about connectivity.
        var netOfPin = new Dictionary<(string PartId, string PinNumber), Net>();
        foreach (var net in nets)
            foreach (var (part, pin) in net.Connections)
                netOfPin[(part.Id, pin.Number)] = net;

        var ground = nets.FirstOrDefault(n => n.IsGround);
        var power = PickPowerNet(doc, nets);

        int column = 3;
        foreach (var part in doc.Parts)
        {
            // Ground symbols and net labels are schematic notation. On a physical board
            // the rail IS the ground, so they have nothing to occupy.
            if (part.Definition.Spice == SpiceKind.None && part.Definition.Symbol == SymbolShape.Ground)
                continue;

            var pins = new List<(Pin, TiePoint)>();
            bool straddles = part.Definition.Pins.Count > 4;

            foreach (var (pin, index) in part.Definition.Pins.Select((p, i) => (p, i)))
            {
                var net = netOfPin.GetValueOrDefault((part.Id, pin.Number));

                TiePoint hole;
                if (net is not null && ground is not null && net.Name == ground.Name)
                    hole = new TiePoint(column + index, '-');
                else if (net is not null && power is not null && net.Name == power.Name)
                    hole = new TiePoint(column + index, '+');
                else if (straddles)
                    // A DIP sits across the channel: first half below, second half above,
                    // which is how the package physically lands.
                    hole = index < part.Definition.Pins.Count / 2
                        ? new TiePoint(column + index, 'E')
                        : new TiePoint(column + part.Definition.Pins.Count - 1 - index, 'F');
                else
                    hole = new TiePoint(column + index * 2, 'E');

                pins.Add((pin, hole));
            }

            placed.Add(new PlacedOnBoard(part, pins));
            column += Math.Max(3, part.Definition.Pins.Count + 2);

            if (column > Columns - 2)
            {
                notes.Add($"วงจรกว้างเกิน {Columns} คอลัมน์ที่วางได้ — {part.Designator} เป็นตัวที่เริ่มล้น "
                             + "ต้องใช้บอร์ดที่สองหรือจัดใหม่");
                break;
            }
        }

        // Every net that lands on more than one tie-point group needs a jumper to join
        // them: the board only connects holes that share a column bank.
        var holeOfPin = placed
            .SelectMany(p => p.Pins.Select(x => ((p.Part.Id, x.Pin.Number), x.At)))
            .ToDictionary(x => x.Item1, x => x.At);

        foreach (var net in nets)
        {
            var holes = net.Connections
                .Select(c => holeOfPin.TryGetValue((c.Part.Id, c.Pin.Number), out var h) ? h : (TiePoint?)null)
                .Where(h => h is not null)
                .Select(h => h!.Value)
                .DistinctBy(h => h.Node)
                .ToList();

            if (holes.Count < 2) continue;

            var colour = net.IsGround ? JumperColour.Ground
                : power is not null && net.Name == power.Name ? JumperColour.Power
                : (JumperColour)(2 + Math.Abs(net.Name.GetHashCode()) % 3);

            // A chain, not a star: that is how someone actually wires it.
            for (int i = 1; i < holes.Count; i++)
                jumpers.Add(new Jumper(holes[i - 1], holes[i], net.Name, colour));
        }

        if (ground is null)
            notes.Add("วงจรยังไม่มีกราวด์ — รางลบของบอร์ดจะไม่ถูกใช้");
        if (power is null)
            notes.Add("ยังหาแหล่งจ่ายหลักไม่เจอ — รางบวกจะไม่ถูกใช้");

        return new Result(placed, jumpers, nets, notes);
    }

    /// <summary>
    /// The positive rail is whatever the main supply drives. Picking the net with the most
    /// power pins on it beats guessing by name, which breaks the moment someone calls a
    /// rail VBAT or 3V3.
    /// </summary>
    private static Net? PickPowerNet(CircuitDocument doc, IReadOnlyList<Net> nets)
    {
        var supplyNets = nets
            .Where(n => !n.IsGround)
            .Select(n => (Net: n, Weight: n.Connections.Count(c => c.Pin.Kind == PinKind.Power)))
            .Where(x => x.Weight > 0)
            .OrderByDescending(x => x.Weight)
            .ToList();

        return supplyNets.Count > 0 ? supplyNets[0].Net : null;
    }
}
