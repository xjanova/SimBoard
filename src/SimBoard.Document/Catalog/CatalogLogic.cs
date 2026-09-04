namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// ลอจิกเกตและไอซีดิจิทัล.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
///
/// Pin numbers here are the DIP pin numbers, laid out the way the package physically is:
/// pins 1..n/2 down the left side, the rest back up the right. That is what someone
/// counting legs on a breadboard is looking at, and counting from the wrong end is the
/// single most expensive mistake in this file.
///
/// The 74HC entries carry the family envelope (V_CC 2–6 V, V_IH 0.7·V_CC, V_IL 0.3·V_CC,
/// 4 mA guaranteed drive) rather than one vendor's exact numbers, because the same part
/// number ships from Nexperia, TI and Toshiba with figures that differ in the third digit.
/// I_CC is the quiescent maximum: in a switching circuit the dynamic current is many times
/// larger and depends entirely on the clock rate and load capacitance.
/// </summary>
public static class CatalogLogic
{
    /// <summary>74HC SSI: quad gates and hex inverters. Same envelope across the family.</summary>
    private static DigitalSpec Hc(double icc) =>
        new(VccMin: 2.0, VccMax: 6.0, VccTypical: 5.0, Icc: icc,
            Vih: 3.15, Vil: 1.35, IoMax: 0.004);

    /// <summary>CD4000B: the wide-supply CMOS family, 3–18 V.</summary>
    private static DigitalSpec Cd4000(double icc, double ioMax) =>
        new(VccMin: 3.0, VccMax: 18.0, VccTypical: 5.0, Icc: icc,
            Vih: 3.5, Vil: 1.5, IoMax: ioMax);

    private const string FloatingInputs =
        "ขาอินพุตที่ไม่ใช้ห้ามปล่อยลอย — อินพุต CMOS ที่ลอยจะแกว่งเอง ทำให้เอาต์พุตมั่วและกินไฟเพิ่ม "
      + "ผูกลง GND หรือขึ้น VCC ให้หมด · ต้องมี C 100n คร่อม VCC-GND ชิดตัวไอซี";

    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
        // ── 74HC gates, DIP-14 ───────────────────────────────────────────

        new()
        {
            Key = "74HC00", Prefix = "U", Name = "74HC00", NameTh = "ไอซีเกต NAND 2 อินพุต 4 ชุด",
            Mpn = "74HC00", Package = "DIP-14",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 8,
            Digital = Hc(20e-6),
            NoteTh = FloatingInputs
                   + " · 74HC00 / 74HC08 / 74HC32 / 74HC86 ใช้ลำดับขาชุดเดียวกัน (A B Y) สลับตัวกันได้ทันที "
                   + "แต่ 74HC02 ไม่ใช่ — ดูโน้ตของ '02",
            Pins =
            [
                P("1", "1A", PinKind.Input, PinSide.Left, 0),
                P("2", "1B", PinKind.Input, PinSide.Left, 1),
                P("3", "1Y", PinKind.Output, PinSide.Left, 2),
                P("4", "2A", PinKind.Input, PinSide.Left, 3),
                P("5", "2B", PinKind.Input, PinSide.Left, 4),
                P("6", "2Y", PinKind.Output, PinSide.Left, 5),
                P("7", "GND", PinKind.Ground, PinSide.Left, 6),
                P("8", "3Y", PinKind.Output, PinSide.Right, 6),
                P("9", "3A", PinKind.Input, PinSide.Right, 5),
                P("10", "3B", PinKind.Input, PinSide.Right, 4),
                P("11", "4Y", PinKind.Output, PinSide.Right, 3),
                P("12", "4A", PinKind.Input, PinSide.Right, 2),
                P("13", "4B", PinKind.Input, PinSide.Right, 1),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC02", Prefix = "U", Name = "74HC02", NameTh = "ไอซีเกต NOR 2 อินพุต 4 ชุด",
            Mpn = "74HC02", Package = "DIP-14",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 8,
            Digital = Hc(20e-6),
            NoteTh = "⚠ ลำดับขาไม่เหมือน '00/'08/'32/'86 — ของ '02 เอาต์พุตมาก่อน (ขา 1 = 1Y, ขา 2-3 = อินพุต) "
                   + "เปลี่ยนไอซีบนบอร์ดเดิมแล้วลืมย้ายสายคือที่พลาดกันบ่อยที่สุดในตระกูลนี้ · " + FloatingInputs,
            Pins =
            [
                P("1", "1Y", PinKind.Output, PinSide.Left, 0),
                P("2", "1A", PinKind.Input, PinSide.Left, 1),
                P("3", "1B", PinKind.Input, PinSide.Left, 2),
                P("4", "2Y", PinKind.Output, PinSide.Left, 3),
                P("5", "2A", PinKind.Input, PinSide.Left, 4),
                P("6", "2B", PinKind.Input, PinSide.Left, 5),
                P("7", "GND", PinKind.Ground, PinSide.Left, 6),
                P("8", "3A", PinKind.Input, PinSide.Right, 6),
                P("9", "3B", PinKind.Input, PinSide.Right, 5),
                P("10", "3Y", PinKind.Output, PinSide.Right, 4),
                P("11", "4A", PinKind.Input, PinSide.Right, 3),
                P("12", "4B", PinKind.Input, PinSide.Right, 2),
                P("13", "4Y", PinKind.Output, PinSide.Right, 1),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC04", Prefix = "U", Name = "74HC04", NameTh = "ไอซีอินเวอร์เตอร์ 6 ชุด",
            Mpn = "74HC04", Package = "DIP-14",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 8,
            Digital = Hc(20e-6),
            NoteTh = "ไม่มีฮิสเทอรีซิส — ถ้าสัญญาณเข้าขอบช้า (จาก RC, เซนเซอร์, สายยาว) เอาต์พุตจะสั่นเป็นชุด "
                   + "งานแบบนั้นต้องใช้ 74HC14 แทน · ห้ามใช้ทำออสซิลเลเตอร์แบบ RC ด้วยเหตุผลเดียวกัน · " + FloatingInputs,
            Pins =
            [
                P("1", "1A", PinKind.Input, PinSide.Left, 0),
                P("2", "1Y", PinKind.Output, PinSide.Left, 1),
                P("3", "2A", PinKind.Input, PinSide.Left, 2),
                P("4", "2Y", PinKind.Output, PinSide.Left, 3),
                P("5", "3A", PinKind.Input, PinSide.Left, 4),
                P("6", "3Y", PinKind.Output, PinSide.Left, 5),
                P("7", "GND", PinKind.Ground, PinSide.Left, 6),
                P("8", "4Y", PinKind.Output, PinSide.Right, 6),
                P("9", "4A", PinKind.Input, PinSide.Right, 5),
                P("10", "5Y", PinKind.Output, PinSide.Right, 4),
                P("11", "5A", PinKind.Input, PinSide.Right, 3),
                P("12", "6Y", PinKind.Output, PinSide.Right, 2),
                P("13", "6A", PinKind.Input, PinSide.Right, 1),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC08", Prefix = "U", Name = "74HC08", NameTh = "ไอซีเกต AND 2 อินพุต 4 ชุด",
            Mpn = "74HC08", Package = "DIP-14",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 8,
            Digital = Hc(20e-6),
            NoteTh = "ลำดับขาเหมือน 74HC00/'32/'86 ทุกประการ ต่างกันแค่ฟังก์ชัน · " + FloatingInputs,
            Pins =
            [
                P("1", "1A", PinKind.Input, PinSide.Left, 0),
                P("2", "1B", PinKind.Input, PinSide.Left, 1),
                P("3", "1Y", PinKind.Output, PinSide.Left, 2),
                P("4", "2A", PinKind.Input, PinSide.Left, 3),
                P("5", "2B", PinKind.Input, PinSide.Left, 4),
                P("6", "2Y", PinKind.Output, PinSide.Left, 5),
                P("7", "GND", PinKind.Ground, PinSide.Left, 6),
                P("8", "3Y", PinKind.Output, PinSide.Right, 6),
                P("9", "3A", PinKind.Input, PinSide.Right, 5),
                P("10", "3B", PinKind.Input, PinSide.Right, 4),
                P("11", "4Y", PinKind.Output, PinSide.Right, 3),
                P("12", "4A", PinKind.Input, PinSide.Right, 2),
                P("13", "4B", PinKind.Input, PinSide.Right, 1),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC14", Prefix = "U", Name = "74HC14", NameTh = "ไอซีอินเวอร์เตอร์ชมิตต์ทริกเกอร์ 6 ชุด",
            Mpn = "74HC14", Package = "DIP-14",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 8,
            // Schmitt levels, not the plain-HC 0.7/0.3·Vcc: V_T+ worst case and V_T- worst case.
            Digital = new DigitalSpec(2.0, 6.0, 5.0, Icc: 20e-6, Vih: 3.15, Vil: 0.9, IoMax: 0.004),
            NoteTh = "มีฮิสเทอรีซิสประมาณ 0.9V ที่ไฟ 5V — ใช้ล้างสัญญาณขอบช้า กันสวิตช์เด้ง และทำ RC ออสซิลเลเตอร์ได้ "
                   + "· ขาเดียวกับ 74HC04 ทุกขา สลับตัวได้ทันที · เป็นตัวกลับสัญญาณ ถ้าต้องการเฟสเดิมต้องต่อสองตัว · "
                   + FloatingInputs,
            Pins =
            [
                P("1", "1A", PinKind.Input, PinSide.Left, 0),
                P("2", "1Y", PinKind.Output, PinSide.Left, 1),
                P("3", "2A", PinKind.Input, PinSide.Left, 2),
                P("4", "2Y", PinKind.Output, PinSide.Left, 3),
                P("5", "3A", PinKind.Input, PinSide.Left, 4),
                P("6", "3Y", PinKind.Output, PinSide.Left, 5),
                P("7", "GND", PinKind.Ground, PinSide.Left, 6),
                P("8", "4Y", PinKind.Output, PinSide.Right, 6),
                P("9", "4A", PinKind.Input, PinSide.Right, 5),
                P("10", "5Y", PinKind.Output, PinSide.Right, 4),
                P("11", "5A", PinKind.Input, PinSide.Right, 3),
                P("12", "6Y", PinKind.Output, PinSide.Right, 2),
                P("13", "6A", PinKind.Input, PinSide.Right, 1),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC32", Prefix = "U", Name = "74HC32", NameTh = "ไอซีเกต OR 2 อินพุต 4 ชุด",
            Mpn = "74HC32", Package = "DIP-14",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 8,
            Digital = Hc(20e-6),
            NoteTh = "ลำดับขาเหมือน 74HC00/'08/'86 · อินพุตที่ไม่ใช้ของเกต OR ต้องผูกลง GND (ผูกขึ้น VCC "
                   + "จะบังคับให้เอาต์พุตสูงตลอด) · " + FloatingInputs,
            Pins =
            [
                P("1", "1A", PinKind.Input, PinSide.Left, 0),
                P("2", "1B", PinKind.Input, PinSide.Left, 1),
                P("3", "1Y", PinKind.Output, PinSide.Left, 2),
                P("4", "2A", PinKind.Input, PinSide.Left, 3),
                P("5", "2B", PinKind.Input, PinSide.Left, 4),
                P("6", "2Y", PinKind.Output, PinSide.Left, 5),
                P("7", "GND", PinKind.Ground, PinSide.Left, 6),
                P("8", "3Y", PinKind.Output, PinSide.Right, 6),
                P("9", "3A", PinKind.Input, PinSide.Right, 5),
                P("10", "3B", PinKind.Input, PinSide.Right, 4),
                P("11", "4Y", PinKind.Output, PinSide.Right, 3),
                P("12", "4A", PinKind.Input, PinSide.Right, 2),
                P("13", "4B", PinKind.Input, PinSide.Right, 1),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC86", Prefix = "U", Name = "74HC86", NameTh = "ไอซีเกต XOR 2 อินพุต 4 ชุด",
            Mpn = "74HC86", Package = "DIP-14",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 8,
            Digital = Hc(20e-6),
            NoteTh = "ลำดับขาเหมือน 74HC00/'08/'32 · ใช้เป็นอินเวอร์เตอร์สั่งได้ (ขา B เป็นตัวเลือกกลับ/ไม่กลับ) "
                   + "ทำครึ่งวงจรบวก และตรวจพาริตี้ · " + FloatingInputs,
            Pins =
            [
                P("1", "1A", PinKind.Input, PinSide.Left, 0),
                P("2", "1B", PinKind.Input, PinSide.Left, 1),
                P("3", "1Y", PinKind.Output, PinSide.Left, 2),
                P("4", "2A", PinKind.Input, PinSide.Left, 3),
                P("5", "2B", PinKind.Input, PinSide.Left, 4),
                P("6", "2Y", PinKind.Output, PinSide.Left, 5),
                P("7", "GND", PinKind.Ground, PinSide.Left, 6),
                P("8", "3Y", PinKind.Output, PinSide.Right, 6),
                P("9", "3A", PinKind.Input, PinSide.Right, 5),
                P("10", "3B", PinKind.Input, PinSide.Right, 4),
                P("11", "4Y", PinKind.Output, PinSide.Right, 3),
                P("12", "4A", PinKind.Input, PinSide.Right, 2),
                P("13", "4B", PinKind.Input, PinSide.Right, 1),
                P("14", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        // ── 74HC MSI, DIP-16 ─────────────────────────────────────────────

        new()
        {
            Key = "74HC138", Prefix = "U", Name = "74HC138", NameTh = "ไอซีถอดรหัส 3 ไป 8 / ดีมัลติเพล็กเซอร์",
            Mpn = "74HC138", Package = "DIP-16",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 9,
            Digital = Hc(80e-6),
            NoteTh = "เอาต์พุตแอกทีฟต่ำทุกขา — ขาที่ถูกเลือกจะเป็น LOW ส่วนที่เหลือ HIGH คนมักต่อกลับด้าน · "
                   + "จะทำงานได้ต้อง /E1 = LOW และ /E2 = LOW และ E3 = HIGH พร้อมกัน ไม่ใช้ก็ต้องผูกไว้ ห้ามลอย · "
                   + "TI เรียกขาชุดเดียวกันว่า A, B, C และ G2A, G2B, G1 ชื่อต่างแต่ขาเดียวกัน",
            Pins =
            [
                P("1", "A0", PinKind.Input, PinSide.Left, 0, "บิตต่ำสุด"),
                P("2", "A1", PinKind.Input, PinSide.Left, 1),
                P("3", "A2", PinKind.Input, PinSide.Left, 2, "บิตสูงสุด"),
                P("4", "/E1", PinKind.Input, PinSide.Left, 3, "อินาเบิล แอกทีฟต่ำ (TI: G2A)"),
                P("5", "/E2", PinKind.Input, PinSide.Left, 4, "อินาเบิล แอกทีฟต่ำ (TI: G2B)"),
                P("6", "E3", PinKind.Input, PinSide.Left, 5, "อินาเบิล แอกทีฟสูง (TI: G1)"),
                P("7", "/Y7", PinKind.Output, PinSide.Left, 6, "แอกทีฟต่ำ"),
                P("8", "GND", PinKind.Ground, PinSide.Left, 7),
                P("9", "/Y6", PinKind.Output, PinSide.Right, 7, "แอกทีฟต่ำ"),
                P("10", "/Y5", PinKind.Output, PinSide.Right, 6, "แอกทีฟต่ำ"),
                P("11", "/Y4", PinKind.Output, PinSide.Right, 5, "แอกทีฟต่ำ"),
                P("12", "/Y3", PinKind.Output, PinSide.Right, 4, "แอกทีฟต่ำ"),
                P("13", "/Y2", PinKind.Output, PinSide.Right, 3, "แอกทีฟต่ำ"),
                P("14", "/Y1", PinKind.Output, PinSide.Right, 2, "แอกทีฟต่ำ"),
                P("15", "/Y0", PinKind.Output, PinSide.Right, 1, "แอกทีฟต่ำ"),
                P("16", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC165", Prefix = "U", Name = "74HC165", NameTh = "ชิฟต์รีจิสเตอร์ ขนานเข้า-อนุกรมออก 8 บิต",
            Mpn = "74HC165", Package = "DIP-16",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 9,
            Digital = Hc(80e-6),
            NoteTh = "ใช้ขยายขาอินพุต — ตรงข้ามกับ 74HC595 ที่ขยายขาเอาต์พุต · /PL (ขา 1) แอกทีฟต่ำ: "
                   + "ดึงลง LOW เพื่อคว้าค่าที่ขา D0–D7 แล้วปล่อยขึ้น HIGH ถึงจะเลื่อนออกได้ ค่าถูกอ่านตอน /PL ต่ำ "
                   + "ไม่ใช่ตอนขอบนาฬิกา · /CE (ขา 15) ไม่ใช้ต้องต่อลง GND ไม่งั้นนาฬิกาไม่เดิน · "
                   + "อินพุตทุกขาต้องมีพูลอัปหรือพูลดาวน์ ปล่อยลอยแล้วอ่านค่ามั่ว · "
                   + "ต่อพ่วงหลายตัวโดยเอา Q7 ของตัวหน้าไปเข้า DS ของตัวถัดไป",
            Pins =
            [
                P("1", "/PL", PinKind.Input, PinSide.Left, 0, "โหลดค่าขนาน แอกทีฟต่ำ (SH//LD)"),
                P("2", "CP", PinKind.Input, PinSide.Left, 1, "นาฬิกา เลื่อนที่ขอบขาขึ้น"),
                P("3", "D4", PinKind.Input, PinSide.Left, 2),
                P("4", "D5", PinKind.Input, PinSide.Left, 3),
                P("5", "D6", PinKind.Input, PinSide.Left, 4),
                P("6", "D7", PinKind.Input, PinSide.Left, 5, "บิตที่ออกก่อน"),
                P("7", "/Q7", PinKind.Output, PinSide.Left, 6, "เอาต์พุตอนุกรมกลับสัญญาณ"),
                P("8", "GND", PinKind.Ground, PinSide.Left, 7),
                P("9", "Q7", PinKind.Output, PinSide.Right, 7, "เอาต์พุตอนุกรม — ต่อเข้า MISO"),
                P("10", "DS", PinKind.Input, PinSide.Right, 6, "อินพุตอนุกรมสำหรับต่อพ่วง ไม่ใช้ต่อ GND"),
                P("11", "D0", PinKind.Input, PinSide.Right, 5),
                P("12", "D1", PinKind.Input, PinSide.Right, 4),
                P("13", "D2", PinKind.Input, PinSide.Right, 3),
                P("14", "D3", PinKind.Input, PinSide.Right, 2),
                P("15", "/CE", PinKind.Input, PinSide.Right, 1, "หยุดนาฬิกาเมื่อ HIGH — ไม่ใช้ต่อ GND"),
                P("16", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC595", Prefix = "U", Name = "74HC595", NameTh = "ชิฟต์รีจิสเตอร์ อนุกรมเข้า-ขนานออก 8 บิต พร้อมแลตช์",
            Mpn = "74HC595", Package = "DIP-16",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 9,
            Digital = Hc(80e-6),
            NoteTh = "ข้อมูลเลื่อนที่ขอบขาขึ้นของ SRCLK แต่จะโผล่ที่ขาเอาต์พุตเมื่อ RCLK ขึ้นเท่านั้น — "
                   + "สลับสองขานี้แล้วภาพจะเหลื่อมไปหนึ่งบิต · /OE (ขา 13) ต้องต่อ GND ไม่งั้นเอาต์พุตเป็น Hi-Z ทั้งแถว · "
                   + "/SRCLR (ขา 10) ต้องต่อ VCC ไม่งั้นข้อมูลถูกล้างตลอด · ต่อพ่วงตัวถัดไปใช้ Q7' (ขา 9) "
                   + "ไม่ใช่ Q7 (ขา 7) ตรงนี้พลาดกันประจำ · ขา QA คือขา 15 อยู่คนละฝั่งกับ QB–QH · "
                   + "ขับ LED ทั้ง 8 ขาพร้อมกันไม่ได้เต็มพิกัด กระแสรวมที่ไหลออกขา GND เป็นตัวจำกัด ต้องมี R อนุกรมทุกดวง",
            Pins =
            [
                P("1", "QB", PinKind.Output, PinSide.Left, 0, "บิต 1"),
                P("2", "QC", PinKind.Output, PinSide.Left, 1, "บิต 2"),
                P("3", "QD", PinKind.Output, PinSide.Left, 2, "บิต 3"),
                P("4", "QE", PinKind.Output, PinSide.Left, 3, "บิต 4"),
                P("5", "QF", PinKind.Output, PinSide.Left, 4, "บิต 5"),
                P("6", "QG", PinKind.Output, PinSide.Left, 5, "บิต 6"),
                P("7", "QH", PinKind.Output, PinSide.Left, 6, "บิต 7"),
                P("8", "GND", PinKind.Ground, PinSide.Left, 7),
                P("9", "QH'", PinKind.Output, PinSide.Right, 7, "อนุกรมออก — ต่อเข้า SER ของตัวถัดไป"),
                P("10", "/SRCLR", PinKind.Input, PinSide.Right, 6, "ล้างรีจิสเตอร์ แอกทีฟต่ำ — ต่อ VCC เมื่อไม่ใช้"),
                P("11", "SRCLK", PinKind.Input, PinSide.Right, 5, "นาฬิกาเลื่อนบิต ขอบขาขึ้น"),
                P("12", "RCLK", PinKind.Input, PinSide.Right, 4, "แลตช์ออกที่ขา ขอบขาขึ้น"),
                P("13", "/OE", PinKind.Input, PinSide.Right, 3, "เปิดเอาต์พุต แอกทีฟต่ำ — ต่อ GND เมื่อใช้งานปกติ"),
                P("14", "SER", PinKind.Input, PinSide.Right, 2, "ข้อมูลอนุกรมเข้า"),
                P("15", "QA", PinKind.Output, PinSide.Right, 1, "บิต 0"),
                P("16", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "74HC4051", Prefix = "U", Name = "74HC4051", NameTh = "อะนาล็อกมัลติเพล็กเซอร์ / ดีมัลติเพล็กเซอร์ 8 ช่อง",
            Mpn = "74HC4051", Package = "DIP-16",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 9,
            // V_CC – V_EE together may not exceed the 10 V rating; V_CC alone is 2–10 V.
            Digital = new DigitalSpec(2.0, 10.0, 5.0, Icc: 80e-6, Vih: 3.15, Vil: 1.35, IoMax: 0.025),
            Params = CatalogBuilder.Params((ParamKey.RdsOn, 80)),
            NoteTh = "VEE (ขา 7) ต้องต่อ GND ถ้าใช้ไฟเลี้ยงเดี่ยว — ปล่อยลอยแล้วสวิตช์ไม่นำ · "
                   + "สัญญาณที่ผ่านต้องอยู่ในช่วง VEE ถึง VCC เท่านั้น เกินไปทางไหนก็วิ่งเข้าไดโอดป้องกัน · "
                   + "เป็นสวิตช์ ผ่านได้สองทิศทาง ใช้เป็นทั้ง mux และ demux · ความต้านทานตอนนำ ~80Ω ที่ 5V "
                   + "จะไปรวมเป็นตัวแบ่งแรงดันกับโหลด ถ้าอ่านด้วย ADC ให้ต่อเข้าอิมพีแดนซ์สูง · "
                   + "รุ่น CD4051 (ไม่ใช่ HC) ทนไฟถึง 20V แต่ Ron สูงกว่ามาก",
            Pins =
            [
                P("1", "Y4", PinKind.Analog, PinSide.Left, 0, "ช่อง 4"),
                P("2", "Y6", PinKind.Analog, PinSide.Left, 1, "ช่อง 6"),
                P("3", "Z", PinKind.Analog, PinSide.Left, 2, "ขาร่วม — เข้าหรือออกก็ได้"),
                P("4", "Y7", PinKind.Analog, PinSide.Left, 3, "ช่อง 7"),
                P("5", "Y5", PinKind.Analog, PinSide.Left, 4, "ช่อง 5"),
                P("6", "/E", PinKind.Input, PinSide.Left, 5, "อินาเบิล แอกทีฟต่ำ — HIGH ตัดทุกช่อง"),
                P("7", "VEE", PinKind.Power, PinSide.Left, 6, "ไฟลบของฝั่งอนาล็อก — ไฟเดี่ยวให้ต่อ GND"),
                P("8", "GND", PinKind.Ground, PinSide.Left, 7, "VSS"),
                P("9", "S2", PinKind.Input, PinSide.Right, 7, "เลือกช่อง บิตสูงสุด (C)"),
                P("10", "S1", PinKind.Input, PinSide.Right, 6, "เลือกช่อง (B)"),
                P("11", "S0", PinKind.Input, PinSide.Right, 5, "เลือกช่อง บิตต่ำสุด (A)"),
                P("12", "Y3", PinKind.Analog, PinSide.Right, 4, "ช่อง 3"),
                P("13", "Y0", PinKind.Analog, PinSide.Right, 3, "ช่อง 0"),
                P("14", "Y1", PinKind.Analog, PinSide.Right, 2, "ช่อง 1"),
                P("15", "Y2", PinKind.Analog, PinSide.Right, 1, "ช่อง 2"),
                P("16", "VCC", PinKind.Power, PinSide.Right, 0),
            ],
        },

        // ── CD4000B family ───────────────────────────────────────────────

        new()
        {
            Key = "CD4017", Prefix = "U", Name = "CD4017B", NameTh = "ไอซีนับ 10 ขั้น เอาต์พุตแยกทีละขา",
            Mpn = "CD4017B", Package = "DIP-16",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 9,
            Digital = Cd4000(5e-6, 0.001),
            NoteTh = "เอาต์พุตขึ้นครั้งละหนึ่งขาเท่านั้น ไล่ Q0→Q9 แล้ววนกลับ · "
                   + "MR (ขา 15) ต้องต่อ GND และ /CE (ขา 13) ต้องต่อ GND ด้วย ไม่งั้นไม่นับ — สองขานี้ลืมกันประจำ · "
                   + "หมายเลขขาไม่เรียงตามลำดับนับ (Q0 = ขา 3, Q1 = ขา 2, Q2 = ขา 4 …) ต้องดูตารางทุกครั้ง · "
                   + "จะนับน้อยกว่า 10 ให้เอาเอาต์พุตขาถัดไปวนกลับเข้า MR · "
                   + "ขับ LED ตรงได้แค่ราว 1 mA ที่ไฟ 5V (สว่างน้อยมาก) ใช้ไฟ 9–12V หรือใส่ทรานซิสเตอร์ถ้าต้องการสว่าง "
                   + "และต้องมีตัวต้านทานอนุกรมเสมอ",
            Pins =
            [
                P("1", "Q5", PinKind.Output, PinSide.Left, 0),
                P("2", "Q1", PinKind.Output, PinSide.Left, 1),
                P("3", "Q0", PinKind.Output, PinSide.Left, 2, "ขึ้นตอนรีเซ็ต"),
                P("4", "Q2", PinKind.Output, PinSide.Left, 3),
                P("5", "Q6", PinKind.Output, PinSide.Left, 4),
                P("6", "Q7", PinKind.Output, PinSide.Left, 5),
                P("7", "Q3", PinKind.Output, PinSide.Left, 6),
                P("8", "VSS", PinKind.Ground, PinSide.Left, 7),
                P("9", "Q8", PinKind.Output, PinSide.Right, 7),
                P("10", "Q4", PinKind.Output, PinSide.Right, 6),
                P("11", "Q9", PinKind.Output, PinSide.Right, 5),
                P("12", "CO", PinKind.Output, PinSide.Right, 4, "หารสิบ — ต่อพ่วงตัวถัดไป"),
                P("13", "/CE", PinKind.Input, PinSide.Right, 3, "หยุดนับเมื่อ HIGH — ไม่ใช้ต่อ GND"),
                P("14", "CP", PinKind.Input, PinSide.Right, 2, "นาฬิกา นับที่ขอบขาขึ้น"),
                P("15", "MR", PinKind.Input, PinSide.Right, 1, "รีเซ็ต แอกทีฟสูง — ไม่ใช้ต่อ GND"),
                P("16", "VDD", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "CD4026", Prefix = "U", Name = "CD4026B", NameTh = "ไอซีนับ 10 ขั้น ขับ 7 เซกเมนต์ในตัว",
            Mpn = "CD4026B", Package = "DIP-16",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 9,
            Digital = Cd4000(5e-6, 0.001),
            NoteTh = "นับและถอดรหัสในตัวเดียว ต่อ 7 เซกเมนต์แบบ 'แคโทดร่วม' เท่านั้น (เอาต์พุตแอกทีฟสูง) · "
                   + "กระแสต่อเซกเมนต์ราว 1 mA ที่ไฟ 5V เท่านั้น ตัวเลขจะมัวมาก ใช้ไฟ 9–12V หรือขับผ่านทรานซิสเตอร์ "
                   + "และยังต้องมีตัวต้านทานอนุกรมทุกเซกเมนต์ · DEI (ขา 3) ต้องต่อ VDD ไม่งั้นจอดับทั้งหลัก · "
                   + "MR (ขา 15) ต้องต่อ GND · ต่อหลายหลักโดยเอา CO (ขา 5) ไปเข้า CP ของหลักถัดไป · "
                   + "ญาติสนิทคือ CD4033 ซึ่งมีฟังก์ชันตัดเลขศูนย์นำหน้า แต่ลำดับขาไม่เหมือนกันทั้งหมด",
            Pins =
            [
                P("1", "CP", PinKind.Input, PinSide.Left, 0, "นาฬิกา นับที่ขอบขาขึ้น"),
                P("2", "/CE", PinKind.Input, PinSide.Left, 1, "หยุดนับเมื่อ HIGH — ไม่ใช้ต่อ GND"),
                P("3", "DEI", PinKind.Input, PinSide.Left, 2, "เปิดจอ — ต้องต่อ VDD ถึงจะติด"),
                P("4", "DEO", PinKind.Output, PinSide.Left, 3, "ส่งต่อสัญญาณเปิดจอ"),
                P("5", "CO", PinKind.Output, PinSide.Left, 4, "หารสิบ — ต่อเข้า CP ของหลักถัดไป"),
                P("6", "f", PinKind.Output, PinSide.Left, 5, "เซกเมนต์ f"),
                P("7", "g", PinKind.Output, PinSide.Left, 6, "เซกเมนต์ g"),
                P("8", "VSS", PinKind.Ground, PinSide.Left, 7),
                P("9", "d", PinKind.Output, PinSide.Right, 7, "เซกเมนต์ d"),
                P("10", "a", PinKind.Output, PinSide.Right, 6, "เซกเมนต์ a"),
                P("11", "b", PinKind.Output, PinSide.Right, 5, "เซกเมนต์ b"),
                P("12", "c", PinKind.Output, PinSide.Right, 4, "เซกเมนต์ c"),
                P("13", "e", PinKind.Output, PinSide.Right, 3, "เซกเมนต์ e"),
                P("14", "Cug", PinKind.Output, PinSide.Right, 2, "เซกเมนต์ c แบบไม่ผ่านเกต — บางดาต้าชีตพิมพ์เป็น NC ปล่อยลอยได้"),
                P("15", "MR", PinKind.Input, PinSide.Right, 1, "รีเซ็ต แอกทีฟสูง — ไม่ใช้ต่อ GND"),
                P("16", "VDD", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "CD4511", Prefix = "U", Name = "CD4511B", NameTh = "ไอซีถอดรหัส BCD เป็น 7 เซกเมนต์ พร้อมแลตช์",
            Mpn = "CD4511B", Package = "DIP-16",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 9,
            Digital = Cd4000(5e-6, 0.025),
            NoteTh = "เอาต์พุตแอกทีฟสูงและจ่ายกระแสได้ — ใช้กับ 7 เซกเมนต์ 'แคโทดร่วม' เท่านั้น "
                   + "ต่อกับแบบแอโนดร่วมไม่ติด ต้องเปลี่ยนไปใช้ 74LS47 แทน · "
                   + "/LT (ขา 3) และ /BL (ขา 4) ต้องต่อ VDD, LE (ขา 5) ต้องต่อ GND ไม่งั้นเลขไม่ขึ้นหรือค้าง · "
                   + "รับเฉพาะ BCD 0–9 ป้อน 10–15 (A–F) แล้วจอดับ ไม่ใช่ตัวอักษร · "
                   + "จ่ายได้ถึงราว 25 mA ต่อเซกเมนต์ แต่ยังต้องมีตัวต้านทานอนุกรมทุกขา ต่อตรงเข้า LED ไอซีพัง · "
                   + "ลำดับอินพุต A–D ไม่เรียงตามหมายเลขขา (A = ขา 7, B = ขา 1, C = ขา 2, D = ขา 6)",
            Pins =
            [
                P("1", "B", PinKind.Input, PinSide.Left, 0, "BCD บิต 1"),
                P("2", "C", PinKind.Input, PinSide.Left, 1, "BCD บิต 2"),
                P("3", "/LT", PinKind.Input, PinSide.Left, 2, "ทดสอบหลอด แอกทีฟต่ำ — ต่อ VDD เมื่อไม่ใช้"),
                P("4", "/BL", PinKind.Input, PinSide.Left, 3, "ดับจอ แอกทีฟต่ำ — ต่อ VDD เมื่อไม่ใช้"),
                P("5", "LE", PinKind.Input, PinSide.Left, 4, "ค้างค่าเมื่อ HIGH — ต่อ GND เมื่อไม่ใช้"),
                P("6", "D", PinKind.Input, PinSide.Left, 5, "BCD บิต 3 (สูงสุด)"),
                P("7", "A", PinKind.Input, PinSide.Left, 6, "BCD บิต 0 (ต่ำสุด)"),
                P("8", "VSS", PinKind.Ground, PinSide.Left, 7),
                P("9", "e", PinKind.Output, PinSide.Right, 7, "เซกเมนต์ e"),
                P("10", "d", PinKind.Output, PinSide.Right, 6, "เซกเมนต์ d"),
                P("11", "c", PinKind.Output, PinSide.Right, 5, "เซกเมนต์ c"),
                P("12", "b", PinKind.Output, PinSide.Right, 4, "เซกเมนต์ b"),
                P("13", "a", PinKind.Output, PinSide.Right, 3, "เซกเมนต์ a"),
                P("14", "g", PinKind.Output, PinSide.Right, 2, "เซกเมนต์ g"),
                P("15", "f", PinKind.Output, PinSide.Right, 1, "เซกเมนต์ f"),
                P("16", "VDD", PinKind.Power, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "CD4066", Prefix = "U", Name = "CD4066B", NameTh = "สวิตช์อนาล็อกสองทาง 4 ชุด",
            Mpn = "CD4066B", Package = "DIP-14",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 8,
            Digital = Cd4000(1e-6, 0.025),
            Params = CatalogBuilder.Params((ParamKey.RdsOn, 270)),
            NoteTh = "เป็นสวิตช์ ไม่ใช่เกต — สัญญาณผ่านได้สองทิศทาง ขา A กับ B ของชุดเดียวกันสลับกันได้ · "
                   + "สัญญาณที่ผ่านต้องอยู่ระหว่าง VSS ถึง VDD เท่านั้น อนาล็อกที่แกว่งลบต้องใช้ไฟเลี้ยงคู่ "
                   + "หรือยกระดับ DC ขึ้นก่อน · Ron ราว 270Ω ที่ไฟ 5V จะกลายเป็นตัวแบ่งแรงดันกับโหลดต่ำ ๆ "
                   + "และเปลี่ยนตามแรงดันสัญญาณ (ทำให้เพี้ยนในงานเสียง) ไฟเลี้ยงสูงขึ้น Ron ต่ำลงมาก · "
                   + "รุ่น 74HC4066 ขาเหมือนกันแต่ Ron ต่ำกว่าหลายเท่า และไฟเลี้ยงสูงสุดแค่ 10V · "
                   + "ขาควบคุมต้องขับเต็มระดับลอจิก ปล่อยลอยแล้วสวิตช์ทำงานมั่ว",
            Pins =
            [
                P("1", "1A", PinKind.Analog, PinSide.Left, 0, "สวิตช์ 1 ปลายหนึ่ง"),
                P("2", "1B", PinKind.Analog, PinSide.Left, 1, "สวิตช์ 1 อีกปลาย"),
                P("3", "2B", PinKind.Analog, PinSide.Left, 2),
                P("4", "2A", PinKind.Analog, PinSide.Left, 3),
                P("5", "2C", PinKind.Input, PinSide.Left, 4, "ควบคุมสวิตช์ 2 — HIGH = ต่อ"),
                P("6", "3C", PinKind.Input, PinSide.Left, 5, "ควบคุมสวิตช์ 3"),
                P("7", "VSS", PinKind.Ground, PinSide.Left, 6),
                P("8", "3A", PinKind.Analog, PinSide.Right, 6),
                P("9", "3B", PinKind.Analog, PinSide.Right, 5),
                P("10", "4B", PinKind.Analog, PinSide.Right, 4),
                P("11", "4A", PinKind.Analog, PinSide.Right, 3),
                P("12", "4C", PinKind.Input, PinSide.Right, 2, "ควบคุมสวิตช์ 4"),
                P("13", "1C", PinKind.Input, PinSide.Right, 1, "ควบคุมสวิตช์ 1 — อยู่ไกลจากสวิตช์ 1 มาก ดูให้ดี"),
                P("14", "VDD", PinKind.Power, PinSide.Right, 0),
            ],
        },

        // ── Darlington sink drivers ──────────────────────────────────────
        //
        // No DigitalSpec: these have no supply pin at all, so VccMin/Max/Typical and Icc
        // would have to be invented. The ratings that matter are the output ones, and
        // those are real parameters below.

        new()
        {
            Key = "ULN2003", Prefix = "U", Name = "ULN2003A", NameTh = "ไอซีขับกระแสดาร์ลิงตัน 7 ช่อง (ดูดลงกราวด์)",
            Mpn = "ULN2003A", Package = "DIP-16",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 9,
            Params = CatalogBuilder.Params((ParamKey.Vceo, 50), (ParamKey.Ic, 0.5), (ParamKey.VceSat, 1.1)),
            NoteTh = "ดูดกระแสลงกราวด์อย่างเดียว (sink) — โหลดต้องต่อจากไฟบวกลงมาที่ขา OUT ต่อกลับด้านไม่ทำงาน · "
                   + "COM (ขา 9) ต้องต่อไฟบวกของโหลด ไดโอดกันแรงย้อนถึงจะทำงาน ลืมต่อแล้วรีเลย์หรือสเต็ปเปอร์จะฆ่าไอซี · "
                   + "500 mA ต่อช่องเป็นค่าสูงสุดของช่องเดียว ใช้หลายช่องพร้อมกันต้องลดลงตามการระบายความร้อน · "
                   + "แรงดันตกคร่อมขาเอาต์พุตราว 1V จึงไม่เหมาะกับ LED แรงดันต่ำที่ต้องการความแม่นยำ · "
                   + "ไม่มีขาไฟเลี้ยง — ขาอินพุตของรุ่น 'A' มีตัวต้านทานเบส 2.7k ในตัว ออกแบบมาสำหรับลอจิก 5V "
                   + "ขับด้วย 3.3V ได้แต่กระแสฐานลดลง ตรวจว่าโหลดยังพอ",
            Pins =
            [
                P("1", "IN1", PinKind.Input, PinSide.Left, 0),
                P("2", "IN2", PinKind.Input, PinSide.Left, 1),
                P("3", "IN3", PinKind.Input, PinSide.Left, 2),
                P("4", "IN4", PinKind.Input, PinSide.Left, 3),
                P("5", "IN5", PinKind.Input, PinSide.Left, 4),
                P("6", "IN6", PinKind.Input, PinSide.Left, 5),
                P("7", "IN7", PinKind.Input, PinSide.Left, 6),
                P("8", "GND", PinKind.Ground, PinSide.Left, 7, "อิมิตเตอร์ร่วม"),
                P("9", "COM", PinKind.Power, PinSide.Right, 7, "แคโทดร่วมของไดโอดกันแรงย้อน — ต่อไฟของโหลด"),
                P("10", "OUT7", PinKind.OpenDrain, PinSide.Right, 6),
                P("11", "OUT6", PinKind.OpenDrain, PinSide.Right, 5),
                P("12", "OUT5", PinKind.OpenDrain, PinSide.Right, 4),
                P("13", "OUT4", PinKind.OpenDrain, PinSide.Right, 3),
                P("14", "OUT3", PinKind.OpenDrain, PinSide.Right, 2),
                P("15", "OUT2", PinKind.OpenDrain, PinSide.Right, 1),
                P("16", "OUT1", PinKind.OpenDrain, PinSide.Right, 0),
            ],
        },

        new()
        {
            Key = "ULN2803", Prefix = "U", Name = "ULN2803A", NameTh = "ไอซีขับกระแสดาร์ลิงตัน 8 ช่อง (ดูดลงกราวด์)",
            Mpn = "ULN2803A", Package = "DIP-18",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 10,
            Params = CatalogBuilder.Params((ParamKey.Vceo, 50), (ParamKey.Ic, 0.5), (ParamKey.VceSat, 1.1)),
            NoteTh = "เหมือน ULN2003 ทุกอย่างแต่มี 8 ช่องและเป็น DIP-18 — ขาไม่ตรงกัน เปลี่ยนตัวแทนกันบนบอร์ดเดิมไม่ได้ · "
                   + "ขาอินพุตเรียง IN1–IN8 ที่ขา 1–8 ส่วนเอาต์พุตเรียงย้อน ขา 11 = OUT8 ไปจนขา 18 = OUT1 "
                   + "จึงตรงข้ามกับอินพุตแบบพับครึ่ง · GND = ขา 9, COM = ขา 10 ต้องต่อไฟของโหลดเสมอเมื่อขับรีเลย์ "
                   + "มอเตอร์ หรือสเต็ปเปอร์ · ดูดลงกราวด์อย่างเดียว โหลดต่อจากไฟบวกลงมา",
            Pins =
            [
                P("1", "IN1", PinKind.Input, PinSide.Left, 0),
                P("2", "IN2", PinKind.Input, PinSide.Left, 1),
                P("3", "IN3", PinKind.Input, PinSide.Left, 2),
                P("4", "IN4", PinKind.Input, PinSide.Left, 3),
                P("5", "IN5", PinKind.Input, PinSide.Left, 4),
                P("6", "IN6", PinKind.Input, PinSide.Left, 5),
                P("7", "IN7", PinKind.Input, PinSide.Left, 6),
                P("8", "IN8", PinKind.Input, PinSide.Left, 7),
                P("9", "GND", PinKind.Ground, PinSide.Left, 8, "อิมิตเตอร์ร่วม"),
                P("10", "COM", PinKind.Power, PinSide.Right, 8, "แคโทดร่วมของไดโอดกันแรงย้อน — ต่อไฟของโหลด"),
                P("11", "OUT8", PinKind.OpenDrain, PinSide.Right, 7),
                P("12", "OUT7", PinKind.OpenDrain, PinSide.Right, 6),
                P("13", "OUT6", PinKind.OpenDrain, PinSide.Right, 5),
                P("14", "OUT5", PinKind.OpenDrain, PinSide.Right, 4),
                P("15", "OUT4", PinKind.OpenDrain, PinSide.Right, 3),
                P("16", "OUT3", PinKind.OpenDrain, PinSide.Right, 2),
                P("17", "OUT2", PinKind.OpenDrain, PinSide.Right, 1),
                P("18", "OUT1", PinKind.OpenDrain, PinSide.Right, 0),
            ],
        },

        // ── optocouplers ─────────────────────────────────────────────────
        //
        // Also no DigitalSpec: an optocoupler is an LED facing a bare phototransistor,
        // with no supply rail of its own. What matters is the LED drive on one side and
        // the transistor rating on the other, and both are recorded as parameters.

        new()
        {
            Key = "PC817", Prefix = "U", Name = "PC817", NameTh = "ออปโตคัปเปลอร์ทรานซิสเตอร์ 4 ขา",
            Mpn = "PC817", Package = "DIP-4",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 8, BodyHeight = 5,
            Params = CatalogBuilder.Params((ParamKey.Vceo, 35), (ParamKey.Ic, 0.05),
                            (ParamKey.If, 0.05), (ParamKey.Vf, 1.2)),
            NoteTh = "ขา 1-2 คือ LED ต้องมีตัวต้านทานจำกัดกระแสเสมอ ต่อตรงเข้าไฟพังทันที ใช้จริงราว 5–10 mA ก็พอ · "
                   + "ฝั่งเอาต์พุตเป็นทรานซิสเตอร์เปล่า ไม่มีขาเบส ต้องมีตัวต้านทานพูลอัปที่ขาคอลเลกเตอร์ · "
                   + "CTR (อัตราขยายกระแส) ต่างกันมากตามรหัสท้ายเบอร์ (A/B/C/D) ตั้งแต่ประมาณ 50% ถึง 600% "
                   + "ล็อตต่างกันได้กระแสเอาต์พุตไม่เท่ากัน ต้องออกแบบเผื่อค่าต่ำสุด และ CTR ยังลดลงตามอายุการใช้งาน · "
                   + "ประโยชน์หลักคือแยกกราวด์สองฝั่งออกจากกัน — ถ้าเดินกราวด์ถึงกันอยู่แล้วก็ไม่ได้อะไรขึ้นมา · "
                   + "ขาแยกฝั่ง LED กับฝั่งทรานซิสเตอร์บน PCB ต้องเว้นระยะให้พอกับแรงดันที่กั้น",
            Pins =
            [
                P("1", "A", PinKind.Passive, PinSide.Left, 0, "แอโนดของ LED (มีจุดวงกลมกำกับที่ตัวถัง)"),
                P("2", "K", PinKind.Passive, PinSide.Left, 1, "แคโทดของ LED"),
                P("3", "E", PinKind.Passive, PinSide.Right, 1, "อิมิตเตอร์"),
                P("4", "C", PinKind.OpenDrain, PinSide.Right, 0, "คอลเลกเตอร์ — ต้องมีพูลอัป"),
            ],
        },

        new()
        {
            Key = "4N35", Prefix = "U", Name = "4N35", NameTh = "ออปโตคัปเปลอร์ทรานซิสเตอร์ มีขาเบส",
            Mpn = "4N35", Package = "DIP-6",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 8, BodyHeight = 6,
            Params = CatalogBuilder.Params((ParamKey.Vceo, 30), (ParamKey.If, 0.06), (ParamKey.Vf, 1.3)),
            NoteTh = "ขา 1-2 คือ LED ต้องมีตัวต้านทานจำกัดกระแสเสมอ · CTR ขั้นต่ำ 100% ที่ IF = 10 mA "
                   + "จึงคาดเดาได้แน่นอนกว่า PC817 ที่ CTR กระจายเป็นเกรด · "
                   + "ขา 3 ผู้ผลิตระบุให้ปล่อยลอย ห้ามใช้เป็นจุดพักสาย · "
                   + "ขา 6 เป็นเบสของโฟโตทรานซิสเตอร์ ปกติปล่อยลอย ถ้าวงจรไวต่อสัญญาณรบกวนให้ต่อ R ค่าสูง "
                   + "(หลักร้อย k ถึง M) ลงอิมิตเตอร์ แต่จะทำให้ความไวและความเร็วลดลง · "
                   + "4N35 / 4N36 / 4N37 ต่างกันหลักที่แรงดันฉนวนกั้นและสเปกบางตัว ลำดับขาเหมือนกัน",
            Pins =
            [
                P("1", "A", PinKind.Passive, PinSide.Left, 0, "แอโนดของ LED"),
                P("2", "K", PinKind.Passive, PinSide.Left, 1, "แคโทดของ LED"),
                P("3", "NC", PinKind.NotConnected, PinSide.Left, 2, "ผู้ผลิตระบุให้ปล่อยลอย"),
                P("4", "E", PinKind.Passive, PinSide.Right, 2, "อิมิตเตอร์"),
                P("5", "C", PinKind.OpenDrain, PinSide.Right, 1, "คอลเลกเตอร์ — ต้องมีพูลอัป"),
                P("6", "B", PinKind.Analog, PinSide.Right, 0, "เบส — ปกติปล่อยลอย"),
            ],
        },
    ];
}
