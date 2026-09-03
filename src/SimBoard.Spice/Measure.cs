namespace SimBoard.Spice;

/// <summary>
/// The measurements the virtual instruments display.
///
/// Everything here is time-weighted. SPICE uses an adaptive timestep, so samples bunch up
/// where the signal moves fast — averaging them naively over-weights edges and reports an
/// RMS that is simply wrong. Every integral below is trapezoidal over real time.
/// </summary>
public static class Measure
{
    public static double Min(SpiceVector v) => v.Count == 0 ? double.NaN : v.Values.Min();
    public static double Max(SpiceVector v) => v.Count == 0 ? double.NaN : v.Values.Max();
    public static double Vpp(SpiceVector v) => v.Count == 0 ? double.NaN : Max(v) - Min(v);

    /// <summary>Time-weighted mean over the whole record.</summary>
    public static double Mean(SpiceVector time, SpiceVector v)
    {
        var (area, span) = Integrate(time, v, static x => x);
        return span > 0 ? area / span : double.NaN;
    }

    /// <summary>Time-weighted RMS. Do not replace this with a plain sample RMS.</summary>
    public static double Rms(SpiceVector time, SpiceVector v)
    {
        var (area, span) = Integrate(time, v, static x => x * x);
        return span > 0 ? Math.Sqrt(area / span) : double.NaN;
    }

    private static (double Area, double Span) Integrate(SpiceVector time, SpiceVector v, Func<double, double> f)
    {
        int n = Math.Min(time.Count, v.Count);
        if (n < 2) return (0, 0);
        double area = 0;
        for (int i = 1; i < n; i++)
        {
            double dt = time.Values[i] - time.Values[i - 1];
            if (dt <= 0) continue;                       // repeated timepoints happen at breakpoints
            area += 0.5 * (f(v.Values[i]) + f(v.Values[i - 1])) * dt;
        }
        return (area, time.Values[n - 1] - time.Values[0]);
    }

    /// <summary>
    /// Fundamental frequency from mid-level crossings, ignoring the first
    /// <paramref name="settleFraction"/> of the record so start-up transients do not count.
    /// Returns null when fewer than two full periods are present.
    /// </summary>
    public static double? Frequency(SpiceVector time, SpiceVector v, double settleFraction = 0.25)
    {
        var rising = RisingCrossings(time, v, settleFraction);
        if (rising.Count < 2) return null;
        // Use the span across all periods, not an average of per-period estimates:
        // it divides out the timestep jitter instead of accumulating it.
        double span = rising[^1] - rising[0];
        return span > 0 ? (rising.Count - 1) / span : null;
    }

    /// <summary>Fraction of each period spent above mid level, 0..1. Null if it does not oscillate.</summary>
    public static double? DutyCycle(SpiceVector time, SpiceVector v, double settleFraction = 0.25)
    {
        double mid = MidLevel(v);
        var rising = RisingCrossings(time, v, settleFraction);
        var falling = Crossings(time, v, mid, rising: false, settleFraction);
        if (rising.Count < 2 || falling.Count < 1) return null;

        double period = (rising[^1] - rising[0]) / (rising.Count - 1);
        if (period <= 0) return null;

        // First fall strictly after the first rise gives one clean high time.
        foreach (var f in falling)
            if (f > rising[0])
                return Math.Clamp((f - rising[0]) / period, 0, 1);
        return null;
    }

    /// <summary>10 %–90 % rise time of the first clean edge, in seconds. Null if no edge is found.</summary>
    public static double? RiseTime(SpiceVector time, SpiceVector v, double settleFraction = 0.25)
    {
        double lo = Min(v), hi = Max(v), swing = hi - lo;
        if (swing <= 0) return null;
        double t10 = lo + 0.1 * swing, t90 = lo + 0.9 * swing;

        var a = Crossings(time, v, t10, rising: true, settleFraction);
        var b = Crossings(time, v, t90, rising: true, settleFraction);
        if (a.Count == 0 || b.Count == 0) return null;

        foreach (var start in a)
            foreach (var end in b)
                if (end > start) return end - start;
        return null;
    }

    /// <summary>Value halfway between the extremes — the threshold the other measurements cross.</summary>
    public static double MidLevel(SpiceVector v) => (Max(v) + Min(v)) / 2.0;

    private static List<double> RisingCrossings(SpiceVector time, SpiceVector v, double settleFraction) =>
        Crossings(time, v, MidLevel(v), rising: true, settleFraction);

    /// <summary>Linearly interpolated crossings of <paramref name="level"/>, in seconds.</summary>
    private static List<double> Crossings(
        SpiceVector time, SpiceVector v, double level, bool rising, double settleFraction)
    {
        var hits = new List<double>();
        int n = Math.Min(time.Count, v.Count);
        if (n < 2) return hits;

        double t0 = time.Values[0], t1 = time.Values[n - 1];
        double after = t0 + (t1 - t0) * Math.Clamp(settleFraction, 0, 0.9);

        for (int i = 1; i < n; i++)
        {
            double a = v.Values[i - 1], b = v.Values[i];
            bool crossed = rising ? a < level && b >= level : a > level && b <= level;
            if (!crossed) continue;

            double dt = time.Values[i] - time.Values[i - 1];
            double frac = Math.Abs(b - a) < double.Epsilon ? 0 : (level - a) / (b - a);
            double t = time.Values[i - 1] + frac * dt;
            if (t >= after) hits.Add(t);
        }
        return hits;
    }
}
