namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// ไดโอด ซีเนอร์ และ LED.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
/// </summary>
public static class CatalogDiode
{
    // Every entry below was typed from general knowledge, not read off a datasheet, so
    // every entry keeps the Unverified default — nothing here overrides it.
    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
        // ── ไดโอดเรียงกระแสมาตรฐาน · 1N400x, 1 A, DO-41 ──────────────────────
        // The whole family differs in reverse voltage and nothing else. t_rr is not a
        // guaranteed parameter on these datasheets, so it is absent rather than invented.
        Dio("1N4001", "ไดโอดเรียงกระแส 1A 50V", "DO-41", "D1N4001",
            vrrm: 50, iF: 1.0, vf: 1.1, ifsm: 30,
            note: "ตระกูล 1N4001–1N4007 ต่างกันที่แรงดันย้อนกลับอย่างเดียว 50/100/200/400/600/800/1000 V · "
                + "เป็นไดโอดช้า ห้ามใช้เป็นตัวฟรีวีลในวงจรสวิตชิงหรือ PWM ความถี่สูง ให้ใช้ UF4007 หรือ FR107 แทน"),
        Dio("1N4002", "ไดโอดเรียงกระแส 1A 100V", "DO-41", "D1N4002", vrrm: 100, iF: 1.0, vf: 1.1, ifsm: 30),
        Dio("1N4003", "ไดโอดเรียงกระแส 1A 200V", "DO-41", "D1N4003", vrrm: 200, iF: 1.0, vf: 1.1, ifsm: 30),
        Dio("1N4004", "ไดโอดเรียงกระแส 1A 400V", "DO-41", "D1N4004", vrrm: 400, iF: 1.0, vf: 1.1, ifsm: 30,
            note: "เบอร์ที่พอสำหรับหม้อแปลงไฟบ้าน 220 V หลังเรียงกระแส แต่คนส่วนใหญ่ใช้ 1N4007 ไปเลยเพราะราคาเท่ากัน"),
        Dio("1N4005", "ไดโอดเรียงกระแส 1A 600V", "DO-41", "D1N4005", vrrm: 600, iF: 1.0, vf: 1.1, ifsm: 30),
        Dio("1N4006", "ไดโอดเรียงกระแส 1A 800V", "DO-41", "D1N4006", vrrm: 800, iF: 1.0, vf: 1.1, ifsm: 30),
        Dio("1N4007", "ไดโอดเรียงกระแส 1A 1000V", "DO-41", "D1N4007", vrrm: 1000, iF: 1.0, vf: 1.1, ifsm: 30,
            note: "เบอร์ที่ควรมีติดกล่องไว้ — ทนแรงดันสูงสุดในตระกูล ใช้แทนเบอร์อื่นได้ทุกตัวโดยราคาไม่ต่างกัน"),

        // ── ไดโอดเรียงกระแส 1.5 A · 1N539x, DO-15 ────────────────────────────
        Dio("1N5391", "ไดโอดเรียงกระแส 1.5A 50V", "DO-15", "D1N5391", vrrm: 50, iF: 1.5, vf: 1.4, ifsm: 50,
            note: "ตระกูล 1N5391–1N5399 · 50/100/200/300/400/500/600/800/1000 V · "
                + "ตัวถัง DO-15 อ้วนกว่า 1N400x ต้องเผื่อระยะรูบนแผ่นปรินต์"),
        Dio("1N5392", "ไดโอดเรียงกระแส 1.5A 100V", "DO-15", "D1N5392", vrrm: 100, iF: 1.5, vf: 1.4, ifsm: 50),
        Dio("1N5393", "ไดโอดเรียงกระแส 1.5A 200V", "DO-15", "D1N5393", vrrm: 200, iF: 1.5, vf: 1.4, ifsm: 50),
        Dio("1N5394", "ไดโอดเรียงกระแส 1.5A 300V", "DO-15", "D1N5394", vrrm: 300, iF: 1.5, vf: 1.4, ifsm: 50),
        Dio("1N5395", "ไดโอดเรียงกระแส 1.5A 400V", "DO-15", "D1N5395", vrrm: 400, iF: 1.5, vf: 1.4, ifsm: 50),
        Dio("1N5396", "ไดโอดเรียงกระแส 1.5A 500V", "DO-15", "D1N5396", vrrm: 500, iF: 1.5, vf: 1.4, ifsm: 50),
        Dio("1N5397", "ไดโอดเรียงกระแส 1.5A 600V", "DO-15", "D1N5397", vrrm: 600, iF: 1.5, vf: 1.4, ifsm: 50),
        Dio("1N5398", "ไดโอดเรียงกระแส 1.5A 800V", "DO-15", "D1N5398", vrrm: 800, iF: 1.5, vf: 1.4, ifsm: 50),
        Dio("1N5399", "ไดโอดเรียงกระแส 1.5A 1000V", "DO-15", "D1N5399", vrrm: 1000, iF: 1.5, vf: 1.4, ifsm: 50),

        // ── ไดโอดเรียงกระแส 3 A · 1N540x, DO-201AD ───────────────────────────
        Dio("1N5400", "ไดโอดเรียงกระแส 3A 50V", "DO-201AD", "D1N5400", vrrm: 50, iF: 3.0, vf: 1.2, ifsm: 200,
            note: "ตระกูล 1N5400–1N5408 · 50/100/200/300/400/500/600/800/1000 V · "
                + "ขาหนากว่า 1N400x มาก ต้องเจาะรูปรินต์ใหญ่ขึ้นและถือขาเป็นทางระบายความร้อน"),
        Dio("1N5401", "ไดโอดเรียงกระแส 3A 100V", "DO-201AD", "D1N5401", vrrm: 100, iF: 3.0, vf: 1.2, ifsm: 200),
        Dio("1N5402", "ไดโอดเรียงกระแส 3A 200V", "DO-201AD", "D1N5402", vrrm: 200, iF: 3.0, vf: 1.2, ifsm: 200),
        Dio("1N5403", "ไดโอดเรียงกระแส 3A 300V", "DO-201AD", "D1N5403", vrrm: 300, iF: 3.0, vf: 1.2, ifsm: 200),
        Dio("1N5404", "ไดโอดเรียงกระแส 3A 400V", "DO-201AD", "D1N5404", vrrm: 400, iF: 3.0, vf: 1.2, ifsm: 200),
        Dio("1N5405", "ไดโอดเรียงกระแส 3A 500V", "DO-201AD", "D1N5405", vrrm: 500, iF: 3.0, vf: 1.2, ifsm: 200),
        Dio("1N5406", "ไดโอดเรียงกระแส 3A 600V", "DO-201AD", "D1N5406", vrrm: 600, iF: 3.0, vf: 1.2, ifsm: 200),
        Dio("1N5407", "ไดโอดเรียงกระแส 3A 800V", "DO-201AD", "D1N5407", vrrm: 800, iF: 3.0, vf: 1.2, ifsm: 200),
        Dio("1N5408", "ไดโอดเรียงกระแส 3A 1000V", "DO-201AD", "D1N5408", vrrm: 1000, iF: 3.0, vf: 1.2, ifsm: 200,
            note: "เบอร์ 3 A ที่หาง่ายที่สุดในร้านไทย ใช้ในภาคจ่ายไฟหม้อแปลงทั่วไป"),

        // ── ไดโอดเรียงกระแส 6 A ──────────────────────────────────────────────
        Dio("6A10", "ไดโอดเรียงกระแส 6A 1000V", "R-6", "D6A10", vrrm: 1000, iF: 6.0, vf: 1.0, ifsm: 400,
            note: "ตระกูล 6A05–6A10 ต่างกันที่แรงดันย้อนกลับ · กระแส 6 A เต็มพิกัดได้เมื่อขาถูกบัดกรีติดทองแดงกว้าง "
                + "และเว้นขาสั้นเท่านั้น ลอยอากาศต้องลดกระแสลงมาก"),

        // ── ฟื้นตัวเร็วและเร็วมาก ────────────────────────────────────────────
        // t_rr is the headline figure of these parts and the datasheets do guarantee it,
        // so these use the shared Diode() helper with all four numbers present.
        Diode("FR107", "ไดโอดฟื้นตัวเร็ว 1A 1000V", "DO-41",
            vrrm: 1000, iF: 1.0, vf: 1.3, trr: 500e-9, spiceModel: "DFR107", ifsm: 30,
            note: "ตระกูล FR101–FR107 · เบอร์ต่ำ trr ราว 150 ns ไล่ขึ้นมาเป็น 500 ns ที่ FR107 "
                + "เพราะทนแรงดันสูงขึ้น ยิ่งเบอร์สูงยิ่งช้า · เร็วพอสำหรับ SMPS ทั่วไป แต่ไม่ใช่ระดับ ultrafast"),
        Diode("UF4007", "ไดโอดฟื้นตัวเร็วมาก 1A 1000V", "DO-41",
            vrrm: 1000, iF: 1.0, vf: 1.7, trr: 75e-9, spiceModel: "DUF4007", ifsm: 30,
            note: "ตัวแทนของ 1N4007 ในวงจรสวิตชิง — ขาและตัวถังเหมือนกันเป๊ะ แต่ trr 75 ns แทนหลักไมโครวินาที · "
                + "แลกมาด้วย Vf 1.7 V สูงกว่า 1N4007 ที่ 1.1 V ร้อนกว่าเมื่อกระแสมาก"),
        Diode("1N4937", "ไดโอดฟื้นตัวเร็ว 1A 600V", "DO-41",
            vrrm: 600, iF: 1.0, vf: 1.2, trr: 200e-9, spiceModel: "D1N4937", ifsm: 30,
            note: "ตระกูล 1N4933–1N4937 ต่างกันที่แรงดันย้อนกลับ 50–600 V · Vf ต่ำกว่า UF4007 แต่ช้ากว่า"),
        Diode("MUR460", "ไดโอดฟื้นตัวเร็วมาก 4A 600V", "DO-201AD",
            vrrm: 600, iF: 4.0, vf: 1.28, trr: 75e-9, spiceModel: "DMUR460",
            note: "ใช้เป็นไดโอดฟรีวีลในอินเวอร์เตอร์และ SMPS กำลังกลาง · "
                + "ระบายความร้อนผ่านขาอย่างเดียว ต้องเว้นขาสั้นและมีทองแดงรองรับ"),
        Dio("BYV26C", "ไดโอดฟื้นตัวเร็วมาก 1A 600V", "SOD-57", "DBYV26C",
            vrrm: 600, iF: 1.0, trr: 30e-9,
            note: "ตระกูล BYV26A/B/C/D/E ต่างกันที่แรงดันย้อนกลับ 200/400/600/800/1000 V — ตัวอักษรท้ายคือสเปกที่ต้องดู · "
                + "trr 30 ns เร็วกว่า UF4007 เท่าตัว ใช้ในภาคสวิตช์ความถี่สูง"),

        // ── ไดโอดสวิตชิงสัญญาณเล็ก ───────────────────────────────────────────
        Diode("1N4148", "ไดโอดสวิตชิงสัญญาณเล็ก 100V", "DO-35",
            vrrm: 100, iF: 0.2, vf: 1.0, trr: 4e-9, spiceModel: "D1N4148",
            note: "ไดโอดสัญญาณที่ใช้มากที่สุดในโลก · กระแสต่อเนื่องแค่ 200 mA อย่าเอาไปเรียงกระแสโหลด · "
                + "Vf 1.0 V คือค่าสูงสุดที่ 10 mA ไม่ใช่ 0.7 V ที่ท่องกันมา"),
        Diode("1N914", "ไดโอดสวิตชิงสัญญาณเล็ก 100V", "DO-35",
            vrrm: 100, iF: 0.2, vf: 1.0, trr: 4e-9, spiceModel: "D1N914",
            note: "เกือบเหมือน 1N4148 ทุกอย่าง ใช้แทนกันได้ในงานทั่วไป — ต่างกันที่สเปกกระแสรั่วย้อนกลับเล็กน้อย"),
        Dio("BAT85", "ไดโอดชอตต์กีสัญญาณ 30V 200mA", "DO-35", "DBAT85",
            vrrm: 30, iF: 0.2, vf: 0.4,
            note: "Vf ต่ำกว่าไดโอดซิลิคอนมาก ใช้ตรวจจับยอดคลื่นสัญญาณเล็กที่ 1N4148 กินแรงดันหมด · "
                + "แลกมาด้วยแรงดันย้อนกลับแค่ 30 V และกระแสรั่วย้อนกลับสูงกว่า 1N4148 หลายเท่า"),

        // ── ชอตต์กี ──────────────────────────────────────────────────────────
        // Reverse recovery is not a specified parameter for these, so no t_rr is recorded.
        Dio("1N5817", "ไดโอดชอตต์กี 1A 20V", "DO-41", "D1N5817", vrrm: 20, iF: 1.0, vf: 0.45, ifsm: 25,
            note: "Vf ต่ำสุดในตระกูล 1N5817/5818/5819 แต่ทนย้อนกลับแค่ 20 V · "
                + "ชอตต์กีมีกระแสรั่วย้อนกลับสูงและเพิ่มเร็วตามอุณหภูมิ อย่าใช้กันไฟย้อนขั้วในจุดที่ร้อนจัด"),
        Dio("1N5818", "ไดโอดชอตต์กี 1A 30V", "DO-41", "D1N5818", vrrm: 30, iF: 1.0, vf: 0.55, ifsm: 25),
        Dio("1N5819", "ไดโอดชอตต์กี 1A 40V", "DO-41", "D1N5819", vrrm: 40, iF: 1.0, vf: 0.60, ifsm: 25,
            note: "เบอร์ชอตต์กี 1 A ที่หาง่ายที่สุด ใช้เป็นไดโอดฟรีวีลและกันไฟย้อนขั้ว · "
                + "แรงดันย้อนกลับ 40 V เป็นเพดานจริง เกินแล้วพังทันที ไม่ใช่แค่รั่ว"),
        Dio("1N5820", "ไดโอดชอตต์กี 3A 20V", "DO-201AD", "D1N5820", vrrm: 20, iF: 3.0, vf: 0.475, ifsm: 80),
        Dio("1N5821", "ไดโอดชอตต์กี 3A 30V", "DO-201AD", "D1N5821", vrrm: 30, iF: 3.0, vf: 0.50, ifsm: 80),
        Dio("1N5822", "ไดโอดชอตต์กี 3A 40V", "DO-201AD", "D1N5822", vrrm: 40, iF: 3.0, vf: 0.525, ifsm: 80,
            note: "ตัวหลักของวงจร buck/boost ขนาดเล็ก · ต้องมีทองแดงระบายความร้อนที่ขา "
                + "ไม่งั้นกระแสรั่วย้อนกลับพุ่งตามอุณหภูมิจนหนีความร้อน (thermal runaway)"),
        Dio("SS34", "ไดโอดชอตต์กี SMD 3A 40V", "SMA (DO-214AC)", "DSS34", vrrm: 40, iF: 3.0, vf: 0.5,
            note: "ตัวถัง SMA หน้าตาเหมือน SS14 (1 A) ทุกประการ ต้องอ่านรหัสบนตัวถัง · "
                + "ตระกูล SS32/SS33/SS34/SS36 = 20/30/40/60 V"),
        Dio("SR360", "ไดโอดชอตต์กี 3A 60V", "DO-201AD", "DSR360", vrrm: 60, iF: 3.0, vf: 0.75,
            note: "SR320/SR340/SR360 = 20/40/60 V กระแส 3 A เท่ากัน · "
                + "ทนย้อนกลับสูงกว่า 1N5822 แต่ Vf สูงตามไปด้วย ไม่ได้ดีกว่าเสมอไป"),
        Dio("BAT54", "ไดโอดชอตต์กี SMD 30V 200mA", "SOT-23", "DBAT54", vrrm: 30, iF: 0.2, vf: 0.8,
            note: "⚠ BAT54 เปล่า ๆ คือไดโอดตัวเดียว แต่ BAT54A/BAT54C/BAT54S เป็นสองตัวในตัวถังเดียว "
                + "และต่อภายในคนละแบบ (A = แอโนดร่วม, C = แคโทดร่วม, S = อนุกรม) หยิบผิดวงจรไม่ทำงาน · "
                + "Vf 0.8 V คือค่าที่ 100 mA ที่กระแสต่ำจะเหลือราว 0.3 V"),

        // ── ไดโอดบริดจ์ ──────────────────────────────────────────────────────
        Bridge("KBP206", "ไดโอดบริดจ์ 2A 600V", "KBP", vrrm: 600, iF: 2.0, vf: 1.1,
            note: "ตระกูล KBP201–KBP210 = 50 V ถึง 1000 V · Vf ที่ระบุคือต่อไดโอดหนึ่งตัว "
                + "แต่กระแสไหลผ่านสองตัวเสมอ แรงดันตกจริงราวสองเท่า"),
        Bridge("KBU810", "ไดโอดบริดจ์ 8A 1000V", "KBU", vrrm: 1000, iF: 8.0, vf: 1.1,
            note: "ตระกูล KBU801–KBU810 = 50 V ถึง 1000 V · 8 A เต็มพิกัดต้องยึดตัวถังกับฮีตซิงก์ "
                + "ถ้าไม่ยึดต้องลดกระแสลงมาก"),
        Bridge("DB107", "ไดโอดบริดจ์ 1A 1000V", "DB-1 (DIP-4)", vrrm: 1000, iF: 1.0, vf: 1.1,
            note: "ตระกูล DB101–DB107 = 50 V ถึง 1000 V · ตัวเล็กสำหรับภาคจ่ายไฟบนแผ่นปรินต์ "
                + "มีทั้งแบบขาเสียบและแบบ SMD ที่ตัวถังเหมือนกัน"),

        // ── ซีเนอร์ 1N47xx · 1 W, DO-41 ──────────────────────────────────────
        // Reverse "rating" of a Zener is its V_z, so no V_RRM is recorded, and forward
        // current is not what these are bought for.
        Zener("1N4728A", 3.3, "3V3"),
        Zener("1N4729A", 3.6, "3V6"),
        Zener("1N4730A", 3.9, "3V9"),
        Zener("1N4731A", 4.3, "4V3"),
        Zener("1N4732A", 4.7, "4V7"),
        Zener("1N4733A", 5.1, "5V1",
            "เบอร์ที่ใช้มากที่สุดกับราง 5 V แต่ซีเนอร์ไม่ใช่เรกูเลเตอร์ — แรงดันขยับตามกระแสโหลด "
            + "ถ้าต้องการ 5 V นิ่งให้ใช้ 7805 หรือ AMS1117"),
        Zener("1N4734A", 5.6, "5V6"),
        Zener("1N4735A", 6.2, "6V2",
            "ช่วง 5–6 V สัมประสิทธิ์อุณหภูมิใกล้ศูนย์ที่สุดในตระกูล เหมาะเป็นแรงดันอ้างอิงมากกว่าเบอร์อื่น"),
        Zener("1N4736A", 6.8, "6V8"),
        Zener("1N4737A", 7.5, "7V5"),
        Zener("1N4738A", 8.2, "8V2"),
        Zener("1N4739A", 9.1, "9V1"),
        Zener("1N4740A", 10, "10V"),
        Zener("1N4741A", 11, "11V"),
        Zener("1N4742A", 12, "12V"),
        Zener("1N4743A", 13, "13V"),
        Zener("1N4744A", 15, "15V",
            "นิยมต่อคร่อมเกต–ซอร์สของมอสเฟตเพื่อกันแรงดันเกินพิกัด Vgs ที่มักอยู่ที่ ±20 V"),
        Zener("1N4745A", 16, "16V"),
        Zener("1N4746A", 18, "18V"),
        Zener("1N4747A", 20, "20V"),
        Zener("1N4748A", 22, "22V"),
        Zener("1N4749A", 24, "24V"),
        Zener("1N4750A", 27, "27V"),
        Zener("1N4751A", 30, "30V"),
        Zener("1N4752A", 33, "33V"),
        Zener("1N4753A", 36, "36V"),
        Zener("1N4754A", 39, "39V"),
        Zener("1N4755A", 43, "43V"),
        Zener("1N4756A", 47, "47V"),
        Zener("1N4757A", 51, "51V"),
    ];

    /// <summary>
    /// The one caution that actually kills these parts, so it is worth repeating on each.
    /// </summary>
    private const string ZenerNote =
        "ต้องมีตัวต้านทานอนุกรมจำกัดกระแสเสมอ — Iz สูงสุด ≈ 1 W ÷ Vz เกินกว่านั้นร้อนพัง · "
        + "ลงท้าย A = ความคลาดเคลื่อน ±5%, ไม่มี A = ±10%";

    /// <summary>
    /// The same entry as <see cref="CatalogBuilder.Diode"/>, but with every figure optional.
    ///
    /// The shared helper takes t_rr as a required number. Most rectifier datasheets never
    /// print one, and a Schottky or a Zener has nothing meaningful to print there at all,
    /// so those entries are built here and simply carry no t_rr — absent, not invented.
    /// Entries whose t_rr is a guaranteed headline figure use the shared helper instead.
    /// </summary>
    private static PartDefinition Dio(
        string mpn, string nameTh, string package, string spiceModel,
        double? vrrm = null, double? iF = null, double? vf = null, double? trr = null,
        double? ifsm = null, double? vz = null, double? pmax = null,
        SymbolShape shape = SymbolShape.Diode, string? note = null)
    {
        var p = new Dictionary<ParamKey, double>();
        void Set(ParamKey key, double? value) { if (value is { } v) p[key] = v; }

        Set(ParamKey.Vrrm, vrrm);
        Set(ParamKey.If, iF);
        Set(ParamKey.Vf, vf);
        Set(ParamKey.Trr, trr);
        Set(ParamKey.Ifsm, ifsm);
        Set(ParamKey.Vz, vz);
        Set(ParamKey.Pmax, pmax);

        return new PartDefinition
        {
            Key = "D-" + mpn,
            Prefix = "D",
            Name = mpn,
            NameTh = nameTh,
            Mpn = mpn,
            Package = package,
            Provenance = Provenance.Unverified,
            Symbol = shape,
            Spice = SpiceKind.Primitive,
            SpiceModel = spiceModel,
            BodyWidth = 3,
            BodyHeight = 2,
            NoteTh = note,
            Params = p,
            Pins =
            [
                P("1", "A", PinKind.Passive, PinSide.Left, 0, "แอโนด"),
                P("2", "K", PinKind.Passive, PinSide.Right, 0, "แคโทด — แถบคาดบนตัวถัง"),
            ],
        };
    }

    /// <summary>A 1 W 1N47xx Zener. V_z is the whole part; nothing else about it is bought.</summary>
    private static PartDefinition Zener(string mpn, double vz, string label, string? extra = null) =>
        Dio(mpn, $"ซีเนอร์ไดโอด {label} 1W", "DO-41", "D" + mpn,
            vz: vz, pmax: 1.0, shape: SymbolShape.Zener,
            note: extra is null ? ZenerNote : ZenerNote + " · " + extra);

    /// <summary>
    /// A four-terminal bridge rectifier: two AC inputs and a DC pair.
    ///
    /// All four pins are passive. Marking "+" as an output would tell the ERC that the
    /// bridge drives the net, and it would then flag the transformer feeding it as a
    /// second driver — a bridge sources nothing it is not given.
    ///
    /// V_f is the drop across one element; current always crosses two of them.
    /// </summary>
    private static PartDefinition Bridge(
        string mpn, string nameTh, string package,
        double vrrm, double iF, double? vf = null, double? ifsm = null,
        string? note = null)
    {
        var p = new Dictionary<ParamKey, double> { [ParamKey.Vrrm] = vrrm, [ParamKey.If] = iF };
        if (vf is { } fwd) p[ParamKey.Vf] = fwd;
        if (ifsm is { } surge) p[ParamKey.Ifsm] = surge;

        return new PartDefinition
        {
            Key = "D-" + mpn,
            Prefix = "D",
            Name = mpn,
            NameTh = nameTh,
            Mpn = mpn,
            Package = package,
            Provenance = Provenance.Unverified,
            Symbol = SymbolShape.IcBody,
            Spice = SpiceKind.Subcircuit,
            SpiceModel = mpn,
            BodyWidth = 6,
            BodyHeight = 6,
            NoteTh = note,
            Params = p,
            Pins =
            [
                P("1", "~1", PinKind.Passive, PinSide.Left, 0, "ไฟสลับเข้า — ไม่มีขั้ว สลับกับ ~2 ได้"),
                P("2", "~2", PinKind.Passive, PinSide.Left, 2, "ไฟสลับเข้า"),
                P("3", "+", PinKind.Passive, PinSide.Right, 0, "ไฟตรงออก ขั้วบวก — ดูสัญลักษณ์ที่พิมพ์บนตัวถัง"),
                P("4", "-", PinKind.Passive, PinSide.Right, 2, "ไฟตรงออก ขั้วลบ"),
            ],
        };
    }
}
