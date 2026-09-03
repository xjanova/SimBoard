namespace SimBoard.Document;

/// <summary>
/// The parts the editor can place, with real pin data.
///
/// Pin numbering and names follow the manufacturer datasheet, because that is what the
/// person at the bench is reading. Where a module exists in incompatible pin orders from
/// different sellers — and several of the common IoT breakouts do — the variant modelled
/// here is named in <see cref="PartDefinition.NoteTh"/> rather than silently picked.
///
/// Analog parts carry a SPICE model and simulate for real. Digital parts carry a
/// <see cref="DigitalSpec"/> instead: SPICE cannot run firmware, so what the tool knows
/// about an ESP32 is its electrical envelope and bus, which is what actually decides
/// whether the board around it works.
/// </summary>
public static class PartCatalog
{
    private static Pin P(string n, string name, PinKind k, PinSide s, int slot, string? d = null)
        => new(n, name, k, s, slot, d);

    // ── passives ─────────────────────────────────────────────────────────

    private static readonly PartDefinition Resistor = new()
    {
        Key = "R", Prefix = "R", Name = "Resistor", NameTh = "ตัวต้านทาน",
        Symbol = SymbolShape.Box, Spice = SpiceKind.Primitive,
        DefaultValue = "10k", Unit = "Ω", BodyWidth = 4, BodyHeight = 2,
        Pins = [P("1", "A", PinKind.Passive, PinSide.Left, 0), P("2", "B", PinKind.Passive, PinSide.Right, 0)],
    };

    private static readonly PartDefinition Capacitor = new()
    {
        Key = "C", Prefix = "C", Name = "Capacitor", NameTh = "ตัวเก็บประจุ",
        Symbol = SymbolShape.CapacitorNonPolar, Spice = SpiceKind.Primitive,
        DefaultValue = "100n", Unit = "F", BodyWidth = 3, BodyHeight = 2,
        Pins = [P("1", "A", PinKind.Passive, PinSide.Left, 0), P("2", "B", PinKind.Passive, PinSide.Right, 0)],
    };

    private static readonly PartDefinition CapacitorPolarised = new()
    {
        Key = "C-ELEC", Prefix = "C", Name = "Electrolytic capacitor", NameTh = "ตัวเก็บประจุอิเล็กโทรไลต์",
        Symbol = SymbolShape.CapacitorPolarised, Spice = SpiceKind.Primitive,
        DefaultValue = "100u", Unit = "F", BodyWidth = 3, BodyHeight = 2,
        NoteTh = "มีขั้ว — ใส่กลับขั้วแล้วระเบิด ขายาว = บวก แถบขาวบนตัวถัง = ลบ",
        Pins = [P("1", "+", PinKind.Passive, PinSide.Left, 0), P("2", "-", PinKind.Passive, PinSide.Right, 0)],
    };

    private static readonly PartDefinition Inductor = new()
    {
        Key = "L", Prefix = "L", Name = "Inductor", NameTh = "ตัวเหนี่ยวนำ",
        Symbol = SymbolShape.Inductor, Spice = SpiceKind.Primitive,
        DefaultValue = "100u", Unit = "H", BodyWidth = 4, BodyHeight = 2,
        Pins = [P("1", "A", PinKind.Passive, PinSide.Left, 0), P("2", "B", PinKind.Passive, PinSide.Right, 0)],
    };

    private static readonly PartDefinition Potentiometer = new()
    {
        Key = "POT", Prefix = "RV", Name = "Potentiometer", NameTh = "ตัวต้านทานปรับค่า",
        Symbol = SymbolShape.Box, Spice = SpiceKind.Primitive,
        DefaultValue = "10k", Unit = "Ω", BodyWidth = 4, BodyHeight = 3,
        Pins =
        [
            P("1", "A", PinKind.Passive, PinSide.Left, 0),
            P("2", "W", PinKind.Passive, PinSide.Top, 0, "ขากลาง — ตัวปรับ"),
            P("3", "B", PinKind.Passive, PinSide.Right, 0),
        ],
    };

    // ── diodes ───────────────────────────────────────────────────────────

    private static PartDefinition Diode(string key, string mpn, string th, string model, string? note = null) => new()
    {
        Key = key, Prefix = "D", Name = mpn, NameTh = th, Mpn = mpn,
        Symbol = SymbolShape.Diode, Spice = SpiceKind.Primitive,
        SpiceModel = model, BodyWidth = 3, BodyHeight = 2, NoteTh = note,
        Pins = [P("1", "A", PinKind.Passive, PinSide.Left, 0, "แอโนด"),
                P("2", "K", PinKind.Passive, PinSide.Right, 0, "แคโทด — แถบคาดบนตัวถัง")],
    };

    private static readonly PartDefinition Led = new()
    {
        Key = "LED", Prefix = "D", Name = "LED", NameTh = "แอลอีดี",
        Symbol = SymbolShape.Led, Spice = SpiceKind.Primitive,
        SpiceModel = "LED_RED", DefaultValue = "RED", BodyWidth = 3, BodyHeight = 2,
        NoteTh = "ต้องมีตัวต้านทานอนุกรมเสมอ — ต่อตรงเข้าไฟพังทันที ขายาว = แอโนด",
        Pins = [P("1", "A", PinKind.Passive, PinSide.Left, 0, "แอโนด — ขายาว"),
                P("2", "K", PinKind.Passive, PinSide.Right, 0, "แคโทด — ขาสั้น ด้านตัดเรียบ")],
    };

    // ── transistors ──────────────────────────────────────────────────────

    private static PartDefinition Bjt(string key, string mpn, string th, SymbolShape sym, string model) => new()
    {
        Key = key, Prefix = "Q", Name = mpn, NameTh = th, Mpn = mpn, Package = "TO-92",
        Symbol = sym, Spice = SpiceKind.Primitive, SpiceModel = model,
        BodyWidth = 4, BodyHeight = 4,
        NoteTh = "ลำดับขาต่างกันตามตระกูล — 2N3904/2N3906 เป็น EBC ส่วน BC547/BC557 เป็น CBE",
        Pins =
        [
            P("1", "B", PinKind.Input, PinSide.Left, 1, "เบส"),
            P("2", "C", PinKind.Passive, PinSide.Top, 0, "คอลเลกเตอร์"),
            P("3", "E", PinKind.Passive, PinSide.Bottom, 0, "อิมิตเตอร์"),
        ],
    };

    private static PartDefinition Mosfet(string key, string mpn, string th, SymbolShape sym, string model) => new()
    {
        Key = key, Prefix = "Q", Name = mpn, NameTh = th, Mpn = mpn, Package = "TO-220",
        Symbol = sym, Spice = SpiceKind.Primitive, SpiceModel = model,
        BodyWidth = 4, BodyHeight = 4,
        Pins =
        [
            P("1", "G", PinKind.Input, PinSide.Left, 1, "เกต"),
            P("2", "D", PinKind.Passive, PinSide.Top, 0, "เดรน"),
            P("3", "S", PinKind.Passive, PinSide.Bottom, 0, "ซอร์ส"),
        ],
    };

    // ── sources and ground ───────────────────────────────────────────────

    private static readonly PartDefinition Vdc = new()
    {
        Key = "VDC", Prefix = "V", Name = "DC source", NameTh = "แหล่งจ่ายไฟตรง",
        Symbol = SymbolShape.VoltageSource, Spice = SpiceKind.Primitive,
        DefaultValue = "5", Unit = "V", BodyWidth = 3, BodyHeight = 4,
        Pins = [P("1", "+", PinKind.Power, PinSide.Top, 0), P("2", "-", PinKind.Ground, PinSide.Bottom, 0)],
    };

    private static readonly PartDefinition Vpulse = new()
    {
        Key = "VPULSE", Prefix = "V", Name = "Pulse source", NameTh = "แหล่งจ่ายพัลส์",
        Symbol = SymbolShape.VoltageSource, Spice = SpiceKind.Primitive,
        DefaultValue = "PULSE(0 5 0 1u 1u 500u 1m)", BodyWidth = 3, BodyHeight = 4,
        NoteTh = "PULSE(ต่ำ สูง หน่วง ขาขึ้น ขาลง กว้าง คาบ)",
        Pins = [P("1", "+", PinKind.Output, PinSide.Top, 0), P("2", "-", PinKind.Ground, PinSide.Bottom, 0)],
    };

    private static readonly PartDefinition Ground = new()
    {
        Key = "GND", Prefix = "GND", Name = "Ground", NameTh = "กราวด์",
        Symbol = SymbolShape.Ground, Spice = SpiceKind.None,
        BodyWidth = 2, BodyHeight = 2,
        NoteTh = "ทุกวงจรต้องมีกราวด์อย่างน้อยหนึ่งจุด ไม่งั้นซิมูเลเตอร์แก้สมการไม่ได้",
        Pins = [P("1", "GND", PinKind.Ground, PinSide.Top, 0)],
    };

    // ── analog ICs ───────────────────────────────────────────────────────

    private static readonly PartDefinition Ne555 = new()
    {
        Key = "NE555", Prefix = "U", Name = "NE555", NameTh = "ไอซีตั้งเวลา 555",
        Mpn = "NE555P", Package = "DIP-8",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit,
        SpiceModel = "UA555", SpiceLibrary = "ua555.lib",
        BodyWidth = 8, BodyHeight = 8,
        NoteTh = "ขา 5 (CTRL) ต่อ C 10n ลงกราวด์ — ปล่อยลอยหรือต่อผิดจะทำให้จุด trip เพี้ยนทั้งวงจร",
        Pins =
        [
            P("1", "GND", PinKind.Ground, PinSide.Left, 0),
            P("2", "TRIG", PinKind.Input, PinSide.Left, 1, "ทริกเมื่อต่ำกว่า 1/3 Vcc"),
            P("3", "OUT", PinKind.Output, PinSide.Left, 2),
            P("4", "RESET", PinKind.Input, PinSide.Left, 3, "แอกทีฟต่ำ — ไม่ใช้ให้ต่อ Vcc"),
            P("5", "CTRL", PinKind.Analog, PinSide.Right, 3, "แรงดันควบคุม — ปกติต่อ C 10n ลงกราวด์"),
            P("6", "THRES", PinKind.Input, PinSide.Right, 2, "รีเซ็ตเมื่อสูงกว่า 2/3 Vcc"),
            P("7", "DISCH", PinKind.OpenDrain, PinSide.Right, 1),
            P("8", "VCC", PinKind.Power, PinSide.Right, 0),
        ],
    };

    private static readonly PartDefinition Lm358 = new()
    {
        Key = "LM358", Prefix = "U", Name = "LM358", NameTh = "ออปแอมป์คู่",
        Mpn = "LM358N", Package = "DIP-8",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit,
        SpiceModel = "LM358", SpiceLibrary = "lm358.lib",
        BodyWidth = 8, BodyHeight = 8,
        Pins =
        [
            P("1", "OUT1", PinKind.Output, PinSide.Left, 0),
            P("2", "IN1-", PinKind.Analog, PinSide.Left, 1),
            P("3", "IN1+", PinKind.Analog, PinSide.Left, 2),
            P("4", "V-", PinKind.Ground, PinSide.Left, 3),
            P("5", "IN2+", PinKind.Analog, PinSide.Right, 3),
            P("6", "IN2-", PinKind.Analog, PinSide.Right, 2),
            P("7", "OUT2", PinKind.Output, PinSide.Right, 1),
            P("8", "V+", PinKind.Power, PinSide.Right, 0),
        ],
    };

    private static PartDefinition Regulator(string key, string mpn, string th, string[] pinNames, string? note) => new()
    {
        Key = key, Prefix = "U", Name = mpn, NameTh = th, Mpn = mpn, Package = "TO-220",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Subcircuit,
        SpiceModel = mpn, BodyWidth = 6, BodyHeight = 4, NoteTh = note,
        Pins =
        [
            P("1", pinNames[0], PinKind.Power, PinSide.Left, 0),
            P("2", pinNames[1], PinKind.Ground, PinSide.Bottom, 0),
            P("3", pinNames[2], PinKind.Output, PinSide.Right, 0),
        ],
    };

    // ── microcontrollers ─────────────────────────────────────────────────

    /// <summary>
    /// ESP32 DevKit v1, the 30-pin board. Modelled as the board, not the bare WROOM
    /// module, because that is what people actually place on a breadboard.
    /// </summary>
    private static readonly PartDefinition Esp32 = new()
    {
        Key = "ESP32-DEVKIT", Prefix = "U", Name = "ESP32 DevKit v1", NameTh = "บอร์ด ESP32 DevKit v1",
        Mpn = "ESP32-WROOM-32", Package = "DevKit 30-pin",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 10, BodyHeight = 16,
        Digital = new DigitalSpec(
            VccMin: 3.0, VccMax: 3.6, VccTypical: 3.3, Icc: 0.240,
            Vih: 2.475, Vil: 0.825, IoMax: 0.040, Bus: Bus.None),
        NoteTh = "ขา I/O ทน 3.3V เท่านั้น ต่อ 5V เข้าตรงจะพัง · GPIO34-39 เป็นอินพุตอย่างเดียว ไม่มีพูลอัปในตัว · "
               + "GPIO0/2/12/15 เป็นขา strapping ต่อผิดตอนบูตแล้วบอร์ดไม่ขึ้น · VIN รับ 5V ผ่านเรกูเลเตอร์บนบอร์ด",
        Pins =
        [
            P("1", "EN", PinKind.Input, PinSide.Left, 0, "รีเซ็ต แอกทีฟต่ำ"),
            P("2", "GPIO36", PinKind.Analog, PinSide.Left, 1, "VP · อินพุตอย่างเดียว · ADC1_0"),
            P("3", "GPIO39", PinKind.Analog, PinSide.Left, 2, "VN · อินพุตอย่างเดียว · ADC1_3"),
            P("4", "GPIO34", PinKind.Analog, PinSide.Left, 3, "อินพุตอย่างเดียว · ADC1_6"),
            P("5", "GPIO35", PinKind.Analog, PinSide.Left, 4, "อินพุตอย่างเดียว · ADC1_7"),
            P("6", "GPIO32", PinKind.Bidirectional, PinSide.Left, 5, "ADC1_4 · touch"),
            P("7", "GPIO33", PinKind.Bidirectional, PinSide.Left, 6, "ADC1_5 · touch"),
            P("8", "GPIO25", PinKind.Bidirectional, PinSide.Left, 7, "DAC1"),
            P("9", "GPIO26", PinKind.Bidirectional, PinSide.Left, 8, "DAC2"),
            P("10", "GPIO27", PinKind.Bidirectional, PinSide.Left, 9),
            P("11", "GPIO14", PinKind.Bidirectional, PinSide.Left, 10),
            P("12", "GPIO12", PinKind.Bidirectional, PinSide.Left, 11, "strapping — ห้ามดึงสูงตอนบูต"),
            P("13", "GND", PinKind.Ground, PinSide.Left, 12),
            P("14", "GPIO13", PinKind.Bidirectional, PinSide.Left, 13),
            P("15", "VIN", PinKind.Power, PinSide.Left, 14, "5V เข้าเรกูเลเตอร์บนบอร์ด"),

            P("16", "3V3", PinKind.Power, PinSide.Right, 0),
            P("17", "GND", PinKind.Ground, PinSide.Right, 1),
            P("18", "GPIO15", PinKind.Bidirectional, PinSide.Right, 2, "strapping"),
            P("19", "GPIO2", PinKind.Bidirectional, PinSide.Right, 3, "strapping · LED บนบอร์ด"),
            P("20", "GPIO0", PinKind.Bidirectional, PinSide.Right, 4, "strapping · ปุ่ม BOOT"),
            P("21", "GPIO4", PinKind.Bidirectional, PinSide.Right, 5),
            P("22", "GPIO16", PinKind.Bidirectional, PinSide.Right, 6, "RX2"),
            P("23", "GPIO17", PinKind.Bidirectional, PinSide.Right, 7, "TX2"),
            P("24", "GPIO5", PinKind.Bidirectional, PinSide.Right, 8, "VSPI CS"),
            P("25", "GPIO18", PinKind.Bidirectional, PinSide.Right, 9, "VSPI SCK"),
            P("26", "GPIO19", PinKind.Bidirectional, PinSide.Right, 10, "VSPI MISO"),
            P("27", "GPIO21", PinKind.Bidirectional, PinSide.Right, 11, "I²C SDA (ค่าเริ่มต้น)"),
            P("28", "GPIO3", PinKind.Bidirectional, PinSide.Right, 12, "RX0 — ใช้ตอนอัปโหลด"),
            P("29", "GPIO1", PinKind.Bidirectional, PinSide.Right, 13, "TX0 — ใช้ตอนอัปโหลด"),
            P("30", "GPIO22", PinKind.Bidirectional, PinSide.Right, 14, "I²C SCL (ค่าเริ่มต้น)"),
        ],
    };

    private static readonly PartDefinition Atmega328 = new()
    {
        Key = "ATMEGA328P", Prefix = "U", Name = "ATmega328P", NameTh = "ไมโครคอนโทรลเลอร์ ATmega328P",
        Mpn = "ATMEGA328P-PU", Package = "DIP-28",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 10, BodyHeight = 15,
        Digital = new DigitalSpec(1.8, 5.5, 5.0, Icc: 0.012, Vih: 3.0, Vil: 1.5, IoMax: 0.040),
        NoteTh = "ขา I/O ทนได้ 40 mA ต่อขา แต่รวมทั้งชิปห้ามเกิน 200 mA · AVCC ต้องต่อไฟด้วยแม้ไม่ใช้ ADC",
        Pins =
        [
            P("1", "RESET", PinKind.Input, PinSide.Left, 0, "PC6 · แอกทีฟต่ำ ต้องมีพูลอัป"),
            P("2", "PD0", PinKind.Bidirectional, PinSide.Left, 1, "RXD"),
            P("3", "PD1", PinKind.Bidirectional, PinSide.Left, 2, "TXD"),
            P("4", "PD2", PinKind.Bidirectional, PinSide.Left, 3, "INT0"),
            P("5", "PD3", PinKind.Bidirectional, PinSide.Left, 4, "INT1 · PWM"),
            P("6", "PD4", PinKind.Bidirectional, PinSide.Left, 5),
            P("7", "VCC", PinKind.Power, PinSide.Left, 6),
            P("8", "GND", PinKind.Ground, PinSide.Left, 7),
            P("9", "PB6", PinKind.Bidirectional, PinSide.Left, 8, "XTAL1"),
            P("10", "PB7", PinKind.Bidirectional, PinSide.Left, 9, "XTAL2"),
            P("11", "PD5", PinKind.Bidirectional, PinSide.Left, 10, "PWM"),
            P("12", "PD6", PinKind.Bidirectional, PinSide.Left, 11, "PWM"),
            P("13", "PD7", PinKind.Bidirectional, PinSide.Left, 12),
            P("14", "PB0", PinKind.Bidirectional, PinSide.Left, 13),

            P("15", "PB1", PinKind.Bidirectional, PinSide.Right, 13, "PWM"),
            P("16", "PB2", PinKind.Bidirectional, PinSide.Right, 12, "SS · PWM"),
            P("17", "PB3", PinKind.Bidirectional, PinSide.Right, 11, "MOSI · PWM"),
            P("18", "PB4", PinKind.Bidirectional, PinSide.Right, 10, "MISO"),
            P("19", "PB5", PinKind.Bidirectional, PinSide.Right, 9, "SCK · LED บนบอร์ด Arduino"),
            P("20", "AVCC", PinKind.Power, PinSide.Right, 8, "ต้องต่อไฟเสมอ"),
            P("21", "AREF", PinKind.Analog, PinSide.Right, 7),
            P("22", "GND", PinKind.Ground, PinSide.Right, 6),
            P("23", "PC0", PinKind.Analog, PinSide.Right, 5, "A0"),
            P("24", "PC1", PinKind.Analog, PinSide.Right, 4, "A1"),
            P("25", "PC2", PinKind.Analog, PinSide.Right, 3, "A2"),
            P("26", "PC3", PinKind.Analog, PinSide.Right, 2, "A3"),
            P("27", "PC4", PinKind.Bidirectional, PinSide.Right, 1, "A4 · SDA"),
            P("28", "PC5", PinKind.Bidirectional, PinSide.Right, 0, "A5 · SCL"),
        ],
    };

    // ── sensors and IoT modules ──────────────────────────────────────────

    private static readonly PartDefinition Ds18b20 = new()
    {
        Key = "DS18B20", Prefix = "U", Name = "DS18B20", NameTh = "เซนเซอร์อุณหภูมิ DS18B20",
        Mpn = "DS18B20", Package = "TO-92",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 7, BodyHeight = 5,
        Digital = new DigitalSpec(3.0, 5.5, 5.0, Icc: 0.0015, Bus: Bus.OneWire),
        NoteTh = "ต้องมีพูลอัป 4.7k ที่ขา DQ ไปไฟบวก ไม่งั้นอ่านไม่ได้ · หันด้านแบนเข้าหาตัว ขาลง ซ้ายไปขวา = GND DQ VDD",
        Pins =
        [
            P("1", "GND", PinKind.Ground, PinSide.Left, 0),
            P("2", "DQ", PinKind.OpenDrain, PinSide.Right, 0, "1-Wire · ต้องมีพูลอัป 4.7k"),
            P("3", "VDD", PinKind.Power, PinSide.Right, 1, "ต่อ GND ได้ถ้าใช้โหมดปรสิต"),
        ],
    };

    private static readonly PartDefinition Dht22 = new()
    {
        Key = "DHT22", Prefix = "U", Name = "DHT22 / AM2302", NameTh = "เซนเซอร์อุณหภูมิ-ความชื้น DHT22",
        Mpn = "AM2302", Package = "4-pin",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 7, BodyHeight = 6,
        Digital = new DigitalSpec(3.3, 6.0, 5.0, Icc: 0.0015, Bus: Bus.OneWire),
        NoteTh = "ต้องมีพูลอัป 4.7k–10k ที่ขา DATA · อ่านได้ทุก 2 วินาทีเท่านั้น เร็วกว่านั้นได้ค่าเดิม",
        Pins =
        [
            P("1", "VCC", PinKind.Power, PinSide.Left, 0),
            P("2", "DATA", PinKind.OpenDrain, PinSide.Right, 0, "ต้องมีพูลอัป"),
            P("3", "NC", PinKind.NotConnected, PinSide.Right, 1, "ผู้ผลิตระบุให้ปล่อยลอย"),
            P("4", "GND", PinKind.Ground, PinSide.Left, 1),
        ],
    };

    private static readonly PartDefinition Mpu6050 = new()
    {
        Key = "MPU6050", Prefix = "U", Name = "MPU-6050 (GY-521)", NameTh = "เซนเซอร์เคลื่อนไหว 6 แกน MPU-6050",
        Mpn = "MPU-6050", Package = "GY-521 module",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 9, BodyHeight = 9,
        Digital = new DigitalSpec(3.3, 5.0, 5.0, Icc: 0.0039, Bus: Bus.I2C, BusAddress: "0x68 (AD0 ต่ำ) / 0x69 (AD0 สูง)", HasIntegratedPullups: true),
        NoteTh = "บอร์ด GY-521 มีเรกูเลเตอร์ รับ 5V ได้ แต่ตัวชิปเป็น 3.3V · ที่อยู่ I²C เปลี่ยนด้วยขา AD0",
        Pins =
        [
            P("1", "VCC", PinKind.Power, PinSide.Left, 0),
            P("2", "GND", PinKind.Ground, PinSide.Left, 1),
            P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2, "I²C clock"),
            P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3, "I²C data"),
            P("5", "XDA", PinKind.OpenDrain, PinSide.Right, 3, "I²C ต่อเซนเซอร์เสริม"),
            P("6", "XCL", PinKind.OpenDrain, PinSide.Right, 2),
            P("7", "AD0", PinKind.Input, PinSide.Right, 1, "เลือกที่อยู่ I²C"),
            P("8", "INT", PinKind.Output, PinSide.Right, 0, "อินเทอร์รัปต์"),
        ],
    };

    private static readonly PartDefinition Ssd1306 = new()
    {
        Key = "SSD1306", Prefix = "U", Name = "SSD1306 OLED 0.96\"", NameTh = "จอโอเลด SSD1306 I²C",
        Mpn = "SSD1306", Package = "4-pin I2C module",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 8, BodyHeight = 6,
        Digital = new DigitalSpec(3.3, 5.0, 3.3, Icc: 0.020, Bus: Bus.I2C, BusAddress: "0x3C (ปกติ) / 0x3D", HasIntegratedPullups: true),
        NoteTh = "⚠ ลำดับขาต่างกันตามผู้ขาย — บางรุ่นเป็น GND VCC SCL SDA บางรุ่น VCC GND SCL SDA "
               + "ดูที่พิมพ์บนบอร์ดทุกครั้ง ต่อสลับ VCC/GND จอไหม้",
        Pins =
        [
            P("1", "GND", PinKind.Ground, PinSide.Left, 0),
            P("2", "VCC", PinKind.Power, PinSide.Left, 1),
            P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2),
            P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3),
        ],
    };

    private static readonly PartDefinition HcSr04 = new()
    {
        Key = "HC-SR04", Prefix = "U", Name = "HC-SR04", NameTh = "เซนเซอร์วัดระยะอัลตราโซนิก HC-SR04",
        Mpn = "HC-SR04", Package = "4-pin module",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 8, BodyHeight = 6,
        Digital = new DigitalSpec(4.5, 5.5, 5.0, Icc: 0.015, Bus: Bus.None),
        NoteTh = "ขา ECHO ออก 5V — ต่อเข้า ESP32 ตรง ๆ ไม่ได้ ต้องผ่านตัวแบ่งแรงดันก่อน",
        Pins =
        [
            P("1", "VCC", PinKind.Power, PinSide.Left, 0),
            P("2", "TRIG", PinKind.Input, PinSide.Left, 1, "พัลส์ 10 µs เพื่อสั่งวัด"),
            P("3", "ECHO", PinKind.Output, PinSide.Left, 2, "ความกว้างพัลส์ = ระยะทาง · ออก 5V"),
            P("4", "GND", PinKind.Ground, PinSide.Left, 3),
        ],
    };

    private static readonly PartDefinition Pir = new()
    {
        Key = "HC-SR501", Prefix = "U", Name = "HC-SR501 PIR", NameTh = "เซนเซอร์ตรวจจับความเคลื่อนไหว PIR",
        Mpn = "HC-SR501", Package = "3-pin module",
        Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 7, BodyHeight = 5,
        Digital = new DigitalSpec(4.5, 20.0, 5.0, Icc: 0.00005, Bus: Bus.None),
        NoteTh = "ต้องรอวอร์มอัป ~60 วินาทีหลังจ่ายไฟ ก่อนนั้นค่าที่อ่านได้เชื่อไม่ได้ · เอาต์พุต 3.3V ต่อ ESP32 ได้ตรง",
        Pins =
        [
            P("1", "VCC", PinKind.Power, PinSide.Left, 0),
            P("2", "OUT", PinKind.Output, PinSide.Right, 0, "สูงเมื่อตรวจจับได้"),
            P("3", "GND", PinKind.Ground, PinSide.Left, 1),
        ],
    };

    private static readonly PartDefinition Ldr = new()
    {
        Key = "LDR", Prefix = "R", Name = "LDR", NameTh = "ตัวต้านทานแปรค่าตามแสง",
        Symbol = SymbolShape.Box, Spice = SpiceKind.Primitive,
        DefaultValue = "10k", Unit = "Ω", BodyWidth = 4, BodyHeight = 2,
        NoteTh = "ใช้เป็นตัวต้านทานที่ค่าเปลี่ยนตามแสง — มืด ~1M สว่าง ~1k · ต้องต่อเป็นตัวแบ่งแรงดันถึงอ่านด้วย ADC ได้",
        Pins = [P("1", "A", PinKind.Passive, PinSide.Left, 0), P("2", "B", PinKind.Passive, PinSide.Right, 0)],
    };

    // ── actuators and connectors ─────────────────────────────────────────

    private static readonly PartDefinition RelayModule = new()
    {
        Key = "RELAY-1CH", Prefix = "K", Name = "Relay module 1ch", NameTh = "โมดูลรีเลย์ 1 ช่อง",
        Package = "module", Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
        BodyWidth = 9, BodyHeight = 7,
        Digital = new DigitalSpec(4.5, 5.5, 5.0, Icc: 0.070, Bus: Bus.None),
        NoteTh = "โมดูลส่วนใหญ่เป็นแอกทีฟต่ำ — ส่ง LOW ถึงจะดูด · ฝั่งคอนแทกต์แยกไฟกับฝั่งควบคุม อย่าเดินสายไฟบ้านใกล้ฝั่งลอจิก",
        Pins =
        [
            P("1", "VCC", PinKind.Power, PinSide.Left, 0),
            P("2", "GND", PinKind.Ground, PinSide.Left, 1),
            P("3", "IN", PinKind.Input, PinSide.Left, 2, "แอกทีฟต่ำ"),
            P("4", "COM", PinKind.Passive, PinSide.Right, 0),
            P("5", "NO", PinKind.Passive, PinSide.Right, 1, "ปกติเปิด"),
            P("6", "NC", PinKind.Passive, PinSide.Right, 2, "ปกติปิด"),
        ],
    };

    private static readonly PartDefinition Servo = new()
    {
        Key = "SG90", Prefix = "M", Name = "SG90 servo", NameTh = "เซอร์โว SG90",
        Package = "3-wire", Symbol = SymbolShape.Motor, Spice = SpiceKind.Behavioural,
        BodyWidth = 7, BodyHeight = 5,
        Digital = new DigitalSpec(4.8, 6.0, 5.0, Icc: 0.650, Bus: Bus.Pwm),
        NoteTh = "กระแสตอนออกแรงพุ่งถึง ~650 mA — อย่าจ่ายจากขา 5V ของบอร์ด MCU ต้องมีแหล่งจ่ายแยก · PWM คาบ 20 ms กว้าง 1–2 ms",
        Pins =
        [
            P("1", "VCC", PinKind.Power, PinSide.Left, 0, "สายแดง"),
            P("2", "SIG", PinKind.Input, PinSide.Left, 1, "สายส้ม/เหลือง · PWM"),
            P("3", "GND", PinKind.Ground, PinSide.Left, 2, "สายน้ำตาล/ดำ"),
        ],
    };

    private static PartDefinition Header(int n) => new()
    {
        Key = $"HDR-{n}", Prefix = "J", Name = $"Header {n}-pin", NameTh = $"คอนเนกเตอร์ {n} ขา",
        Symbol = SymbolShape.Connector, Spice = SpiceKind.None,
        BodyWidth = 4, BodyHeight = n * 2,
        Pins = [.. Enumerable.Range(1, n).Select(i =>
            P(i.ToString(), i.ToString(), PinKind.Passive, PinSide.Right, i - 1))],
    };

    private static readonly PartDefinition PushButton = new()
    {
        Key = "SW-PUSH", Prefix = "SW", Name = "Push button", NameTh = "สวิตช์กด",
        Symbol = SymbolShape.Switch, Spice = SpiceKind.Primitive,
        BodyWidth = 4, BodyHeight = 3,
        NoteTh = "ต้องมีพูลอัปหรือพูลดาวน์เสมอ ไม่งั้นขาลอยและอ่านค่ามั่ว · เด้ง (bounce) ~5–20 ms ต้องกรองในโค้ด",
        Pins = [P("1", "A", PinKind.Passive, PinSide.Left, 0), P("2", "B", PinKind.Passive, PinSide.Right, 0)],
    };

    // ── the catalog ──────────────────────────────────────────────────────

    public static IReadOnlyList<PartDefinition> All { get; } =
    [
        Resistor, Capacitor, CapacitorPolarised, Inductor, Potentiometer,
        Diode("D-1N4148", "1N4148", "ไดโอดสวิตชิ่งเร็ว", "D1N4148"),
        Diode("D-1N4007", "1N4007", "ไดโอดเรกติไฟเออร์ 1000V", "D1N4007"),
        Diode("D-1N5819", "1N5819", "ไดโอดชอตต์กี 40V", "D1N5819", "แรงดันตกต่ำ ~0.45V เหมาะกับวงจรประหยัดไฟ"),
        Led,
        Bjt("Q-2N3904", "2N3904", "ทรานซิสเตอร์ NPN", SymbolShape.BjtNpn, "Q2N3904"),
        Bjt("Q-2N3906", "2N3906", "ทรานซิสเตอร์ PNP", SymbolShape.BjtPnp, "Q2N3906"),
        Bjt("Q-BC547", "BC547", "ทรานซิสเตอร์ NPN ตระกูลยุโรป", SymbolShape.BjtNpn, "QBC547"),
        Mosfet("Q-IRFZ44N", "IRFZ44N", "มอสเฟต N-channel 55V 49A", SymbolShape.MosfetN, "IRFZ44N"),
        Mosfet("Q-IRLZ44N", "IRLZ44N", "มอสเฟต N-channel ขับด้วยลอจิก 5V", SymbolShape.MosfetN, "IRLZ44N"),
        Vdc, Vpulse, Ground,
        Ne555, Lm358,
        Regulator("LM7805", "LM7805", "เรกูเลเตอร์ 5V", ["IN", "GND", "OUT"],
            "ต้องมี C 0.33µ ที่ขาเข้าและ 0.1µ ที่ขาออก ไม่งั้นแกว่ง · แรงดันเข้าต้องสูงกว่าออกอย่างน้อย 2V"),
        Regulator("LM317", "LM317", "เรกูเลเตอร์ปรับค่าได้", ["IN", "ADJ", "OUT"],
            "Vout = 1.25 × (1 + R2/R1) · ต้องมีโหลดขั้นต่ำ ~10 mA ถึงจะคุมแรงดันได้"),
        Esp32, Atmega328,
        Ds18b20, Dht22, Mpu6050, Ssd1306, HcSr04, Pir, Ldr,
        RelayModule, Servo, PushButton,
        Header(2), Header(3), Header(4),
    ];

    private static readonly Dictionary<string, PartDefinition> Index =
        All.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static PartDefinition? Find(string key) => Index.GetValueOrDefault(key);

    public static PartDefinition Require(string key) =>
        Find(key) ?? throw new KeyNotFoundException($"No part definition '{key}' in the catalog.");

    /// <summary>Parts ngspice can produce real numbers for.</summary>
    public static IEnumerable<PartDefinition> Simulatable => All.Where(p => p.IsSimulatable);

    /// <summary>Digital and firmware-driven parts, modelled by their electrical envelope.</summary>
    public static IEnumerable<PartDefinition> Digital => All.Where(p => p.Digital is not null);
}
