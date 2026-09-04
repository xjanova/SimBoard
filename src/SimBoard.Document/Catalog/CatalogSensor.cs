namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// เซนเซอร์และโมดูล IoT.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
///
/// Every entry below was typed from working knowledge, not read off a datasheet, so every
/// entry is Unverified. The figure that matters most here is not the supply range but the
/// logic level: a 3.3 V part on a 5 V bus survives for a while and then does not, and a
/// 5 V output into an ESP32 pin is destroyed immediately. Where a module exists in
/// versions that differ on exactly that point — a regulator and level shifter fitted or
/// not — the note says so rather than the catalogue picking one silently.
/// </summary>
public static class CatalogSensor
{
    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
        // ── temperature and humidity ──────────────────────────────────────

        new PartDefinition
        {
            Key = "DHT11", Prefix = "U", Name = "DHT11",
            NameTh = "เซนเซอร์อุณหภูมิ-ความชื้น DHT11",
            Mpn = "DHT11", Package = "4-pin",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 7, BodyHeight = 6,
            Digital = new DigitalSpec(3.0, 5.5, 5.0, Icc: 0.0025, Bus: Bus.OneWire),
            NoteTh = "ต้องมีพูลอัป 4.7k–10k ที่ขา DATA — บอร์ดสำเร็จรูป 3 ขามีมาให้แล้ว ตัวเปล่า 4 ขาไม่มี · "
                   + "อ่านได้ไม่เกิน 1 ครั้งต่อวินาที · ความละเอียด 1 °C และ 1 %RH เท่านั้น และวัดต่ำกว่า 0 °C ไม่ได้ "
                   + "งานที่ต้องการความละเอียดจริงให้ใช้ DHT22 หรือ SHT31 · ใช้ได้ทั้ง 3.3V และ 5V ระดับลอจิกอ้างอิงไฟที่จ่าย",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "DATA", PinKind.OpenDrain, PinSide.Right, 0, "ต้องมีพูลอัป"),
                P("3", "NC", PinKind.NotConnected, PinSide.Right, 1, "ผู้ผลิตระบุให้ปล่อยลอย"),
                P("4", "GND", PinKind.Ground, PinSide.Left, 1),
            ],
        },

        new PartDefinition
        {
            Key = "LM35", Prefix = "U", Name = "LM35",
            NameTh = "เซนเซอร์อุณหภูมิแบบแอนะล็อก LM35",
            Mpn = "LM35DZ", Package = "TO-92", Pinout = "+Vs Vout GND",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 6, BodyHeight = 5,
            Digital = new DigitalSpec(4.0, 30.0, 5.0, Icc: 0.00006, Bus: Bus.Analog),
            NoteTh = "เอาต์พุต 10 mV ต่อ 1 °C เทียบกับกราวด์ ใช้ได้เลยไม่ต้องปรับเทียบ · "
                   + "ต่อไฟบวกอย่างเดียววัดได้ตั้งแต่ +2 °C ขึ้นไป จะวัดติดลบต้องมีไฟลบหรือวงจรออฟเซต · "
                   + "หันด้านแบนเข้าหาตัวและขาลง ซ้ายไปขวาคือ +Vs, Vout, GND — สลับกับ TMP36 ไม่ได้ · "
                   + "ที่ 5V เต็มสเกล 150 °C ได้แค่ 1.5V ใช้ ADC ช่วง 3.3V แล้วเสียความละเอียดไปมาก",
            Pins =
            [
                P("1", "+Vs", PinKind.Power, PinSide.Left, 0),
                P("2", "Vout", PinKind.Analog, PinSide.Right, 0, "10 mV ต่อ 1 °C"),
                P("3", "GND", PinKind.Ground, PinSide.Left, 1),
            ],
        },

        new PartDefinition
        {
            Key = "DS18B20-PROBE", Prefix = "U", Name = "DS18B20 waterproof probe",
            NameTh = "เซนเซอร์อุณหภูมิ DS18B20 หัวโพรบกันน้ำ",
            Mpn = "DS18B20", Package = "stainless probe, 3-wire",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 7, BodyHeight = 5,
            Digital = new DigitalSpec(3.0, 5.5, 5.0, Icc: 0.0015, Bus: Bus.OneWire),
            NoteTh = "ชิปตัวเดียวกับ DS18B20 TO-92 แต่หุ้มปลอกสเตนเลส · ต้องมีพูลอัป 4.7k ที่ขา DQ ไปไฟบวก · "
                   + "⚠ สีสายไม่ตรงกันทุกเจ้า ที่พบบ่อยคือ แดง = VDD, ดำหรือน้ำเงิน = GND, เหลืองหรือขาว = DQ "
                   + "แต่มีล็อตที่สลับ ให้วัดด้วยมิเตอร์ก่อนจ่ายไฟ · สายยาวหลายเมตรอาจต้องลดพูลอัปเหลือ ~2.2k · "
                   + "ต่อหลายตัวบนสายเดียวกันได้เพราะแต่ละตัวมีหมายเลข ROM ของตัวเอง",
            Pins =
            [
                P("1", "VDD", PinKind.Power, PinSide.Left, 0, "สายแดง (ปกติ)"),
                P("2", "DQ", PinKind.OpenDrain, PinSide.Right, 0, "1-Wire · ต้องมีพูลอัป 4.7k"),
                P("3", "GND", PinKind.Ground, PinSide.Left, 1, "สายดำหรือน้ำเงิน (ปกติ)"),
            ],
        },

        new PartDefinition
        {
            Key = "BME280", Prefix = "U", Name = "BME280",
            NameTh = "เซนเซอร์อุณหภูมิ-ความชื้น-ความกดอากาศ BME280",
            Mpn = "BME280", Package = "GY-BME280 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(1.71, 3.6, 3.3, Icc: 0.0007,
                Bus: Bus.I2C, BusAddress: "0x76 (SDO ต่ำ) / 0x77 (SDO สูง)"),
            NoteTh = "ตัวชิปเป็น 3.3V เท่านั้น · บอร์ดสีม่วงตัวยาวมีเรกูเลเตอร์กับเลเวลชิฟต์ รับ 5V ได้ "
                   + "ส่วนบอร์ดสี่เหลี่ยมเล็กสีน้ำเงินไม่มี ป้อน 5V พังทันที — ดูว่ามีไอซีตัวเล็กข้างคอนเนกเตอร์ไหม · "
                   + "⚠ ของที่ขายเป็น BME280 ราคาถูกจำนวนมากคือ BMP280 ที่ไม่มีความชื้น อ่าน chip ID ดูก่อน "
                   + "(0x60 = BME280, 0x58 = BMP280) · ทำงานแบบ SPI ได้ด้วยโดยดึง CSB ลงต่ำ · "
                   + "โมดูลบางรุ่นไม่มีพูลอัป I²C มาให้ ต้องใส่ 4.7k เอง",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2, "I²C clock · SPI SCK"),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3, "I²C data · SPI MOSI"),
                P("5", "CSB", PinKind.Input, PinSide.Right, 1, "ต่ำ = โหมด SPI · ดึงสูงหรือปล่อย = I²C"),
                P("6", "SDO", PinKind.Bidirectional, PinSide.Right, 0, "เลือกที่อยู่ I²C · SPI MISO"),
            ],
        },

        new PartDefinition
        {
            Key = "BMP280", Prefix = "U", Name = "BMP280",
            NameTh = "เซนเซอร์ความกดอากาศ-อุณหภูมิ BMP280",
            Mpn = "BMP280", Package = "GY-BMP280 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(1.71, 3.6, 3.3, Icc: 0.0007,
                Bus: Bus.I2C, BusAddress: "0x76 (SDO ต่ำ) / 0x77 (SDO สูง)"),
            NoteTh = "วัดความกดอากาศกับอุณหภูมิเท่านั้น ไม่มีความชื้น — ใช้แทน BME280 ในงานวัดความชื้นไม่ได้ · "
                   + "chip ID อ่านได้ 0x58 (BME280 ได้ 0x60) ใช้แยกของสองตัวนี้ที่หน้าตาเหมือนกัน · "
                   + "ตัวชิป 3.3V ถ้าบอร์ดไม่มีเรกูเลเตอร์ห้ามป้อน 5V · ใช้วัดความสูงได้แต่ต้องรู้ความกดที่ระดับน้ำทะเลของวันนั้น "
                   + "ไม่งั้นคลาดเคลื่อนได้หลายสิบเมตร",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2, "I²C clock · SPI SCK"),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3, "I²C data · SPI MOSI"),
                P("5", "CSB", PinKind.Input, PinSide.Right, 1, "ต่ำ = โหมด SPI"),
                P("6", "SDO", PinKind.Bidirectional, PinSide.Right, 0, "เลือกที่อยู่ I²C · SPI MISO"),
            ],
        },

        new PartDefinition
        {
            Key = "SHT31", Prefix = "U", Name = "SHT31-D",
            NameTh = "เซนเซอร์อุณหภูมิ-ความชื้นความแม่นยำสูง SHT31",
            Mpn = "SHT31-DIS-B", Package = "breakout",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(2.15, 5.5, 3.3, Icc: 0.0015,
                Bus: Bus.I2C, BusAddress: "0x44 (ADDR ต่ำ) / 0x45 (ADDR สูง)"),
            NoteTh = "แม่นกว่า DHT22 ชัดเจนและอ่านซ้ำได้เร็ว เหมาะกับงานที่ต้องเอาตัวเลขไปใช้จริง · "
                   + "ตัวชิปรับได้ถึง 5.5V แต่ระดับลอจิกอ้างอิงไฟที่จ่ายเข้าจริง ป้อน 5V แล้วบัสจะเป็น 5V ต่อ ESP32 ตรงไม่ได้ · "
                   + "ขา ADDR ต้องต่อ ไม่ปล่อยลอย ไม่งั้นที่อยู่ไม่แน่นอน · "
                   + "มีฮีตเตอร์ในตัวไว้ไล่ความชื้นที่เกาะ เปิดค้างไว้ค่าอุณหภูมิจะสูงเกินจริงหลายองศา",
            Pins =
            [
                P("1", "VIN", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3),
                P("5", "ADDR", PinKind.Input, PinSide.Right, 1, "เลือกที่อยู่ · ห้ามปล่อยลอย"),
                P("6", "ALERT", PinKind.Output, PinSide.Right, 0, "แจ้งเมื่อเกินขีดที่ตั้งไว้"),
            ],
        },

        new PartDefinition
        {
            Key = "MAX6675", Prefix = "U", Name = "MAX6675",
            NameTh = "โมดูลอ่านเทอร์โมคัปเปิล K-type MAX6675",
            Mpn = "MAX6675ISA", Package = "module + K-type probe",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(3.0, 5.5, 5.0, Icc: 0.0015, Bus: Bus.Spi),
            NoteTh = "อ่านอย่างเดียว ไม่มี MOSI — ใช้แค่ SCK, CS, SO · ช่วง 0–1024 °C ละเอียด 0.25 °C วัดติดลบไม่ได้ · "
                   + "แปลงค่าใช้เวลาราว 0.2 s อ่านถี่กว่านั้นได้ค่าเดิม · "
                   + "ขั้วเทอร์โมคัปเปิลสลับแล้วอุณหภูมิจะวิ่งลงแทนที่จะขึ้น — สาย K-type ขั้วลบเป็นแม่เหล็ก ใช้แยกได้ · "
                   + "ปลายโพรบต่อถึงตัวถังโลหะในหลายรุ่น จิ้มลงของที่มีไฟแล้วกราวด์ลอยทั้งวงจร · "
                   + "เลิกผลิตแล้ว งานใหม่ให้ใช้ MAX31855 (3.3V เท่านั้น) หรือ MAX31856",
            Pins =
            [
                P("1", "GND", PinKind.Ground, PinSide.Left, 0),
                P("2", "VCC", PinKind.Power, PinSide.Left, 1),
                P("3", "SCK", PinKind.Input, PinSide.Left, 2),
                P("4", "CS", PinKind.Input, PinSide.Left, 3, "แอกทีฟต่ำ"),
                P("5", "SO", PinKind.Output, PinSide.Left, 4, "ข้อมูลออก · ต่อเข้า MISO"),
                P("6", "T+", PinKind.Analog, PinSide.Right, 0, "เทอร์โมคัปเปิลขั้วบวก"),
                P("7", "T-", PinKind.Analog, PinSide.Right, 1, "เทอร์โมคัปเปิลขั้วลบ · เส้นที่ดูดแม่เหล็ก"),
            ],
        },

        // ── motion, orientation, magnetics ────────────────────────────────

        new PartDefinition
        {
            Key = "ADXL345", Prefix = "U", Name = "ADXL345 (GY-291)",
            NameTh = "เซนเซอร์ความเร่ง 3 แกน ADXL345",
            Mpn = "ADXL345BCCZ", Package = "GY-291 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 9,
            Digital = new DigitalSpec(2.0, 3.6, 3.3, Icc: 0.00014,
                Bus: Bus.I2C, BusAddress: "0x53 (SDO ต่ำ) / 0x1D (SDO สูง)"),
            NoteTh = "ตัวชิปกินไฟ 2.0–3.6V บอร์ด GY-291 มีเรกูเลเตอร์จึงรับ 5V ที่ขา VCC ได้ แต่ขาสัญญาณยังเป็น 3.3V · "
                   + "จะใช้ I²C ต้องดึงขา CS ขึ้นไฟบวก ปล่อยลอยชิปจะเข้าโหมด SPI แล้วหาไม่เจอทั้งบัส · "
                   + "ที่อยู่ปกติของบอร์ดคือ 0x53 (SDO ลงกราวด์) แต่ไลบรารีหลายตัวตั้งค่าเริ่มต้นเป็น 0x1D · "
                   + "วัดได้ถึง ±16 g และมีโหมดตรวจการเคาะกับการตกอิสระในตัว ไม่ต้องคำนวณเอง",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "CS", PinKind.Input, PinSide.Left, 2, "ดึงสูง = I²C · ต่ำ = SPI"),
                P("4", "INT1", PinKind.Output, PinSide.Left, 3),
                P("5", "INT2", PinKind.Output, PinSide.Right, 3),
                P("6", "SDO", PinKind.Bidirectional, PinSide.Right, 2, "เลือกที่อยู่ I²C · SPI MISO"),
                P("7", "SDA", PinKind.OpenDrain, PinSide.Right, 1, "I²C data · SPI MOSI"),
                P("8", "SCL", PinKind.OpenDrain, PinSide.Right, 0, "I²C clock · SPI SCK"),
            ],
        },

        new PartDefinition
        {
            Key = "HMC5883L", Prefix = "U", Name = "HMC5883L (GY-271)",
            NameTh = "เข็มทิศดิจิทัล 3 แกน HMC5883L",
            Mpn = "HMC5883L", Package = "GY-271 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 8, BodyHeight = 7,
            Digital = new DigitalSpec(2.16, 3.6, 3.3, Icc: 0.0001,
                Bus: Bus.I2C, BusAddress: "0x1E (คงที่ เปลี่ยนไม่ได้)"),
            NoteTh = "⚠ บอร์ด GY-271 ที่ขายอยู่ตอนนี้ส่วนใหญ่เป็น QMC5883L ไม่ใช่ HMC5883L ทั้งที่พิมพ์เหมือนกัน — "
                   + "คนละที่อยู่ (0x0D) คนละรีจิสเตอร์ ต้องใช้ไลบรารีคนละตัว สแกนบัสดูก่อนว่าเจอ 0x1E หรือ 0x0D · "
                   + "Honeywell เลิกผลิต HMC5883L แล้ว · ที่อยู่ตายตัว ต่อสองตัวบนบัสเดียวกันไม่ได้ · "
                   + "วางใกล้ลำโพง มอเตอร์ หรือสายไฟกระแสสูงแล้วค่าเพี้ยนทันที ต้องคาลิเบรตในตำแหน่งติดตั้งจริง · "
                   + "ตัวชิป 3.3V บอร์ดส่วนใหญ่มีเรกูเลเตอร์ให้ต่อ 5V ได้ แต่ขาสัญญาณยังเป็น 3.3V",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3),
                P("5", "DRDY", PinKind.Output, PinSide.Right, 0, "ข้อมูลพร้อม · แอกทีฟต่ำ"),
            ],
        },

        new PartDefinition
        {
            Key = "MPU9250", Prefix = "U", Name = "MPU-9250 (GY-9250)",
            NameTh = "เซนเซอร์เคลื่อนไหว 9 แกน MPU-9250",
            Mpn = "MPU-9250", Package = "GY-9250 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 11,
            Digital = new DigitalSpec(2.4, 3.6, 3.3, Icc: 0.0035,
                Bus: Bus.I2C, BusAddress: "0x68 (AD0 ต่ำ) / 0x69 (AD0 สูง) · แมกนีโตมิเตอร์ AK8963 อยู่ที่ 0x0C"),
            NoteTh = "⚠ ของที่ขายจำนวนมากเป็น MPU-6500 ซึ่งไม่มีแมกนีโตมิเตอร์ — อ่าน WHO_AM_I ได้ 0x70 แทนที่จะเป็น 0x71 "
                   + "เจอแบบนั้นคือได้ 6 แกน ไม่ใช่ 9 · InvenSense เลิกผลิตแล้ว งานใหม่ให้ดู ICM-20948 · "
                   + "ตัวชิป 3.3V เท่านั้น บอร์ด GY-9250 บางรุ่นไม่มีเรกูเลเตอร์ ป้อน 5V พังทันที ดูให้ดีก่อน · "
                   + "ที่อยู่ 0x68 ชนกับ DS3231 และ MPU-6050 บนบัสเดียวกัน · "
                   + "แมกนีโตมิเตอร์อยู่หลังบัส I²C ภายใน ต้องเปิดโหมด bypass ก่อนถึงจะมองเห็น 0x0C",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2, "I²C clock · SPI SCK"),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3, "I²C data · SPI MOSI"),
                P("5", "EDA", PinKind.OpenDrain, PinSide.Left, 4, "I²C เสริมสำหรับเซนเซอร์ต่อพ่วง"),
                P("6", "ECL", PinKind.OpenDrain, PinSide.Right, 4),
                P("7", "AD0", PinKind.Input, PinSide.Right, 3, "เลือกที่อยู่ I²C · SPI MISO"),
                P("8", "INT", PinKind.Output, PinSide.Right, 2, "อินเทอร์รัปต์"),
                P("9", "NCS", PinKind.Input, PinSide.Right, 1, "ดึงต่ำ = โหมด SPI"),
                P("10", "FSYNC", PinKind.Input, PinSide.Right, 0, "ไม่ใช้ให้ต่อกราวด์"),
            ],
        },

        // ── distance ──────────────────────────────────────────────────────

        new PartDefinition
        {
            Key = "VL53L0X", Prefix = "U", Name = "VL53L0X",
            NameTh = "เซนเซอร์วัดระยะเลเซอร์ ToF VL53L0X",
            Mpn = "VL53L0CXV0DH", Package = "GY-530 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(2.6, 3.5, 3.3, Icc: 0.019,
                Bus: Bus.I2C, BusAddress: "0x29 (ค่าจากโรงงาน · เปลี่ยนได้เฉพาะในซอฟต์แวร์)"),
            NoteTh = "วัดได้ราว 30–1200 mm ละเอียดระดับมิลลิเมตร แต่กลางแดดจ้าระยะใช้งานจริงหดเหลือไม่กี่สิบเซนติเมตร · "
                   + "⚠ ทุกตัวมาจากโรงงานที่ 0x29 เหมือนกันหมดและไม่มีขาเลือกที่อยู่ — จะต่อหลายตัวต้องดึง XSHUT ให้ต่ำ "
                   + "ปลุกทีละตัวแล้วสั่งเปลี่ยนที่อยู่ในโค้ด และต้องทำใหม่ทุกครั้งที่รีบูต · "
                   + "ตัวชิป 2.6–3.5V บอร์ด GY-530 มีเรกูเลเตอร์กับเลเวลชิฟต์จึงรับ 5V ได้ · "
                   + "อย่าลอกฟิล์มใสหน้าเลนส์ออก และอย่าให้มีกระจกหรืออะคริลิกคลุมหน้าเซนเซอร์ แสงสะท้อนกลับทำให้วัดเพี้ยน",
            Pins =
            [
                P("1", "VIN", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3),
                P("5", "GPIO1", PinKind.Output, PinSide.Right, 1, "อินเทอร์รัปต์เมื่อวัดเสร็จ"),
                P("6", "XSHUT", PinKind.Input, PinSide.Right, 0, "ดึงต่ำเพื่อปิดตัวเซนเซอร์ · ใช้ตอนตั้งที่อยู่ใหม่"),
            ],
        },

        new PartDefinition
        {
            Key = "GP2Y0A21", Prefix = "U", Name = "GP2Y0A21YK0F",
            NameTh = "เซนเซอร์วัดระยะอินฟราเรด Sharp 10–80 cm",
            Mpn = "GP2Y0A21YK0F", Package = "3-pin JST",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 7, BodyHeight = 5,
            Digital = new DigitalSpec(4.5, 5.5, 5.0, Icc: 0.030, Bus: Bus.Analog),
            NoteTh = "เอาต์พุตแอนะล็อกไม่เป็นเชิงเส้น ต้องแปลงด้วยตารางหรือสมการ ไม่ใช่คูณตรง ๆ · "
                   + "⚠ ใกล้กว่า 10 cm ค่าจะย้อนกลับลง — 2V อาจหมายถึง 8 cm หรือ 25 cm ก็ได้ ต้องกันไม่ให้วัตถุเข้าใกล้กว่านั้น "
                   + "หรือใช้เซนเซอร์ตัวที่สองยืนยัน · กระแสกินเป็นจังหวะตามการยิงแสง ต้องมี C 10µ คร่อมไฟที่ตัวเซนเซอร์ "
                   + "ไม่งั้นค่าที่อ่านกระตุก · เอาต์พุตสูงสุดราว 3.1V ต่อ ADC ของ ESP32 ควรผ่านตัวแบ่งแรงดัน · "
                   + "ตระกูลเดียวกันมีหลายช่วง (A02 = 20–150 cm, A41 = 4–30 cm) สมการแปลงคนละตัวกัน ดูรหัสให้ครบ · "
                   + "หัวต่อเป็น JST 3 ขาเฉพาะรุ่น เสียบเบรดบอร์ดตรง ๆ ไม่ได้",
            Pins =
            [
                P("1", "Vo", PinKind.Analog, PinSide.Right, 0, "แรงดันออก ไม่เป็นเชิงเส้น"),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "Vcc", PinKind.Power, PinSide.Left, 0),
            ],
        },

        // ── light and colour ──────────────────────────────────────────────

        new PartDefinition
        {
            Key = "BH1750", Prefix = "U", Name = "BH1750 (GY-302)",
            NameTh = "เซนเซอร์วัดความสว่างเป็นลักซ์ BH1750",
            Mpn = "BH1750FVI", Package = "GY-302 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 8, BodyHeight = 7,
            Digital = new DigitalSpec(2.4, 3.6, 3.3, Icc: 0.00012,
                Bus: Bus.I2C, BusAddress: "0x23 (ADDR ต่ำหรือปล่อยลอย) / 0x5C (ADDR สูง)",
                HasIntegratedPullups: true),
            NoteTh = "อ่านออกมาเป็นหน่วย lux ตรง ๆ ไม่ต้องแปลงเองเหมือน LDR และไม่เลื่อนตามอุณหภูมิ · "
                   + "บอร์ด GY-302 มีเรกูเลเตอร์กับพูลอัป I²C มาให้ จึงต่อ 5V ที่ VCC ได้ · "
                   + "ขา ADDR ปล่อยลอยได้ บอร์ดดึงลงไว้แล้วจึงเป็น 0x23 · "
                   + "โหมดความละเอียดสูงใช้เวลาแปลงราว 120 ms อ่านถี่กว่านั้นจะได้ค่าเดิม · "
                   + "วัดได้ถึง ~65,000 lux เลยกว่านั้นค่าจะอิ่มตัว ยิงไฟฉายจ่อจึงอ่านตันได้",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3),
                P("5", "ADDR", PinKind.Input, PinSide.Right, 0, "เลือกที่อยู่ · ปล่อยลอยได้"),
            ],
        },

        new PartDefinition
        {
            Key = "TCS3200", Prefix = "U", Name = "TCS3200 / TCS230",
            NameTh = "เซนเซอร์ตรวจสี TCS3200",
            Mpn = "TCS3200", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 9,
            Digital = new DigitalSpec(2.7, 5.5, 5.0, Icc: 0.0016, Bus: Bus.None),
            NoteTh = "เอาต์พุตเป็นความถี่ ไม่ใช่แรงดัน — ต้องนับพัลส์หรือวัดคาบเอา ต่อเข้า ADC แล้วอ่านไม่ได้เรื่อง · "
                   + "S2/S3 เลือกฟิลเตอร์สี (แดง/เขียว/น้ำเงิน/ไม่มีฟิลเตอร์) ต้องสลับทีละสีแล้ววัดทีละครั้ง · "
                   + "S0/S1 หารความถี่ลง ตั้ง 100 % แล้วไมโครคอนโทรลเลอร์ช้า ๆ นับไม่ทัน ปกติใช้ 20 % · "
                   + "LED ขาว 4 ดวงบนบอร์ดกินกระแสหลายสิบ mA มากกว่าตัวชิปเองหลายเท่า · "
                   + "ต้องคาลิเบรตค่าขาวกับค่าดำใหม่ทุกครั้งที่เปลี่ยนระยะหรือแสงรอบข้าง ไม่งั้นสีที่อ่านได้เพี้ยนหมด · "
                   + "ลำดับขาบนหัวต่อไม่เหมือนกันทุกผู้ขาย ให้ดูตัวอักษรที่พิมพ์บนบอร์ด",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "S0", PinKind.Input, PinSide.Left, 2, "ตั้งอัตราส่วนความถี่"),
                P("4", "S1", PinKind.Input, PinSide.Left, 3, "ตั้งอัตราส่วนความถี่"),
                P("5", "S2", PinKind.Input, PinSide.Right, 3, "เลือกฟิลเตอร์สี"),
                P("6", "S3", PinKind.Input, PinSide.Right, 2, "เลือกฟิลเตอร์สี"),
                P("7", "OUT", PinKind.Output, PinSide.Right, 1, "ความถี่เป็นสัดส่วนกับความเข้มแสงของสีที่เลือก"),
                P("8", "OE", PinKind.Input, PinSide.Right, 0, "แอกทีฟต่ำ · ไม่ใช้ให้ต่อกราวด์"),
            ],
        },

        new PartDefinition
        {
            Key = "TSL2561", Prefix = "U", Name = "TSL2561",
            NameTh = "เซนเซอร์วัดความสว่างสองช่อง TSL2561",
            Mpn = "TSL2561T", Package = "breakout",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(2.7, 3.6, 3.3, Icc: 0.00024,
                Bus: Bus.I2C, BusAddress: "0x39 (ADDR ลอย) / 0x29 (ADDR ต่ำ) / 0x49 (ADDR สูง)"),
            NoteTh = "มีโฟโตไดโอดสองตัว ตัวหนึ่งรับแสงรวมอีกตัวรับเฉพาะอินฟราเรด เอามาหักกันได้ค่าที่ใกล้เคียงสายตาคนกว่า BH1750 · "
                   + "ตัวชิป 3.3V บอร์ดของ Adafruit มีเรกูเลเตอร์กับเลเวลชิฟต์ ส่วนบอร์ดจีนราคาถูกบางรุ่นไม่มี · "
                   + "แสงแรงเกินเกนที่ตั้งไว้ค่าจะอิ่มตัวแล้ววิ่งกลับลง ดูเหมือนมืดลงทั้งที่สว่างขึ้น ต้องปรับเกนอัตโนมัติ · "
                   + "เลิกผลิตแล้ว ของใหม่ให้ใช้ TSL2591",
            Pins =
            [
                P("1", "VIN", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3),
                P("5", "ADDR", PinKind.Input, PinSide.Right, 1, "เลือกที่อยู่ I²C"),
                P("6", "INT", PinKind.OpenDrain, PinSide.Right, 0, "แจ้งเมื่อแสงเกินขีดที่ตั้งไว้"),
            ],
        },

        // ── gas ───────────────────────────────────────────────────────────

        new PartDefinition
        {
            Key = "MQ-2", Prefix = "U", Name = "MQ-2",
            NameTh = "เซนเซอร์แก๊สไวไฟและควัน MQ-2",
            Mpn = "MQ-2", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 8, BodyHeight = 6,
            Digital = new DigitalSpec(4.9, 5.1, 5.0, Icc: 0.150, Bus: Bus.Analog),
            NoteTh = "ฮีตเตอร์ต้องได้ 5.0V ±0.1 พอดีและกินกระแสราว 150 mA ตลอดเวลา — ดึงจากขา 5V ของบอร์ด MCU ไม่ไหว "
                   + "ต้องมีแหล่งจ่ายแยก และตัวเซนเซอร์จะร้อนจับไม่ได้ ถือเป็นเรื่องปกติ · "
                   + "ตัวใหม่ต้องเบิร์นอิน 24–48 ชม. ก่อนค่าจะนิ่ง และต้องอุ่นเครื่องอย่างน้อยหลายนาทีทุกครั้งที่เปิด · "
                   + "ขา AO อ้างอิงไฟ 5V ต่อเข้า ADC ของ ESP32 ตรง ๆ ไม่ได้ ต้องผ่านตัวแบ่งแรงดัน · "
                   + "DO เป็นแค่การเทียบกับค่าที่หมุนตั้งด้วยโพเทนชิโอมิเตอร์ ไม่ใช่ตัวเลข ppm · "
                   + "⚠ แยกชนิดแก๊สไม่ได้ ค่าที่ได้บอกได้แค่ว่า 'มีแก๊สไวไฟมากขึ้น' ห้ามใช้แทนเครื่องตรวจแก๊สที่รับรองความปลอดภัย",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0, "5.0V ±0.1 · ~150 mA"),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "DO", PinKind.Output, PinSide.Right, 0, "เทียบกับค่าที่ตั้งด้วยโพเทนชิโอมิเตอร์ · ออก 5V"),
                P("4", "AO", PinKind.Analog, PinSide.Right, 1, "อ้างอิง 5V · ต้องแบ่งแรงดันก่อนเข้า ESP32"),
            ],
        },

        new PartDefinition
        {
            Key = "MQ-135", Prefix = "U", Name = "MQ-135",
            NameTh = "เซนเซอร์คุณภาพอากาศ MQ-135",
            Mpn = "MQ-135", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 8, BodyHeight = 6,
            Digital = new DigitalSpec(4.9, 5.1, 5.0, Icc: 0.150, Bus: Bus.Analog),
            NoteTh = "ฮีตเตอร์ 5.0V ±0.1 กินกระแสราว 150 mA ตลอดเวลา ต้องมีแหล่งจ่ายแยกเหมือน MQ ตัวอื่น · "
                   + "⚠ ตัวเลข ppm CO₂ ที่ไลบรารีคำนวณให้เชื่อไม่ได้ถ้ายังไม่คาลิเบรต R0 ในอากาศสะอาด "
                   + "และถึงคาลิเบรตแล้วค่าก็ยังเลื่อนตามความชื้นและอุณหภูมิ ใช้ดูแนวโน้มได้ ใช้อ้างตัวเลขไม่ได้ · "
                   + "ตอบสนองแอมโมเนีย แอลกอฮอล์ เบนซีน ควัน รวม ๆ กัน แยกชนิดไม่ได้ · "
                   + "AO อ้างอิง 5V ต่อ ESP32 ต้องผ่านตัวแบ่งแรงดัน",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0, "5.0V ±0.1 · ~150 mA"),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "DO", PinKind.Output, PinSide.Right, 0, "ออก 5V"),
                P("4", "AO", PinKind.Analog, PinSide.Right, 1, "อ้างอิง 5V"),
            ],
        },

        // ── current, power, load ──────────────────────────────────────────

        new PartDefinition
        {
            Key = "ACS712", Prefix = "U", Name = "ACS712",
            NameTh = "เซนเซอร์วัดกระแสแบบฮอลล์ ACS712",
            Mpn = "ACS712", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 7,
            Digital = new DigitalSpec(4.5, 5.5, 5.0, Icc: 0.010, Bus: Bus.Analog),
            NoteTh = "⚠ มีสามรุ่นที่ใช้บอร์ดหน้าตาเดียวกัน — 05B = ±5 A ให้ 185 mV/A, 20A = ±20 A ให้ 100 mV/A, "
                   + "30A = ±30 A ให้ 66 mV/A ต้องอ่านรหัสบนตัวชิปก่อนใส่สูตร ใส่ผิดค่ากระแสผิดเป็นเท่าตัว · "
                   + "เอาต์พุตอยู่ที่ Vcc/2 คือ 2.5V ตอนกระแสเป็นศูนย์ แล้วแกว่งขึ้นลงรอบจุดนั้น — "
                   + "ต่อเข้า ADC 3.3V ตรง ๆ ไม่ได้ ต้องแบ่งแรงดันหรือใช้ MCU ที่ ADC รับ 5V · "
                   + "สัญญาณรบกวนสูง ต้องเฉลี่ยหลายตัวอย่าง และวัดกระแสน้อย ๆ ความละเอียดไม่พอ รุ่น 30A วัดหลักสิบ mA ไม่เห็น · "
                   + "ตัวชิปแยกฝั่งกระแสกับฝั่งลอจิกออกจากกันจริง แต่ระยะห่างลายทองแดงบนบอร์ดราคาถูกไม่พอสำหรับไฟบ้าน",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "OUT", PinKind.Analog, PinSide.Left, 2, "นิ่งที่ 2.5V เมื่อกระแสเป็นศูนย์"),
                P("4", "IP+", PinKind.Passive, PinSide.Right, 0, "กระแสเข้า"),
                P("5", "IP-", PinKind.Passive, PinSide.Right, 1, "กระแสออก"),
            ],
        },

        new PartDefinition
        {
            Key = "INA219", Prefix = "U", Name = "INA219",
            NameTh = "เซนเซอร์วัดกระแสและกำลังไฟ I²C INA219",
            Mpn = "INA219", Package = "breakout",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(3.0, 5.5, 3.3, Icc: 0.001,
                Bus: Bus.I2C, BusAddress: "0x40 (ค่าเริ่มต้น) · เลือกได้ 0x40–0x4F ด้วยขา A0/A1",
                HasIntegratedPullups: true),
            NoteTh = "วัดฝั่งไฟบวก (high-side) ได้ถึง 26V และรายงานกระแสกับกำลังออกมาเป็นตัวเลขเลย ไม่ต้องแปลงเอง · "
                   + "ชันต์บนบอร์ดมาตรฐานคือ 0.1Ω 2W จึงวัดได้สูงสุดราว 3.2 A และตกคร่อม 320 mV ที่กระแสเต็มสเกล — "
                   + "แรงดันที่หายไปนี้มีผลกับโหลดที่ไวต่อไฟตก · "
                   + "ต้องตั้งค่าคาลิเบรชันรีจิสเตอร์ให้ตรงกับชันต์ที่ใช้จริง ไม่งั้นตัวเลขกระแสผิดทั้งชุด · "
                   + "Vin− คือฝั่งที่ไปโหลด สลับกับ Vin+ แล้วค่ากระแสติดลบ · "
                   + "วัดได้แต่ฝั่งบวกที่อ้างกราวด์ร่วม จะวัดฝั่งกราวด์ของโหลดไม่ได้",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3),
                P("5", "VIN+", PinKind.Passive, PinSide.Right, 0, "จากแหล่งจ่าย"),
                P("6", "VIN-", PinKind.Passive, PinSide.Right, 1, "ไปโหลด"),
            ],
        },

        new PartDefinition
        {
            Key = "HX711", Prefix = "U", Name = "HX711",
            NameTh = "โมดูลขยายสัญญาณโหลดเซลล์ HX711",
            Mpn = "HX711", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 9,
            Digital = new DigitalSpec(2.6, 5.5, 5.0, Icc: 0.0015, Bus: Bus.None),
            NoteTh = "ไม่ใช่ SPI — เป็นสองสาย DT/SCK แบบเฉพาะตัว ต้องใช้ไลบรารีของมันเอง ต่อเข้าขา SPI ปกติแล้วไม่ทำงาน · "
                   + "ไฟกระตุ้นโหลดเซลล์ดึงมาจากไฟเลี้ยงโดยตรง ไฟไม่นิ่งเท่ากับน้ำหนักแกว่ง — อย่าใช้ไฟชุดเดียวกับมอเตอร์หรือรีเลย์ · "
                   + "บอร์ดสีแดงส่วนใหญ่ต่อขา RATE ลงกราวด์ไว้ ได้ 10 ครั้งต่อวินาที จะเอา 80 ต้องตัดลายแล้วต่อขึ้นไฟบวก · "
                   + "สายโหลดเซลล์สี่เส้นมาตรฐาน: แดง = E+, ดำ = E−, เขียว = A+, ขาว = A− สลับเขียวกับขาวแล้วน้ำหนักติดลบ · "
                   + "ต้อง tare ทุกครั้งที่เปิดเครื่อง และค่าจะเลื่อนตามอุณหภูมิ งานชั่งจริงต้องคาลิเบรตด้วยตุ้มน้ำหนักที่รู้ค่า",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "DT", PinKind.Output, PinSide.Left, 2, "ข้อมูลออก · ต่ำเมื่อค่าพร้อม"),
                P("4", "SCK", PinKind.Input, PinSide.Left, 3, "คล็อกที่ MCU ตีเอง"),
                P("5", "E+", PinKind.Power, PinSide.Right, 0, "ไฟกระตุ้นโหลดเซลล์ · สายแดง"),
                P("6", "E-", PinKind.Ground, PinSide.Right, 1, "สายดำ"),
                P("7", "A+", PinKind.Analog, PinSide.Right, 2, "สัญญาณจากโหลดเซลล์ · สายเขียว"),
                P("8", "A-", PinKind.Analog, PinSide.Right, 3, "สายขาว"),
            ],
        },

        // ── real-time clocks ──────────────────────────────────────────────

        new PartDefinition
        {
            Key = "DS3231", Prefix = "U", Name = "DS3231 (ZS-042)",
            NameTh = "โมดูลนาฬิกาเรียลไทม์ DS3231",
            Mpn = "DS3231SN", Package = "ZS-042 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(2.3, 5.5, 3.3, Icc: 0.0002,
                Bus: Bus.I2C, BusAddress: "0x68 (คงที่) · อีอีพรอม AT24C32 บนบอร์ดอยู่ที่ 0x57",
                HasIntegratedPullups: true),
            NoteTh = "แม่นกว่า DS1307 มากเพราะมีคริสตัลชดเชยอุณหภูมิอยู่ในตัวชิป คลาดเคลื่อนระดับไม่กี่นาทีต่อปี · "
                   + "⚠ บอร์ด ZS-042 มีวงจรชาร์จแบตในตัวที่ออกแบบมาสำหรับ LIR2032 ซึ่งชาร์จได้ — "
                   + "ใส่ CR2032 ธรรมดาแล้วมันจะอัดไฟเข้าไป ร้อน บวม และรั่ว ต้องถอดไดโอดหรือตัวต้านทานชุดชาร์จออกก่อน · "
                   + "ทำงานได้ทั้ง 3.3V และ 5V ระดับลอจิกอ้างอิงไฟที่จ่ายเข้า จ่าย 5V แล้วต่อ ESP32 ตรง ๆ ไม่ได้ · "
                   + "ที่อยู่ 0x68 ตายตัวและชนกับ MPU-6050/MPU-9250 บนบัสเดียวกัน · "
                   + "ขา SQW เป็นโอเพนเดรน ต้องมีพูลอัปถึงจะใช้เป็นสัญญาณได้",
            Pins =
            [
                P("1", "32K", PinKind.Output, PinSide.Right, 0, "คล็อก 32.768 kHz · โอเพนเดรน"),
                P("2", "SQW", PinKind.OpenDrain, PinSide.Right, 1, "คลื่นสี่เหลี่ยม/สัญญาณปลุก · ต้องมีพูลอัป"),
                P("3", "SCL", PinKind.OpenDrain, PinSide.Left, 2),
                P("4", "SDA", PinKind.OpenDrain, PinSide.Left, 3),
                P("5", "VCC", PinKind.Power, PinSide.Left, 0),
                P("6", "GND", PinKind.Ground, PinSide.Left, 1),
            ],
        },

        new PartDefinition
        {
            Key = "DS1307", Prefix = "U", Name = "DS1307 (Tiny RTC)",
            NameTh = "โมดูลนาฬิกาเรียลไทม์ DS1307",
            Mpn = "DS1307", Package = "Tiny RTC module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(4.5, 5.5, 5.0, Icc: 0.0015,
                Bus: Bus.I2C, BusAddress: "0x68 (คงที่) · อีอีพรอม AT24C32 บนบอร์ดอยู่ที่ 0x57",
                HasIntegratedPullups: true),
            NoteTh = "⚠ ต้องใช้ 5V — ที่ 3.3V ชิปจะสลับไปกินไฟจากแบตและหยุดตอบ I²C ทั้งที่ดูเหมือนต่อถูก · "
                   + "พูลอัปบนบอร์ด Tiny RTC ต่อขึ้นไป 5V ทำให้บัสเป็น 5V ต่อ ESP32 หรือ Pi ตรง ๆ ไม่ได้ "
                   + "ต้องถอดพูลอัปสองตัวออกหรือใช้เลเวลชิฟต์ · "
                   + "ไม่มีการชดเชยอุณหภูมิ เดินเพี้ยนได้หลายนาทีต่อเดือน งานที่ต้องแม่นให้ใช้ DS3231 ซึ่งเปลี่ยนแทนได้เลย "
                   + "เพราะที่อยู่และรีจิสเตอร์เวลาชุดแรกเหมือนกัน · "
                   + "บอร์ดนี้ก็มีวงจรชาร์จแบตเหมือน ZS-042 ระวังใส่ CR2032 ที่ชาร์จไม่ได้ · "
                   + "ขา SQW เป็นโอเพนเดรน ต้องมีพูลอัป",
            Pins =
            [
                P("1", "VCC", PinKind.Power, PinSide.Left, 0, "ต้องเป็น 5V"),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "SDA", PinKind.OpenDrain, PinSide.Left, 2, "พูลอัปบนบอร์ดไป 5V"),
                P("4", "SCL", PinKind.OpenDrain, PinSide.Left, 3, "พูลอัปบนบอร์ดไป 5V"),
                P("5", "SQW", PinKind.OpenDrain, PinSide.Right, 0, "ต้องมีพูลอัป"),
                P("6", "BAT", PinKind.Power, PinSide.Right, 1, "แบตสำรองเวลา"),
            ],
        },

        // ── storage and radio ─────────────────────────────────────────────

        new PartDefinition
        {
            Key = "W25Q32", Prefix = "U", Name = "W25Q32",
            NameTh = "หน่วยความจำแฟลช SPI 32 Mbit W25Q32",
            Mpn = "W25Q32JVSSIQ", Package = "SOIC-8 208-mil",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(2.7, 3.6, 3.3, Icc: 0.004, Bus: Bus.Spi),
            NoteTh = "3.3V เท่านั้น ขาสัญญาณไม่ทน 5V — ต่อกับ Arduino UNO ตรง ๆ พัง ต้องมีเลเวลชิฟต์ · "
                   + "32 Mbit = 4 MByte ไม่ใช่ 32 MByte · "
                   + "ไม่ใช้ WP กับ HOLD ต้องดึงขึ้นไฟบวก ปล่อยลอยแล้วอ่านเขียนมั่ว · "
                   + "ลบได้ทีละเซกเตอร์ 4 kB เป็นอย่างน้อย เขียนทับไบต์เดียวโดยไม่ลบก่อนไม่ได้ · "
                   + "กระแสตอนลบหรือเขียนสูงกว่าตอนอ่านหลายเท่า อย่าจ่ายผ่านขา 3.3V ที่ใช้ร่วมกับโมดูลวิทยุ · "
                   + "ขา 1 อยู่มุมที่มีจุดวงกลม นับทวนเข็มนาฬิการอบตัวถัง",
            Pins =
            [
                P("1", "CS", PinKind.Input, PinSide.Left, 0, "แอกทีฟต่ำ"),
                P("2", "DO", PinKind.Output, PinSide.Left, 1, "MISO · IO1"),
                P("3", "WP", PinKind.Input, PinSide.Left, 2, "แอกทีฟต่ำ · ไม่ใช้ให้ดึงสูง · IO2"),
                P("4", "GND", PinKind.Ground, PinSide.Left, 3),
                P("5", "DI", PinKind.Bidirectional, PinSide.Right, 3, "MOSI · IO0"),
                P("6", "CLK", PinKind.Input, PinSide.Right, 2),
                P("7", "HOLD", PinKind.Input, PinSide.Right, 1, "แอกทีฟต่ำ · ไม่ใช้ให้ดึงสูง · IO3"),
                P("8", "VCC", PinKind.Power, PinSide.Right, 0, "3.3V เท่านั้น"),
            ],
        },

        new PartDefinition
        {
            Key = "NRF24L01", Prefix = "U", Name = "nRF24L01+",
            NameTh = "โมดูลวิทยุ 2.4 GHz nRF24L01+",
            Mpn = "nRF24L01P", Package = "8-pin 2×4 module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(1.9, 3.6, 3.3, Icc: 0.0115, Bus: Bus.Spi),
            NoteTh = "ไฟเลี้ยงต้อง 3.3V เท่านั้น ป้อน 5V พังทันที — แต่ขาสัญญาณทน 5V ได้ จึงต่อกับ Arduino UNO "
                   + "โดยไม่ต้องเลเวลชิฟต์ ขอแค่ VCC มาจาก 3.3V · "
                   + "⚠ ขา 3.3V ของ Arduino UNO จ่ายกระแสพีคตอนส่งไม่ไหว ต้องบัดกรี C 10µ คร่อม VCC-GND ที่ตัวโมดูล "
                   + "อาการ 'ต่อครบแล้วสองตัวไม่คุยกัน' ส่วนใหญ่มาจากตรงนี้ · "
                   + "รุ่น PA/LNA ที่มีเสาอากาศกินกระแสพีคเกิน 100 mA และแรงเกินจนรับกันไม่ได้ถ้าวางใกล้กว่าราวหนึ่งเมตร · "
                   + "ต้องตั้ง channel, address และ data rate ให้ตรงกันทั้งสองฝั่ง · "
                   + "มีของปลอมที่ใช้ชิปเก่าไม่มี '+' ปนอยู่มาก ตั้ง 250 kbps แล้วไม่ทำงานคือสัญญาณหนึ่งของของปลอม",
            Pins =
            [
                P("1", "GND", PinKind.Ground, PinSide.Left, 0),
                P("2", "VCC", PinKind.Power, PinSide.Left, 1, "3.3V เท่านั้น"),
                P("3", "CE", PinKind.Input, PinSide.Left, 2, "เปิดรับ/ส่ง"),
                P("4", "CSN", PinKind.Input, PinSide.Left, 3, "SPI chip select · แอกทีฟต่ำ"),
                P("5", "SCK", PinKind.Input, PinSide.Right, 3),
                P("6", "MOSI", PinKind.Input, PinSide.Right, 2),
                P("7", "MISO", PinKind.Output, PinSide.Right, 1),
                P("8", "IRQ", PinKind.OpenDrain, PinSide.Right, 0, "แอกทีฟต่ำ · ไม่ต่อก็ได้"),
            ],
        },

        new PartDefinition
        {
            Key = "RC522", Prefix = "U", Name = "MFRC522 (RC522)",
            NameTh = "โมดูลอ่านบัตร RFID 13.56 MHz RC522",
            Mpn = "MFRC522", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(2.5, 3.6, 3.3, Icc: 0.026, Bus: Bus.Spi),
            NoteTh = "3.3V เท่านั้น ทั้งไฟเลี้ยงและขาสัญญาณ บอร์ดไม่มีเลเวลชิฟต์ — ต่อกับ Arduino UNO ที่ 5V "
                   + "ถือว่าเกินสเปก หลายตัวรอดแต่หลายตัวก็ตาย ควรใส่ตัวแบ่งแรงดันที่ขาอินพุต · "
                   + "อ่านได้เฉพาะบัตร MIFARE 13.56 MHz พวงกุญแจ 125 kHz (EM4100) ที่หน้าตาคล้ายกันอ่านไม่ได้เลย · "
                   + "ระยะอ่านจริงแค่ราว 2–5 cm และลดลงอีกถ้ามีโลหะอยู่หลังบอร์ด · "
                   + "⚠ กุญแจจากโรงงานของ MIFARE Classic คือ FF FF FF FF FF FF และตัวบัตรเองถูกโคลนได้ด้วยเครื่องราคาไม่กี่ร้อยบาท "
                   + "ห้ามใช้ UID ของบัตรเป็นระบบความปลอดภัยจริงจัง",
            Pins =
            [
                P("1", "SDA", PinKind.Input, PinSide.Left, 0, "จริง ๆ คือ SPI SS/CS ไม่ใช่ I²C"),
                P("2", "SCK", PinKind.Input, PinSide.Left, 1),
                P("3", "MOSI", PinKind.Input, PinSide.Left, 2),
                P("4", "MISO", PinKind.Output, PinSide.Left, 3),
                P("5", "IRQ", PinKind.OpenDrain, PinSide.Right, 3, "ไม่ต่อก็ได้ถ้าใช้แบบวนอ่าน"),
                P("6", "GND", PinKind.Ground, PinSide.Right, 2),
                P("7", "RST", PinKind.Input, PinSide.Right, 1, "แอกทีฟต่ำ"),
                P("8", "3.3V", PinKind.Power, PinSide.Right, 0, "3.3V เท่านั้น"),
            ],
        },

        new PartDefinition
        {
            Key = "ESP-01", Prefix = "U", Name = "ESP-01 (ESP8266)",
            NameTh = "โมดูล Wi-Fi ESP-01",
            Mpn = "ESP-01", Package = "8-pin 2×4 header",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 9, BodyHeight = 8,
            Digital = new DigitalSpec(3.0, 3.6, 3.3, Icc: 0.080, Bus: Bus.Uart),
            NoteTh = "3.3V เท่านั้น ทั้งไฟเลี้ยงและขาสัญญาณ ป้อน 5V พังทันที · "
                   + "⚠ กระแสพุ่งถึงราว 300 mA ตอนส่ง — เรกูเลเตอร์ 3.3V บนบอร์ด Arduino จ่ายไม่พอ ต้องใช้เรกูเลเตอร์แยก "
                   + "ที่จ่ายได้อย่างน้อย 500 mA พร้อม C ค้างไว้ อาการรีบูตเองซ้ำ ๆ เกือบทั้งหมดคือไฟไม่พอ · "
                   + "CH_PD (EN) ต้องดึงขึ้น 3.3V เสมอ ไม่งั้นไม่บูต และ RST ต้องดึงสูงด้วย · "
                   + "จะแฟลชเฟิร์มแวร์ต้องดึง GPIO0 ลงกราวด์ตอนจ่ายไฟ · "
                   + "หัวต่อระยะ 2.54 mm แต่เป็นแถวคู่ เสียบเบรดบอร์ดแล้วขาลัดถึงกัน ต้องใช้อะแดปเตอร์ · "
                   + "ใช้งานได้จริงแค่ GPIO0 กับ GPIO2 งานที่ต้องการขามากกว่านี้ให้ใช้ NodeMCU หรือ ESP32",
            Pins =
            [
                P("1", "GND", PinKind.Ground, PinSide.Left, 0),
                P("2", "GPIO2", PinKind.Bidirectional, PinSide.Left, 1, "ต้องสูงตอนบูต"),
                P("3", "GPIO0", PinKind.Bidirectional, PinSide.Left, 2, "ดึงต่ำตอนจ่ายไฟ = โหมดแฟลช"),
                P("4", "RX", PinKind.Input, PinSide.Left, 3, "รับ 3.3V เท่านั้น"),
                P("5", "TX", PinKind.Output, PinSide.Right, 3, "ออก 3.3V"),
                P("6", "CH_PD", PinKind.Input, PinSide.Right, 2, "ต้องดึงขึ้น 3.3V เสมอ"),
                P("7", "RST", PinKind.Input, PinSide.Right, 1, "แอกทีฟต่ำ · ดึงสูงไว้"),
                P("8", "VCC", PinKind.Power, PinSide.Right, 0, "3.3V · พีคถึง ~300 mA"),
            ],
        },

        // ── motor and stepper drivers ─────────────────────────────────────

        new PartDefinition
        {
            Key = "L298N", Prefix = "U", Name = "L298N module",
            NameTh = "โมดูลขับมอเตอร์ L298N",
            Mpn = "L298N", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 12,
            Digital = new DigitalSpec(5.0, 35.0, 12.0, Icc: 0.036, Bus: Bus.Pwm),
            Params = Params((ParamKey.VinMax, 35), (ParamKey.IoutMax, 2)),
            NoteTh = "ข้างในเป็นทรานซิสเตอร์ไบโพลาร์ ตกคร่อมราว 2–3 V ต่อช่อง — จ่าย 12V มอเตอร์ได้จริงราว 9–10 V "
                   + "และไอซีร้อนจนต้องมีฮีตซิงก์ · "
                   + "⚠ เรกูเลเตอร์ 5V บนบอร์ดใช้ได้เมื่อไฟมอเตอร์ไม่เกิน 12V และเสียบจัมเปอร์ 5V-EN ไว้เท่านั้น "
                   + "เกิน 12V ต้องถอดจัมเปอร์แล้วป้อน 5V เข้าเอง ไม่งั้นเรกูเลเตอร์ไหม้ · "
                   + "จัมเปอร์ ENA/ENB ถ้าเสียบค้างไว้จะเปิดเต็มตลอด ปรับความเร็วด้วย PWM ไม่ได้ ต้องถอดออกแล้วต่อเข้าขา PWM · "
                   + "อินพุตรับ 3.3V จาก ESP32 ได้ แต่กราวด์ต้องต่อร่วมกันเสมอ · "
                   + "งานใหม่ใช้ TB6612FNG หรือ DRV8833 คุ้มกว่าทั้งเรื่องแรงดันที่หายไปและความร้อน",
            Pins =
            [
                P("1", "VMS", PinKind.Power, PinSide.Left, 0, "ไฟมอเตอร์ 5–35V"),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "5V", PinKind.Power, PinSide.Left, 2, "ออกจากเรกูเลเตอร์บนบอร์ดเมื่อ VMS ≤ 12V"),
                P("4", "ENA", PinKind.Input, PinSide.Left, 3, "PWM ช่อง A · ต้องถอดจัมเปอร์ก่อน"),
                P("5", "IN1", PinKind.Input, PinSide.Left, 4),
                P("6", "IN2", PinKind.Input, PinSide.Left, 5),
                P("7", "IN3", PinKind.Input, PinSide.Left, 6),
                P("8", "IN4", PinKind.Input, PinSide.Left, 7),
                P("9", "ENB", PinKind.Input, PinSide.Left, 8, "PWM ช่อง B"),
                P("10", "OUT1", PinKind.Output, PinSide.Right, 0, "มอเตอร์ A"),
                P("11", "OUT2", PinKind.Output, PinSide.Right, 1, "มอเตอร์ A"),
                P("12", "OUT3", PinKind.Output, PinSide.Right, 2, "มอเตอร์ B"),
                P("13", "OUT4", PinKind.Output, PinSide.Right, 3, "มอเตอร์ B"),
            ],
        },

        new PartDefinition
        {
            Key = "DRV8833", Prefix = "U", Name = "DRV8833",
            NameTh = "โมดูลขับมอเตอร์ DRV8833",
            Mpn = "DRV8833", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 12,
            Digital = new DigitalSpec(2.7, 10.8, 5.0, Icc: 0.003, Bus: Bus.Pwm),
            Params = Params((ParamKey.VinMax, 10.8), (ParamKey.IoutMax, 1.5)),
            NoteTh = "เป็น MOSFET ตกคร่อมน้อยกว่า L298N มาก มอเตอร์ได้แรงดันเกือบเต็มและตัวไอซีแทบไม่ร้อน · "
                   + "⚠ ไฟมอเตอร์สูงสุด 10.8V เท่านั้น ป้อน 12V พังทันที — ต่างจาก L298N ที่รับได้ถึง 35V "
                   + "คนที่ย้ายจาก L298N มาพลาดตรงนี้บ่อย · "
                   + "1.5 A ต่อช่องต่อเนื่อง พีคได้ 2 A ต่อขนานสองช่องเพื่อเพิ่มกระแสได้ · "
                   + "ไม่มีเรกูเลเตอร์บนบอร์ด ขาลอจิกใช้ได้ทั้ง 3.3V และ 5V · "
                   + "ขา nSLEEP ต้องดึงสูงถึงจะทำงาน บอร์ดบางรุ่นพูลอัปมาให้ บางรุ่นต้องต่อเอง · "
                   + "nFAULT เป็นโอเพนเดรน แจ้งกระแสเกินหรือร้อนเกิน ต้องมีพูลอัปถึงจะอ่านได้",
            Pins =
            [
                P("1", "VM", PinKind.Power, PinSide.Left, 0, "ไฟมอเตอร์ 2.7–10.8V"),
                P("2", "GND", PinKind.Ground, PinSide.Left, 1),
                P("3", "AIN1", PinKind.Input, PinSide.Left, 2),
                P("4", "AIN2", PinKind.Input, PinSide.Left, 3),
                P("5", "BIN1", PinKind.Input, PinSide.Left, 4),
                P("6", "BIN2", PinKind.Input, PinSide.Left, 5),
                P("7", "nSLEEP", PinKind.Input, PinSide.Left, 6, "ต้องดึงสูงถึงจะทำงาน"),
                P("8", "nFAULT", PinKind.OpenDrain, PinSide.Left, 7, "ต้องมีพูลอัป"),
                P("9", "AOUT1", PinKind.Output, PinSide.Right, 0, "มอเตอร์ A"),
                P("10", "AOUT2", PinKind.Output, PinSide.Right, 1, "มอเตอร์ A"),
                P("11", "BOUT1", PinKind.Output, PinSide.Right, 2, "มอเตอร์ B"),
                P("12", "BOUT2", PinKind.Output, PinSide.Right, 3, "มอเตอร์ B"),
            ],
        },

        new PartDefinition
        {
            Key = "A4988", Prefix = "U", Name = "A4988",
            NameTh = "โมดูลขับสเต็ปเปอร์มอเตอร์ A4988",
            Mpn = "A4988", Package = "carrier module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 14,
            Digital = new DigitalSpec(3.0, 5.5, 5.0, Icc: 0.005, Bus: Bus.None),
            Params = Params((ParamKey.VinMax, 35), (ParamKey.IoutMax, 1.0)),
            NoteTh = "⚠ ต้องหมุนตั้งกระแสด้วยโพเทนชิโอมิเตอร์ก่อนต่อมอเตอร์เสมอ วัด Vref เทียบกราวด์ "
                   + "(Vref = I × 8 × Rsense; บอร์ดใช้ Rsense 0.1Ω หรือ 0.05Ω ต่างกัน ต้องอ่านค่าตัวต้านทานบนบอร์ดก่อน) "
                   + "ตั้งเกินแล้วมอเตอร์ร้อนและไดรเวอร์ตาย · "
                   + "⚠ ต้องมี C อิเล็กโทรไลต์ 100µ ขึ้นไปคร่อม VMOT ใกล้บอร์ด ไฟกระชากตอนเสียบสายฆ่าไอซีได้ในครั้งแรกเลย · "
                   + "ห้ามถอดสายมอเตอร์ขณะจ่ายไฟ · "
                   + "RESET กับ SLEEP ต้องต่อถึงกัน ไม่งั้นบอร์ดค้างอยู่ในโหมดรีเซ็ตและไม่ขยับ · "
                   + "จ่ายได้ราว 1 A ต่อขดโดยไม่มีฮีตซิงก์ ถึง 2 A เมื่อมีฮีตซิงก์และลมเป่า · "
                   + "MS1/MS2/MS3 ตั้งไมโครสเต็ป ปล่อยลอยทั้งหมด = สเต็ปเต็ม · "
                   + "DRV8825 หน้าตาเกือบเหมือนกันแต่ตำแหน่งโพเทนชิโอมิเตอร์และสูตร Vref คนละแบบ สลับกันแล้วตั้งกระแสผิด",
            Pins =
            [
                P("1", "VMOT", PinKind.Power, PinSide.Left, 0, "ไฟมอเตอร์ 8–35V"),
                P("2", "GND-M", PinKind.Ground, PinSide.Left, 1, "กราวด์ฝั่งมอเตอร์"),
                P("3", "VDD", PinKind.Power, PinSide.Left, 2, "ไฟลอจิก 3–5.5V"),
                P("4", "GND-L", PinKind.Ground, PinSide.Left, 3, "กราวด์ฝั่งลอจิก"),
                P("5", "STEP", PinKind.Input, PinSide.Left, 4, "หนึ่งพัลส์ = หนึ่งสเต็ป"),
                P("6", "DIR", PinKind.Input, PinSide.Left, 5, "ทิศทาง"),
                P("7", "EN", PinKind.Input, PinSide.Left, 6, "แอกทีฟต่ำ · ปล่อยลอย = เปิดใช้งาน"),
                P("8", "MS1", PinKind.Input, PinSide.Left, 7, "ไมโครสเต็ป"),
                P("9", "MS2", PinKind.Input, PinSide.Left, 8, "ไมโครสเต็ป"),
                P("10", "MS3", PinKind.Input, PinSide.Left, 9, "ไมโครสเต็ป"),
                P("11", "RESET", PinKind.Input, PinSide.Right, 8, "แอกทีฟต่ำ · ต้องต่อกับ SLEEP"),
                P("12", "SLEEP", PinKind.Input, PinSide.Right, 9, "แอกทีฟต่ำ · ต้องต่อกับ RESET"),
                P("13", "1A", PinKind.Output, PinSide.Right, 0, "ขดที่ 1"),
                P("14", "1B", PinKind.Output, PinSide.Right, 1, "ขดที่ 1"),
                P("15", "2A", PinKind.Output, PinSide.Right, 2, "ขดที่ 2"),
                P("16", "2B", PinKind.Output, PinSide.Right, 3, "ขดที่ 2"),
            ],
        },

        new PartDefinition
        {
            Key = "TB6612FNG", Prefix = "U", Name = "TB6612FNG",
            NameTh = "โมดูลขับมอเตอร์ TB6612FNG",
            Mpn = "TB6612FNG", Package = "module",
            Symbol = SymbolShape.IcBody, Spice = SpiceKind.Behavioural,
            Provenance = Provenance.Unverified,
            BodyWidth = 10, BodyHeight = 12,
            Digital = new DigitalSpec(2.7, 5.5, 5.0, Icc: 0.0015, Bus: Bus.Pwm),
            Params = Params((ParamKey.VinMax, 13.5), (ParamKey.IoutMax, 1.2)),
            NoteTh = "ใช้แทน L298N ได้เกือบทุกงานและดีกว่าแทบทุกทาง — ตกคร่อมต่ำ ไม่ต้องใช้ฮีตซิงก์ ตัวเล็กกว่ามาก · "
                   + "แยกไฟสองชุด: VM คือไฟมอเตอร์ 2.5–13.5V ส่วน VCC คือไฟลอจิก 2.7–5.5V ต่อกับ ESP32 ที่ 3.3V ได้ตรง ๆ "
                   + "ห้ามสลับสองขานี้ · "
                   + "⚠ ขา STBY ต้องดึงขึ้นไฟลอจิกถึงจะมีเอาต์พุต ปล่อยลอยแล้วเงียบทั้งบอร์ด — "
                   + "เป็นสาเหตุอันดับหนึ่งของอาการ 'ต่อครบแล้วมอเตอร์ไม่หมุน' · "
                   + "1.2 A ต่อช่องต่อเนื่อง พีค 3.2 A ต่อขนานสองช่องเพื่อเพิ่มกระแสได้ถ้าสั่งพร้อมกัน",
            Pins =
            [
                P("1", "VM", PinKind.Power, PinSide.Left, 0, "ไฟมอเตอร์ 2.5–13.5V"),
                P("2", "VCC", PinKind.Power, PinSide.Left, 1, "ไฟลอจิก 2.7–5.5V"),
                P("3", "GND", PinKind.Ground, PinSide.Left, 2),
                P("4", "STBY", PinKind.Input, PinSide.Left, 3, "ต้องดึงสูงถึงจะทำงาน"),
                P("5", "AIN1", PinKind.Input, PinSide.Left, 4),
                P("6", "AIN2", PinKind.Input, PinSide.Left, 5),
                P("7", "PWMA", PinKind.Input, PinSide.Left, 6),
                P("8", "BIN1", PinKind.Input, PinSide.Left, 7),
                P("9", "BIN2", PinKind.Input, PinSide.Left, 8),
                P("10", "PWMB", PinKind.Input, PinSide.Left, 9),
                P("11", "AO1", PinKind.Output, PinSide.Right, 0, "มอเตอร์ A"),
                P("12", "AO2", PinKind.Output, PinSide.Right, 1, "มอเตอร์ A"),
                P("13", "BO1", PinKind.Output, PinSide.Right, 2, "มอเตอร์ B"),
                P("14", "BO2", PinKind.Output, PinSide.Right, 3, "มอเตอร์ B"),
            ],
        },
    ];
}
