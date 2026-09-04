namespace SimBoard.Document;

/// <summary>
/// Datasheet parameters, used to decide whether one part can stand in for another.
///
/// Names follow the datasheet, not an invention of ours, because the person at the bench
/// is reading the datasheet. A value that is not known is simply absent — never a zero,
/// which would read as "rated for nothing" and silently fail every substitution rule.
/// </summary>
public enum ParamKey
{
    // Bipolar transistors
    /// <summary>Collector-emitter breakdown, V.</summary>
    Vceo,
    /// <summary>Collector-base breakdown, V.</summary>
    Vcbo,
    /// <summary>Emitter-base breakdown, V.</summary>
    Vebo,
    /// <summary>Continuous collector current, A.</summary>
    Ic,
    /// <summary>Peak collector current, A.</summary>
    IcPeak,
    /// <summary>Total dissipation at 25 C, W.</summary>
    Ptot,
    HfeMin,
    HfeMax,
    /// <summary>Transition frequency, Hz.</summary>
    Ft,
    /// <summary>Collector-emitter saturation voltage, V.</summary>
    VceSat,

    // Field-effect transistors
    /// <summary>Drain-source breakdown, V.</summary>
    Vds,
    /// <summary>Continuous drain current, A.</summary>
    Id,
    /// <summary>On resistance, ohms.</summary>
    RdsOn,
    /// <summary>Worst-case gate threshold, V.</summary>
    VgsThMax,
    /// <summary>Total gate charge, C.</summary>
    Qg,

    // Diodes
    /// <summary>Peak repetitive reverse voltage, V.</summary>
    Vrrm,
    /// <summary>Average forward current, A.</summary>
    If,
    /// <summary>Non-repetitive surge current, A.</summary>
    Ifsm,
    /// <summary>Forward voltage drop, V.</summary>
    Vf,
    /// <summary>Reverse recovery time, s.</summary>
    Trr,
    /// <summary>Zener / regulation voltage, V.</summary>
    Vz,

    // Regulators and analog ICs
    /// <summary>Maximum input voltage, V.</summary>
    VinMax,
    /// <summary>Output voltage, V.</summary>
    Vout,
    /// <summary>Maximum output current, A.</summary>
    IoutMax,
    /// <summary>Dropout voltage, V.</summary>
    Dropout,
    /// <summary>Gain-bandwidth product, Hz.</summary>
    Gbw,
    /// <summary>Slew rate, V/s.</summary>
    SlewRate,
    /// <summary>Input offset voltage, V.</summary>
    Vio,

    // Passives
    /// <summary>Working voltage, V.</summary>
    Vmax,
    /// <summary>Tolerance, fraction (0.05 = 5 %).</summary>
    Tolerance,
    /// <summary>Power rating, W.</summary>
    Pmax,
}

/// <summary>Device polarity. Substituting across it is never valid.</summary>
public enum Polarity
{
    None,
    Npn,
    Pnp,
    NChannel,
    PChannel,
}

/// <summary>
/// Where a figure came from.
///
/// This exists because the consequence of a wrong V_CEO is not a warning dialog, it is a
/// dead board. Nothing typed from memory may be presented as if it were read off a
/// datasheet, and the UI says which it is before it says anything else.
/// </summary>
public enum Provenance
{
    /// <summary>Typed from general knowledge. Must be checked before anyone solders on it.</summary>
    Unverified,
    /// <summary>Checked line by line against the manufacturer datasheet.</summary>
    Datasheet,
    /// <summary>Contributed by a user of the product.</summary>
    Community,
}

/// <summary>Engineering-notation formatting, the way a datasheet prints a figure.</summary>
public static class Eng
{
    private static readonly (double Scale, string Suffix)[] Prefixes =
    [
        (1e9, "G"), (1e6, "M"), (1e3, "k"), (1, ""),
        (1e-3, "m"), (1e-6, "µ"), (1e-9, "n"), (1e-12, "p"),
    ];

    public static string Format(double value, string unit)
    {
        if (value == 0) return $"0 {unit}";
        double abs = Math.Abs(value);
        foreach (var (scale, suffix) in Prefixes)
            if (abs >= scale)
                return $"{value / scale:0.###} {suffix}{unit}";
        return $"{value:G3} {unit}";
    }

    public static string UnitOf(ParamKey key) => key switch
    {
        ParamKey.Vceo or ParamKey.Vcbo or ParamKey.Vebo or ParamKey.VceSat or ParamKey.Vds
            or ParamKey.VgsThMax or ParamKey.Vrrm or ParamKey.Vf or ParamKey.Vz
            or ParamKey.VinMax or ParamKey.Vout or ParamKey.Dropout or ParamKey.Vio
            or ParamKey.Vmax => "V",
        ParamKey.Ic or ParamKey.IcPeak or ParamKey.Id or ParamKey.If or ParamKey.Ifsm
            or ParamKey.IoutMax => "A",
        ParamKey.Ptot or ParamKey.Pmax => "W",
        ParamKey.Ft or ParamKey.Gbw => "Hz",
        ParamKey.RdsOn => "Ω",
        ParamKey.Qg => "C",
        ParamKey.Trr => "s",
        ParamKey.SlewRate => "V/s",
        _ => "",
    };

    public static string Format(PartDefinition p, ParamKey key) =>
        p.Get(key) is { } v ? Format(v, UnitOf(key)) : "—";
}
