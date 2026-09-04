namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// ออปแอมป์ ตัวเปรียบเทียบ และไอซีตั้งเวลา.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
///
/// Every entry here is <see cref="Provenance.Unverified"/>: the figures were typed from
/// general knowledge, not read off a datasheet. Check before you solder.
///
/// Conventions used in this file, so the numbers mean one thing and not two:
/// <list type="bullet">
///   <item><see cref="ParamKey.VinMax"/> is the maximum <em>total</em> supply, V+ to V−.
///     A part rated ±18 V is recorded as 36.</item>
///   <item><see cref="ParamKey.Vio"/> is the <em>maximum</em> input offset at 25 °C for
///     the commercial grade, not the typical figure, because that is the number a
///     substitution has to survive. Where the max is not known it is omitted.</item>
///   <item><see cref="ParamKey.Gbw"/> and <see cref="ParamKey.SlewRate"/> are typical
///     figures — they are always quoted as typicals and there is no useful minimum.</item>
///   <item>Comparators carry no Gbw or SlewRate at all. They are not linear amplifiers
///     and neither figure is specified for them; the number that matters is response
///     time, which this parameter set has no key for, so it lives in the note.</item>
/// </list>
///
/// Keys are prefixed "U-" so they cannot collide with the bare "LM358" / "NE555" entries
/// still living in <see cref="PartCatalog"/>'s core list. Those two predate this file and
/// carry no ratings; when they are folded into this family the prefix can go.
/// </summary>
public static class CatalogOpAmp
{
    // ── shared pinouts ───────────────────────────────────────────────────
    //
    // A DIP-8 dual op-amp and a DIP-14 quad op-amp each have one pinout that the whole
    // industry uses, so it is written once here and shared. Every part that deviates —
    // LM339, LM311, the 555 family, LM386 — is written out in full below instead, because
    // the deviation is exactly the thing that catches people.

    /// <summary>Builds the ratings dictionary, dropping anything not known.</summary>
    private static Dictionary<ParamKey, double> Ratings(
        double? gbw = null, double? slew = null, double? vio = null,
        double? supplyMax = null, double? ioutMax = null)
    {
        var d = new Dictionary<ParamKey, double>(5);
        if (gbw is { } g) d[ParamKey.Gbw] = g;
        if (slew is { } s) d[ParamKey.SlewRate] = s;
        if (vio is { } v) d[ParamKey.Vio] = v;
        if (supplyMax is { } vs) d[ParamKey.VinMax] = vs;
        if (ioutMax is { } i) d[ParamKey.IoutMax] = i;
        return d;
    }

    /// <summary>
    /// The universal DIP-8 dual pinout: 1 OUT1, 2 IN1−, 3 IN1+, 4 V−, 5 IN2+, 6 IN2−,
    /// 7 OUT2, 8 V+. LM358, TL072, NE5532, MCP6002, OPA2134, RC4558 and the LM393
    /// comparator all share it, which is why a dual op-amp drops straight into a socket
    /// meant for another one.
    /// </summary>
    private static PartDefinition Dual(
        string mpn, string nameTh, string spiceModel,
        Dictionary<ParamKey, double> ratings, string note,
        PinKind outKind = PinKind.Output,
        string negName = "V-",
        PinKind negKind = PinKind.Power,
        string negNote = "ไฟเลี้ยงขั้วลบ — ต่อลงกราวด์เมื่อใช้ไฟเลี้ยงเดี่ยว",
        string? outNote = null,
        string package = "DIP-8") => new()
    {
        Key = "U-" + mpn,
        Prefix = "U",
        Name = mpn,
        NameTh = nameTh,
        Mpn = mpn,
        Package = package,
        Provenance = Provenance.Unverified,
        Symbol = SymbolShape.IcBody,
        Spice = SpiceKind.Subcircuit,
        SpiceModel = spiceModel,
        SpiceLibrary = spiceModel.ToLowerInvariant() + ".lib",
        BodyWidth = 8,
        BodyHeight = 8,
        NoteTh = note,
        Params = ratings,
        Pins =
        [
            P("1", "OUT1", outKind, PinSide.Left, 0, outNote),
            P("2", "IN1-", PinKind.Analog, PinSide.Left, 1, "อินพุตกลับเฟส ช่อง 1"),
            P("3", "IN1+", PinKind.Analog, PinSide.Left, 2, "อินพุตไม่กลับเฟส ช่อง 1"),
            P("4", negName, negKind, PinSide.Left, 3, negNote),
            P("5", "IN2+", PinKind.Analog, PinSide.Right, 3, "อินพุตไม่กลับเฟส ช่อง 2"),
            P("6", "IN2-", PinKind.Analog, PinSide.Right, 2, "อินพุตกลับเฟส ช่อง 2"),
            P("7", "OUT2", outKind, PinSide.Right, 1, outNote),
            P("8", "V+", PinKind.Power, PinSide.Right, 0, "ไฟเลี้ยงขั้วบวก"),
        ],
    };

    /// <summary>
    /// The universal DIP-14 quad op-amp pinout: 1 OUT1, 2 IN1−, 3 IN1+, 4 V+, 5 IN2+,
    /// 6 IN2−, 7 OUT2, 8 OUT3, 9 IN3−, 10 IN3+, 11 V−, 12 IN4+, 13 IN4−, 14 OUT4.
    /// LM324, TL074, TL084 and MCP6004 share it.
    ///
    /// The LM339 quad comparator does NOT — it is a different chip in the same package,
    /// with V+ on pin 3 and ground on pin 12. It is written out separately below.
    /// </summary>
    private static PartDefinition Quad(
        string mpn, string nameTh, string spiceModel,
        Dictionary<ParamKey, double> ratings, string note,
        string negName = "V-",
        PinKind negKind = PinKind.Power,
        string negNote = "ไฟเลี้ยงขั้วลบ — ต่อลงกราวด์เมื่อใช้ไฟเลี้ยงเดี่ยว",
        string package = "DIP-14") => new()
    {
        Key = "U-" + mpn,
        Prefix = "U",
        Name = mpn,
        NameTh = nameTh,
        Mpn = mpn,
        Package = package,
        Provenance = Provenance.Unverified,
        Symbol = SymbolShape.IcBody,
        Spice = SpiceKind.Subcircuit,
        SpiceModel = spiceModel,
        SpiceLibrary = spiceModel.ToLowerInvariant() + ".lib",
        BodyWidth = 8,
        BodyHeight = 14,
        NoteTh = note,
        Params = ratings,
        Pins =
        [
            P("1", "OUT1", PinKind.Output, PinSide.Left, 0),
            P("2", "IN1-", PinKind.Analog, PinSide.Left, 1),
            P("3", "IN1+", PinKind.Analog, PinSide.Left, 2),
            P("4", "V+", PinKind.Power, PinSide.Left, 3, "ไฟเลี้ยงขั้วบวก — อยู่ขา 4 ไม่ใช่ขา 14"),
            P("5", "IN2+", PinKind.Analog, PinSide.Left, 4),
            P("6", "IN2-", PinKind.Analog, PinSide.Left, 5),
            P("7", "OUT2", PinKind.Output, PinSide.Left, 6),
            P("8", "OUT3", PinKind.Output, PinSide.Right, 6),
            P("9", "IN3-", PinKind.Analog, PinSide.Right, 5),
            P("10", "IN3+", PinKind.Analog, PinSide.Right, 4),
            P("11", negName, negKind, PinSide.Right, 3, negNote),
            P("12", "IN4+", PinKind.Analog, PinSide.Right, 2),
            P("13", "IN4-", PinKind.Analog, PinSide.Right, 1),
            P("14", "OUT4", PinKind.Output, PinSide.Right, 0),
        ],
    };

    /// <summary>
    /// The DIP-8 single op-amp pinout the 741 established and TL071/TL081 kept:
    /// 1 OFFSET N1, 2 IN−, 3 IN+, 4 V−, 5 OFFSET N2, 6 OUT, 7 V+, 8 NC.
    ///
    /// Note that V+ is pin 7 and V− is pin 4 — the opposite ends from a dual, where they
    /// are pins 8 and 4. Pin 8 on a single is genuinely unconnected.
    /// </summary>
    private static PartDefinition Single(
        string mpn, string nameTh, string spiceModel,
        Dictionary<ParamKey, double> ratings, string note,
        string package = "DIP-8") => new()
    {
        Key = "U-" + mpn,
        Prefix = "U",
        Name = mpn,
        NameTh = nameTh,
        Mpn = mpn,
        Package = package,
        Provenance = Provenance.Unverified,
        Symbol = SymbolShape.IcBody,
        Spice = SpiceKind.Subcircuit,
        SpiceModel = spiceModel,
        SpiceLibrary = spiceModel.ToLowerInvariant() + ".lib",
        BodyWidth = 8,
        BodyHeight = 8,
        NoteTh = note,
        Params = ratings,
        Pins =
        [
            P("1", "OFFSET1", PinKind.Analog, PinSide.Left, 0, "ปรับออฟเซต — ไม่ใช้ให้ปล่อยลอย"),
            P("2", "IN-", PinKind.Analog, PinSide.Left, 1, "อินพุตกลับเฟส"),
            P("3", "IN+", PinKind.Analog, PinSide.Left, 2, "อินพุตไม่กลับเฟส"),
            P("4", "V-", PinKind.Power, PinSide.Left, 3, "ไฟเลี้ยงขั้วลบ"),
            P("5", "OFFSET2", PinKind.Analog, PinSide.Right, 3, "ปรับออฟเซต — ไม่ใช้ให้ปล่อยลอย"),
            P("6", "OUT", PinKind.Output, PinSide.Right, 2),
            P("7", "V+", PinKind.Power, PinSide.Right, 1, "ไฟเลี้ยงขั้วบวก — ขา 7 ไม่ใช่ขา 8"),
            P("8", "NC", PinKind.NotConnected, PinSide.Right, 0, "ผู้ผลิตระบุให้ปล่อยลอย"),
        ],
    };

    // ── the catalogue ────────────────────────────────────────────────────

    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
        // ── general-purpose op-amps, single supply ────────────────────────

        Dual("LM358", "ออปแอมป์คู่ ใช้ไฟเลี้ยงเดี่ยวได้", "LM358",
            Ratings(gbw: 1e6, slew: 0.3e6, vio: 7e-3, supplyMax: 32),
            "ออปแอมป์ที่เจอบ่อยที่สุดในงานทั่วไป · ใช้ไฟเลี้ยงเดี่ยว 3–32V ได้ อินพุตกินลงถึง 0V "
          + "แต่เอาต์พุตขึ้นไม่ถึงราง V+ ขาดอยู่ราว 1.5V · ภาคเอาต์พุตมี crossover distortion "
          + "ใช้กับสัญญาณเสียงแล้วเพี้ยนชัดเจน · LM2904 คือตัวเดียวกันเกรดอุณหภูมิกว้างกว่า"),

        Quad("LM324", "ออปแอมป์สี่ตัวในตัวถังเดียว ใช้ไฟเลี้ยงเดี่ยวได้", "LM324",
            Ratings(gbw: 1e6, slew: 0.3e6, vio: 7e-3, supplyMax: 32),
            "LM358 สี่ช่องในตัวถัง DIP-14 คุณสมบัติเหมือนกันทุกอย่าง · "
          + "⚠ ลำดับขาไม่เหมือน LM339 ทั้งที่เป็น DIP-14 เหมือนกัน — สลับใส่กันแล้วไฟเลี้ยงจะเข้าที่ขาอินพุต "
          + "ที่นี่ V+ อยู่ขา 4 และ V− อยู่ขา 11"),

        Single("LM741", "ออปแอมป์เดี่ยวรุ่นคลาสสิก", "LM741",
            Ratings(gbw: 1e6, slew: 0.5e6, vio: 6e-3, supplyMax: 44),
            "ตัวตั้งต้นของออปแอมป์ทั้งหมด ยังใช้เรียนอยู่แต่ล้าสมัยแล้ว · ต้องใช้ไฟเลี้ยงสองขั้ว ±5V ขึ้นไป "
          + "อินพุตกินลงถึงกราวด์ไม่ได้ จึงทำวงจรไฟเลี้ยงเดี่ยวอ้างกราวด์ไม่ได้ · "
          + "ปรับออฟเซตด้วย POT 10k คร่อมขา 1 กับ 5 แล้วขากลางไป V− · ขา 8 ไม่ต่ออะไรเลย"),

        // ── JFET-input op-amps ────────────────────────────────────────────

        Single("TL071", "ออปแอมป์เดี่ยว อินพุต JFET เสียงรบกวนต่ำ", "TL071",
            Ratings(gbw: 3e6, slew: 13e6, vio: 10e-3, supplyMax: 36),
            "อินพุต JFET อิมพีแดนซ์สูงมาก กระแสไบแอสระดับพิโคแอมป์ เหมาะกับเซนเซอร์อิมพีแดนซ์สูง · "
          + "ย่านอินพุตไม่รวม V− ถ้าอินพุตต่ำกว่าขีดนั้น เอาต์พุตจะกลับเฟส (phase inversion) "
          + "แล้ววงจรป้อนกลับล็อกค้าง — เป็นอาการที่คนหาสาเหตุไม่เจอบ่อยที่สุดของตระกูลนี้"),

        Dual("TL072", "ออปแอมป์คู่ อินพุต JFET สลูว์เรตสูง", "TL072",
            Ratings(gbw: 3e6, slew: 13e6, vio: 10e-3, supplyMax: 36),
            "มาตรฐานของงานเสียงและวงจรกรองสัญญาณ เร็วกว่า LM358 ราว 40 เท่า · "
          + "ต้องใช้ไฟเลี้ยงสองขั้ว อินพุตกินลงถึง V− ไม่ได้ ต่างจาก LM358 ตรงนี้ "
          + "เอา TL072 ไปแทน LM358 ในวงจรไฟเลี้ยงเดี่ยวแล้วเงียบสนิท · "
          + "ป้อนอินพุตต่ำกว่าย่านแล้วเอาต์พุตกลับเฟสค้าง"),

        Quad("TL074", "ออปแอมป์สี่ตัว อินพุต JFET", "TL074",
            Ratings(gbw: 3e6, slew: 13e6, vio: 10e-3, supplyMax: 36),
            "TL072 สี่ช่อง ลำดับขาเหมือน LM324 ทุกประการ เปลี่ยนแทนกันได้ทางกายภาพ "
          + "แต่ TL074 ต้องมีไฟเลี้ยงสองขั้ว ส่วน LM324 ไม่ต้อง"),

        Single("TL081", "ออปแอมป์เดี่ยว อินพุต JFET รุ่นทั่วไป", "TL081",
            Ratings(gbw: 3e6, slew: 13e6, supplyMax: 36),
            "ตระกูลเดียวกับ TL071 ขาเหมือนกันทุกขา แต่เสียงรบกวนและออฟเซตสูงกว่า — "
          + "ใช้แทนกันได้ในงานทั่วไป ไม่ควรใช้แทนในภาคขยายเสียง · ค่าออฟเซตสูงสุดยังไม่ได้ตรวจ จึงไม่ใส่ไว้"),

        Dual("TL082", "ออปแอมป์คู่ อินพุต JFET รุ่นทั่วไป", "TL082",
            Ratings(gbw: 3e6, slew: 13e6, supplyMax: 36),
            "รุ่นประหยัดของ TL072 ขาเหมือนกันทุกขา · เสียงรบกวนสูงกว่า ใช้ในภาคเสียงแล้วได้ยินฮิสส์ "
          + "ที่เหลือแทนกันได้"),

        Quad("TL084", "ออปแอมป์สี่ตัว อินพุต JFET รุ่นทั่วไป", "TL084",
            Ratings(gbw: 3e6, slew: 13e6, supplyMax: 36),
            "รุ่นประหยัดของ TL074 ขาเหมือนกันทุกขาและเหมือน LM324"),

        // ── audio-grade op-amps ───────────────────────────────────────────

        Dual("NE5532", "ออปแอมป์คู่สำหรับงานเสียง เสียงรบกวนต่ำมาก", "NE5532",
            Ratings(gbw: 10e6, slew: 9e6, vio: 4e-3, supplyMax: 44),
            "มาตรฐานของภาคปรีแอมป์และมิกเซอร์ ขับโหลด 600Ω ได้ตรง · "
          + "กินไฟราว 8 mA ต่อช่อง สูงกว่าออปแอมป์ทั่วไปหลายเท่า อย่าจ่ายจากรางเล็ก ๆ · "
          + "ต้องมี C บายพาส 100n ติดขาไฟทุกตัว ไม่งั้นออสซิลเลต · NE5532A เป็นเกรดออฟเซตและนอยส์ต่ำกว่า"),

        Dual("OPA2134", "ออปแอมป์คู่เกรดออดิโอ อินพุต FET", "OPA2134",
            Ratings(gbw: 8e6, slew: 20e6, supplyMax: 36),
            "เกรดออดิโอ ราคาสูงกว่า TL072 หลายเท่า ขาเหมือนกันทุกขา เปลี่ยนแทนกันได้ทันที · "
          + "ต้องใช้ไฟเลี้ยงสองขั้ว ±2.5V ถึง ±18V · "
          + "ค่าออฟเซตสูงสุดยังไม่ได้ตรวจ จึงไม่ใส่ไว้แทนที่จะเดา"),

        Dual("RC4558", "ออปแอมป์คู่ราคาถูก นิยมในภาคเสียง", "RC4558",
            Ratings(gbw: 3e6, slew: 1e6, supplyMax: 36),
            "หัวใจของเอฟเฟกต์กีตาร์และแอมป์ราคาประหยัดจำนวนมาก ขาเหมือน LM358/TL072 · "
          + "สลูว์เรตแค่ 1 V/µs — สัญญาณแอมพลิจูดใหญ่ที่ความถี่สูงจะเพี้ยนแบบ slew limiting · "
          + "NJM4558 / JRC4558 คือตัวเดียวกันคนละผู้ผลิต ส่วน NJM4560 เป็นรุ่นเร็วกว่าที่ขาเหมือนกัน"),

        // ── CMOS rail-to-rail op-amps ─────────────────────────────────────

        Dual("MCP6002", "ออปแอมป์คู่ CMOS แรงดันต่ำ เอาต์พุตแกว่งถึงราง", "MCP6002",
            Ratings(gbw: 1e6, slew: 0.6e6, vio: 4.5e-3, supplyMax: 6.0),
            "⚠ ไฟเลี้ยงได้ 1.8–6.0V เท่านั้น ต่อ 12V หรือ ±15V เข้าไปพังทันที ต่างจากออปแอมป์ตัวอื่นในลิสต์นี้ · "
          + "อินพุตและเอาต์พุตแกว่งถึงรางทั้งสองข้าง ต่อเข้า ADC ของ MCU 3.3V ได้ตรง · "
          + "กินไฟราว 100 µA ต่อช่อง เหมาะกับงานแบตเตอรี่ · ขาเหมือน LM358 ทุกขา",
            negName: "VSS", negNote: "ไฟเลี้ยงขั้วลบ / กราวด์"),

        Quad("MCP6004", "ออปแอมป์สี่ตัว CMOS แรงดันต่ำ เอาต์พุตแกว่งถึงราง", "MCP6004",
            Ratings(gbw: 1e6, slew: 0.6e6, vio: 4.5e-3, supplyMax: 6.0),
            "MCP6002 สี่ช่อง ลำดับขาเหมือน LM324 · ⚠ ไฟเลี้ยงสูงสุด 6.0V ใส่แทน LM324 ในวงจร 12V แล้วพัง",
            negName: "VSS", negNote: "ไฟเลี้ยงขั้วลบ / กราวด์"),

        // ── comparators ───────────────────────────────────────────────────

        Dual("LM393", "ตัวเปรียบเทียบแรงดันคู่ เอาต์พุตโอเพนคอลเลกเตอร์", "LM393",
            Ratings(vio: 5e-3, supplyMax: 36),
            "⚠ เอาต์พุตเป็นโอเพนคอลเลกเตอร์ ต้องมีพูลอัป 1k–10k ไปไฟบวกเสมอ ไม่งั้นวัดได้แต่ 0V ตลอด · "
          + "ไม่ใช่ออปแอมป์ — ไม่มีวงจรชดเชยเฟส เอาไปทำวงจรป้อนกลับเชิงเส้นแล้วออสซิลเลต · "
          + "ขาเหมือน LM358 ทุกขา จึงใส่ผิดตัวได้ง่ายมาก ดูเบอร์บนตัวถังก่อนเสียบ · "
          + "พูลอัปต่อไปรางคนละระดับกับไฟเลี้ยงได้ ใช้แปลงระดับลอจิก 5V เป็น 3.3V ได้เลย · "
          + "เวลาตอบสนองราว 1.3 µs · LM2903 คือรุ่นเกรดยานยนต์ที่ขาเหมือนกัน",
            outKind: PinKind.OpenDrain,
            outNote: "โอเพนคอลเลกเตอร์ — ต้องมีพูลอัป",
            negName: "GND", negKind: PinKind.Ground,
            negNote: "กราวด์"),

        new PartDefinition
        {
            Key = "U-LM339",
            Prefix = "U",
            Name = "LM339",
            NameTh = "ตัวเปรียบเทียบแรงดันสี่ตัว เอาต์พุตโอเพนคอลเลกเตอร์",
            Mpn = "LM339N",
            Package = "DIP-14",
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = "LM339",
            SpiceLibrary = "lm339.lib",
            BodyWidth = 8,
            BodyHeight = 14,
            NoteTh = "⚠ ลำดับขาไม่เหมือน LM324 เลย ทั้งที่เป็น DIP-14 เหมือนกัน — ที่นี่ V+ อยู่ขา 3 และ GND อยู่ขา 12 "
                   + "เอา LM339 ใส่แทน LM324 ไฟเลี้ยงจะเข้าที่ขาอินพุตทันที · "
                   + "เอาต์พุตทั้งสี่ช่องเป็นโอเพนคอลเลกเตอร์ ต้องมีพูลอัปทุกช่องที่ใช้ · "
                   + "ต่อเอาต์พุตหลายช่องเข้าด้วยกันได้เป็น wired-AND ใช้ทำวงจรตรวจหน้าต่างแรงดัน · "
                   + "LM393 คือรุ่นสองช่อง DIP-8 ที่คุณสมบัติเหมือนกัน",
            Params = Ratings(vio: 5e-3, supplyMax: 36),
            Pins =
            [
                P("1", "OUT2", PinKind.OpenDrain, PinSide.Left, 0, "โอเพนคอลเลกเตอร์ — ต้องมีพูลอัป"),
                P("2", "OUT1", PinKind.OpenDrain, PinSide.Left, 1, "โอเพนคอลเลกเตอร์ — ต้องมีพูลอัป"),
                P("3", "V+", PinKind.Power, PinSide.Left, 2, "⚠ ไฟเลี้ยงขั้วบวกอยู่ขา 3 ไม่ใช่ขา 4 แบบ LM324"),
                P("4", "IN1-", PinKind.Analog, PinSide.Left, 3),
                P("5", "IN1+", PinKind.Analog, PinSide.Left, 4),
                P("6", "IN2-", PinKind.Analog, PinSide.Left, 5),
                P("7", "IN2+", PinKind.Analog, PinSide.Left, 6),
                P("8", "IN3-", PinKind.Analog, PinSide.Right, 6),
                P("9", "IN3+", PinKind.Analog, PinSide.Right, 5),
                P("10", "IN4-", PinKind.Analog, PinSide.Right, 4),
                P("11", "IN4+", PinKind.Analog, PinSide.Right, 3),
                P("12", "GND", PinKind.Ground, PinSide.Right, 2, "⚠ กราวด์อยู่ขา 12 ไม่ใช่ขา 11 แบบ LM324"),
                P("13", "OUT4", PinKind.OpenDrain, PinSide.Right, 1, "โอเพนคอลเลกเตอร์ — ต้องมีพูลอัป"),
                P("14", "OUT3", PinKind.OpenDrain, PinSide.Right, 0, "โอเพนคอลเลกเตอร์ — ต้องมีพูลอัป"),
            ],
        },

        new PartDefinition
        {
            Key = "U-LM311",
            Prefix = "U",
            Name = "LM311",
            NameTh = "ตัวเปรียบเทียบแรงดันเดี่ยว เอาต์พุตแยกขาคอลเลกเตอร์กับอิมิตเตอร์",
            Mpn = "LM311N",
            Package = "DIP-8",
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = "LM311",
            SpiceLibrary = "lm311.lib",
            BodyWidth = 8,
            BodyHeight = 8,
            NoteTh = "⚠ ขาอินพุตสลับกับออปแอมป์ทั่วไป — ขา 2 คือ IN+ และขา 3 คือ IN− ตรงข้ามกับ LM358/LM741 ทุกตัว "
                   + "นี่คือความผิดพลาดที่พบบ่อยที่สุดของไอซีตัวนี้ · "
                   + "เอาต์พุตเป็นทรานซิสเตอร์เปลือย ขา 7 คือคอลเลกเตอร์ ขา 1 คืออิมิตเตอร์ "
                   + "ปกติต่อขา 1 ลงกราวด์แล้วใส่พูลอัปที่ขา 7 · เพราะแยกขาแบบนี้จึงต่อพูลอัปไปรางอื่นได้ "
                   + "หรือขับโหลดที่อ้างกับ V+ ก็ได้ · ขา 6 เป็นขา strobe ปิดเอาต์พุตได้",
            Params = Ratings(vio: 7.5e-3, supplyMax: 36, ioutMax: 0.05),
            Pins =
            [
                P("1", "GND/EMIT", PinKind.Passive, PinSide.Left, 0, "อิมิตเตอร์ของทรานซิสเตอร์เอาต์พุต — ปกติต่อลงกราวด์"),
                P("2", "IN+", PinKind.Analog, PinSide.Left, 1, "⚠ อินพุตไม่กลับเฟส — สลับกับออปแอมป์ทั่วไป"),
                P("3", "IN-", PinKind.Analog, PinSide.Left, 2, "⚠ อินพุตกลับเฟส — สลับกับออปแอมป์ทั่วไป"),
                P("4", "V-", PinKind.Power, PinSide.Left, 3, "ไฟเลี้ยงขั้วลบ — ต่อลงกราวด์เมื่อใช้ไฟเลี้ยงเดี่ยว"),
                P("5", "BAL", PinKind.Analog, PinSide.Right, 3, "ปรับสมดุล — ไม่ใช้ให้ปล่อยลอย"),
                P("6", "BAL/STRB", PinKind.Analog, PinSide.Right, 2, "ปรับสมดุล / strobe ปิดเอาต์พุต"),
                P("7", "OUT", PinKind.OpenDrain, PinSide.Right, 1, "คอลเลกเตอร์เปลือย — ต้องมีพูลอัป"),
                P("8", "V+", PinKind.Power, PinSide.Right, 0, "ไฟเลี้ยงขั้วบวก"),
            ],
        },

        // ── timers ────────────────────────────────────────────────────────

        new PartDefinition
        {
            Key = "U-NE555",
            Prefix = "U",
            Name = "NE555",
            NameTh = "ไอซีตั้งเวลา 555",
            Mpn = "NE555P",
            Package = "DIP-8",
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = "UA555",
            SpiceLibrary = "ua555.lib",
            BodyWidth = 8,
            BodyHeight = 8,
            NoteTh = "ขา 5 (CTRL) ต่อ C 10n ลงกราวด์ — ปล่อยลอยแล้วจุด trip เพี้ยนไปตามสัญญาณรบกวนทั้งวงจร · "
                   + "ขา 4 (RESET) ไม่ใช้ต้องต่อไปที่ VCC ปล่อยลอยแล้ววงจรรีเซ็ตตัวเองแบบสุ่ม · "
                   + "เอาต์พุตกระชากได้ถึง 200 mA ตอนสลับสถานะ ทำให้รางไฟกระเพื่อมจนไอซีตัวอื่นบนบอร์ดรวน "
                   + "ต้องมี C 100n ติดขา 8 ให้ชิดที่สุด · "
                   + "รุ่น CMOS (TLC555 / ICM7555 / NE7555) ขาเหมือนกันทุกขา แต่ขับกระแสได้น้อยกว่ามาก "
                   + "เปลี่ยนแทนกันแล้ววงจรที่ขับโหลดตรง ๆ จะไม่ทำงาน",
            Params = Ratings(supplyMax: 16, ioutMax: 0.2),
            Pins =
            [
                P("1", "GND", PinKind.Ground, PinSide.Left, 0),
                P("2", "TRIG", PinKind.Input, PinSide.Left, 1, "ทริกเมื่อแรงดันต่ำกว่า 1/3 VCC"),
                P("3", "OUT", PinKind.Output, PinSide.Left, 2, "ขับได้ทั้งจ่ายและจมกระแส 200 mA"),
                P("4", "RESET", PinKind.Input, PinSide.Left, 3, "แอกทีฟต่ำ — ไม่ใช้ต้องต่อ VCC"),
                P("5", "CTRL", PinKind.Analog, PinSide.Right, 3, "แรงดันควบคุม — ปกติต่อ C 10n ลงกราวด์"),
                P("6", "THRES", PinKind.Input, PinSide.Right, 2, "รีเซ็ตเมื่อแรงดันสูงกว่า 2/3 VCC"),
                P("7", "DISCH", PinKind.OpenDrain, PinSide.Right, 1, "คายประจุ C ตั้งเวลา — คอลเลกเตอร์เปลือย"),
                P("8", "VCC", PinKind.Power, PinSide.Right, 0, "4.5–16V"),
            ],
        },

        new PartDefinition
        {
            Key = "U-TLC555",
            Prefix = "U",
            Name = "TLC555",
            NameTh = "ไอซีตั้งเวลา 555 แบบ CMOS กินไฟต่ำ",
            Mpn = "TLC555CP",
            Package = "DIP-8",
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = "TLC555",
            SpiceLibrary = "tlc555.lib",
            BodyWidth = 8,
            BodyHeight = 8,
            NoteTh = "ขาเหมือน NE555 ทุกขา เปลี่ยนแทนกันได้ทางกายภาพ · "
                   + "กินไฟระดับไมโครแอมป์และอินพุตเป็น CMOS จึงใช้ R ตั้งเวลาได้ถึงหลายเมกะโอห์ม "
                   + "ทำคาบยาว ๆ ได้โดยไม่ต้องใช้ C ใหญ่ · "
                   + "⚠ ขับกระแสไม่สมมาตร — จมกระแสได้ราว 100 mA แต่จ่ายออกได้เพียงหลักสิบมิลลิแอมป์ "
                   + "วงจร NE555 เดิมที่ขับ LED หรือรีเลย์จากขา 3 ฝั่งบวกจะทำงานไม่ไหว ต้องเปลี่ยนไปต่อแบบจมกระแส · "
                   + "ไฟเลี้ยงสูงสุด 15V ต่ำกว่า NE555 เล็กน้อย",
            Params = Ratings(supplyMax: 15),
            Pins =
            [
                P("1", "GND", PinKind.Ground, PinSide.Left, 0),
                P("2", "TRIG", PinKind.Input, PinSide.Left, 1, "ทริกเมื่อแรงดันต่ำกว่า 1/3 VDD"),
                P("3", "OUT", PinKind.Output, PinSide.Left, 2, "จมกระแสได้มากกว่าที่จ่ายออกมาก"),
                P("4", "RESET", PinKind.Input, PinSide.Left, 3, "แอกทีฟต่ำ — ไม่ใช้ต้องต่อ VDD"),
                P("5", "CTRL", PinKind.Analog, PinSide.Right, 3, "แรงดันควบคุม — ปกติต่อ C 10n ลงกราวด์"),
                P("6", "THRES", PinKind.Input, PinSide.Right, 2, "รีเซ็ตเมื่อแรงดันสูงกว่า 2/3 VDD"),
                P("7", "DISCH", PinKind.OpenDrain, PinSide.Right, 1, "คายประจุ C ตั้งเวลา"),
                P("8", "VDD", PinKind.Power, PinSide.Right, 0, "2–15V"),
            ],
        },

        new PartDefinition
        {
            Key = "U-NE556",
            Prefix = "U",
            Name = "NE556",
            NameTh = "ไอซีตั้งเวลา 555 สองชุดในตัวถังเดียว",
            Mpn = "NE556N",
            Package = "DIP-14",
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = "NE556",
            SpiceLibrary = "ne556.lib",
            BodyWidth = 8,
            BodyHeight = 14,
            NoteTh = "555 สองชุดที่ใช้ไฟเลี้ยงร่วมกัน · ⚠ ขาไฟไม่ได้อยู่ที่เดิม — VCC อยู่ขา 14 และ GND อยู่ขา 7 "
                   + "ไม่ใช่ขา 8 กับขา 1 แบบ 555 · ลำดับขาในแต่ละชุดก็ไม่ได้เรียงเหมือน 555 ต้องดูตารางขาทุกครั้ง · "
                   + "ทั้งสองชุดใช้รางไฟเดียวกัน กระแสกระชากของชุดหนึ่งรบกวนจังหวะของอีกชุดได้ "
                   + "ถ้าต้องการคาบที่แม่นจริงให้ใช้ 555 สองตัวแยกกัน · "
                   + "ขา CTRL ของทั้งสองชุดควรมี C 10n ลงกราวด์เหมือนกัน",
            Params = Ratings(supplyMax: 16, ioutMax: 0.2),
            Pins =
            [
                P("1", "DISCH1", PinKind.OpenDrain, PinSide.Left, 0, "คายประจุ ชุด 1"),
                P("2", "THRES1", PinKind.Input, PinSide.Left, 1, "สูงกว่า 2/3 VCC ชุด 1"),
                P("3", "CTRL1", PinKind.Analog, PinSide.Left, 2, "ปกติต่อ C 10n ลงกราวด์"),
                P("4", "RESET1", PinKind.Input, PinSide.Left, 3, "แอกทีฟต่ำ — ไม่ใช้ต้องต่อ VCC"),
                P("5", "OUT1", PinKind.Output, PinSide.Left, 4),
                P("6", "TRIG1", PinKind.Input, PinSide.Left, 5, "ต่ำกว่า 1/3 VCC ชุด 1"),
                P("7", "GND", PinKind.Ground, PinSide.Left, 6, "⚠ กราวด์อยู่ขา 7"),
                P("8", "TRIG2", PinKind.Input, PinSide.Right, 6, "ต่ำกว่า 1/3 VCC ชุด 2"),
                P("9", "OUT2", PinKind.Output, PinSide.Right, 5),
                P("10", "RESET2", PinKind.Input, PinSide.Right, 4, "แอกทีฟต่ำ — ไม่ใช้ต้องต่อ VCC"),
                P("11", "CTRL2", PinKind.Analog, PinSide.Right, 3, "ปกติต่อ C 10n ลงกราวด์"),
                P("12", "THRES2", PinKind.Input, PinSide.Right, 2, "สูงกว่า 2/3 VCC ชุด 2"),
                P("13", "DISCH2", PinKind.OpenDrain, PinSide.Right, 1, "คายประจุ ชุด 2"),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0, "⚠ ไฟเลี้ยงอยู่ขา 14"),
            ],
        },

        // ── power op-amp ──────────────────────────────────────────────────

        new PartDefinition
        {
            Key = "U-LM386",
            Prefix = "U",
            Name = "LM386",
            NameTh = "ไอซีขยายเสียงกำลังต่ำ",
            Mpn = "LM386N-1",
            Package = "DIP-8",
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = "LM386",
            SpiceLibrary = "lm386.lib",
            BodyWidth = 8,
            BodyHeight = 8,
            NoteTh = "แอมป์ขับลำโพงเล็ก ๆ ที่ต่อได้ด้วยชิ้นส่วนไม่กี่ตัว · "
                   + "อัตราขยายคงที่ 20 เท่า ต่อ C 10µ คร่อมขา 1 กับ 8 จะได้ 200 เท่า "
                   + "(ขั้วบวกของ C ไปขา 1) · ขา 7 (BYPASS) ต่อ C 10µ ลงกราวด์ช่วยลดเสียงฮัม · "
                   + "ขา 5 นิ่งอยู่ที่ครึ่งหนึ่งของไฟเลี้ยง ต้องผ่าน C 220µ ก่อนเข้าลำโพง ต่อตรงลำโพงไหม้ · "
                   + "⚠ รุ่นย่อยรับไฟไม่เท่ากัน — LM386N-1 และ N-3 รับได้ถึง 12V ส่วน N-4 รับได้ถึง 18V "
                   + "ค่าที่ใส่ไว้นี้เป็นของ N-1 · ออสซิลเลตง่ายมาก ต้องมีวงจร Zobel (10Ω อนุกรม 50n) ที่เอาต์พุต",
            Params = Ratings(supplyMax: 12),
            Pins =
            [
                P("1", "GAIN", PinKind.Analog, PinSide.Left, 0, "ต่อ C คร่อมกับขา 8 เพื่อเพิ่มอัตราขยาย"),
                P("2", "IN-", PinKind.Analog, PinSide.Left, 1, "ปกติต่อลงกราวด์"),
                P("3", "IN+", PinKind.Analog, PinSide.Left, 2, "อินพุตสัญญาณ"),
                P("4", "GND", PinKind.Ground, PinSide.Left, 3),
                P("5", "VOUT", PinKind.Output, PinSide.Right, 3, "อยู่ที่ครึ่งหนึ่งของไฟเลี้ยง — ต้องผ่าน C ก่อนเข้าลำโพง"),
                P("6", "VS", PinKind.Power, PinSide.Right, 2, "ไฟเลี้ยง — ดูรุ่นย่อยก่อนจ่ายเกิน 12V"),
                P("7", "BYPASS", PinKind.Analog, PinSide.Right, 1, "ต่อ C 10µ ลงกราวด์ลดเสียงฮัม"),
                P("8", "GAIN", PinKind.Analog, PinSide.Right, 0, "ต่อ C คร่อมกับขา 1 เพื่อเพิ่มอัตราขยาย"),
            ],
        },
    ];
}
