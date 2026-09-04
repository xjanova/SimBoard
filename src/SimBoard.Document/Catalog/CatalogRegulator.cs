namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// เรกูเลเตอร์และภาคจ่ายไฟ.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
///
/// Every entry below is Unverified: typed from working knowledge, not read line by line
/// off a datasheet. Dropout is carried wherever it is known, because it — not the output
/// voltage — is what decides whether a design runs from a battery.
///
/// Lead order is the other thing that kills boards here. A 7805, a 7905 and an LD1117V33
/// are the same TO-220 outline with three different pin orders, so the parts whose order
/// is not IN-GND-OUT are written out longhand with their real pin numbers rather than
/// pushed through <see cref="CatalogBuilder.Regulator"/>, which numbers 1/2/3 in
/// electrical order.
/// </summary>
public static class CatalogRegulator
{
    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
        // ── 78xx positive fixed, TO-220, pin 1-2-3 = IN-GND-OUT ──────────────

        Regulator("LM7805", "ไอซีเรกูเลเตอร์ +5V 1.5A", "TO-220", ["IN", "GND", "OUT"],
            vout: 5, ioutMax: 1.5, vinMax: 35, dropout: 2,
            note: "ตัวถัง TO-220 เรียงขา IN-GND-OUT (หันด้านพิมพ์เข้าหาตัว ขาลง) ครีบโลหะต่อกับขากราวด์ · "
                + "ดรอปเอาต์ ~2V จริง ๆ ต้องป้อนไม่ต่ำกว่า 7.5V — จ่ายจากแบต 6V ได้ไม่นิ่ง · "
                + "ต้องมี C 0.33µF ที่อินพุตและ 0.1µF ที่เอาต์พุตชิดตัวไอซี ไม่งั้นแกว่งเป็นความถี่สูง · "
                + "ส่วนต่างแรงดันคูณกระแสกลายเป็นความร้อนทั้งหมด 12V→5V ที่ 1A = 7W ต้องมีฮีตซิงก์ · "
                + "LM7805 / L7805CV / MC7805 / KA7805 คือตัวเดียวกัน ต่างแค่ผู้ผลิต"),

        Regulator("LM7806", "ไอซีเรกูเลเตอร์ +6V 1.5A", "TO-220", ["IN", "GND", "OUT"],
            vout: 6, ioutMax: 1.5, vinMax: 35, dropout: 2),

        Regulator("LM7808", "ไอซีเรกูเลเตอร์ +8V 1.5A", "TO-220", ["IN", "GND", "OUT"],
            vout: 8, ioutMax: 1.5, vinMax: 35, dropout: 2),

        Regulator("LM7809", "ไอซีเรกูเลเตอร์ +9V 1.5A", "TO-220", ["IN", "GND", "OUT"],
            vout: 9, ioutMax: 1.5, vinMax: 35, dropout: 2,
            note: "ต้องป้อนไม่ต่ำกว่า ~11.5V — ใช้อะแดปเตอร์ 9V จ่ายเข้าแล้วไม่ได้ 9V ออก"),

        Regulator("LM7812", "ไอซีเรกูเลเตอร์ +12V 1.5A", "TO-220", ["IN", "GND", "OUT"],
            vout: 12, ioutMax: 1.5, vinMax: 35, dropout: 2,
            note: "ต้องป้อนไม่ต่ำกว่า ~14.5V · หม้อแปลง 12VAC เรียงกระแสและกรองแล้วได้ราว 16-17V ใช้ได้พอดี"),

        Regulator("LM7815", "ไอซีเรกูเลเตอร์ +15V 1.5A", "TO-220", ["IN", "GND", "OUT"],
            vout: 15, ioutMax: 1.5, vinMax: 35, dropout: 2,
            note: "คู่กับ LM7915 สำหรับภาคจ่ายไฟบวก-ลบของออปแอมป์"),

        Regulator("LM7818", "ไอซีเรกูเลเตอร์ +18V 1.5A", "TO-220", ["IN", "GND", "OUT"],
            vout: 18, ioutMax: 1.5, vinMax: 35, dropout: 2),

        Regulator("LM7824", "ไอซีเรกูเลเตอร์ +24V 1.5A", "TO-220", ["IN", "GND", "OUT"],
            vout: 24, ioutMax: 1.5, vinMax: 40, dropout: 2,
            note: "ไฟเข้าสูงสุด 40V สูงกว่าเบอร์อื่นในตระกูลที่ 35V"),

        Regulator("LM78M05", "ไอซีเรกูเลเตอร์ +5V 500mA", "TO-220", ["IN", "GND", "OUT"],
            vout: 5, ioutMax: 0.5, vinMax: 35, dropout: 2,
            note: "หน้าตาและลำดับขาเหมือน 7805 ทุกอย่าง แต่จ่ายได้แค่ครึ่งเดียว — หยิบผิดกล่องแล้วโหลด 1A จะตัดด้วยความร้อนเป็นจังหวะ"),

        // ── low-dropout replacement for the 7805 ─────────────────────────────

        Regulator("LM2940CT-5.0", "ไอซีเรกูเลเตอร์ LDO +5V 1A", "TO-220", ["IN", "GND", "OUT"],
            vout: 5, ioutMax: 1.0, vinMax: 26, dropout: 0.5,
            note: "ใส่แทน 7805 ได้ตรงขา แต่ต้องการส่วนต่างแค่ ~0.5V — จ่ายจากแบตรถหรือแบต 6V ที่ใกล้หมดได้ · "
                + "⚠ ต้องใช้ C เอาต์พุต ≥22µF ที่มี ESR อยู่ในช่วงที่ดาต้าชีตกำหนด ใส่เซรามิกล้วนแล้ววงจรแกว่ง · "
                + "ทนไฟกลับขั้วและทรานเซียนต์ในระบบไฟรถยนต์"),

        // ── 79xx negative fixed, TO-220, pin 1-2-3 = GND-IN-OUT ──────────────
        // Different pin order from the 78xx, same outline. Written longhand so the pin
        // numbers are the package's own.

        Negative("LM7905", "ไอซีเรกูเลเตอร์ -5V 1.5A", vout: -5, vinMax: -35,
            note: "⚠ ลำดับขาไม่เหมือน 78xx — TO-220 ของ 79xx คือ GND-IN-OUT ใส่แทนที่ 7805 โดยไม่ดูขาแล้วไหม้ทันที · "
                + "ครีบโลหะต่ออยู่กับขาอินพุต ไม่ใช่กราวด์ ยึดร่วมฮีตซิงก์กับตัวอื่นต้องมีแผ่นฉนวนคั่น · "
                + "ตัวเลขติดลบเพราะเป็นเรกูเลเตอร์ขั้วลบ ไฟเข้าต้องต่ำกว่ากราวด์ · "
                + "คู่กับ LM7805 ทำภาคจ่ายไฟ ±5V"),

        Negative("LM7909", "ไอซีเรกูเลเตอร์ -9V 1.5A", vout: -9, vinMax: -35),

        Negative("LM7912", "ไอซีเรกูเลเตอร์ -12V 1.5A", vout: -12, vinMax: -35,
            note: "คู่กับ LM7812 · เรียงขา GND-IN-OUT ไม่เหมือนตัวบวก"),

        Negative("LM7915", "ไอซีเรกูเลเตอร์ -15V 1.5A", vout: -15, vinMax: -35,
            note: "คู่กับ LM7815 สำหรับภาคจ่ายไฟ ±15V ของออปแอมป์"),

        Negative("LM7924", "ไอซีเรกูเลเตอร์ -24V 1.5A", vout: -24, vinMax: -40),

        // ── 78Lxx, TO-92, pin 1-2-3 = OUT-GND-IN ─────────────────────────────
        // Reverse of the TO-220 78xx order. This one catches people every time.

        LowPower78L("78L05", "ไอซีเรกูเลเตอร์ +5V 100mA", vout: 5,
            note: "⚠ TO-92 เรียงขา OUT-GND-IN เมื่อหันด้านแบนเข้าหาตัวและขาลง — กลับทางกับ 7805 ตัวถัง TO-220 · "
                + "จ่ายได้แค่ 100mA อย่าเอาไปเลี้ยงรีเลย์ เซอร์โว หรือโมดูล WiFi · "
                + "ดรอปเอาต์ ~1.7V ต้องป้อนไม่ต่ำกว่า ~7V"),

        LowPower78L("78L09", "ไอซีเรกูเลเตอร์ +9V 100mA", vout: 9),

        LowPower78L("78L12", "ไอซีเรกูเลเตอร์ +12V 100mA", vout: 12,
            note: "เรียงขา OUT-GND-IN เหมือน 78L05 · ต้องป้อนไม่ต่ำกว่า ~14V"),

        // ── adjustable three-terminal ────────────────────────────────────────

        new PartDefinition
        {
            Key = "LM317", Prefix = "U", Name = "LM317", NameTh = "เรกูเลเตอร์ปรับค่าได้ 1.25–37V 1.5A",
            Mpn = "LM317T", Package = "TO-220", Pinout = "ADJ-OUT-IN",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = "LM317",
            DefaultValue = "5", Unit = "V", BodyWidth = 6, BodyHeight = 4,
            Params = Params((ParamKey.IoutMax, 1.5), (ParamKey.Dropout, 2)),
            NoteTh = "แรงดันออก = 1.25 × (1 + R2/R1) โดย R1 ต่อระหว่าง OUT กับ ADJ (ปกติ 240Ω) และ R2 จาก ADJ ลงกราวด์ · "
                   + "⚠ TO-220 เรียงขา ADJ-OUT-IN ไม่ใช่ IN-GND-OUT — เสียบแทน 7805 ไม่ได้ · "
                   + "ต้องมีโหลดอย่างน้อย ~10mA ตลอดเวลา ไม่งั้นแรงดันออกลอยสูงกว่าที่ตั้งไว้ · "
                   + "ข้อจำกัดคือส่วนต่าง Vin−Vout ต้องไม่เกิน 40V ไม่ใช่แรงดันเข้าสูงสุด — ป้อน 60V แล้วเอาต์พุต 30V ยังอยู่ในสเปก · "
                   + "ตอนจ่ายเต็ม 1.5A ต้องมีส่วนต่างราว 3V",
            Pins =
            [
                P("1", "ADJ", PinKind.Analog, PinSide.Bottom, 0, "ขาปรับ — กระแสไหลออก ~50µA"),
                P("2", "OUT", PinKind.Output, PinSide.Right, 0),
                P("3", "IN", PinKind.Power, PinSide.Left, 0),
            ],
        },

        new PartDefinition
        {
            Key = "LM337", Prefix = "U", Name = "LM337", NameTh = "เรกูเลเตอร์ปรับค่าได้ขั้วลบ -1.25 ถึง -37V 1.5A",
            Mpn = "LM337T", Package = "TO-220", Pinout = "ADJ-IN-OUT",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = "LM337",
            DefaultValue = "-5", Unit = "V", BodyWidth = 6, BodyHeight = 4,
            Params = Params((ParamKey.IoutMax, 1.5), (ParamKey.Dropout, 2)),
            NoteTh = "⚠ เรียงขา ADJ-IN-OUT — ไม่เหมือน LM317 ที่เป็น ADJ-OUT-IN ทั้งที่เป็นคู่กัน สลับตัวกันบนบอร์ดเดียวกันแล้วพัง · "
                   + "แรงดันออก = -1.25 × (1 + R2/R1) · ต้องมีโหลดขั้นต่ำเหมือน LM317",
            Pins =
            [
                P("1", "ADJ", PinKind.Analog, PinSide.Bottom, 0, "ขาปรับ"),
                P("2", "IN", PinKind.Power, PinSide.Left, 0, "ไฟเข้าขั้วลบ"),
                P("3", "OUT", PinKind.Output, PinSide.Right, 0),
            ],
        },

        // ── 1117 family LDO, SOT-223 / TO-220, pin 1-2-3 = GND-OUT-IN ────────

        Ldo1117("AMS1117-1.8", "ไอซีเรกูเลเตอร์ LDO 1.8V 1A", vout: 1.8, ioutMax: 1.0),

        Ldo1117("AMS1117-2.5", "ไอซีเรกูเลเตอร์ LDO 2.5V 1A", vout: 2.5, ioutMax: 1.0),

        Ldo1117("AMS1117-3.3", "ไอซีเรกูเลเตอร์ LDO 3.3V 1A", vout: 3.3, ioutMax: 1.0,
            note: "เบอร์ที่อยู่บนบอร์ด ESP/Arduino แทบทุกใบ · "
                + "ดรอปเอาต์ ~1.1V ที่โหลดเต็ม — ป้อนจากแบตลิเธียม 3.7V ทำ 3.3V ไม่ได้ ต้อง 4.5V ขึ้นไป · "
                + "กินไฟนิ่งเอง ~5mA ตลอดเวลา งานที่ต้องนอนด้วยแบตอย่าใช้เบอร์นี้ · "
                + "SOT-223 ขา 1=GND 2=OUT 3=IN ครีบใหญ่ต่อกับ OUT · "
                + "ต้องมี C ≥10µF ที่เอาต์พุต ไม่งั้นแกว่ง · "
                + "5V 1A ลงมา 3.3V คือความร้อน 1.7W บนตัวถังจิ๋ว อย่าดึงเต็มพิกัด"),

        Ldo1117("AMS1117-5.0", "ไอซีเรกูเลเตอร์ LDO 5V 1A", vout: 5.0, ioutMax: 1.0,
            note: "⚠ ต้องการไฟเข้าอย่างน้อย ~6.5V — ต่อจาก USB 5V แล้วเอาต์พุตไม่ถึง 5V "
                + "เป็นความผิดพลาดที่เจอบ่อยที่สุดของเบอร์นี้"),

        new PartDefinition
        {
            Key = "AMS1117-ADJ", Prefix = "U", Name = "AMS1117-ADJ", NameTh = "ไอซีเรกูเลเตอร์ LDO ปรับค่าได้ 1A",
            Mpn = "AMS1117-ADJ", Package = "SOT-223", Pinout = "ADJ-OUT-IN",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = "AMS1117-ADJ",
            DefaultValue = "3.3", Unit = "V", BodyWidth = 6, BodyHeight = 4,
            Params = Params((ParamKey.IoutMax, 1.0), (ParamKey.VinMax, 15), (ParamKey.Dropout, 1.1)),
            NoteTh = "แรงดันออก = 1.25 × (1 + R2/R1) · "
                   + "ขา 1 เป็น ADJ ไม่ใช่ GND — บอร์ดที่ออกแบบไว้สำหรับเบอร์ค่าคงที่ใส่ตัวนี้แทนไม่ได้",
            Pins =
            [
                P("1", "ADJ", PinKind.Analog, PinSide.Bottom, 0, "ขาปรับ"),
                P("2", "OUT", PinKind.Output, PinSide.Right, 0, "ต่อกับครีบระบายความร้อน"),
                P("3", "IN", PinKind.Power, PinSide.Left, 0),
            ],
        },

        new PartDefinition
        {
            Key = "LM1117-3.3", Prefix = "U", Name = "LM1117-3.3", NameTh = "ไอซีเรกูเลเตอร์ LDO 3.3V 800mA",
            Mpn = "LM1117T-3.3", Package = "SOT-223", Pinout = "GND-OUT-IN",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = "LM1117-3.3",
            BodyWidth = 6, BodyHeight = 4,
            Params = Params((ParamKey.Vout, 3.3), (ParamKey.IoutMax, 0.8), (ParamKey.Dropout, 1.2)),
            NoteTh = "ต้นแบบของตระกูล 1117 ขาเหมือน AMS1117 แต่จ่ายได้ 800mA ไม่ใช่ 1A — สลับเบอร์แล้วต้องคิดกระแสใหม่ · "
                   + "ต้องมี C แทนทาลัมหรืออิเล็กโทรไลต์ ≥10µF ที่เอาต์พุตเพื่อความเสถียร",
            Pins =
            [
                P("1", "GND", PinKind.Ground, PinSide.Bottom, 0),
                P("2", "OUT", PinKind.Output, PinSide.Right, 0, "ต่อกับครีบระบายความร้อน"),
                P("3", "IN", PinKind.Power, PinSide.Left, 0),
            ],
        },

        new PartDefinition
        {
            Key = "LD1117V33", Prefix = "U", Name = "LD1117V33", NameTh = "ไอซีเรกูเลเตอร์ LDO 3.3V 800mA ตัวถัง TO-220",
            Mpn = "LD1117V33", Package = "TO-220", Pinout = "GND-OUT-IN",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = "LD1117-3.3",
            BodyWidth = 6, BodyHeight = 4,
            Params = Params((ParamKey.Vout, 3.3), (ParamKey.IoutMax, 0.8), (ParamKey.Dropout, 1.2)),
            NoteTh = "⚠ ตัวถัง TO-220 หน้าตาเหมือน 7805 เป๊ะ แต่เรียงขา GND-OUT-IN ไม่ใช่ IN-GND-OUT — "
                   + "เป็นกับดักที่เจอบ่อยเวลาซ่อมของ ตรวจขาก่อนบัดกรีทุกครั้ง · "
                   + "ดรอปเอาต์ ~1.2V ทำ 3.3V จาก 5V ได้สบาย แต่จากแบต 3.7V ไม่ได้",
            Pins =
            [
                P("1", "GND", PinKind.Ground, PinSide.Bottom, 0),
                P("2", "OUT", PinKind.Output, PinSide.Right, 0, "ต่อกับครีบระบายความร้อน"),
                P("3", "IN", PinKind.Power, PinSide.Left, 0),
            ],
        },

        // ── micropower LDO, TO-92 ────────────────────────────────────────────

        new PartDefinition
        {
            Key = "LP2950-3.3", Prefix = "U", Name = "LP2950-3.3", NameTh = "ไอซีเรกูเลเตอร์ LDO 3.3V 100mA กินไฟนิ่งต่ำ",
            Mpn = "LP2950CZ-3.3", Package = "TO-92", Pinout = "OUT-GND-IN",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = "LP2950-3.3",
            BodyWidth = 6, BodyHeight = 4,
            Params = Params((ParamKey.Vout, 3.3), (ParamKey.IoutMax, 0.1),
                            (ParamKey.VinMax, 30), (ParamKey.Dropout, 0.38)),
            NoteTh = "ออกแบบมาให้ใส่แทน 78L05 ได้ตรงขา — TO-92 เรียงขา OUT-GND-IN เหมือนกัน · "
                   + "ดรอปเอาต์แค่ ~0.4V และกินไฟนิ่งเอง ~75µA เหมาะกับงานแบต · "
                   + "จ่ายได้ 100mA เท่านั้น · LP2951 เป็นตัวเดียวกันแต่ตัวถัง 8 ขาและปรับแรงดันได้",
            Pins =
            [
                P("1", "OUT", PinKind.Output, PinSide.Right, 0),
                P("2", "GND", PinKind.Ground, PinSide.Bottom, 0),
                P("3", "IN", PinKind.Power, PinSide.Left, 0),
            ],
        },

        new PartDefinition
        {
            Key = "LP2950-5.0", Prefix = "U", Name = "LP2950-5.0", NameTh = "ไอซีเรกูเลเตอร์ LDO 5V 100mA กินไฟนิ่งต่ำ",
            Mpn = "LP2950CZ-5.0", Package = "TO-92", Pinout = "OUT-GND-IN",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = "LP2950-5.0",
            BodyWidth = 6, BodyHeight = 4,
            Params = Params((ParamKey.Vout, 5.0), (ParamKey.IoutMax, 0.1),
                            (ParamKey.VinMax, 30), (ParamKey.Dropout, 0.38)),
            NoteTh = "ใส่แทน 78L05 ได้ตรงขา (OUT-GND-IN) แต่ต้องการส่วนต่างแค่ ~0.4V — "
                   + "จ่าย 5V จากแบต 6V ที่ใกล้หมดได้ ในขณะที่ 78L05 ทำไม่ได้",
            Pins =
            [
                P("1", "OUT", PinKind.Output, PinSide.Right, 0),
                P("2", "GND", PinKind.Ground, PinSide.Bottom, 0),
                P("3", "IN", PinKind.Power, PinSide.Left, 0),
            ],
        },

        new PartDefinition
        {
            Key = "MCP1700-3302E", Prefix = "U", Name = "MCP1700-3.3", NameTh = "ไอซีเรกูเลเตอร์ LDO 3.3V 250mA กินไฟนิ่ง 1.6µA",
            Mpn = "MCP1700-3302E/TO", Package = "TO-92", Pinout = "GND-IN-OUT",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = "MCP1700-3.3",
            BodyWidth = 6, BodyHeight = 4,
            Params = Params((ParamKey.Vout, 3.3), (ParamKey.IoutMax, 0.25),
                            (ParamKey.VinMax, 6.0), (ParamKey.Dropout, 0.178)),
            NoteTh = "⚠ ไฟเข้าสูงสุด 6V เท่านั้น — ป้อน 9V หรือ 12V พังทันที เป็นข้อแตกต่างสำคัญจาก 78L05 ที่ทน 30V · "
                   + "กินไฟนิ่งเอง ~1.6µA จ่ายจากแบตลิเธียม 3.7V ทำ 3.3V ได้เพราะดรอปเอาต์แค่ ~0.18V ที่ 250mA · "
                   + "ต้องมี C เซรามิก 1µF ทั้งขาเข้าและขาออก · "
                   + "MCP1702 เป็นคนละลำดับขากับ MCP1700 อย่าสลับเบอร์โดยไม่เปิดดาต้าชีตของเบอร์ที่ซื้อมา",
            Pins =
            [
                P("1", "GND", PinKind.Ground, PinSide.Bottom, 0),
                P("2", "IN", PinKind.Power, PinSide.Left, 0),
                P("3", "OUT", PinKind.Output, PinSide.Right, 0),
            ],
        },

        // ── switching modules ────────────────────────────────────────────────
        // Modelled as the board people actually clip onto a breadboard, not the bare
        // controller IC. SPICE cannot solve a switching loop from these figures, so they
        // are behavioural: a supply load and a defined output.

        new PartDefinition
        {
            Key = "MOD-LM2596", Prefix = "U", Name = "LM2596 buck module",
            NameTh = "โมดูลลดแรงดันแบบสวิตชิ่ง LM2596 ปรับค่าได้",
            Mpn = "LM2596S-ADJ", Package = "module 4-pin",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            DefaultValue = "5", Unit = "V", BodyWidth = 8, BodyHeight = 6,
            Params = Params((ParamKey.IoutMax, 2.0), (ParamKey.VinMax, 35), (ParamKey.Dropout, 1.5)),
            NoteTh = "ต้องหมุนตั้งแรงดันออกด้วยตัวปรับ (หลายรอบ) ก่อนต่อโหลดทุกครั้ง — มาจากโรงงานตั้งไว้เท่าไรไม่แน่นอน · "
                   + "ลดแรงดันได้อย่างเดียว เพิ่มไม่ได้ และไฟเข้าต้องสูงกว่าเอาต์พุตอย่างน้อย ~1.5V · "
                   + "ไฟเข้าต่ำสุด 4.5V สูงสุด 35V · "
                   + "3A ที่โฆษณาคือค่าพีค ใช้ต่อเนื่องจริงราว 2A และต้องมีลมผ่านหรือฮีตซิงก์ · "
                   + "⚠ ไม่มีวงจรกันต่อกลับขั้ว ต่อไฟกลับครั้งเดียวจบ · "
                   + "ประสิทธิภาพ ~85% ไม่ร้อนเหมือนเรกูเลเตอร์เชิงเส้น แต่มีสัญญาณรบกวนที่เอาต์พุต ไม่เหมาะกับภาคอนาล็อกละเอียด",
            Pins =
            [
                P("1", "IN+", PinKind.Power, PinSide.Left, 0),
                P("2", "IN-", PinKind.Ground, PinSide.Left, 1),
                P("3", "OUT+", PinKind.Output, PinSide.Right, 0),
                P("4", "OUT-", PinKind.Ground, PinSide.Right, 1, "ต่อถึงกับ IN- ภายในโมดูล ไม่แยกกราวด์"),
            ],
        },

        new PartDefinition
        {
            Key = "MOD-MP1584", Prefix = "U", Name = "MP1584EN buck module",
            NameTh = "โมดูลลดแรงดันตัวจิ๋ว MP1584EN ปรับค่าได้",
            Mpn = "MP1584EN", Package = "module 4-pin",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            DefaultValue = "5", Unit = "V", BodyWidth = 8, BodyHeight = 6,
            Params = Params((ParamKey.IoutMax, 3.0), (ParamKey.VinMax, 28)),
            NoteTh = "ไฟเข้า 4.5–28V สวิตช์ที่ 1.5MHz จึงใช้ตัวเหนี่ยวนำเล็กและบอร์ดจิ๋วได้ · "
                   + "3A คือค่าสูงสุดของชิป ใช้ต่อเนื่องราว 2A บอร์ดก็ร้อนจนจับไม่ได้แล้ว · "
                   + "ตัวปรับเป็นแบบรอบเดียว หมุนนิดเดียวแรงดันกระโดด ตั้งค่าโดยไม่มีโหลดแล้ววัดก่อนต่อของ · "
                   + "โมดูล mini-360 ที่ขายกันคือชิปตัวเดียวกัน",
            Pins =
            [
                P("1", "IN+", PinKind.Power, PinSide.Left, 0),
                P("2", "IN-", PinKind.Ground, PinSide.Left, 1),
                P("3", "OUT+", PinKind.Output, PinSide.Right, 0),
                P("4", "OUT-", PinKind.Ground, PinSide.Right, 1),
            ],
        },

        new PartDefinition
        {
            Key = "MOD-XL4015", Prefix = "U", Name = "XL4015 buck module",
            NameTh = "โมดูลลดแรงดัน XL4015 5A ปรับค่าได้",
            Mpn = "XL4015", Package = "module 4-pin",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            DefaultValue = "12", Unit = "V", BodyWidth = 8, BodyHeight = 6,
            Params = Params((ParamKey.IoutMax, 5.0), (ParamKey.VinMax, 36)),
            NoteTh = "⚠ ไฟเข้าต่ำสุด 8V — ต่อจาก 5V หรือ 6V ไม่ทำงาน เป็นข้อผิดพลาดที่เจอบ่อยที่สุดของโมดูลนี้ · "
                   + "จ่ายได้ถึง 5A แต่ต้องมีฮีตซิงก์และลม ต่อเนื่องจริงราว 3A · "
                   + "บางรุ่นมีวงจรจำกัดกระแส (CC) เป็นตัวปรับอีกตัวหนึ่ง ใช้ชาร์จแบตได้ · "
                   + "ลดแรงดันได้อย่างเดียว",
            Pins =
            [
                P("1", "IN+", PinKind.Power, PinSide.Left, 0),
                P("2", "IN-", PinKind.Ground, PinSide.Left, 1),
                P("3", "OUT+", PinKind.Output, PinSide.Right, 0),
                P("4", "OUT-", PinKind.Ground, PinSide.Right, 1),
            ],
        },

        new PartDefinition
        {
            Key = "MOD-MT3608", Prefix = "U", Name = "MT3608 boost module",
            NameTh = "โมดูลเพิ่มแรงดัน (บูสต์) MT3608 ปรับค่าได้",
            Mpn = "MT3608", Package = "module 4-pin",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            DefaultValue = "12", Unit = "V", BodyWidth = 8, BodyHeight = 6,
            Params = Params((ParamKey.VinMax, 24)),
            NoteTh = "เพิ่มแรงดันได้อย่างเดียว ไฟเข้า 2–24V ออกได้ถึง ~28V และเอาต์พุตต้องสูงกว่าอินพุตเสมอ · "
                   + "⚠ วงจรบูสต์ไม่ตัดทางเดินไฟ แม้ไม่ทำงานไฟเข้ายังทะลุถึงเอาต์พุตผ่านตัวเหนี่ยวนำและไดโอด — "
                   + "ห้ามใช้เป็นสวิตช์ตัดไฟ และห้ามลัดวงจรขาออก · "
                   + "กระแสออกลดลงตามอัตราการยกแรงดัน จาก 3.7V ไป 12V ได้จริงไม่กี่ร้อย mA ไม่ใช่ 2A ตามที่ร้านเขียน · "
                   + "ไม่มีวงจรกันต่อกลับขั้ว",
            Pins =
            [
                P("1", "IN+", PinKind.Power, PinSide.Left, 0),
                P("2", "IN-", PinKind.Ground, PinSide.Left, 1),
                P("3", "OUT+", PinKind.Output, PinSide.Right, 0),
                P("4", "OUT-", PinKind.Ground, PinSide.Right, 1),
            ],
        },
    ];

    // ── local shapes ─────────────────────────────────────────────────────────
    // Three families whose physical pin order is not IN-GND-OUT. CatalogBuilder.Regulator
    // numbers its pins 1/2/3 in electrical order, which would print the wrong number
    // against the wrong leg for these — so they get their own shape rather than a note
    // apologising for the numbering.

    /// <summary>A 79xx negative fixed regulator: TO-220, pin 1-2-3 = GND, IN, OUT.</summary>
    private static PartDefinition Negative(
        string mpn, string nameTh, double vout, double vinMax, string? note = null) => new()
    {
        Key = mpn, Prefix = "U", Name = mpn, NameTh = nameTh, Mpn = mpn,
        Package = "TO-220", Pinout = "GND-IN-OUT",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = mpn,
        BodyWidth = 6, BodyHeight = 4, NoteTh = note,
        Params = Params((ParamKey.Vout, vout), (ParamKey.IoutMax, 1.5),
                        (ParamKey.VinMax, vinMax), (ParamKey.Dropout, 2)),
        Pins =
        [
            P("1", "GND", PinKind.Ground, PinSide.Bottom, 0),
            P("2", "IN", PinKind.Power, PinSide.Left, 0, "ไฟเข้าขั้วลบ · ต่อถึงครีบระบายความร้อน"),
            P("3", "OUT", PinKind.Output, PinSide.Right, 0),
        ],
    };

    /// <summary>A 78Lxx in TO-92: pin 1-2-3 = OUT, GND, IN — the reverse of the TO-220 part.</summary>
    private static PartDefinition LowPower78L(
        string mpn, string nameTh, double vout, string? note = null) => new()
    {
        Key = mpn, Prefix = "U", Name = mpn, NameTh = nameTh, Mpn = mpn,
        Package = "TO-92", Pinout = "OUT-GND-IN",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = mpn,
        BodyWidth = 6, BodyHeight = 4, NoteTh = note,
        Params = Params((ParamKey.Vout, vout), (ParamKey.IoutMax, 0.1),
                        (ParamKey.VinMax, 30), (ParamKey.Dropout, 1.7)),
        Pins =
        [
            P("1", "OUT", PinKind.Output, PinSide.Right, 0),
            P("2", "GND", PinKind.Ground, PinSide.Bottom, 0),
            P("3", "IN", PinKind.Power, PinSide.Left, 0),
        ],
    };

    /// <summary>A 1117-family LDO in SOT-223: pin 1-2-3 = GND, OUT, IN, tab tied to OUT.</summary>
    private static PartDefinition Ldo1117(
        string mpn, string nameTh, double vout, double ioutMax, string? note = null) => new()
    {
        Key = mpn, Prefix = "U", Name = mpn, NameTh = nameTh, Mpn = mpn,
        Package = "SOT-223", Pinout = "GND-OUT-IN",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit, SpiceModel = mpn,
        BodyWidth = 6, BodyHeight = 4, NoteTh = note,
        Params = Params((ParamKey.Vout, vout), (ParamKey.IoutMax, ioutMax),
                        (ParamKey.VinMax, 15), (ParamKey.Dropout, 1.1)),
        Pins =
        [
            P("1", "GND", PinKind.Ground, PinSide.Bottom, 0),
            P("2", "OUT", PinKind.Output, PinSide.Right, 0, "ต่อถึงครีบระบายความร้อน"),
            P("3", "IN", PinKind.Power, PinSide.Left, 0),
        ],
    };
}
