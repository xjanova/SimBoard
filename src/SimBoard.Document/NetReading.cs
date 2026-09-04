namespace SimBoard.Document;

/// <summary>
/// What a net did during a run.
///
/// Reporting one sample is only honest for a signal that holds still. The first version
/// of the sheet overlay printed the last value in the buffer, so a pulsed circuit read
/// "0.000 V" on every net purely because the run happened to end in the low phase — a
/// number that was true of one instant and false about the circuit.
/// </summary>
public sealed record NetReading(double Min, double Max, double Final)
{
    /// <summary>True when the net barely moves, so a single figure describes it.</summary>
    public bool IsSteady => Max - Min <= Math.Max(1e-6, Math.Abs(Max) * 0.02);

    public double Swing => Max - Min;

    /// <summary>A steady net shows its value; a moving one shows the range it covered.</summary>
    public string Label => IsSteady ? $"{Final:0.000} V" : $"{Min:0.00}–{Max:0.00} V";

    public static NetReading From(IReadOnlyList<double> values)
    {
        double lo = double.MaxValue, hi = double.MinValue;
        foreach (var v in values)
        {
            if (v < lo) lo = v;
            if (v > hi) hi = v;
        }
        return values.Count == 0 ? new NetReading(0, 0, 0) : new NetReading(lo, hi, values[^1]);
    }
}
