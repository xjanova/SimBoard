namespace SimBoard.Document;

/// <summary>
/// Shorthand for writing catalogue entries.
///
/// The catalogue is split into one file per family so it can grow without every addition
/// touching the same file. These helpers keep an entry to the few lines that actually
/// differ between parts, so a hundred transistors read as a table rather than a hundred
/// object initialisers.
///
/// A parameter that is not known is left out. Never pass zero for "unknown": zero reads
/// as "rated for nothing", and every substitution rule that compares against it would
/// quietly reject or accept the wrong parts.
/// </summary>
public static class CatalogBuilder
{
    public static Pin P(string number, string name, PinKind kind, PinSide side, int slot, string? note = null)
        => new(number, name, kind, side, slot, note);

    public static Dictionary<ParamKey, double> Params(params (ParamKey Key, double Value)[] pairs)
    {
        var d = new Dictionary<ParamKey, double>(pairs.Length);
        foreach (var (k, v) in pairs) d[k] = v;
        return d;
    }

    /// <summary>
    /// A bipolar transistor. Pin order is the SPICE convention (B, C, E) regardless of
    /// the physical lead order, which <paramref name="pinout"/> records separately —
    /// they are different facts and conflating them is how legs get bent the wrong way.
    /// </summary>
    public static PartDefinition Bjt(
        string mpn, string nameTh, Polarity polarity, string package, string? pinout,
        double vceo, double ic, double ptot, double hfeMin, double hfeMax, double ft,
        string spiceModel, Provenance provenance = Provenance.Unverified,
        string? note = null, double? vebo = null, double? vceSat = null) => new()
    {
        Key = "Q-" + mpn,
        Prefix = "Q",
        Name = mpn,
        NameTh = nameTh,
        Mpn = mpn,
        Package = package,
        Pinout = pinout,
        Polarity = polarity,
        Provenance = provenance,
        Symbol = polarity == Polarity.Npn ? SymbolShape.BjtNpn : SymbolShape.BjtPnp,
        Spice = SpiceKind.Primitive,
        SpiceModel = spiceModel,
        BodyWidth = 4,
        BodyHeight = 4,
        NoteTh = note,
        Params = Merge(
            Params((ParamKey.Vceo, vceo), (ParamKey.Ic, ic), (ParamKey.Ptot, ptot),
                   (ParamKey.HfeMin, hfeMin), (ParamKey.HfeMax, hfeMax), (ParamKey.Ft, ft)),
            (ParamKey.Vebo, vebo), (ParamKey.VceSat, vceSat)),
        Pins =
        [
            P("1", "B", PinKind.Input, PinSide.Left, 1, "เบส"),
            P("2", "C", PinKind.Passive, PinSide.Top, 0, "คอลเลกเตอร์"),
            P("3", "E", PinKind.Passive, PinSide.Bottom, 0, "อิมิตเตอร์"),
        ],
    };

    public static PartDefinition Mosfet(
        string mpn, string nameTh, Polarity channel, string package, string? pinout,
        double vds, double id, double rdsOn, double vgsThMax, double ptot,
        string spiceModel, Provenance provenance = Provenance.Unverified,
        string? note = null, double? qg = null) => new()
    {
        Key = "Q-" + mpn,
        Prefix = "Q",
        Name = mpn,
        NameTh = nameTh,
        Mpn = mpn,
        Package = package,
        Pinout = pinout,
        Polarity = channel,
        Provenance = provenance,
        Symbol = channel == Polarity.NChannel ? SymbolShape.MosfetN : SymbolShape.MosfetP,
        Spice = SpiceKind.Primitive,
        SpiceModel = spiceModel,
        BodyWidth = 4,
        BodyHeight = 4,
        NoteTh = note,
        Params = Merge(
            Params((ParamKey.Vds, vds), (ParamKey.Id, id), (ParamKey.RdsOn, rdsOn),
                   (ParamKey.VgsThMax, vgsThMax), (ParamKey.Ptot, ptot)),
            (ParamKey.Qg, qg)),
        Pins =
        [
            P("1", "G", PinKind.Input, PinSide.Left, 1, "เกต"),
            P("2", "D", PinKind.Passive, PinSide.Top, 0, "เดรน"),
            P("3", "S", PinKind.Passive, PinSide.Bottom, 0, "ซอร์ส"),
        ],
    };

    public static PartDefinition Diode(
        string mpn, string nameTh, string package,
        double vrrm, double iF, double vf, double trr,
        string spiceModel, Provenance provenance = Provenance.Unverified,
        string? note = null, double? ifsm = null, double? vz = null,
        SymbolShape shape = SymbolShape.Diode) => new()
    {
        Key = "D-" + mpn,
        Prefix = "D",
        Name = mpn,
        NameTh = nameTh,
        Mpn = mpn,
        Package = package,
        Provenance = provenance,
        Symbol = shape,
        Spice = SpiceKind.Primitive,
        SpiceModel = spiceModel,
        BodyWidth = 3,
        BodyHeight = 2,
        NoteTh = note,
        Params = Merge(
            Params((ParamKey.Vrrm, vrrm), (ParamKey.If, iF), (ParamKey.Vf, vf), (ParamKey.Trr, trr)),
            (ParamKey.Ifsm, ifsm), (ParamKey.Vz, vz)),
        Pins =
        [
            P("1", "A", PinKind.Passive, PinSide.Left, 0, "แอโนด"),
            P("2", "K", PinKind.Passive, PinSide.Right, 0, "แคโทด — แถบคาดบนตัวถัง"),
        ],
    };

    /// <summary>A three-terminal linear regulator, IN / GND-or-ADJ / OUT.</summary>
    public static PartDefinition Regulator(
        string mpn, string nameTh, string package, string[] pinNames,
        double vout, double ioutMax, double vinMax, double dropout,
        Provenance provenance = Provenance.Unverified, string? note = null) => new()
    {
        Key = mpn,
        Prefix = "U",
        Name = mpn,
        NameTh = nameTh,
        Mpn = mpn,
        Package = package,
        Provenance = provenance,
        Symbol = SymbolShape.IcBody,
        Spice = SpiceKind.Subcircuit,
        SpiceModel = mpn,
        BodyWidth = 6,
        BodyHeight = 4,
        NoteTh = note,
        Params = Params((ParamKey.Vout, vout), (ParamKey.IoutMax, ioutMax),
                        (ParamKey.VinMax, vinMax), (ParamKey.Dropout, dropout)),
        Pins =
        [
            P("1", pinNames[0], PinKind.Power, PinSide.Left, 0),
            P("2", pinNames[1], pinNames[1] == "ADJ" ? PinKind.Analog : PinKind.Ground, PinSide.Bottom, 0),
            P("3", pinNames[2], PinKind.Output, PinSide.Right, 0),
        ],
    };

    /// <summary>Drops parameters whose value is unknown, so absent stays absent.</summary>
    private static Dictionary<ParamKey, double> Merge(
        Dictionary<ParamKey, double> baseline, params (ParamKey Key, double? Value)[] optional)
    {
        foreach (var (k, v) in optional)
            if (v is { } value) baseline[k] = value;
        return baseline;
    }
}
