namespace SimBoard.Parts;

public enum PartCategory { Bjt, Mosfet, Diode, Zener, Regulator, OpAmp, Logic, Passive, Other }

public enum Polarity
{
    None,
    /// <summary>BJT.</summary>
    Npn, Pnp,
    /// <summary>FET.</summary>
    NChannel, PChannel,
}

/// <summary>
/// Parameters we compare on. Names follow the datasheet, not our own invention, so a
/// technician reading a spec sheet recognises them without translation.
/// </summary>
public enum ParamKey
{
    // BJT — Vceo collector-emitter breakdown V, Vebo emitter-base breakdown V,
    // Ic continuous collector current A, Ptot dissipation at 25 C in W,
    // HfeMin/HfeMax current gain band, Ft transition frequency Hz,
    // VceSat collector-emitter saturation voltage V.
    Vceo,
    Vebo,
    Ic,
    Ptot,
    HfeMin,
    HfeMax,
    Ft,
    VceSat,

    // MOSFET — Vds drain-source breakdown V, Id continuous drain current A,
    // RdsOn on-resistance in ohms, VgsThMax worst-case gate threshold V,
    // Qg total gate charge in coulombs.
    Vds,
    Id,
    RdsOn,
    VgsThMax,
    Qg,

    // Diode — Vrrm peak repetitive reverse voltage V, If average forward current A,
    // Vf forward drop V, Trr reverse recovery time in seconds.
    Vrrm,
    If,
    Vf,
    Trr,
}

/// <summary>
/// Where a number came from. This exists because a wrong V_CEO does not produce a
/// warning — it produces a dead board. Nothing seeded from memory may be presented
/// as if it were read off a datasheet.
/// </summary>
public enum Provenance
{
    /// <summary>Typed in from general knowledge. Must be checked before anyone solders on it.</summary>
    Unverified,
    /// <summary>Checked line by line against the manufacturer datasheet.</summary>
    Datasheet,
    /// <summary>Contributed by a user of the product.</summary>
    Community,
}

/// <summary>
/// A part in the library. <see cref="Pinout"/> is not decoration: two transistors can
/// match on every electrical parameter and still destroy each other's circuit because
/// the legs are in a different order. Paper cross-reference tables routinely omit it.
/// </summary>
public sealed record Part(
    string Mpn,
    PartCategory Category,
    string Package,
    string Description,
    IReadOnlyDictionary<ParamKey, double> Params,
    Polarity Polarity = Polarity.None,
    string? Pinout = null,
    Provenance Provenance = Provenance.Unverified,
    string? Notes = null)
{
    public double? Get(ParamKey key) => Params.TryGetValue(key, out var v) ? v : null;

    public bool Has(ParamKey key) => Params.ContainsKey(key);

    public override string ToString() => $"{Mpn} ({Package})";
}

/// <summary>Formats engineering quantities the way the datasheet prints them.</summary>
public static class Eng
{
    private static readonly (double Scale, string Suffix)[] Prefixes =
    [
        (1e9, "G"), (1e6, "M"), (1e3, "k"), (1, ""), (1e-3, "m"), (1e-6, "µ"), (1e-9, "n"), (1e-12, "p"),
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

    public static string Format(Part p, ParamKey key) =>
        p.Get(key) is { } v ? Format(v, UnitOf(key)) : "—";

    public static string UnitOf(ParamKey key) => key switch
    {
        ParamKey.Vceo or ParamKey.Vebo or ParamKey.VceSat or ParamKey.Vds
            or ParamKey.VgsThMax or ParamKey.Vrrm or ParamKey.Vf => "V",
        ParamKey.Ic or ParamKey.Id or ParamKey.If => "A",
        ParamKey.Ptot => "W",
        ParamKey.Ft => "Hz",
        ParamKey.RdsOn => "Ω",
        ParamKey.Qg => "C",
        ParamKey.Trr => "s",
        _ => "",
    };
}
