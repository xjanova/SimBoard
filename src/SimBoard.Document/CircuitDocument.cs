namespace SimBoard.Document;

/// <summary>A point on the placement grid. One step is 2.54 mm — 0.1 inch.</summary>
public readonly record struct GridPoint(int X, int Y)
{
    public GridPoint Offset(int dx, int dy) => new(X + dx, Y + dy);
    public override string ToString() => $"({X},{Y})";
}

public enum Rotation { R0 = 0, R90 = 90, R180 = 180, R270 = 270 }

/// <summary>One placed part. Owns only what is true of this placement.</summary>
public sealed class PartInstance
{
    public required string Id { get; init; }
    public required PartDefinition Definition { get; init; }
    public required string Designator { get; set; }
    public GridPoint Position { get; set; }
    public Rotation Rotation { get; set; }
    public string? Value { get; set; }
    public bool Locked { get; set; }

    /// <summary>
    /// Where each pin actually sits on the grid, after rotation. Connectivity is decided
    /// by these coordinates, so this is the single source of truth for what touches what.
    /// </summary>
    public IEnumerable<(Pin Pin, GridPoint At)> PinPositions()
    {
        foreach (var pin in Definition.Pins)
        {
            var local = LocalPin(pin);
            yield return (pin, Position.Offset(local.dx, local.dy));
        }
    }

    public GridPoint PinAt(Pin pin)
    {
        var (dx, dy) = LocalPin(pin);
        return Position.Offset(dx, dy);
    }

    /// <summary>Pin offset from the part origin, in grid steps, with rotation applied.</summary>
    private (int dx, int dy) LocalPin(Pin pin)
    {
        int w = Definition.BodyWidth, h = Definition.BodyHeight;

        // Unrotated: pins sit one step outside the body edge, at their slot along it.
        var (x, y) = pin.Side switch
        {
            PinSide.Left => (-1, pin.Slot + 1),
            PinSide.Right => (w, pin.Slot + 1),
            PinSide.Top => (pin.Slot + 1, -1),
            PinSide.Bottom => (pin.Slot + 1, h),
            _ => (0, 0),
        };

        return Rotation switch
        {
            Rotation.R0 => (x, y),
            Rotation.R90 => (h - y, x),
            Rotation.R180 => (w - x, h - y),
            Rotation.R270 => (y, w - x),
            _ => (x, y),
        };
    }

    public override string ToString() => $"{Designator} {Definition.Key}";
}

/// <summary>An orthogonal wire segment between two grid points.</summary>
public sealed class Wire
{
    public required string Id { get; init; }
    public GridPoint A { get; set; }
    public GridPoint B { get; set; }

    /// <summary>Every grid point the segment passes through, so T-junctions are found.</summary>
    public IEnumerable<GridPoint> Points()
    {
        if (A.X == B.X)
        {
            for (int y = Math.Min(A.Y, B.Y); y <= Math.Max(A.Y, B.Y); y++) yield return new GridPoint(A.X, y);
        }
        else if (A.Y == B.Y)
        {
            for (int x = Math.Min(A.X, B.X); x <= Math.Max(A.X, B.X); x++) yield return new GridPoint(x, A.Y);
        }
        else
        {
            // Diagonal wires are not allowed on a schematic; treat as two endpoints only.
            yield return A;
            yield return B;
        }
    }

    public override string ToString() => $"{A}→{B}";
}

/// <summary>A user-placed name that forces every touching net to share it.</summary>
public sealed class NetLabel
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public GridPoint At { get; set; }
}

/// <summary>
/// A resolved electrical node: everything that is connected together.
/// <see cref="SpiceName"/> is what goes into the netlist — ground is always "0".
/// </summary>
public sealed class Net
{
    public required string Name { get; init; }
    public required IReadOnlyList<(PartInstance Part, Pin Pin)> Connections { get; init; }
    public required IReadOnlyList<GridPoint> Points { get; init; }
    public bool IsGround { get; init; }

    public string SpiceName => IsGround ? "0" : Name;
    public int PinCount => Connections.Count;
    public override string ToString() => $"{Name} ({PinCount} pins)";
}

/// <summary>
/// The circuit as edited. Everything the simulator, the ERC and the PCB read comes from
/// here — there is one document and every view is a projection of it.
/// </summary>
public sealed class CircuitDocument
{
    private int _nextId;

    public List<PartInstance> Parts { get; } = [];
    public List<Wire> Wires { get; } = [];
    public List<NetLabel> Labels { get; } = [];

    public string Title { get; set; } = "untitled";

    private string NextId(string prefix) => $"{prefix}{++_nextId}";

    /// <summary>
    /// Moves the id counter past everything already in the document. A loaded project
    /// arrives with ids the counter has never issued, and without this the first part
    /// placed afterwards would take an id that already belongs to something — which
    /// silently breaks undo, since commands find parts by id.
    /// </summary>
    public void ReseedIds()
    {
        int highest = 0;
        foreach (var id in Parts.Select(p => p.Id)
                     .Concat(Wires.Select(w => w.Id))
                     .Concat(Labels.Select(l => l.Id)))
        {
            var digits = id.AsSpan(1);
            if (int.TryParse(digits, out var n) && n > highest) highest = n;
        }
        _nextId = highest;
    }

    /// <summary>Places a part, assigning the next free designator for its prefix.</summary>
    public PartInstance Place(PartDefinition def, GridPoint at, Rotation rotation = Rotation.R0)
    {
        var instance = new PartInstance
        {
            Id = NextId("p"),
            Definition = def,
            Designator = NextDesignator(def.Prefix),
            Position = at,
            Rotation = rotation,
            Value = def.DefaultValue,
        };
        Parts.Add(instance);
        return instance;
    }

    /// <summary>R1, R2, R3… skipping any the user has already taken.</summary>
    public string NextDesignator(string prefix)
    {
        var taken = Parts
            .Where(p => p.Designator.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(p => int.TryParse(p.Designator[prefix.Length..], out var n) ? n : 0)
            .ToHashSet();

        int i = 1;
        while (taken.Contains(i)) i++;
        return prefix == "GND" ? "GND" : $"{prefix}{i}";
    }

    public Wire Connect(GridPoint a, GridPoint b)
    {
        var w = new Wire { Id = NextId("w"), A = a, B = b };
        Wires.Add(w);
        return w;
    }

    public void Remove(PartInstance part) => Parts.Remove(part);
    public void Remove(Wire wire) => Wires.Remove(wire);

    /// <summary>The part whose body covers this grid point, if any.</summary>
    public PartInstance? PartAt(GridPoint p) => Parts.FirstOrDefault(part =>
    {
        var (w, h) = Footprint(part);
        return p.X >= part.Position.X && p.X < part.Position.X + w
            && p.Y >= part.Position.Y && p.Y < part.Position.Y + h;
    });

    /// <summary>The pin exactly at this grid point, if any.</summary>
    public (PartInstance Part, Pin Pin)? PinAt(GridPoint p)
    {
        foreach (var part in Parts)
            foreach (var (pin, at) in part.PinPositions())
                if (at == p) return (part, pin);
        return null;
    }

    /// <summary>Body size after rotation.</summary>
    public static (int W, int H) Footprint(PartInstance part) =>
        part.Rotation is Rotation.R90 or Rotation.R270
            ? (part.Definition.BodyHeight, part.Definition.BodyWidth)
            : (part.Definition.BodyWidth, part.Definition.BodyHeight);

    /// <summary>
    /// Resolves what is connected to what.
    ///
    /// Union-find over grid points: a wire unions every point along its run, so a segment
    /// crossing a pin picks it up as a T-junction the way a real schematic does. A net
    /// containing any ground pin becomes SPICE node 0, which is what makes a placed GND
    /// symbol actually mean something.
    /// </summary>
    public IReadOnlyList<Net> ExtractNets()
    {
        var parent = new Dictionary<GridPoint, GridPoint>();

        GridPoint Find(GridPoint p)
        {
            if (!parent.TryGetValue(p, out var up)) { parent[p] = p; return p; }
            if (up == p) return p;
            var root = Find(up);
            parent[p] = root;                      // path compression
            return root;
        }

        void Union(GridPoint a, GridPoint b)
        {
            var (ra, rb) = (Find(a), Find(b));
            if (ra != rb) parent[ra] = rb;
        }

        foreach (var wire in Wires)
        {
            GridPoint? prev = null;
            foreach (var p in wire.Points())
            {
                Find(p);
                if (prev is { } q) Union(q, p);
                prev = p;
            }
        }

        var pinsAt = new Dictionary<GridPoint, List<(PartInstance, Pin)>>();
        foreach (var part in Parts)
            foreach (var (pin, at) in part.PinPositions())
            {
                Find(at);
                (pinsAt.TryGetValue(at, out var list) ? list : pinsAt[at] = []).Add((part, pin));
            }

        // A label forces its point into whatever net it sits on, and names it.
        var labelAt = Labels.ToDictionary(l => l.At, l => l.Name);
        foreach (var l in Labels) Find(l.At);

        var groups = parent.Keys
            .GroupBy(Find)
            .Where(g => g.Any(p => pinsAt.ContainsKey(p)));   // a wire touching nothing is not a net

        var nets = new List<Net>();
        int auto = 0;
        foreach (var g in groups)
        {
            var points = g.ToList();
            var connections = points
                .SelectMany(p => pinsAt.TryGetValue(p, out var l) ? l : [])
                .ToList();

            bool isGround = connections.Any(c => c.Item2.Kind == PinKind.Ground);
            string? named = points.Select(p => labelAt.GetValueOrDefault(p)).FirstOrDefault(n => n is not null);

            nets.Add(new Net
            {
                Name = isGround ? "GND" : named ?? $"N{++auto:D3}",
                Connections = connections,
                Points = points,
                IsGround = isGround,
            });
        }

        return nets;
    }
}
