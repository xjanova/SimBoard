namespace SimBoard.Parts;

/// <summary>
/// The seed library: the parts a repair bench actually reaches for.
///
/// EVERY entry here is <see cref="Provenance.Unverified"/> — the figures are typed from
/// general knowledge, not read off a datasheet, and the pinout letters in particular vary
/// between manufacturers of the same part number. Nothing in this file may be presented
/// as authoritative until someone has checked it line by line against the PDF. The
/// consequence of a wrong V_CEO is not a warning dialog; it is a dead board.
///
/// Pinout convention: TO-92 and TO-126 read left→right with the flat/label face toward
/// you and the leads pointing down. TO-220 reads left→right facing the printed side.
/// </summary>
public static class PartLibrary
{
    private static Dictionary<ParamKey, double> P(params (ParamKey, double)[] pairs) =>
        pairs.ToDictionary(p => p.Item1, p => p.Item2);

    private static Part Bjt(string mpn, Polarity pol, string pkg, string pinout, string desc,
        double vceo, double ic, double ptot, double hfeMin, double hfeMax, double ft) =>
        new(mpn, PartCategory.Bjt, pkg, desc,
            P((ParamKey.Vceo, vceo), (ParamKey.Ic, ic), (ParamKey.Ptot, ptot),
              (ParamKey.HfeMin, hfeMin), (ParamKey.HfeMax, hfeMax), (ParamKey.Ft, ft)),
            pol, pinout);

    private static Part Fet(string mpn, Polarity pol, string pkg, string pinout, string desc,
        double vds, double id, double rdsOn, double vgsTh, double qg) =>
        new(mpn, PartCategory.Mosfet, pkg, desc,
            P((ParamKey.Vds, vds), (ParamKey.Id, id), (ParamKey.RdsOn, rdsOn),
              (ParamKey.VgsThMax, vgsTh), (ParamKey.Qg, qg)),
            pol, pinout);

    private static Part Dio(string mpn, string pkg, string desc,
        double vrrm, double iF, double vf, double trr) =>
        new(mpn, PartCategory.Diode, pkg, desc,
            P((ParamKey.Vrrm, vrrm), (ParamKey.If, iF), (ParamKey.Vf, vf), (ParamKey.Trr, trr)));

    public static IReadOnlyList<Part> All { get; } =
    [
        // ── small-signal NPN ──────────────────────────────────────────────
        Bjt("2N3904",  Polarity.Npn, "TO-92", "EBC", "NPN สัญญาณเล็ก อเนกประสงค์",      40, 0.2,  0.625, 100, 300, 300e6),
        Bjt("PN2222A", Polarity.Npn, "TO-92", "EBC", "NPN อเนกประสงค์ กระแสสูงกว่า 3904", 40, 0.6,  0.625, 100, 300, 300e6),
        Bjt("2N4401",  Polarity.Npn, "TO-92", "EBC", "NPN สวิตช์ กระแส 600 mA",          40, 0.6,  0.625, 100, 300, 250e6),
        Bjt("2N5551",  Polarity.Npn, "TO-92", "EBC", "NPN แรงดันสูง 160 V",              160, 0.6, 0.625,  80, 250, 100e6),
        Bjt("BC547B",  Polarity.Npn, "TO-92", "CBE", "NPN สัญญาณเล็ก ตระกูลยุโรป",        45, 0.1,  0.5,   200, 450, 300e6),
        Bjt("BC548B",  Polarity.Npn, "TO-92", "CBE", "NPN สัญญาณเล็ก 30 V",              30, 0.1,  0.5,   200, 450, 300e6),
        Bjt("BC337",   Polarity.Npn, "TO-92", "CBE", "NPN กระแส 800 mA",                 45, 0.8,  0.625, 100, 630, 200e6),
        Bjt("S8050",   Polarity.Npn, "TO-92", "EBC", "NPN กระแสสูง ราคาถูก",             25, 0.7,  1.0,    85, 300, 100e6),

        // ── small-signal PNP ──────────────────────────────────────────────
        Bjt("2N3906",  Polarity.Pnp, "TO-92", "EBC", "PNP สัญญาณเล็ก คู่ของ 2N3904",     40, 0.2,  0.625, 100, 300, 250e6),
        Bjt("2N4403",  Polarity.Pnp, "TO-92", "EBC", "PNP สวิตช์ กระแส 600 mA",          40, 0.6,  0.625, 100, 300, 200e6),
        Bjt("2N5401",  Polarity.Pnp, "TO-92", "EBC", "PNP แรงดันสูง 150 V",             150, 0.6,  0.625,  60, 240, 100e6),
        Bjt("BC557B",  Polarity.Pnp, "TO-92", "CBE", "PNP สัญญาณเล็ก คู่ของ BC547",      45, 0.1,  0.5,   200, 450, 150e6),
        Bjt("BC558B",  Polarity.Pnp, "TO-92", "CBE", "PNP สัญญาณเล็ก 30 V",              30, 0.1,  0.5,   200, 450, 150e6),
        Bjt("BC327",   Polarity.Pnp, "TO-92", "CBE", "PNP กระแส 800 mA",                 45, 0.8,  0.625, 100, 630, 100e6),
        Bjt("S8550",   Polarity.Pnp, "TO-92", "EBC", "PNP กระแสสูง ราคาถูก",             25, 0.7,  1.0,    85, 300, 100e6),

        // ── power NPN ─────────────────────────────────────────────────────
        Bjt("BD139",    Polarity.Npn, "TO-126", "ECB", "NPN กำลังกลาง 1.5 A",           80,  1.5, 12.5,  40, 250, 190e6),
        Bjt("2SD882",   Polarity.Npn, "TO-126", "ECB", "NPN กำลังกลาง 3 A",             30,  3.0, 10.0,  60, 300,  90e6),
        Bjt("TIP31C",   Polarity.Npn, "TO-220", "BCE", "NPN กำลัง 3 A",                100,  3.0, 40.0,  10,  50,   3e6),
        Bjt("TIP41C",   Polarity.Npn, "TO-220", "BCE", "NPN กำลัง 6 A",                100,  6.0, 65.0,  15,  75,   3e6),
        Bjt("MJE13003", Polarity.Npn, "TO-220", "BCE", "NPN แรงดันสูง สำหรับ SMPS",     400,  1.5, 40.0,   8,  40,   4e6),
        Bjt("2N3055",   Polarity.Npn, "TO-3",   "BE+", "NPN กำลังสูง 15 A (ตัวถัง = C)", 60, 15.0, 115.0,  20,  70, 2.5e6),

        // ── power PNP ─────────────────────────────────────────────────────
        Bjt("BD140",  Polarity.Pnp, "TO-126", "ECB", "PNP กำลังกลาง คู่ของ BD139",  80, 1.5, 12.5, 40, 250, 75e6),
        Bjt("2SB772", Polarity.Pnp, "TO-126", "ECB", "PNP กำลังกลาง คู่ของ D882",   30, 3.0, 10.0, 60, 300, 80e6),
        Bjt("TIP32C", Polarity.Pnp, "TO-220", "BCE", "PNP กำลัง 3 A",              100, 3.0, 40.0, 10,  50,  3e6),
        Bjt("TIP42C", Polarity.Pnp, "TO-220", "BCE", "PNP กำลัง 6 A คู่ของ TIP41C", 100, 6.0, 65.0, 15,  75,  3e6),

        // ── MOSFET ────────────────────────────────────────────────────────
        Fet("IRFZ44N",  Polarity.NChannel, "TO-220", "GDS", "N-ch 55 V 49 A",           55,  49, 0.0175, 4.0,  63e-9),
        Fet("IRF3205",  Polarity.NChannel, "TO-220", "GDS", "N-ch 55 V กระแสสูงมาก",     55, 110, 0.0080, 4.0, 146e-9),
        Fet("IRLZ44N",  Polarity.NChannel, "TO-220", "GDS", "N-ch ขับด้วยลอจิก 5 V",     55,  47, 0.0220, 2.0,  48e-9),
        Fet("IRF540N",  Polarity.NChannel, "TO-220", "GDS", "N-ch 100 V 33 A",         100,  33, 0.0440, 4.0,  71e-9),
        Fet("IRF740",   Polarity.NChannel, "TO-220", "GDS", "N-ch 400 V สำหรับ SMPS",  400,  10, 0.5500, 4.0,  63e-9),
        Fet("2N7000",   Polarity.NChannel, "TO-92",  "SGD", "N-ch สัญญาณเล็ก",           60, 0.2, 5.0000, 3.0,   1e-9),
        Fet("IRF9540N", Polarity.PChannel, "TO-220", "GDS", "P-ch 100 V 23 A",         100,  23, 0.1170, 4.0,  70e-9),

        // ── diodes ────────────────────────────────────────────────────────
        Dio("1N4001", "DO-41",  "เรกติไฟเออร์ 50 V 1 A",         50, 1.0, 1.10, 2.0e-6),
        Dio("1N4004", "DO-41",  "เรกติไฟเออร์ 400 V 1 A",       400, 1.0, 1.10, 2.0e-6),
        Dio("1N4007", "DO-41",  "เรกติไฟเออร์ 1000 V 1 A",     1000, 1.0, 1.10, 2.0e-6),
        Dio("1N5408", "DO-201", "เรกติไฟเออร์ 1000 V 3 A",     1000, 3.0, 1.20, 2.0e-6),
        Dio("FR107",  "DO-41",  "เรกติไฟเออร์เร็ว 1000 V 1 A", 1000, 1.0, 1.30, 500e-9),
        Dio("UF4007", "DO-41",  "อัลตราฟาสต์ 1000 V 1 A",      1000, 1.0, 1.70,  75e-9),
        Dio("1N4148", "DO-35",  "สวิตชิ่งเร็ว 100 V",           100, 0.2, 1.00, 4.0e-9),
        Dio("1N5819", "DO-41",  "ชอตต์กี 40 V 1 A",              40, 1.0, 0.45, 1.0e-9),
        Dio("1N5822", "DO-201", "ชอตต์กี 40 V 3 A",              40, 3.0, 0.53, 1.0e-9),
    ];

    private static readonly Dictionary<string, Part> Index =
        All.ToDictionary(p => p.Mpn, StringComparer.OrdinalIgnoreCase);

    public static Part? Find(string mpn)
    {
        if (Index.TryGetValue(mpn.Trim(), out var exact)) return exact;

        // Technicians type what is printed on the part: "D882" for 2SD882, "C1815" for
        // 2SC1815. The Japanese 2S prefix is almost never marked on the package.
        var q = mpn.Trim();
        return Index.Values.FirstOrDefault(p =>
                   p.Mpn.Equals("2S" + q, StringComparison.OrdinalIgnoreCase))
            ?? Index.Values.FirstOrDefault(p =>
                   p.Mpn.Replace("2S", "", StringComparison.OrdinalIgnoreCase)
                        .Equals(q, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<Part> InCategory(PartCategory c) => [.. All.Where(p => p.Category == c)];

    /// <summary>True while any part still carries figures nobody has checked against a datasheet.</summary>
    public static bool HasUnverifiedData => All.Any(p => p.Provenance == Provenance.Unverified);
}
