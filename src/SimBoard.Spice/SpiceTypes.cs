namespace SimBoard.Spice;

/// <summary>Why a simulation did not produce results. Never surface the raw engine text to a user.</summary>
public enum SpiceFailure
{
    /// <summary>ngspice was not found on disk.</summary>
    EngineMissing,
    /// <summary>The netlist was rejected before analysis started.</summary>
    NetlistRejected,
    /// <summary>The solver could not converge. This is the one users actually hit.</summary>
    NonConvergence,
    /// <summary>ngspice ran but wrote no usable rawfile.</summary>
    NoOutput,
    /// <summary>The rawfile was malformed.</summary>
    RawFileCorrupt,
    /// <summary>The run exceeded its budget.</summary>
    Timeout,
    /// <summary>The user pressed Stop.</summary>
    Cancelled,
}

/// <summary>
/// A simulation failure carrying enough structure for the UI to explain itself.
/// <see cref="Node"/> is the node ngspice blamed, when it named one — that is what a
/// "why did the simulation fail" panel highlights on the schematic.
/// </summary>
public sealed class SpiceException(SpiceFailure failure, string message, string? node = null, string? engineLog = null)
    : Exception(message)
{
    public SpiceFailure Failure { get; } = failure;
    public string? Node { get; } = node;
    /// <summary>Raw engine output. For the log and bug reports — not for the user's screen.</summary>
    public string? EngineLog { get; } = engineLog;
}

/// <summary>One traced quantity over the sweep: a node voltage, a branch current, or the sweep axis itself.</summary>
public sealed class SpiceVector(string name, string unit, double[] values)
{
    /// <summary>ngspice's own name, e.g. <c>v(out)</c>, <c>time</c>, <c>i(v1)</c>.</summary>
    public string Name { get; } = name;
    /// <summary>ngspice's type word: <c>voltage</c>, <c>current</c>, <c>time</c>, <c>frequency</c>.</summary>
    public string Unit { get; } = unit;
    public double[] Values { get; } = values;
    public int Count => Values.Length;

    /// <summary>Node name without the <c>v(...)</c> wrapper, for matching against the schematic's nets.</summary>
    public string NodeName
    {
        get
        {
            var n = Name;
            if (n.Length > 3 && (n.StartsWith("v(", StringComparison.OrdinalIgnoreCase)
                              || n.StartsWith("i(", StringComparison.OrdinalIgnoreCase)) && n[^1] == ')')
                return n[2..^1];
            return n;
        }
    }

    public override string ToString() => $"{Name} [{Unit}] × {Count}";
}

/// <summary>The result of one analysis run. Vectors are looked up by ngspice name or by bare node name.</summary>
public sealed class SimulationResult
{
    private readonly Dictionary<string, SpiceVector> _byName;

    public SimulationResult(string plotName, IReadOnlyList<SpiceVector> vectors, TimeSpan elapsed, string engineLog)
    {
        PlotName = plotName;
        Vectors = vectors;
        Elapsed = elapsed;
        EngineLog = engineLog;
        _byName = new Dictionary<string, SpiceVector>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in vectors)
        {
            _byName[v.Name] = v;
            _byName.TryAdd(v.NodeName, v);
        }
    }

    /// <summary>e.g. "Transient Analysis".</summary>
    public string PlotName { get; }
    public IReadOnlyList<SpiceVector> Vectors { get; }
    /// <summary>Wall-clock time for the whole run, including process start.</summary>
    public TimeSpan Elapsed { get; }
    public string EngineLog { get; }
    public int PointCount => Vectors.Count == 0 ? 0 : Vectors[0].Count;

    /// <summary>The sweep axis — time for a transient, frequency for an AC run.</summary>
    public SpiceVector? Sweep =>
        Vectors.FirstOrDefault(v => v.Unit is "time" or "frequency")
        ?? (Vectors.Count > 0 ? Vectors[0] : null);

    public SpiceVector? this[string name] => _byName.GetValueOrDefault(name);

    public SpiceVector Require(string name) =>
        _byName.GetValueOrDefault(name)
        ?? throw new SpiceException(SpiceFailure.NoOutput,
            $"Vector '{name}' is not in the result. Present: {string.Join(", ", _byName.Keys.Take(24))}");
}
