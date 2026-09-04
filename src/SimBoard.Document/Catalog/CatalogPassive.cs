namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// อุปกรณ์พาสซีฟ.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
///
/// What is here is what a plain R, C or L cannot say for itself: the dielectric that
/// decides whether 100 nF is still 100 nF under bias, the voltage rating that decides
/// whether a capacitor survives the rail, the crystal that needs load caps, the fuse
/// that must not be replaced with a bigger one. E12 resistor values are deliberately
/// absent — the generic resistor with an editable value already covers them, and a
/// hundred entries that differ only in a number make the list harder to search, not
/// richer.
///
/// The local helpers below exist so the one fact that must never vary — every figure in
/// this file was typed from general knowledge and none of it has been checked against a
/// datasheet — is written in nine places instead of forty-three.
/// </summary>
public static class CatalogPassive
{
    // ── helpers ──────────────────────────────────────────────────────────
    // CatalogBuilder has no helper for two-terminal passives, so the shapes that repeat
    // are built here. Provenance is set inside each one and never taken as a parameter.

    /// <summary>Keeps an unknown parameter absent instead of writing a zero for it.</summary>
    private static Dictionary<ParamKey, double> Optional(params (ParamKey Key, double? Value)[] pairs)
    {
        var d = new Dictionary<ParamKey, double>();
        foreach (var (k, v) in pairs)
            if (v is { } value) d[k] = value;
        return d;
    }

    private static PartDefinition Cap(
        string key, string name, string nameTh, string defaultValue, bool polarised,
        string? package = null, double? vmax = null, double? tolerance = null,
        string? note = null) => new()
    {
        Key = key,
        Prefix = "C",
        Name = name,
        NameTh = nameTh,
        Package = package,
        Symbol = polarised ? SymbolShape.CapacitorPolarised : SymbolShape.CapacitorNonPolar,
        Spice = SpiceKind.Primitive,
        DefaultValue = defaultValue,
        Unit = "F",
        BodyWidth = 3,
        BodyHeight = 2,
        Provenance = Provenance.Unverified,
        NoteTh = note,
        Params = Optional((ParamKey.Vmax, vmax), (ParamKey.Tolerance, tolerance)),
        Pins =
        [
            P("1", polarised ? "+" : "A", PinKind.Passive, PinSide.Left, 0,
              polarised ? "ขั้วบวก — ขายาว" : null),
            P("2", polarised ? "-" : "B", PinKind.Passive, PinSide.Right, 0,
              polarised ? "ขั้วลบ — แถบบนตัวถัง" : null),
        ],
    };

    /// <summary>
    /// A quartz crystal. Emitted as nothing: ngspice will not start a Pierce oscillator
    /// in any run length a person will wait for, so the netlist says it skipped the part
    /// rather than pretending to model it.
    /// </summary>
    private static PartDefinition Xtal(
        string key, string name, string nameTh, string frequency, string package,
        double? tolerance = null, string? note = null) => new()
    {
        Key = key,
        Prefix = "Y",
        Name = name,
        NameTh = nameTh,
        Mpn = null,
        Package = package,
        Symbol = SymbolShape.Crystal,
        Spice = SpiceKind.Primitive,
        DefaultValue = frequency,
        Unit = "Hz",
        BodyWidth = 4,
        BodyHeight = 3,
        Provenance = Provenance.Unverified,
        NoteTh = note,
        Params = Optional((ParamKey.Tolerance, tolerance)),
        Pins =
        [
            P("1", "1", PinKind.Passive, PinSide.Left, 0),
            P("2", "2", PinKind.Passive, PinSide.Right, 0),
        ],
    };

    private static PartDefinition Coil(
        string key, string name, string nameTh, string defaultValue,
        string? package = null, string? note = null) => new()
    {
        Key = key,
        Prefix = key.StartsWith("FB-", StringComparison.Ordinal) ? "FB" : "L",
        Name = name,
        NameTh = nameTh,
        Package = package,
        Symbol = SymbolShape.Inductor,
        Spice = SpiceKind.Primitive,
        DefaultValue = defaultValue,
        Unit = "H",
        BodyWidth = 4,
        BodyHeight = 2,
        Provenance = Provenance.Unverified,
        NoteTh = note,
        Pins =
        [
            P("1", "A", PinKind.Passive, PinSide.Left, 0),
            P("2", "B", PinKind.Passive, PinSide.Right, 0),
        ],
    };

    /// <summary>
    /// Anything the simulator has to treat as a plain resistance: thermistors, fuses,
    /// varistors. The editable value is that resistance in ohms, which is why every one
    /// of them carries a note saying what the value means and what it does not model.
    /// </summary>
    private static PartDefinition Resistive(
        string key, string prefix, string name, string nameTh, string defaultValue,
        string? package = null, double? vmax = null, double? pmax = null,
        double? tolerance = null, string? note = null) => new()
    {
        Key = key,
        Prefix = prefix,
        Name = name,
        NameTh = nameTh,
        Package = package,
        Symbol = SymbolShape.Box,
        Spice = SpiceKind.Primitive,
        DefaultValue = defaultValue,
        Unit = "Ω",
        BodyWidth = 4,
        BodyHeight = 2,
        Provenance = Provenance.Unverified,
        NoteTh = note,
        Params = Optional((ParamKey.Vmax, vmax), (ParamKey.Pmax, pmax),
                          (ParamKey.Tolerance, tolerance)),
        Pins =
        [
            P("1", "A", PinKind.Passive, PinSide.Left, 0),
            P("2", "B", PinKind.Passive, PinSide.Right, 0),
        ],
    };

    /// <summary>A trimmer: terminal 2 is the wiper on every Bourns-pattern part.</summary>
    private static PartDefinition Trimmer(
        string key, string name, string nameTh, string package,
        double? pmax = null, double? tolerance = null, string? note = null) => new()
    {
        Key = key,
        Prefix = "RV",
        Name = name,
        NameTh = nameTh,
        Package = package,
        Symbol = SymbolShape.Box,
        Spice = SpiceKind.Primitive,
        DefaultValue = "10k",
        Unit = "Ω",
        BodyWidth = 4,
        BodyHeight = 3,
        Provenance = Provenance.Unverified,
        NoteTh = note,
        Params = Optional((ParamKey.Pmax, pmax), (ParamKey.Tolerance, tolerance)),
        Pins =
        [
            P("1", "A", PinKind.Passive, PinSide.Left, 0),
            P("2", "W", PinKind.Passive, PinSide.Top, 0, "ขากลาง — ตัวปรับ"),
            P("3", "B", PinKind.Passive, PinSide.Right, 0),
        ],
    };

    /// <summary>
    /// A bare PCB relay. The coil is modelled by its DC resistance, which is what the
    /// behavioural emitter derives from Vcc/Icc; the contacts carry no model at all,
    /// because what they switch is not on this schematic.
    /// </summary>
    private static PartDefinition Relay(
        string key, string mpn, string nameTh,
        double vccMin, double vccMax, double vccTypical, double coilCurrent,
        string? note = null) => new()
    {
        Key = key,
        Prefix = "K",
        Name = mpn,
        NameTh = nameTh,
        Mpn = mpn,
        Package = "SRD / PCB relay, 5 pin",
        Symbol = SymbolShape.IcBody,
        Spice = SpiceKind.Behavioural,
        BodyWidth = 8,
        BodyHeight = 7,
        Provenance = Provenance.Unverified,
        Digital = new DigitalSpec(vccMin, vccMax, vccTypical, Icc: coilCurrent),
        NoteTh = note,
        Pins =
        [
            P("1", "A1", PinKind.Power, PinSide.Left, 0, "คอยล์ ขั้วบวก"),
            P("2", "A2", PinKind.Ground, PinSide.Left, 1, "คอยล์ ขั้วลบ"),
            P("3", "COM", PinKind.Passive, PinSide.Right, 0, "ขาร่วมของหน้าสัมผัส"),
            P("4", "NO", PinKind.Passive, PinSide.Right, 1, "ปกติเปิด"),
            P("5", "NC", PinKind.Passive, PinSide.Right, 2, "ปกติปิด"),
        ],
    };

    /// <summary>
    /// A mains transformer. There is no two-terminal element that stands in for coupled
    /// windings, so this is deliberately not simulatable — the netlist reports it as
    /// skipped instead of quietly emitting the primary and losing the secondary.
    /// </summary>
    private static PartDefinition Transformer(
        string key, string name, string nameTh, bool centreTapped, string? note = null)
    {
        List<Pin> pins =
        [
            P("1", "P1", PinKind.Passive, PinSide.Left, 0, "ปฐมภูมิ — ไฟบ้าน"),
            P("2", "P2", PinKind.Passive, PinSide.Left, 2, "ปฐมภูมิ — ไฟบ้าน"),
            P("3", "S1", PinKind.Passive, PinSide.Right, 0, "ทุติยภูมิ"),
        ];
        if (centreTapped)
            pins.Add(P("4", "CT", PinKind.Passive, PinSide.Right, 1, "ขากลางของทุติยภูมิ"));
        pins.Add(P(centreTapped ? "5" : "4", "S2", PinKind.Passive, PinSide.Right, 2, "ทุติยภูมิ"));

        return new PartDefinition
        {
            Key = key,
            Prefix = "T",
            Name = name,
            NameTh = nameTh,
            Package = "EI core, chassis mount",
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Primitive,
            BodyWidth = 8,
            BodyHeight = 7,
            Provenance = Provenance.Unverified,
            NoteTh = note,
            Params = Optional((ParamKey.Vmax, 250d)),
            Pins = pins,
        };
    }

    // ── the catalogue ────────────────────────────────────────────────────

    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
        // ── คริสตัลและตัวกำเนิดความถี่ ──────────────────────────────────
        Xtal("XTAL-16M", "16 MHz crystal", "คริสตัล 16 MHz", "16M", "HC-49S",
            tolerance: 30e-6,
            note: "ความถี่ยอดนิยมของ AVR/8051 · ต้องมีคาปาโหลดสองตัวลงกราวด์ ค่าที่ใช้ ≈ 2×(CL − Cstray) "
                + "โดย Cstray ราว 3–5 pF ดังนั้น CL 20 pF มักลงตัวที่ 22 pF · ค่า CL ต่างกันตามรุ่น (18/20/32 pF) "
                + "ใส่ผิดแล้วความถี่เพี้ยนหรือไม่แกว่งเลย · ซิมูเลเตอร์ข้ามคริสตัลไป ngspice สตาร์ตวงจร Pierce "
                + "ไม่ไหวในเวลาที่รอได้"),

        Xtal("XTAL-12M", "12 MHz crystal", "คริสตัล 12 MHz", "12M", "HC-49S",
            tolerance: 30e-6,
            note: "ใช้กับ USB ของ 8051/PIC บางเบอร์ เพราะหาร 12 MHz ลงตัวกับ 1.5/12 Mbps · "
                + "งาน UART ที่ต้องการ baud เป๊ะ ๆ ให้ใช้ 11.0592 MHz แทน"),

        Xtal("XTAL-11M0592", "11.0592 MHz crystal", "คริสตัล 11.0592 MHz", "11.0592M", "HC-49S",
            tolerance: 30e-6,
            note: "เลือกเบอร์นี้เพราะหารลงตัวกับ baud rate มาตรฐาน (9600/19200/57600/115200) ได้พอดี "
                + "ไม่มีเศษ ต่างจาก 12 MHz ที่คลาดเคลื่อนจนสื่อสารพลาดที่ baud สูง"),

        Xtal("XTAL-8M", "8 MHz crystal", "คริสตัล 8 MHz", "8M", "HC-49S",
            tolerance: 30e-6,
            note: "ตรงกับความถี่ออสซิลเลเตอร์ภายในของ AVR — ใส่คริสตัลแล้วต้องแก้ fuse ให้เลือกแหล่งคล็อกภายนอกด้วย "
                + "ไม่งั้นชิปยังใช้ตัวในอยู่ · ตั้ง fuse เป็นคล็อกภายนอกแล้วไม่มีคริสตัล ชิปจะเงียบและโปรแกรมไม่เข้า"),

        Xtal("XTAL-32K768", "32.768 kHz crystal", "คริสตัลนาฬิกา 32.768 kHz", "32.768k",
            "ทรงกระบอก 3×8 มม.", tolerance: 20e-6,
            note: "ใช้กับ RTC และโหมดประหยัดไฟ หาร 2 ครบ 15 ครั้งได้ 1 Hz พอดี · CL มีทั้ง 6, 7 และ 12.5 pF "
                + "ต้องดูให้ตรงรุ่น ผิดค่าแล้วนาฬิกาเดินเร็วหรือช้าเป็นนาทีต่อวัน · กำลังขับต่ำมาก (ระดับไมโครวัตต์) "
                + "เอาโพรบสโคปจิ้มขาตรง ๆ มันจะหยุดแกว่งทันที ให้วัดที่ขาที่ MCU กำหนดหรือดูสัญญาณเอาต์พุตแทน · "
                + "ตัวถังกระจกบอบบาง ดัดขาชิดตัวถังแล้วร้าวเงียบ ๆ"),

        new PartDefinition
        {
            Key = "RESONATOR-16M",
            Prefix = "Y",
            Name = "Ceramic resonator 16 MHz (3-pin)",
            NameTh = "เซรามิกเรโซเนเตอร์ 16 MHz ชนิด 3 ขา",
            Package = "3-pin",
            Symbol = SymbolShape.Crystal,
            Spice = SpiceKind.Primitive,
            DefaultValue = "16M",
            Unit = "Hz",
            BodyWidth = 4,
            BodyHeight = 4,
            Provenance = Provenance.Unverified,
            Params = Params((ParamKey.Tolerance, 0.005)),
            NoteTh = "ขากลางคือกราวด์ — มีคาปาโหลดอยู่ในตัวแล้ว ไม่ต้องต่อเพิ่ม ราคาถูกและลงแผ่นง่ายกว่าคริสตัล · "
                   + "ความเที่ยงราว ±0.5% ซึ่งแย่กว่าคริสตัลหลักร้อยเท่า ใช้กับ USB นาฬิกา หรืองานที่ต้องการ baud "
                   + "แม่นยำไม่ได้ · มีรุ่น 2 ขาที่ไม่มีคาปาในตัว ต้องต่อคาปาโหลดเองเหมือนคริสตัล ดูจำนวนขาก่อนซื้อ",
            Pins =
            [
                P("1", "1", PinKind.Passive, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Bottom, 0, "ขากลาง — ต่อกราวด์"),
                P("3", "3", PinKind.Passive, PinSide.Right, 0),
            ],
        },

        new PartDefinition
        {
            Key = "OSC-16M-DIP14",
            Prefix = "Y",
            Name = "16 MHz crystal oscillator (half-can)",
            NameTh = "ออสซิลเลเตอร์สำเร็จรูป 16 MHz ตัวถังโลหะ",
            Package = "DIP-14 half-can",
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Behavioural,
            BodyWidth = 7,
            BodyHeight = 5,
            Provenance = Provenance.Unverified,
            Digital = new DigitalSpec(4.5, 5.5, 5.0, Icc: 0.020, IoMax: 0.020),
            NoteTh = "เป็นวงจรออสซิลเลเตอร์ครบชุดในตัวถังเดียว ต้องจ่ายไฟและได้คลื่นสี่เหลี่ยมออกมาเลย "
                   + "ต่างจากคริสตัลที่ต้องอาศัยวงจรใน MCU · ตัวถังโลหะครึ่งซีกใช้รูแบบ DIP-14 แต่มีขาจริงแค่ 4 มุม: "
                   + "ขา 1 = enable (บางรุ่นปล่อยลอย) ขา 7 = GND ขา 8 = เอาต์พุต ขา 14 = VCC · "
                   + "กินไฟหลักสิบมิลลิแอมป์ ต้องมีคาปาบายพาส 100 nF ติดตัวถัง · "
                   + "ป้อนเข้าขา XTAL1 ของ MCU ได้ แต่ห้ามต่อขา XTAL2 ทิ้งไว้",
            Pins =
            [
                P("1", "EN", PinKind.Input, PinSide.Left, 0, "บางรุ่นเป็น NC — ถ้าเป็น enable ต้องดึงสูง"),
                P("7", "GND", PinKind.Ground, PinSide.Left, 2),
                P("8", "OUT", PinKind.Output, PinSide.Right, 2, "คลื่นสี่เหลี่ยมระดับ CMOS"),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        // ── ตัวเหนี่ยวนำและเฟอร์ไรต์บีด ─────────────────────────────────
        Coil("L-POWER", "Shielded power inductor", "ตัวเหนี่ยวนำกำลังแบบมีชีลด์", "10u",
            package: "SMD shielded drum",
            note: "ใช้ในภาคสวิตชิ่ง บั๊ก/บูสต์ · ตัวเลขที่ตัดสินว่าใช้ได้ไหมคือกระแสอิ่มตัว (Isat) ไม่ใช่ค่า µH — "
                + "เกิน Isat แล้วค่าเหนี่ยวนำตกฮวบ กระแสพุ่งเป็นยอดแหลม มอสเฟตกับไอซีพังโดยไม่มีอาการเตือน · "
                + "ต้องดูทั้ง Isat และกระแสที่ทำให้ร้อนเกิน (Irms) ผู้ผลิตให้มาคนละค่า · "
                + "แบบมีชีลด์แพงกว่าแต่ไม่แผ่สนามไปกวนวงจรข้างเคียง"),

        Coil("L-DRUM", "Unshielded drum-core inductor", "ตัวเหนี่ยวนำแกนกลมแบบเปลือย", "100u",
            package: "Radial drum",
            note: "ถูกและหาง่าย แต่แผ่สนามแม่เหล็กออกรอบตัว วางใกล้สายสัญญาณ คริสตัล หรือขา ADC แล้วมีสัญญาณรบกวนเข้ามา · "
                + "ในเลย์เอาต์ให้วางห่างและอย่าเดินสายสัญญาณลอดใต้ตัวมัน"),

        Coil("L-AXIAL", "Axial RF choke", "โช้กแกนเฟอร์ไรต์ขาตรงแนวแกน", "100u",
            package: "Axial",
            note: "หน้าตาเหมือนตัวต้านทานและใช้แถบสีเหมือนกัน แต่หน่วยเป็น µH — หยิบสลับกันบ่อยมาก "
                + "สังเกตว่าตัวถังอ้วนกว่าและปลายมนกว่า · รับกระแสได้น้อย ใช้กรองสัญญาณและ RF "
                + "ไม่ใช่ของสำหรับภาคจ่ายไฟกระแสสูง"),

        Coil("FB-600", "Ferrite bead 600 Ω @ 100 MHz", "เฟอร์ไรต์บีด 600 Ω ที่ 100 MHz (สายสัญญาณ)", "1u",
            package: "0603 / 0805",
            note: "บีดไม่ใช่ตัวเหนี่ยวนำ — มันคือตัวต้านทานที่โผล่มาเฉพาะย่านความถี่สูง ส่วนที่ DC แทบเป็นเส้นลวด · "
                + "ตัวเลข 600 Ω วัดที่ 100 MHz จุดเดียว ที่ความถี่อื่นได้ไม่เท่านี้ · เอาไปคั่นหน้าคาปาเซรามิกในภาคจ่ายไฟ "
                + "จะเกิดวงจรกำทอน ทำให้แรงดันแกว่งแรงกว่าตอนไม่ใส่ ต้องมีตัวหน่วง (คาปา ESR สูงหรือ RC) ควบด้วย · "
                + "ในซิมนี้แทนด้วยตัวเหนี่ยวนำ 1 µH ซึ่งไม่ตรงกับพฤติกรรมจริงที่เป็นความสูญเสีย"),

        Coil("FB-120-2A", "Ferrite bead 120 Ω @ 100 MHz, power line", "เฟอร์ไรต์บีดสำหรับสายไฟเลี้ยง 120 Ω ที่ 100 MHz", "200n",
            package: "0805 / 1206",
            note: "รุ่นสำหรับสายไฟเลี้ยง ความต้านทาน DC ต่ำเพื่อไม่ให้แรงดันตก · อิมพีแดนซ์ที่ผู้ผลิตโฆษณาวัดตอนไม่มีกระแส DC ไหล "
                + "พอมีกระแสจริงแกนอิ่มตัวและค่าจะเหลือน้อยกว่าที่พิมพ์มาก ดูกราฟ derating ก่อนเชื่อตัวเลข · "
                + "อย่าเอาบีดสายสัญญาณตัวเล็กมาใช้แทน มันร้อนและขาด"),

        // ── ตัวเก็บประจุ ────────────────────────────────────────────────
        Cap("C-CER-C0G-50V", "Ceramic C0G/NP0 50 V", "คาปาเซรามิก C0G/NP0 50 V", "100p", polarised: false,
            package: "0603 / 0805 / disc", vmax: 50, tolerance: 0.05,
            note: "เซรามิกชั้นดีที่สุด ค่านิ่งทั้งตามอุณหภูมิและตามแรงดันคร่อม จึงเป็นตัวเดียวในกลุ่มเซรามิกที่ใช้กับ "
                + "วงจรจับเวลา ออสซิลเลเตอร์ และฟิลเตอร์ได้ · ค่าเกิน 10 nF หายากและแพง เกินกว่านั้นต้องข้ามไปใช้ฟิล์ม"),

        Cap("C-CER-X7R-50V", "Ceramic X7R 50 V", "คาปาเซรามิก X7R 50 V", "100n", polarised: false,
            package: "0603 / 0805 / disc", vmax: 50, tolerance: 0.10,
            note: "ตัวหลักสำหรับบายพาสไฟเลี้ยง วางให้ชิดขา VCC ของไอซีที่สุด · ค่าจริงตกลงเมื่อมีแรงดันคร่อม (DC bias) "
                + "ยิ่งตัวถังเล็กและแรงดันทนต่ำยิ่งตกแรง เผื่อค่าไว้เสมอถ้าจุดนั้นสำคัญ · ไม่เหมาะกับวงจรจับเวลา "
                + "และมีอาการเสียงร้อง (ไมโครโฟนิก) เมื่อแรงดันคร่อมเปลี่ยนเร็ว"),

        Cap("C-CER-Y5V-50V", "Ceramic Y5V/Z5U 50 V", "คาปาเซรามิก Y5V/Z5U 50 V", "100n", polarised: false,
            package: "0603 / 0805 / disc", vmax: 50,
            note: "ถูกที่สุดและแย่ที่สุดในกลุ่ม ค่าคลาด −20/+80% และตกได้หลายสิบเปอร์เซ็นต์ที่ปลายช่วงอุณหภูมิ · "
                + "ใช้ได้แค่บายพาสหยาบ ๆ ห้ามใช้ที่ต้องการค่าแน่นอน · ของถูกในถาดคละมักเป็นเกรดนี้ทั้งที่พิมพ์ 104 "
                + "เหมือน X7R ทุกประการ ดูที่ผู้ขายระบุเกรด ไม่ใช่ที่ตัวเลขบนตัวถัง"),

        Cap("C-CER-DISC-1KV", "Ceramic disc 1 kV", "คาปาเซรามิกจานไฟสูง 1 kV", "1n", polarised: false,
            package: "Radial disc", vmax: 1000,
            note: "ใช้ในสนับเบอร์ วงจรจุดระเบิด และภาคไฟสูง · แรงดันที่พิมพ์เป็น DC — ห้ามเอาไปคร่อมไฟบ้าน AC "
                + "ต้องใช้ตัวที่รับรองความปลอดภัยคลาส X หรือ Y เท่านั้น · ค่าคลาดกว้าง (มัก ±20% หรือมากกว่า) "
                + "และเปลี่ยนตามอุณหภูมิมาก"),

        Cap("C-FILM-MKT-63V", "Polyester film (MKT) 63 V", "คาปาฟิล์มโพลีเอสเตอร์ (MKT) 63 V", "100n", polarised: false,
            package: "Radial box", vmax: 63, tolerance: 0.10,
            note: "ค่านิ่ง ไม่มีอาการค่าตกตามแรงดันแบบเซรามิก จึงเป็นตัวเลือกมาตรฐานของวงจรเสียง ฟิลเตอร์ และจับเวลา · "
                + "ตัวใหญ่กว่าเซรามิกมากที่ค่าเท่ากัน · มีทั้งรุ่น ±5% และ ±10% ถ้าวงจรต้องการค่าแม่นต้องระบุตอนซื้อ"),

        Cap("C-FILM-MKT-250V", "Polyester film (MKT) 250 V", "คาปาฟิล์มโพลีเอสเตอร์ (MKT) 250 V", "100n", polarised: false,
            package: "Radial box", vmax: 250, tolerance: 0.10,
            note: "แรงดันที่พิมพ์เป็น DC ค่ากับไฟ AC ต่ำกว่านั้นมาก (มักเหลือราวหนึ่งในสาม) · "
                + "จะคร่อมสายไฟบ้านต้องใช้คลาส X2 เท่านั้น ตัวนี้ใช้ไม่ได้แม้ตัวเลขจะดูพอ"),

        Cap("C-FILM-MKP-630V", "Polypropylene film (MKP) 630 V", "คาปาฟิล์มโพลีโพรพิลีน (MKP) 630 V", "100n", polarised: false,
            package: "Radial box", vmax: 630, tolerance: 0.05,
            note: "สูญเสียต่ำและทน dV/dt สูง ใช้ทำสนับเบอร์ ภาคเอาต์พุตคลาส D ครอสโอเวอร์ลำโพง และวงจรเรโซแนนซ์ · "
                + "แพงและตัวใหญ่กว่า MKT อย่าเอาไปใส่แทนในจุดที่ MKT พอ · ในวงจรสนับเบอร์ห้ามใช้อิเล็กโทรไลต์แทนเด็ดขาด"),

        Cap("C-FILM-X2-275VAC", "Class X2 film 275 VAC", "คาปาฟิล์มคลาส X2 275 VAC (คร่อมไฟบ้าน)", "100n", polarised: false,
            package: "Radial box", vmax: 275,
            note: "แรงดัน 275 เป็นค่า AC ต่อเนื่อง ไม่ใช่ DC · เฉพาะคลาส X2 เท่านั้นที่ต่อคร่อมสายไฟบ้าน L–N ได้ "
                + "เพราะออกแบบให้เสียแบบเปิดวงจร ไม่ลัดวงจร ห้ามใช้คาปาธรรมดาที่พิมพ์ 275 V แทนเด็ดขาด · "
                + "ต้องมีตัวต้านทานคายประจุ 1–2 MΩ คร่อมไว้ ไม่งั้นถอดปลั๊กแล้วยังมีไฟค้างที่ขาปลั๊กจนดูดได้ · "
                + "เสื่อมตามอายุ ในอะแดปเตอร์และภาคจ่ายไฟเก่ามักเป็นตัวต้นเหตุของฟิวส์ขาด"),

        Cap("C-ELEC-16V", "Electrolytic 16 V", "คาปาอิเล็กโทรไลต์ 16 V", "470u", polarised: true,
            package: "Radial", vmax: 16, tolerance: 0.20,
            note: "มีขั้ว ใส่กลับแล้วบวมและระเบิด แถบบนตัวถังคือขั้วลบ ขายาวคือบวก · เผื่อแรงดันอย่างน้อย 1.5 เท่าของไฟที่ใช้จริง "
                + "ราง 12 V ควรขยับไปใช้ตัว 25 V ไม่ใช่ 16 V · ค่าคลาด ±20% เป็นเรื่องปกติของกลุ่มนี้"),

        Cap("C-ELEC-25V", "Electrolytic 25 V", "คาปาอิเล็กโทรไลต์ 25 V", "220u", polarised: true,
            package: "Radial", vmax: 25, tolerance: 0.20,
            note: "ขนาดที่พอดีกับราง 12 V · ตัวเก็บประจุชนิดนี้แห้งเมื่อใช้ไปนาน ๆ ค่าตกและ ESR สูงขึ้น "
                + "อาการทั่วไปของเครื่องเก่าคือไฟกระเพื่อมและรีสตาร์ตเอง เปลี่ยนคาปาก่อนไล่จับที่อื่น"),

        Cap("C-ELEC-50V", "Electrolytic 50 V", "คาปาอิเล็กโทรไลต์ 50 V", "100u", polarised: true,
            package: "Radial", vmax: 50, tolerance: 0.20,
            note: "ใช้หลังบริดจ์ของหม้อแปลง 24 V AC ซึ่งแรงดันยอดสูงกว่าที่วัดได้ตอนมีโหลดมาก · "
                + "ตอนไม่มีโหลดแรงดันหลังบริดจ์จะพุ่งขึ้นอีก ให้เลือกจากค่ายอดสูงสุด ไม่ใช่ค่าที่วัดตอนใช้งาน"),

        Cap("C-ELEC-450V", "Electrolytic 450 V (bulk)", "คาปาอิเล็กโทรไลต์ 450 V (ตัวเก็บไฟหลัก)", "100u", polarised: true,
            package: "Radial / snap-in", vmax: 450, tolerance: 0.20,
            note: "⚠ ตัวเก็บไฟหลักหลังบริดจ์ของภาคจ่ายไฟสวิตชิ่งไฟบ้าน เก็บประจุค้างไว้เป็นนาทีหลังถอดปลั๊ก "
                + "แตะแล้วถึงตายได้ — วัดแรงดันคร่อมก่อนจับทุกครั้ง และคายประจุผ่านตัวต้านทานหลักกิโลโอห์ม "
                + "ห้ามใช้ไขควงลัดขาเพราะกระแสพุ่งทำลายตัวมันเองและสะเก็ดกระเด็น · "
                + "เปลี่ยนแล้วต้องใส่ขั้วให้ถูก ตัวนี้ระเบิดแรงกว่าตัวเล็กมาก"),

        Cap("C-ELEC-LOWESR-25V", "Low-ESR electrolytic 25 V, 105 °C", "คาปาอิเล็กโทรไลต์ ESR ต่ำ 25 V 105 °C", "470u", polarised: true,
            package: "Radial", vmax: 25, tolerance: 0.20,
            note: "สำหรับภาคสวิตชิ่งที่มีกระแสกระเพื่อมสูง · เอาตัวธรรมดา 85 °C มาใส่แทนแล้วจะร้อน บวม และพังภายในไม่กี่เดือน "
                + "แม้ค่าและแรงดันจะเท่ากันทุกอย่าง · อายุใช้งานลดลงครึ่งหนึ่งทุก ๆ 10 °C ที่ร้อนขึ้น "
                + "ดังนั้นตำแหน่งที่วางบนบอร์ดสำคัญพอ ๆ กับสเปก"),

        Cap("C-TANT-16V", "Tantalum 16 V", "คาปาแทนทาลัม 16 V", "10u", polarised: true,
            package: "SMD case A–D", vmax: 16, tolerance: 0.10,
            note: "มีขั้ว และแถบบนตัวถังคือขั้วบวก ซึ่งตรงข้ามกับอิเล็กโทรไลต์อะลูมิเนียมที่แถบคือลบ — จุดที่สลับกันบ่อยที่สุด · "
                + "เสียแบบลัดวงจรและลุกไหม้ ไม่ใช่แค่หยุดทำงาน จึงต้องลดแรงดันใช้งานเหลือราวครึ่งหนึ่งของค่าที่พิมพ์ · "
                + "ห้ามใช้ตรงจุดที่มีกระแสพุ่งสูง เช่น ต่อตรงเข้าเอาต์พุตแหล่งจ่ายที่ไม่มีตัวจำกัดกระแส"),

        Cap("C-POLYMER-6V3", "Aluminium polymer 6.3 V", "คาปาพอลิเมอร์อะลูมิเนียม 6.3 V", "220u", polarised: true,
            package: "SMD / radial can", vmax: 6.3, tolerance: 0.20,
            note: "ESR ต่ำมากและไม่แห้งแบบอิเล็กโทรไลต์ธรรมดา ใช้ที่ภาคจ่ายไฟ CPU และรางแรงดันต่ำกระแสสูง · "
                + "แรงดันทนต่ำ ต้องเผื่อให้มาก ราง 5 V ต้องใช้ตัว 10 V ขึ้นไป · แพงกว่าหลายเท่า "
                + "ใช้เฉพาะจุดที่ ESR สำคัญจริง ๆ"),

        // ── ตัวต้านทานปรับค่าแบบตั้งครั้งเดียว ─────────────────────────
        Trimmer("RV-3296W", "Trimmer 3296W (25-turn cermet)", "ตัวต้านทานปรับละเอียด 25 รอบ 3296W",
            "3296W", pmax: 0.5, tolerance: 0.10,
            note: "หมุน 25 รอบเต็มช่วง ปรับได้ละเอียดกว่าแบบรอบเดียวมาก · ขา 2 คือขากลาง (ตัวปรับ) เสมอ · "
                + "หมุนจนสุดแล้วยังหมุนต่อได้พร้อมเสียงคลิก นั่นคือคลัตช์กันพัง ไม่ใช่ของเสีย · "
                + "รหัสท้ายบอกทิศทางปรับและระยะขา (W ปรับด้านข้าง, X/Y/Z ต่างกันไป) ลงแผ่นปรินต์ผิดรุ่นแล้วหมุนไม่ได้ "
                + "หรือขาไม่ลงรู · ของถูกสีน้ำเงินในตลาดคือของเลียนแบบเบอร์นี้ ค่าเพี้ยนกว่าและสึกเร็วกว่า"),

        Trimmer("RV-3386P", "Trimmer 3386P (single-turn cermet)", "ตัวต้านทานปรับค่ารอบเดียว 3386P",
            "3386P", pmax: 0.5, tolerance: 0.10,
            note: "เซอร์เมทรอบเดียว ปรับเร็วแต่ละเอียดน้อยกว่า 3296 · ขา 2 คือขากลาง · "
                + "ตระกูล 3386 มีรหัสท้ายหลายแบบที่ต่างกันทั้งทิศทางปรับและระยะขา ต้องดูสเปกก่อนออกแบบแผ่นปรินต์"),

        Trimmer("RV-RM065", "Trimmer RM065 (single-turn carbon)", "ตัวต้านทานปรับค่าตัวเล็กสีน้ำเงิน RM065",
            "RM065",
            note: "ตัวปรับราคาถูกที่มาในชุดคละ · เป็นคาร์บอน ไม่ใช่เซอร์เมท ค่าเลื่อนตามอุณหภูมิและสึกเร็ว "
                + "เหมาะกับงานตั้งครั้งเดียวแล้วไม่แตะอีก ไม่เหมาะกับจุดปรับที่ต้องหมุนบ่อย · "
                + "หมุนได้ราวสามในสี่รอบเท่านั้น ฝืนต่อแล้วหน้าสัมผัสข้างในขาด · "
                + "ผู้ขายส่วนใหญ่ระบุกำลังราว 0.1 W ซึ่งต่ำมาก อย่าเอาไปรับกระแส"),

        // ── ฟิวส์และอุปกรณ์ป้องกัน ──────────────────────────────────────
        Resistive("F-GLASS-5X20-F", "F", "Glass fuse 5×20 mm, fast (F)", "ฟิวส์แก้ว 5×20 มม. ชนิดขาดเร็ว (F)",
            "50m", package: "5×20 mm", vmax: 250,
            note: "ชนิด F ขาดเร็ว ใช้กับวงจรอิเล็กทรอนิกส์ที่ไม่มีกระแสพุ่งตอนเปิดเครื่อง · "
                + "⚠ ห้ามใส่ค่ากระแสสูงกว่าเดิมเพื่อให้เลิกขาด — ฟิวส์ขาดคืออาการ ไม่ใช่สาเหตุ ต้องหาต้นเหตุก่อน · "
                + "แรงดัน 250 V ที่ระบุคือค่าที่ฟิวส์ตัดอาร์กได้ ใช้ฟิวส์ไฟบ้านกับวงจร DC แรงดันสูงไม่ได้ · "
                + "ค่าที่แก้ได้ในซิมคือความต้านทานตอนปกติ ใส่ 1G เพื่อจำลองสภาพขาด ตัวจำลองไม่ตัดวงจรเองเมื่อกระแสเกิน"),

        Resistive("F-GLASS-5X20-T", "F", "Glass fuse 5×20 mm, slow (T)", "ฟิวส์แก้ว 5×20 มม. ชนิดหน่วงเวลา (T)",
            "80m", package: "5×20 mm", vmax: 250,
            note: "ชนิด T หน่วงเวลา ทนกระแสพุ่งตอนเปิดเครื่องของหม้อแปลง มอเตอร์ และภาคสวิตชิ่งได้ · "
                + "เอา F ไปใส่แทน T แล้วขาดทุกครั้งที่เปิดเครื่อง เอา T ไปใส่แทน F แล้วอุปกรณ์ไหม้ก่อนฟิวส์ขาด — "
                + "ตัวอักษรบนฝาโลหะสำคัญพอ ๆ กับตัวเลขกระแส · ค่าที่แก้ได้ในซิมคือความต้านทานตอนปกติ"),

        Resistive("F-BLADE-ATO", "F", "Blade fuse (ATO), 32 V", "ฟิวส์ก้ามปูรถยนต์ (ATO) 32 V",
            "5m", package: "ATO blade", vmax: 32,
            note: "ระบบไฟรถยนต์ 12 V · สีบอกกระแส: 5A น้ำตาลอ่อน 7.5A น้ำตาล 10A แดง 15A น้ำเงิน 20A เหลือง "
                + "25A ใส 30A เขียว · แรงดันแค่ 32 V ห้ามเอาไปใช้กับไฟบ้าน · "
                + "รถบางรุ่นใช้ขนาด mini/micro ที่เสียบแทนกันไม่ได้ ดูขนาดก่อนซื้อ"),

        Resistive("F-PTC-RESET", "F", "Resettable fuse (PTC polyfuse)", "ฟิวส์รีเซ็ตได้ ชนิด PTC",
            "100m", package: "Radial",
            note: "กระแสที่พิมพ์คือกระแสที่ทนได้ตลอด (hold) ส่วนกระแสที่ทำให้ตัดจริงราวสองเท่า (trip) และใช้เวลาเป็นวินาที "
                + "จึงไม่ได้ปกป้องอุปกรณ์ที่พังเร็วกว่านั้น มันกันไฟไหม้สายไฟมากกว่ากันไอซีพัง · "
                + "เบอร์อย่าง 60R110 อ่านว่า 60 V, hold 1.10 A · หลังตัดต้องตัดไฟออกก่อนจึงจะกลับสภาพ "
                + "และความต้านทานจะสูงขึ้นเล็กน้อยทุกครั้งที่ตัด · พิกัดแรงดันต่างกันมากในแต่ละรุ่น (ตั้งแต่ราว 6 V ถึงหลายสิบโวลต์) "
                + "ต้องดูสเปกของเบอร์ที่ใช้จริง"),

        Resistive("MOV-14D471K", "RV", "MOV 14D471K", "วาริสเตอร์กันไฟกระชาก 14D471K (ไฟบ้าน 220 V)",
            "1G", package: "Radial disc 14 mm", vmax: 300,
            note: "อ่านเบอร์: 14 = เส้นผ่านศูนย์กลาง 14 มม., 471 = แรงดันวาริสเตอร์ 470 V ซึ่งเป็นเบอร์ที่ใช้กับไฟบ้าน 220 V · "
                + "ค่า 300 V ที่แสดงคือแรงดัน AC ต่อเนื่องที่ทนได้ ไม่ใช่แรงดันที่มันเริ่มทำงาน · "
                + "⚠ ต้องมีฟิวส์อนุกรมอยู่ก่อนเสมอ เพราะเมื่อเสื่อมมันจะลัดวงจรและลุกไหม้ · "
                + "เสื่อมลงทุกครั้งที่รับไฟกระชาก ตัวที่ดำ แตก หรือพองต้องเปลี่ยน · "
                + "ในซิมเป็นแค่ตัวต้านทานค่าสูง ไม่ได้จำลองการตัดยอดแรงดัน"),

        // ── เทอร์มิสเตอร์ ───────────────────────────────────────────────
        Resistive("NTC-10K-B3950", "RT", "NTC 10k B3950", "เทอร์มิสเตอร์ NTC 10k B3950",
            "10k", package: "Glass bead / probe",
            note: "10k คือความต้านทานที่ 25 °C ความต้านทานลดลงเมื่อร้อนขึ้น · ค่า B (3950) คือรูปของเส้นโค้ง "
                + "ต้องเลือกตารางในเฟิร์มแวร์ให้ตรงกัน ใช้ตาราง B3435 กับตัว B3950 อ่านคลาดได้หลายสิบองศาที่ปลายช่วง · "
                + "ต้องต่อเป็นตัวแบ่งแรงดันกับตัวต้านทานคงที่ (นิยม 4.7k) จึงจะอ่านด้วย ADC ได้ · "
                + "กระแสที่ไหลผ่านทำให้ตัวมันร้อนเอง อย่าใช้ตัวต้านทานแบ่งค่าต่ำเกินไป อุณหภูมิที่อ่านได้จะสูงกว่าจริง"),

        Resistive("NTC-100K-B3950", "RT", "NTC 100k B3950", "เทอร์มิสเตอร์ NTC 100k B3950",
            "100k", package: "Glass bead / cartridge",
            note: "ใช้กับหัวฉีดเครื่องพิมพ์ 3 มิติเป็นหลัก เพราะที่อุณหภูมิสูงยังเหลือความต้านทานมากพอให้อ่านได้ · "
                + "ตัวแบ่งมาตรฐานของบอร์ดพิมพ์คือ 4.7k · สายขาดหรือหลุดจะอ่านได้เป็นอุณหภูมิต่ำผิดปกติ "
                + "เฟิร์มแวร์ที่ไม่มีตัวตรวจจับจะสั่งฮีตเตอร์ทำงานค้าง"),

        Resistive("NTC-10K-B3435", "RT", "NTC 10k B3435", "เทอร์มิสเตอร์ NTC 10k B3435",
            "10k", package: "Glass bead / probe",
            note: "ค่าที่ 25 °C เท่ากับ B3950 ทุกประการ แต่เส้นโค้งต่างกัน — สลับกันแล้วค่าเพี้ยนมากที่อุณหภูมิสูงหรือต่ำ "
                + "และวัดที่อุณหภูมิห้องจะดูปกติดี จึงจับผิดยาก · พบมากในเครื่องปรับอากาศ ตู้เย็น และหัววัดอุตสาหกรรม"),

        Resistive("NTC-INRUSH-5D9", "RT", "Inrush limiter NTC 5D-9", "เอ็นทีซีจำกัดกระแสพุ่ง 5D-9",
            "5", package: "Radial disc 9 mm",
            note: "อ่านเบอร์: 5 Ω ที่ 25 °C ตัวถังโต 9 มม. · ต่ออนุกรมที่ขาไลน์เพื่อกันกระแสพุ่งตอนเปิดเครื่อง "
                + "พอร้อนแล้วความต้านทานเหลือน้อยมาก · ร้อนจัดตอนทำงาน อย่าวางชิดสายไฟ พลาสติก หรือคาปา · "
                + "ปิดแล้วเปิดใหม่ทันทีมันยังร้อนอยู่ จึงไม่ได้กันกระแสพุ่งรอบนั้น · "
                + "พิกัดกระแสต่างกันตามผู้ผลิตแม้เบอร์ขึ้นต้นเหมือนกัน ต้องดูสเปกก่อนใช้"),

        // ── คอยล์รีเลย์และหม้อแปลง ──────────────────────────────────────
        Relay("SRD-05VDC-SL-C", "SRD-05VDC-SL-C", "รีเลย์ 5V 1 คอนแทกต์ (SRD-05VDC-SL-C)",
            vccMin: 4.5, vccMax: 5.5, vccTypical: 5.0, coilCurrent: 0.0714,
            note: "⚠ ต้องมีไดโอด (เช่น 1N4007) คร่อมคอยล์กลับขั้วเสมอ ไม่งั้นแรงดันย้อนตอนตัดจะพังทรานซิสเตอร์ที่ขับ · "
                + "คอยล์ราว 70 Ω กินราว 70 mA ที่ 5 V ขา MCU ขับตรงไม่ได้ ต้องผ่านทรานซิสเตอร์หรือมอสเฟต · "
                + "หน้าสัมผัสที่ระบุ 10 A 250 VAC เป็นค่ากับโหลดตัวต้านทาน โหลดมอเตอร์ หลอดไส้ หรือสวิตชิ่ง "
                + "ต้องลดลงมามาก · เว้นระยะบนแผ่นปรินต์ระหว่างฝั่งไฟบ้านกับฝั่งลอจิกให้พอ · "
                + "ในซิมจำลองเฉพาะคอยล์เป็นความต้านทาน ส่วนหน้าสัมผัสไม่มีแบบจำลอง"),

        Relay("SRD-12VDC-SL-C", "SRD-12VDC-SL-C", "รีเลย์ 12V 1 คอนแทกต์ (SRD-12VDC-SL-C)",
            vccMin: 10.8, vccMax: 13.2, vccTypical: 12.0, coilCurrent: 0.030,
            note: "คอยล์ราว 400 Ω กินราว 30 mA ที่ 12 V ซึ่งน้อยกว่ารุ่น 5 V ครึ่งหนึ่ง ถ้ามีไฟ 12 V อยู่แล้วให้เลือกรุ่นนี้ · "
                + "ต้องมีไดโอดคร่อมคอยล์เหมือนกัน · แรงดันดูดต่ำสุดราว 75% ของ 12 V ไฟตกกว่านั้นแล้วจะดูดไม่ติด "
                + "หรือหน้าสัมผัสสั่นจนไหม้ · โมดูลรีเลย์สำเร็จรูปส่วนใหญ่ใช้รุ่น 5 V อย่าสลับกันโดยดูแค่หน้าตา"),

        Transformer("T-EI-220-12", "Mains transformer 220 V → 12 V, 0.5 A",
            "หม้อแปลงไฟบ้าน 220V → 12V 0.5A แกน EI", centreTapped: false,
            note: "⚠ ฝั่ง 220 V อันตรายถึงชีวิต ต้องมีฟิวส์ที่ขาไลน์ และเว้นระยะกับฝั่งแรงดันต่ำให้พอ · "
                + "แรงดันที่ระบุคือค่าตอนจ่ายโหลดเต็มพิกัด ตอนไม่มีโหลดจะสูงกว่านั้นราว 10–20% · "
                + "หลังบริดจ์และคาปา แรงดัน DC จะขึ้นไปราว √2 เท่าของค่า AC — 12 V AC ได้ราว 17 V DC "
                + "จึงต้องใช้คาปา 25 V ไม่ใช่ 16 V · ซิมูเลเตอร์ไม่จำลองหม้อแปลง ให้ใช้แหล่งจ่ายไฟแทนตอนทดสอบฝั่งทุติยภูมิ"),

        Transformer("T-EI-220-9-0-9", "Mains transformer 220 V → 9-0-9 V, 1 A (centre-tapped)",
            "หม้อแปลงไฟบ้าน 220V → 9-0-9V 1A มีขากลาง", centreTapped: true,
            note: "ฝั่งทุติยภูมิมีขากลาง (CT) ใช้ทำไฟบวก-ลบ หรือเรียงกระแสแบบสองไดโอด · "
                + "วัดจากปลายถึงปลายได้ 18 V AC ไม่ใช่ 9 V — ต่อผิดจุดแล้วแรงดันเกินไปเท่าตัวและคาปาระเบิด · "
                + "⚠ ฝั่ง 220 V อันตรายถึงชีวิต ต้องมีฟิวส์ที่ขาไลน์ · "
                + "ซิมูเลเตอร์ไม่จำลองหม้อแปลง ให้ใช้แหล่งจ่ายไฟสองชุดแทน"),
    ];
}
