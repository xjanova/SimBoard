namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// มอสเฟตและ IGBT.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
///
/// Every entry here was typed from general knowledge and is therefore Unverified.
/// R_DS(on) is the datasheet maximum at the gate drive the part is specified for —
/// 10 V for a standard-threshold part, 5 V for a logic-level one — which is why the two
/// figures are not comparable across the two groups without reading the note.
///
/// P-channel ratings are printed negative on the datasheet (−100 V, −23 A). They are
/// stored here as magnitudes so comparisons work, and each P-channel note says so.
/// </summary>
public static class CatalogMosfet
{
    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
        // ── N-channel power, standard threshold (needs ~10 V of gate drive) ──

        Mosfet("IRFZ44N", "มอสเฟตกำลัง N-channel แรงดันต่ำ กระแสสูง", Polarity.NChannel, "TO-220", "GDS",
            vds: 55, id: 49, rdsOn: 0.0175, vgsThMax: 4.0, ptot: 94, qg: 63e-9,
            spiceModel: "IRFZ44N",
            note: "ไม่ใช่ลอจิกเลเวล — R_DS(on) ระบุที่ VGS = 10V ขับตรงจากขา 5V หรือ 3.3V ของ MCU "
                + "มันจะเปิดไม่สุด ตกคร่อมสูง แล้วร้อนจนพัง ถ้าจะขับจาก MCU ให้ใช้ IRLZ44N ซึ่งขาเหมือนกัน · "
                + "ต้องมีตัวต้านทาน 10k ดึงเกตลงกราวด์ ไม่งั้นตอนบูตเกตลอยแล้วโหลดทำงานเอง · "
                + "แท็บโลหะต่อกับขา D ไม่ใช่กราวด์"),

        Mosfet("IRFZ34N", "มอสเฟตกำลัง N-channel 55V กระแสปานกลาง", Polarity.NChannel, "TO-220", "GDS",
            vds: 55, id: 29, rdsOn: 0.040, vgsThMax: 4.0, ptot: 68,
            spiceModel: "IRFZ34N",
            note: "รุ่นกระแสน้อยกว่า IRFZ44N ในตระกูลเดียวกัน ขาและตัวถังเหมือนกัน · "
                + "ไม่ใช่ลอจิกเลเวล คู่ลอจิกเลเวลคือ IRLZ34N ซึ่งใส่แทนกันได้ขาต่อขา"),

        Mosfet("IRF540N", "มอสเฟตกำลัง N-channel 100V ใช้งานทั่วไป", Polarity.NChannel, "TO-220", "GDS",
            vds: 100, id: 33, rdsOn: 0.044, vgsThMax: 4.0, ptot: 130, qg: 71e-9,
            spiceModel: "IRF540N",
            note: "IRF540 รุ่นเก่า (ไม่มี N ท้ายเบอร์) เป็นคนละสเปก — กระแสน้อยกว่าและ R_DS(on) สูงกว่าราวเท่าตัว "
                + "อย่าถือว่าเบอร์เดียวกัน · ไม่ใช่ลอจิกเลเวล ต้องขับเกต 10V · คู่ลอจิกเลเวลคือ IRL540N"),

        Mosfet("IRF3205", "มอสเฟตกำลัง N-channel R_DS(on) ต่ำมาก", Polarity.NChannel, "TO-220", "GDS",
            vds: 55, id: 110, rdsOn: 0.008, vgsThMax: 4.0, ptot: 200, qg: 146e-9,
            spiceModel: "IRF3205",
            note: "นิยมในอินเวอร์เตอร์และคอนโทรลมอเตอร์เพราะ R_DS(on) ต่ำมาก · Qg 146nC สูง "
                + "ขาพอร์ตของ MCU จ่ายกระแสไม่ทัน ต้องใช้ไอซีขับเกตจริง ๆ ไม่งั้นช่วงสวิตช์จะยาวและร้อน · "
                + "ไม่ใช่ลอจิกเลเวล · กระแส 110A เป็นค่าที่ตัวชิปทน ขาและตัวถัง TO-220 จริง ๆ พาไปได้ไม่ถึงครึ่ง"),

        Mosfet("STP55NF06", "มอสเฟตกำลัง N-channel 60V กระแสสูง", Polarity.NChannel, "TO-220", "GDS",
            vds: 60, id: 50, rdsOn: 0.018, vgsThMax: 4.0, ptot: 110,
            spiceModel: "STP55NF06",
            note: "มีรุ่น STP55NF06L ที่เป็นลอจิกเลเวล ต่างกันแค่ตัว L ท้ายเบอร์ แต่การขับเกตคนละเรื่องกัน — "
                + "ตัวนี้ (ไม่มี L) ต้องขับ 10V ซื้อผิดตัวแล้ววงจรจะร้อนโดยไม่รู้สาเหตุ"),

        Mosfet("IRF510", "มอสเฟตกำลัง N-channel ตัวเล็ก 100V", Polarity.NChannel, "TO-220", "GDS",
            vds: 100, id: 5.6, rdsOn: 0.54, vgsThMax: 4.0, ptot: 43,
            spiceModel: "IRF510",
            note: "ตัวเล็ก กระแสน้อย นิยมในภาค RF กำลังต่ำและงานทดลอง · ไม่ใช่ลอจิกเลเวล ต้องขับเกต 10V"),

        Mosfet("IRF520", "มอสเฟตกำลัง N-channel 100V ใช้ในโมดูลสวิตช์", Polarity.NChannel, "TO-220", "GDS",
            vds: 100, id: 9.2, rdsOn: 0.27, vgsThMax: 4.0, ptot: 60,
            spiceModel: "IRF520",
            note: "โมดูล \"IRF520 MOSFET driver\" ที่ขายคู่กับ Arduino ใช้ตัวนี้ และเป็นที่มาของอาการยอดฮิต: "
                + "สั่ง PWM แล้วโหลดหรี่ ตัวมอสเฟตร้อน เพราะเกตมาตรฐานเปิดไม่สุดที่ 5V ยิ่งที่ 3.3V แทบไม่เปิดเลย "
                + "ถ้าจะขับจาก MCU ให้เปลี่ยนตัวบนโมดูลเป็น IRLZ44N หรือ FQP30N06L · "
                + "IRF520N เป็นคนละสเปกกับ IRF520"),

        Mosfet("IRF630", "มอสเฟตกำลัง N-channel 200V", Polarity.NChannel, "TO-220", "GDS",
            vds: 200, id: 9, rdsOn: 0.40, vgsThMax: 4.0, ptot: 74,
            spiceModel: "IRF630",
            note: "IRF630 กับ IRF630N ไม่เท่ากัน — รุ่น N มี R_DS(on) ต่ำกว่าและทนกำลังได้มากกว่า "
                + "ตรวจเบอร์บนตัวก่อนใส่แทน · ไม่ใช่ลอจิกเลเวล"),

        Mosfet("IRF740", "มอสเฟตกำลัง N-channel 400V สำหรับสวิตชิ่ง", Polarity.NChannel, "TO-220", "GDS",
            vds: 400, id: 10, rdsOn: 0.55, vgsThMax: 4.0, ptot: 125,
            spiceModel: "IRF740",
            note: "ใช้ในสวิตชิ่งซัพพลายฝั่งไฟสูง · แท็บโลหะต่อกับขา D ซึ่งอยู่ที่แรงดันสูงถึงระดับไฟเมน "
                + "ห้ามแตะ และต้องมีแผ่นฉนวนกับปลอกรองน็อตก่อนยึดฮีตซิงก์ · ไม่ใช่ลอจิกเลเวล ต้องมีวงจรขับเกต"),

        Mosfet("IRF840", "มอสเฟตกำลัง N-channel 500V สำหรับเพาเวอร์ซัพพลาย", Polarity.NChannel, "TO-220", "GDS",
            vds: 500, id: 8, rdsOn: 0.85, vgsThMax: 4.0, ptot: 125,
            spiceModel: "IRF840",
            note: "รุ่น 500V ที่พบมากที่สุดในเพาเวอร์ซัพพลายและบัลลาสต์ · แท็บ = ขา D อยู่ที่ไฟเมนที่เรียงกระแสแล้ว "
                + "ประมาณ 320VDC ต้องมีฉนวนก่อนยึดฮีตซิงก์และคายประจุตัวเก็บประจุก่อนจับ · ไม่ใช่ลอจิกเลเวล"),

        Mosfet("IRFP460", "มอสเฟตกำลัง N-channel 500V ตัวถัง TO-247", Polarity.NChannel, "TO-247", "GDS",
            vds: 500, id: 20, rdsOn: 0.27, vgsThMax: 4.0, ptot: 280,
            spiceModel: "IRFP460",
            note: "ใช้ในอินเวอร์เตอร์ เครื่องเชื่อม และเครื่องเสียงกำลังสูง · แท็บ = ขา D ที่แรงดันสูง ต้องมีฉนวน · "
                + "Qg สูงมาก ต้องใช้ไอซีขับเกตที่จ่ายกระแสได้ระดับแอมป์ ขับด้วยทรานซิสเตอร์เล็ก ๆ จะสวิตช์ช้าและระเบิด"),

        // ── N-channel power, logic level (opens properly from a 5 V pin) ──

        Mosfet("IRLZ44N", "มอสเฟตกำลัง N-channel ลอจิกเลเวล 55V", Polarity.NChannel, "TO-220", "GDS",
            vds: 55, id: 47, rdsOn: 0.022, vgsThMax: 2.0, ptot: 110,
            spiceModel: "IRLZ44N",
            note: "ลอจิกเลเวล — R_DS(on) ระบุที่ VGS = 5V จึงขับตรงจากขา 5V ของ Arduino ได้ · "
                + "แต่ที่ 3.3V ของ ESP32 ยังเปิดไม่เต็ม ถ้ากระแสโหลดสูงต้องใช้ไอซีขับเกตหรือมอสเฟตที่ระบุ R_DS(on) ที่ 2.5V · "
                + "เป็นตัวแทนขาต่อขาของ IRFZ44N เมื่อต้องขับจาก MCU"),

        Mosfet("IRLZ34N", "มอสเฟตกำลัง N-channel ลอจิกเลเวล 55V กระแสปานกลาง", Polarity.NChannel, "TO-220", "GDS",
            vds: 55, id: 30, rdsOn: 0.035, vgsThMax: 2.0, ptot: 68,
            spiceModel: "IRLZ34N",
            note: "ลอจิกเลเวล ขับจาก 5V ได้ · เป็นตัวแทนขาต่อขาของ IRFZ34N สำหรับงานที่ขับตรงจาก MCU"),

        Mosfet("IRL540N", "มอสเฟตกำลัง N-channel ลอจิกเลเวล 100V", Polarity.NChannel, "TO-220", "GDS",
            vds: 100, id: 36, rdsOn: 0.044, vgsThMax: 2.0, ptot: 140,
            spiceModel: "IRL540N",
            note: "ลอจิกเลเวลรุ่น 100V · ต่างจาก IRF540N แค่ตัวอักษรเดียว แต่ IRF540N ต้องขับเกต 10V "
                + "หยิบผิดตัวจากลิ้นชักเดียวกันคืออาการมอสเฟตร้อนโดยไม่รู้สาเหตุ"),

        Mosfet("FQP30N06L", "มอสเฟตกำลัง N-channel ลอจิกเลเวล 60V", Polarity.NChannel, "TO-220", "GDS",
            vds: 60, id: 32, rdsOn: 0.035, vgsThMax: 2.5, ptot: 79,
            spiceModel: "FQP30N06L",
            note: "ลอจิกเลเวล เปิดได้เต็มที่ VGS = 5V เป็นตัวที่แนะนำกันมากที่สุดในงาน Arduino · "
                + "FQP30N06 (ไม่มี L) เป็นเกตมาตรฐาน ขับจาก MCU ไม่ได้ — ตัว L ท้ายเบอร์คือทั้งหมดของความต่าง"),

        // ── P-channel (datasheet figures are negative; stored here as magnitudes) ──

        Mosfet("IRF9540N", "มอสเฟตกำลัง P-channel 100V", Polarity.PChannel, "TO-220", "GDS",
            vds: 100, id: 23, rdsOn: 0.117, vgsThMax: 4.0, ptot: 140,
            spiceModel: "IRF9540N",
            note: "P-channel เปิดเมื่อเกตต่ำกว่าซอร์ส ค่าจริงในดาต้าชีตเป็นลบทั้งหมด (−100V, −23A, VGS(th) −4V) "
                + "ที่นี่เก็บเป็นค่าสัมบูรณ์ · ใช้เป็นสวิตช์ฝั่งไฟบวก ซอร์สต้องต่อไฟบวก ไม่ใช่ต่อโหลด "
                + "ต่อกลับด้านไดโอดตัวในจะนำตลอดและปิดไม่ลง · ตอนปิดต้องมีตัวต้านทานดึงเกตขึ้นไฟบวก · "
                + "คู่ N-channel คือ IRF540N"),

        Mosfet("IRF4905", "มอสเฟตกำลัง P-channel 55V กระแสสูง", Polarity.PChannel, "TO-220", "GDS",
            vds: 55, id: 74, rdsOn: 0.020, vgsThMax: 4.0, ptot: 200,
            spiceModel: "IRF4905",
            note: "P-channel R_DS(on) ต่ำ นิยมเป็นสวิตช์ฝั่งไฟบวกและวงจรกันไฟกลับขั้ว · ค่าจริงเป็นลบ · "
                + "VGS ทนได้ ±20V ถ้าไฟที่ซอร์สสูงกว่านั้นต้องมีซีเนอร์คร่อม G-S ไม่งั้นเกตทะลุตอนต่อไฟ"),

        Mosfet("IRF9Z34N", "มอสเฟตกำลัง P-channel 55V สำหรับกันไฟกลับขั้ว", Polarity.PChannel, "TO-220", "GDS",
            vds: 55, id: 19, rdsOn: 0.100, vgsThMax: 4.0, ptot: 68,
            spiceModel: "IRF9Z34N",
            note: "ใช้กันไฟกลับขั้วแทนไดโอดอนุกรม เสียแรงดันน้อยกว่ามาก — ต่อโดยให้ไดโอดตัวในหันตามทางกระแสปกติ "
                + "แล้วมอสเฟตจะขนานทับไดโอดตัวเองเมื่อเปิด · ค่าจริงเป็นลบ"),

        // ── small-signal ──────────────────────────────────────────────────

        Mosfet("2N7000", "มอสเฟตสัญญาณเล็ก N-channel ตัวถัง TO-92", Polarity.NChannel, "TO-92", "SGD",
            vds: 60, id: 0.2, rdsOn: 5.0, vgsThMax: 3.0, ptot: 0.35,
            spiceModel: "2N7000",
            note: "หันด้านแบนเข้าหาตัว ขาลง ซ้ายไปขวาคือ S G D — สลับกับ BS170 ที่เป็น D G S ทั้งที่ตัวถังเหมือนกัน "
                + "ใส่แทนกันตรง ๆ ไม่ได้ · กระแสต่อเนื่องแค่ 200mA ใช้ขับรีเลย์เล็ก LED หรือแปลงระดับลอจิก ไม่ใช่มอเตอร์ · "
                + "VGS(th) สูงได้ถึง 3V จึงขับจาก 3.3V ไม่แน่นอน ที่ 5V ถึงจะใช้ได้จริง"),

        Mosfet("BS170", "มอสเฟตสัญญาณเล็ก N-channel ตัวถัง TO-92", Polarity.NChannel, "TO-92", "DGS",
            vds: 60, id: 0.5, rdsOn: 5.0, vgsThMax: 3.0, ptot: 0.83,
            spiceModel: "BS170",
            note: "ลำดับขาเป็น D G S สลับด้านกับ 2N7000 ที่เป็น S G D — นี่คือกับดักที่คนโดนบ่อยที่สุดของสองตัวนี้ "
                + "เพราะสเปกใกล้กันมากจนเข้าใจว่าใส่แทนกันได้ · VGS(th) ถึง 3V เหมือนกัน ขับจาก 3.3V ไม่แน่นอน"),

        Mosfet("2N7002", "มอสเฟตสัญญาณเล็ก N-channel ตัวถัง SOT-23", Polarity.NChannel, "SOT-23", "GSD",
            vds: 60, id: 0.115, rdsOn: 7.5, vgsThMax: 2.5, ptot: 0.3,
            spiceModel: "2N7002",
            note: "ขา 1 = G, 2 = S, 3 = D · กระแสต่อเนื่องราว 115mA เท่านั้น ใช้เป็นสวิตช์สัญญาณและแปลงระดับลอจิก "
                + "ไม่ใช่ขับโหลด · R_DS(on) และกำลังที่ทนได้ต่างกันตามผู้ผลิตพอสมควร ตัวเลขที่นี่อิงสเปกฝั่ง Nexperia"),

        Mosfet("BSS138", "มอสเฟต N-channel SOT-23 ลอจิกเลเวล สำหรับแปลงระดับ I²C", Polarity.NChannel, "SOT-23", "GSD",
            vds: 50, id: 0.2, rdsOn: 3.5, vgsThMax: 1.5, ptot: 0.36,
            spiceModel: "BSS138",
            note: "ตัวที่ใช้ในวงจรแปลงระดับลอจิกสองทิศทาง (โมดูล level shifter 4 ช่องสีแดง) · "
                + "ต้องมีพูลอัปทั้งสองฝั่ง ฝั่ง 3.3V และฝั่ง 5V ไม่งั้นวงจรไม่ทำงานเลย · "
                + "ฝั่งแรงดันต่ำต้องต่อที่ซอร์ส ฝั่งสูงต่อที่เดรน สลับด้านแล้วใช้ไม่ได้ · "
                + "กระแสได้แค่ 200mA ใช้กับสัญญาณเท่านั้น"),

        Mosfet("AO3400A", "มอสเฟต N-channel SOT-23 ลอจิกเลเวล กระแสสูง", Polarity.NChannel, "SOT-23", "GSD",
            vds: 30, id: 5.7, rdsOn: 0.028, vgsThMax: 1.4, ptot: 1.4,
            spiceModel: "AO3400A",
            note: "ลอจิกเลเวลจริง มี R_DS(on) ระบุถึงที่ VGS = 2.5V จึงขับตรงจาก ESP32 3.3V ได้ "
                + "เป็นเหตุผลที่พบตัวนี้ในโมดูลจีนแทบทุกตัว · ขา 1 = G, 2 = S, 3 = D · "
                + "SOT-23 ระบายความร้อนผ่านลายทองแดงบนแผ่นวงจรอย่างเดียว กระแสที่ใช้ได้จริงต่ำกว่า 5.7A มาก "
                + "ถ้าลายทองแดงเล็ก"),

        // ── IGBT ──────────────────────────────────────────────────────────
        //
        // Written out rather than through Mosfet(): an IGBT has no R_DS(on) — its
        // collector-emitter drop is a roughly fixed voltage, not a resistance — and
        // passing one would be inventing a figure that does not exist for the device.

        new()
        {
            Key = "Q-FGA25N120ANTD",
            Prefix = "Q",
            Name = "FGA25N120ANTD",
            NameTh = "ไอจีบีที 1200V 25A พร้อมไดโอดเร็วในตัว",
            Mpn = "FGA25N120ANTD",
            Package = "TO-3P",
            Pinout = "GCE",
            Polarity = Polarity.NChannel,
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.MosfetN,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = "FGA25N120ANTD",
            BodyWidth = 4,
            BodyHeight = 4,
            NoteTh = "IGBT ไม่ใช่มอสเฟต — แรงดันตกคร่อม C-E เป็นค่าคงที่ราว 2V ไม่ว่ากระแสจะเท่าไร "
                   + "จึงคุ้มที่แรงดันสูงกระแสสูง แต่ที่แรงดันต่ำหรือกระแสน้อยแพ้มอสเฟตชัดเจน "
                   + "และสวิตช์เร็วมาก ๆ ไม่ได้เพราะมีหางกระแสตอนปิด · ขาเป็น G C E ไม่ใช่ G D S · "
                   + "มีไดโอดเร็วต่อกลับขั้วอยู่ในตัว ใช้กับโหลดขดลวดได้เลย · "
                   + "1200V ต้องคิดเรื่องระยะห่างของลายวงจรและความปลอดภัยตั้งแต่ออกแบบ",
            Params = Params((ParamKey.Vceo, 1200), (ParamKey.Ic, 25)),
            Pins =
            [
                P("1", "G", PinKind.Input, PinSide.Left, 1, "เกต"),
                P("2", "C", PinKind.Passive, PinSide.Top, 0, "คอลเลกเตอร์"),
                P("3", "E", PinKind.Passive, PinSide.Bottom, 0, "อิมิตเตอร์"),
            ],
        },

        new()
        {
            Key = "Q-H20R1203",
            Prefix = "Q",
            Name = "H20R1203",
            NameTh = "ไอจีบีที 1200V 20A ที่ใช้ในเตาแม่เหล็กไฟฟ้า",
            Mpn = "IHW20N120R3",
            Package = "TO-247",
            Pinout = "GCE",
            Polarity = Polarity.NChannel,
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.MosfetN,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = "H20R1203",
            BodyWidth = 4,
            BodyHeight = 4,
            NoteTh = "เบอร์ที่พิมพ์บนตัวคือ H20R1203 ส่วนเบอร์ผู้ผลิตคือ IHW20N120R3 — ร้านอะไหล่เรียกคนละอย่างกัน · "
                   + "เป็นตัวที่พังบ่อยที่สุดในเตาแม่เหล็กไฟฟ้า แต่เปลี่ยนตัวเดียวมักพังซ้ำ "
                   + "ต้องตรวจไดโอดบริดจ์ ตัวเก็บประจุเรโซแนนซ์ และวงจรขับเกตด้วยทุกครั้ง · "
                   + "แท็บโลหะต่อกับขา C ซึ่งอยู่ที่ไฟเมนที่เรียงกระแสแล้ว ต้องคายประจุก่อนจับ",
            Params = Params((ParamKey.Vceo, 1200), (ParamKey.Ic, 20)),
            Pins =
            [
                P("1", "G", PinKind.Input, PinSide.Left, 1, "เกต"),
                P("2", "C", PinKind.Passive, PinSide.Top, 0, "คอลเลกเตอร์"),
                P("3", "E", PinKind.Passive, PinSide.Bottom, 0, "อิมิตเตอร์"),
            ],
        },
    ];
}
