namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// ทรานซิสเตอร์ไบโพลาร์.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
///
/// Every entry below is <see cref="Provenance.Unverified"/>: typed from working knowledge,
/// not read off a datasheet. Parameters that were not known are absent rather than filled
/// in with a plausible-looking number, and part numbers that exist in incompatible
/// variants say so in <see cref="PartDefinition.NoteTh"/> instead of quietly picking one.
///
/// Lead order is the field that gets boards killed, so it is stated per part rather than
/// inherited from the package: TO-92 splits three ways — EBC (2N3904 family, S8050),
/// CBE (BC546-550 family) and ECB (Japanese 2SC/2SA types) — and TO-220/TO-126 power
/// types here are BCE with the metal tab on the collector.
/// </summary>
public static class CatalogBjt
{
    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
        // ── small signal, TO-92 EBC ──────────────────────────────────────────

        Bjt("2N3904", "ทรานซิสเตอร์ NPN อเนกประสงค์", Polarity.Npn, "TO-92", "EBC",
            vceo: 40, ic: 0.2, ptot: 0.625, hfeMin: 100, hfeMax: 300, ft: 300e6,
            spiceModel: "Q2N3904", vebo: 6, vceSat: 0.2,
            note: "ใช้กันมากที่สุดในงานสัญญาณเล็ก · คู่ PNP คือ 2N3906 · ขา EBC ไม่เหมือน BC547 ที่เป็น CBE "
                + "เอามาใส่แทนกันต้องบิดขาก่อน"),

        Bjt("2N3906", "ทรานซิสเตอร์ PNP อเนกประสงค์", Polarity.Pnp, "TO-92", "EBC",
            vceo: 40, ic: 0.2, ptot: 0.625, hfeMin: 100, hfeMax: 300, ft: 250e6,
            spiceModel: "Q2N3906", vebo: 5, vceSat: 0.25,
            note: "คู่ PNP ของ 2N3904 ใช้เป็นตัวสวิตช์ฝั่งไฟบวก · ขา EBC"),

        Bjt("2N2222A", "ทรานซิสเตอร์ NPN สวิตช์กระแสปานกลาง", Polarity.Npn, "TO-92", "EBC",
            vceo: 40, ic: 0.8, ptot: 0.625, hfeMin: 100, hfeMax: 300, ft: 300e6,
            spiceModel: "Q2N2222A", vebo: 6, vceSat: 0.3,
            note: "ตัวถัง TO-92 (เบอร์เต็มมักเป็น PN2222A) ขา EBC — ของเดิมเป็นตัวถังโลหะ TO-18 ขาเรียงคนละแบบ "
                + "และทนกำลังได้มากกว่า อย่าเอาสเปกสองตัวถังมาปนกัน · รุ่นไม่มี A ทน Vceo แค่ 30 V"),

        Bjt("2N2907A", "ทรานซิสเตอร์ PNP สวิตช์กระแสปานกลาง", Polarity.Pnp, "TO-92", "EBC",
            vceo: 60, ic: 0.6, ptot: 0.625, hfeMin: 100, hfeMax: 300, ft: 200e6,
            spiceModel: "Q2N2907A", vebo: 5,
            note: "คู่ PNP ของ 2N2222A · ตัวถัง TO-92 ขา EBC ส่วนตัวถังโลหะ TO-18 ขาเรียงคนละแบบ"),

        Bjt("2N4401", "ทรานซิสเตอร์ NPN สัญญาณเล็กกระแสสูง", Polarity.Npn, "TO-92", "EBC",
            vceo: 40, ic: 0.6, ptot: 0.625, hfeMin: 100, hfeMax: 300, ft: 250e6,
            spiceModel: "Q2N4401", vebo: 6, vceSat: 0.4,
            note: "เหมือน 2N3904 แต่ทนกระแสได้ 600 mA พอขับรีเลย์เล็กได้ · คู่ PNP คือ 2N4403 · ขา EBC"),

        Bjt("2N4403", "ทรานซิสเตอร์ PNP สัญญาณเล็กกระแสสูง", Polarity.Pnp, "TO-92", "EBC",
            vceo: 40, ic: 0.6, ptot: 0.625, hfeMin: 100, hfeMax: 300, ft: 200e6,
            spiceModel: "Q2N4403", vebo: 5,
            note: "คู่ PNP ของ 2N4401 · ขา EBC"),

        Bjt("2N5551", "ทรานซิสเตอร์ NPN แรงดันสูง 160 V", Polarity.Npn, "TO-92", "EBC",
            vceo: 160, ic: 0.6, ptot: 0.625, hfeMin: 80, hfeMax: 250, ft: 100e6,
            spiceModel: "Q2N5551", vebo: 6,
            note: "ทนได้ถึง 160 V ใช้ในวงจรไฟสูงที่กระแสไม่มาก เช่น ขับหลอดหรือไบแอสฝั่งไฟสูง · "
                + "คู่ PNP คือ 2N5401 · ขา EBC"),

        Bjt("2N5401", "ทรานซิสเตอร์ PNP แรงดันสูง 150 V", Polarity.Pnp, "TO-92", "EBC",
            vceo: 150, ic: 0.6, ptot: 0.625, hfeMin: 60, hfeMax: 240, ft: 100e6,
            spiceModel: "Q2N5401",
            note: "คู่ PNP ของ 2N5551 · ขา EBC"),

        // ── small signal, TO-92 CBE (ตระกูล BC ยุโรป) ────────────────────────

        Bjt("BC547", "ทรานซิสเตอร์ NPN สัญญาณเล็ก", Polarity.Npn, "TO-92", "CBE",
            vceo: 45, ic: 0.1, ptot: 0.5, hfeMin: 110, hfeMax: 800, ft: 300e6,
            spiceModel: "QBC547", vebo: 6, vceSat: 0.25,
            note: "ขา CBE ตรงข้ามกับ 2N3904 ที่เป็น EBC — ใส่แทนกันโดยไม่บิดขาแล้ววงจรไม่ทำงาน · "
                + "อักษรท้ายเบอร์บอกช่วงเกน A 110–220, B 200–450, C 420–800 ที่ขายทั่วไปคือ B · "
                + "ทนกระแสแค่ 100 mA ขับรีเลย์ไม่ไหว ต้องใช้ BC337 แทน"),

        Bjt("BC548", "ทรานซิสเตอร์ NPN สัญญาณเล็ก 30 V", Polarity.Npn, "TO-92", "CBE",
            vceo: 30, ic: 0.1, ptot: 0.5, hfeMin: 110, hfeMax: 800, ft: 300e6,
            spiceModel: "QBC548", vebo: 6, vceSat: 0.25,
            note: "เหมือน BC547 ทุกอย่างยกเว้นทนแรงดันแค่ 30 V · ในตระกูลเดียวกัน BC546 = 65 V, BC547 = 45 V · ขา CBE"),

        Bjt("BC549", "ทรานซิสเตอร์ NPN สัญญาณเล็ก เสียงรบกวนต่ำ", Polarity.Npn, "TO-92", "CBE",
            vceo: 30, ic: 0.1, ptot: 0.5, hfeMin: 200, hfeMax: 800, ft: 300e6,
            spiceModel: "QBC549", vebo: 5,
            note: "รุ่นเสียงรบกวนต่ำของตระกูล BC547 ใช้ในภาคปรีไมค์/หัวเข็ม · Vebo แค่ 5 V ต่ำกว่า BC547/548 "
                + "ที่เป็น 6 V ระวังตอนใช้ E เป็นขาอินพุต · ขา CBE"),

        Bjt("BC557", "ทรานซิสเตอร์ PNP สัญญาณเล็ก", Polarity.Pnp, "TO-92", "CBE",
            vceo: 45, ic: 0.1, ptot: 0.5, hfeMin: 125, hfeMax: 500, ft: 150e6,
            spiceModel: "QBC557", vebo: 5,
            note: "คู่ PNP ของ BC547 · ขา CBE · อักษรท้ายเบอร์บอกช่วงเกนเหมือนตระกูล BC547 "
                + "และช่วงของกลุ่มไม่เท่ากับฝั่ง NPN เป๊ะ ๆ"),

        Bjt("BC558", "ทรานซิสเตอร์ PNP สัญญาณเล็ก 30 V", Polarity.Pnp, "TO-92", "CBE",
            vceo: 30, ic: 0.1, ptot: 0.5, hfeMin: 125, hfeMax: 500, ft: 150e6,
            spiceModel: "QBC558", vebo: 5,
            note: "คู่ PNP ของ BC548 ทนแรงดัน 30 V · ขา CBE"),

        Bjt("BC337", "ทรานซิสเตอร์ NPN ขับกระแส 800 mA", Polarity.Npn, "TO-92", null,
            vceo: 45, ic: 0.8, ptot: 0.625, hfeMin: 100, hfeMax: 630, ft: 100e6,
            spiceModel: "QBC337", vebo: 5, vceSat: 0.7,
            note: "ตัวถัง TO-92 เท่า BC547 แต่ทนกระแสถึง 800 mA ใช้ขับรีเลย์เล็กหรือ LED หลายดวงได้ · "
                + "เลขท้ายเบอร์ -16/-25/-40 บอกช่วงเกน (สูงขึ้นตามเลข) · ขา CBE"),

        Bjt("BC327", "ทรานซิสเตอร์ PNP ขับกระแส 800 mA", Polarity.Pnp, "TO-92", null,
            vceo: 45, ic: 0.8, ptot: 0.625, hfeMin: 100, hfeMax: 630, ft: 100e6,
            spiceModel: "QBC327", vebo: 5,
            note: "คู่ PNP ของ BC337 ใช้เป็นตัวขับฝั่งไฟบวก · เลขท้ายเบอร์ -16/-25/-40 บอกช่วงเกน · ขา CBE"),

        // ── small signal, TO-92 ECB (ตระกูลญี่ปุ่นและจีน) ────────────────────

        BjtPartial("S8050", "ทรานซิสเตอร์ NPN สัญญาณเล็กกระแสสูง (จีน)", Polarity.Npn, "TO-92", "EBC",
            vceo: 25, ic: 0.5, spiceModel: "QS8050", ptot: 0.625, ft: 100e6,
            note: "ขา EBC ไม่เหมือน BC547 ที่เป็น CBE — คนมักหยิบมาใส่แทนกันแล้วงงว่าทำไมไม่ทำงาน · "
                + "เบอร์ SS8050 เป็นคนละตัว ทนได้ราว 1.5 A / 1 W ส่วน S8050 ธรรมดาราว 0.5 A ดูเบอร์เต็มบนตัวก่อน · "
                + "รหัสท้ายเบอร์ (D331, J331 ฯลฯ) บอกช่วง hFE ซึ่งไม่ได้ใส่ไว้ที่นี่เพราะแต่ละโรงงานแบ่งไม่เหมือนกัน"),

        BjtPartial("S8550", "ทรานซิสเตอร์ PNP สัญญาณเล็กกระแสสูง (จีน)", Polarity.Pnp, "TO-92", "EBC",
            vceo: 25, ic: 0.5, spiceModel: "QS8550", ptot: 0.625, ft: 100e6,
            note: "คู่ PNP ของ S8050 · ขา EBC · เบอร์ SS8550 เป็นรุ่นกระแสสูงกว่า คนละตัวกัน"),

        Bjt("2SC945", "ทรานซิสเตอร์ NPN สัญญาณเล็กแบบญี่ปุ่น", Polarity.Npn, "TO-92", "ECB",
            vceo: 50, ic: 0.1, ptot: 0.25, hfeMin: 90, hfeMax: 600, ft: 250e6,
            spiceModel: "Q2SC945",
            note: "พิมพ์บนตัวว่า C945 · ขา ECB แบบญี่ปุ่น ไม่ตรงกับทั้ง 2N3904 (EBC) และ BC547 (CBE) · "
                + "คู่ PNP คือ 2SA733 · Ic ที่ผู้ผลิตแต่ละเจ้าให้ไม่เท่ากัน (100–150 mA) ตรงนี้ใช้ค่าต่ำไว้ก่อน · "
                + "อักษรท้ายเบอร์ (P/Q/R) บอกช่วงเกน"),

        Bjt("2SA733", "ทรานซิสเตอร์ PNP สัญญาณเล็กแบบญี่ปุ่น", Polarity.Pnp, "TO-92", "ECB",
            vceo: 50, ic: 0.1, ptot: 0.25, hfeMin: 90, hfeMax: 600, ft: 180e6,
            spiceModel: "Q2SA733",
            note: "พิมพ์บนตัวว่า A733 · คู่ PNP ของ 2SC945 มักเจอคู่กันในบอร์ดวิทยุ/เครื่องเสียงญี่ปุ่น · ขา ECB"),

        Bjt("2SC1815", "ทรานซิสเตอร์ NPN สัญญาณเล็กงานเสียง", Polarity.Npn, "TO-92", "ECB",
            vceo: 50, ic: 0.15, ptot: 0.4, hfeMin: 70, hfeMax: 700, ft: 80e6,
            spiceModel: "Q2SC1815", vebo: 5,
            note: "พิมพ์บนตัวว่า C1815 ใช้มากในภาคขยายเสียง · ขา ECB · อักษรท้ายเบอร์ O/Y/GR/BL บอกช่วงเกน "
                + "70–140 / 120–240 / 200–400 / 350–700 · ของที่ขายตอนนี้ส่วนใหญ่เป็นของผลิตใหม่ "
                + "เกนไม่ตรงกลุ่มที่พิมพ์เสมอไป วงจรที่พึ่งเกนควรวัดก่อนใส่"),

        Bjt("2SA1015", "ทรานซิสเตอร์ PNP สัญญาณเล็กงานเสียง", Polarity.Pnp, "TO-92", "ECB",
            vceo: 50, ic: 0.15, ptot: 0.4, hfeMin: 70, hfeMax: 400, ft: 80e6,
            spiceModel: "Q2SA1015", vebo: 5,
            note: "พิมพ์บนตัวว่า A1015 · คู่ PNP ของ 2SC1815 · ขา ECB · อักษรท้ายเบอร์ O/Y/GR บอกช่วงเกน"),

        // ── กำลังปานกลาง TO-126 / TO-220 (ขา BCE แผ่นหลังคือขา C) ────────────

        Bjt("BD135", "ทรานซิสเตอร์ NPN กำลังปานกลาง 45 V", Polarity.Npn, "TO-126", null,
            vceo: 45, ic: 1.5, ptot: 12.5, hfeMin: 40, hfeMax: 250, ft: 50e6,
            spiceModel: "QBD135", vebo: 5, vceSat: 0.5,
            note: "ตระกูลเดียวกับ BD137 (60 V) และ BD139 (80 V) ต่างกันแค่แรงดัน · "
                + "แผ่นโลหะหลังต่อกับขา C · 12.5 W คือค่าตอนติดฮีตซิงก์ ปล่อยลอยได้จริงราว 1 W"),

        Bjt("BD136", "ทรานซิสเตอร์ PNP กำลังปานกลาง 45 V", Polarity.Pnp, "TO-126", null,
            vceo: 45, ic: 1.5, ptot: 12.5, hfeMin: 40, hfeMax: 250, ft: 75e6,
            spiceModel: "QBD136", vebo: 5,
            note: "คู่ PNP ของ BD135 · ขา BCE แผ่นหลังคือขา C"),

        Bjt("BD137", "ทรานซิสเตอร์ NPN กำลังปานกลาง 60 V", Polarity.Npn, "TO-126", null,
            vceo: 60, ic: 1.5, ptot: 12.5, hfeMin: 40, hfeMax: 250, ft: 50e6,
            spiceModel: "QBD137", vebo: 5, vceSat: 0.5,
            note: "รุ่น 60 V ของตระกูล BD135/137/139 · ขา BCE แผ่นหลังคือขา C · คู่ PNP คือ BD138"),

        Bjt("BD138", "ทรานซิสเตอร์ PNP กำลังปานกลาง 60 V", Polarity.Pnp, "TO-126", null,
            vceo: 60, ic: 1.5, ptot: 12.5, hfeMin: 40, hfeMax: 250, ft: 75e6,
            spiceModel: "QBD138", vebo: 5,
            note: "คู่ PNP ของ BD137 · ขา BCE"),

        Bjt("BD139", "ทรานซิสเตอร์ NPN กำลังปานกลาง 80 V", Polarity.Npn, "TO-126", null,
            vceo: 80, ic: 1.5, ptot: 12.5, hfeMin: 40, hfeMax: 250, ft: 50e6,
            spiceModel: "QBD139", vebo: 5, vceSat: 0.5,
            note: "ตัวขับกำลังปานกลางที่ใช้บ่อยที่สุด · ขา BCE และแผ่นโลหะหลังต่อกับขา C — "
                + "ยึดฮีตซิงก์ร่วมกับตัวอื่นต้องมีแผ่นไมกาคั่น ไม่งั้น C ถึงกันหมด · "
                + "เลขกลุ่มท้ายเบอร์ (-6, -10, -16) บอกช่วงเกน · คู่ PNP คือ BD140"),

        Bjt("BD140", "ทรานซิสเตอร์ PNP กำลังปานกลาง 80 V", Polarity.Pnp, "TO-126", null,
            vceo: 80, ic: 1.5, ptot: 12.5, hfeMin: 40, hfeMax: 250, ft: 75e6,
            spiceModel: "QBD140", vebo: 5,
            note: "คู่ PNP ของ BD139 · ขา BCE แผ่นหลังคือขา C"),

        BjtPartial("2SD882", "ทรานซิสเตอร์ NPN กำลังปานกลาง 3 A", Polarity.Npn, "TO-126", null,
            vceo: 30, ic: 3, spiceModel: "Q2SD882", ptot: 10,
            note: "พิมพ์บนตัวว่า D882 · ทนกระแส 3 A แต่แรงดันแค่ 30 V ห้ามเอาไปใช้ฝั่งไฟสูง · "
                + "ขา BCE แผ่นหลังคือขา C · คู่ PNP คือ 2SB772 เจอคู่กันบ่อยในโมดูลจีน · "
                + "อักษรท้ายเบอร์บอกช่วงเกน (ไม่ได้ใส่ไว้ที่นี่เพราะแต่ละเจ้าแบ่งไม่เหมือนกัน)"),

        BjtPartial("2SB772", "ทรานซิสเตอร์ PNP กำลังปานกลาง 3 A", Polarity.Pnp, "TO-126", null,
            vceo: 30, ic: 3, spiceModel: "Q2SB772", ptot: 10,
            note: "พิมพ์บนตัวว่า B772 · คู่ PNP ของ 2SD882 ใช้เป็นตัวขับฝั่งไฟบวก · ขา BCE"),

        Bjt("TIP31C", "ทรานซิสเตอร์ NPN กำลัง 3 A", Polarity.Npn, "TO-220", "BCE",
            vceo: 100, ic: 3, ptot: 40, hfeMin: 10, hfeMax: 50, ft: 3e6,
            spiceModel: "QTIP31C", vebo: 5, vceSat: 1.2,
            note: "ขา BCE แผ่นหลังคือขา C ต่อถึงตัวถัง · ตัวอักษรท้ายเบอร์คือแรงดัน TIP31 = 40 V, "
                + "A = 60 V, B = 80 V, C = 100 V หยิบผิดตัวคือพัง · 40 W ได้เฉพาะตอนมีฮีตซิงก์ · "
                + "คู่ PNP คือ TIP32C"),

        Bjt("TIP32C", "ทรานซิสเตอร์ PNP กำลัง 3 A", Polarity.Pnp, "TO-220", "BCE",
            vceo: 100, ic: 3, ptot: 40, hfeMin: 10, hfeMax: 50, ft: 3e6,
            spiceModel: "QTIP32C", vebo: 5,
            note: "คู่ PNP ของ TIP31C · ขา BCE · ตัวอักษรท้ายเบอร์คือแรงดันเหมือนฝั่ง NPN"),

        Bjt("TIP41C", "ทรานซิสเตอร์ NPN กำลัง 6 A", Polarity.Npn, "TO-220", "BCE",
            vceo: 100, ic: 6, ptot: 65, hfeMin: 15, hfeMax: 75, ft: 3e6,
            spiceModel: "QTIP41C", vebo: 5, vceSat: 1.5,
            note: "ตัวขับกำลัง 6 A ที่หาง่ายที่สุดตามร้าน · ขา BCE แผ่นหลังคือขา C · "
                + "TIP41/41A/41B/41C = 40/60/80/100 V · เกนต่ำ (hFE ~15 ที่กระแสสูง) "
                + "ต้องมีตัวขับหน้าเสมอ ต่อตรงกับขา MCU ไม่พอกิน · คู่ PNP คือ TIP42C"),

        Bjt("TIP42C", "ทรานซิสเตอร์ PNP กำลัง 6 A", Polarity.Pnp, "TO-220", "BCE",
            vceo: 100, ic: 6, ptot: 65, hfeMin: 15, hfeMax: 75, ft: 3e6,
            spiceModel: "QTIP42C", vebo: 5,
            note: "คู่ PNP ของ TIP41C ใช้ทำภาคเอาต์พุตแบบผลัก-ดึง · ขา BCE"),

        // ── ดาร์ลิงตัน ──────────────────────────────────────────────────────

        BjtPartial("TIP122", "ทรานซิสเตอร์ดาร์ลิงตัน NPN 5 A", Polarity.Npn, "TO-220", "BCE",
            vceo: 100, ic: 5, spiceModel: "QTIP122", ptot: 65, hfeMin: 1000,
            vebo: 5, vceSat: 2.0,
            note: "ดาร์ลิงตัน — เกนสูงมาก ขับด้วยกระแสเบสไม่กี่ mA ได้ แต่ Vbe ตอนนำกระแสสูงราว 2.5 V "
                + "และ Vce(sat) ราว 2 V ที่ 3 A คือทิ้งความร้อนราว 6 W ต้องมีฮีตซิงก์เสมอ · "
                + "มีตัวต้านทานคร่อม B-E และไดโอดกันย้อนอยู่ในตัวแล้ว วัดด้วยมิเตอร์จะอ่านค่าแปลก ๆ · "
                + "TIP120/121/122 = 60/80/100 V · ขา BCE"),

        BjtPartial("TIP127", "ทรานซิสเตอร์ดาร์ลิงตัน PNP 5 A", Polarity.Pnp, "TO-220", "BCE",
            vceo: 100, ic: 5, spiceModel: "QTIP127", ptot: 65, hfeMin: 1000,
            vebo: 5, vceSat: 2.0,
            note: "คู่ PNP ของ TIP122 · TIP125/126/127 = 60/80/100 V · Vce(sat) สูงแบบดาร์ลิงตัน ร้อนง่าย · ขา BCE"),

        BjtPartial("BD679", "ทรานซิสเตอร์ดาร์ลิงตัน NPN 4 A", Polarity.Npn, "TO-126", null,
            vceo: 80, ic: 4, spiceModel: "QBD679", ptot: 40, hfeMin: 750,
            note: "ดาร์ลิงตันตัวถัง TO-126 เล็กกว่า TIP122 แต่ขับได้ 4 A · Vce(sat) สูงแบบดาร์ลิงตัน "
                + "ร้อนเร็วเพราะตัวถังเล็ก ต้องมีฮีตซิงก์ · คู่ PNP คือ BD680 · ขา BCE"),

        BjtPartial("BD680", "ทรานซิสเตอร์ดาร์ลิงตัน PNP 4 A", Polarity.Pnp, "TO-126", null,
            vceo: 80, ic: 4, spiceModel: "QBD680", ptot: 40, hfeMin: 750,
            note: "คู่ PNP ของ BD679 · ขา BCE"),

        // ── ของคลาสสิกและตัวสวิตช์ไฟสูง ──────────────────────────────────────

        BjtPartial("2N3055", "ทรานซิสเตอร์กำลัง NPN ตัวถังโลหะ", Polarity.Npn, "TO-3", null,
            vceo: 60, ic: 15, spiceModel: "Q2N3055", ptot: 115,
            hfeMin: 20, hfeMax: 70, ft: 0.8e6, vebo: 7, vceSat: 1.1,
            note: "ตัวถัง TO-3 มีสองขาคือ B กับ E ส่วนตัวถังโลหะทั้งใบคือขา C — ยึดกับแชสซีโดยไม่มีแผ่นไมกา "
                + "เท่ากับเอา C ลงกราวด์ · ตำแหน่งขา B กับ E ต้องดูจากตัวจริง ตารางส่วนใหญ่วาดกลับด้าน "
                + "จึงไม่ใส่ลำดับขาไว้ที่นี่ · fT ต่ำมาก (~0.8 MHz) ใช้สวิตช์เร็ว ๆ ไม่ได้ และรุ่นเก่ากับรุ่น "
                + "epitaxial ให้ค่าความถี่ไม่เท่ากัน · 115 W คือค่าเมื่อตัวถังอยู่ที่ 25 °C จริง ๆ ได้น้อยกว่านั้นมาก"),

        BjtPartial("MJ2955", "ทรานซิสเตอร์กำลัง PNP ตัวถังโลหะ", Polarity.Pnp, "TO-3", null,
            vceo: 60, ic: 15, spiceModel: "QMJ2955", ptot: 115,
            hfeMin: 20, hfeMax: 70, vebo: 7,
            note: "คู่ PNP ของ 2N3055 ใช้ทำภาคเอาต์พุตแบบผลัก-ดึงในแอมป์เก่า · ตัวถังโลหะคือขา C เหมือนกัน "
                + "ต้องมีแผ่นไมกาคั่นเสมอ"),

        BjtPartial("MJE13003", "ทรานซิสเตอร์สวิตช์แรงดันสูง 1.5 A", Polarity.Npn, "TO-126", null,
            vceo: 400, ic: 1.5, spiceModel: "QMJE13003", hfeMin: 8, vebo: 9,
            note: "ตัวสวิตช์ในหลอดตะเกียบและบัลลาสต์อิเล็กทรอนิกส์ ถอดจากของเสียมาใช้ได้ · "
                + "Vceo 400 V แต่ Vces (ตอนที่ B-E มีทางลัดผ่านตัวต้านทาน) ถึงราว 700 V — "
                + "ตัวเลข 700 V ใช้ได้เฉพาะตอนมีตัวต้านทานคร่อม B-E จริง ๆ · "
                + "บางยี่ห้อเป็น TO-126 บางยี่ห้อ TO-220 กำลังที่ทนได้จึงไม่เท่ากัน ตรงนี้จึงไม่ใส่ค่า Ptot ไว้ · ขา BCE"),

        BjtPartial("MJE13005", "ทรานซิสเตอร์สวิตช์แรงดันสูง 4 A", Polarity.Npn, "TO-220", "BCE",
            vceo: 400, ic: 4, spiceModel: "QMJE13005", ptot: 75, hfeMin: 8, vebo: 9,
            note: "ตัวสวิตช์ในสวิตชิ่งเพาเวอร์ซัพพลายและเครื่องชาร์จ · เหมือน MJE13003 แต่ทน 4 A · "
                + "Vceo 400 V / Vces ราว 700 V · ขา BCE แผ่นหลังคือขา C"),

        BjtPartial("MJE13007", "ทรานซิสเตอร์สวิตช์แรงดันสูง 8 A", Polarity.Npn, "TO-220", "BCE",
            vceo: 400, ic: 8, spiceModel: "QMJE13007", ptot: 80, hfeMin: 8, vebo: 9,
            note: "ตัวใหญ่สุดของตระกูล 13003 / 13005 / 13007 (1.5 / 4 / 8 A) · เกนต่ำมาก (hFE ราว 8) "
                + "ต้องจ่ายกระแสเบสหนัก ๆ ถึงจะอิ่มตัว ขับด้วยขา MCU ตรง ๆ ไม่ได้เด็ดขาด · "
                + "Vceo 400 V / Vces ราว 700 V · ขา BCE แผ่นหลังคือขา C"),
    ];

    /// <summary>
    /// The same entry as <see cref="CatalogBuilder.Bjt"/>, for the parts where a headline
    /// figure genuinely is not known here.
    ///
    /// <c>Bjt</c> requires h_FE, f_T and P_tot; most Darlingtons publish no f_T and no
    /// h_FE maximum, the Chinese TO-92 types are graded differently by every factory, and
    /// a TO-3 has no left-to-right lead order to record at all. The only way to satisfy
    /// the required parameters would be to invent numbers, so they are left absent — an
    /// absent parameter is handled by the substitution engine, an invented one is not.
    /// </summary>
    private static PartDefinition BjtPartial(
        string mpn, string nameTh, Polarity polarity, string package, string? pinout,
        double vceo, double ic, string spiceModel, string? note = null,
        double? ptot = null, double? hfeMin = null, double? hfeMax = null, double? ft = null,
        double? vebo = null, double? vceSat = null)
    {
        var values = Params((ParamKey.Vceo, vceo), (ParamKey.Ic, ic));
        Set(ParamKey.Ptot, ptot);
        Set(ParamKey.HfeMin, hfeMin);
        Set(ParamKey.HfeMax, hfeMax);
        Set(ParamKey.Ft, ft);
        Set(ParamKey.Vebo, vebo);
        Set(ParamKey.VceSat, vceSat);

        return new PartDefinition
        {
            Key = "Q-" + mpn,
            Prefix = "Q",
            Name = mpn,
            NameTh = nameTh,
            Mpn = mpn,
            Package = package,
            Pinout = pinout,
            Polarity = polarity,
            Provenance = Provenance.Unverified,
            Symbol = polarity == Polarity.Npn ? SymbolShape.BjtNpn : SymbolShape.BjtPnp,
            Spice = SpiceKind.Primitive,
            SpiceModel = spiceModel,
            BodyWidth = 4,
            BodyHeight = 4,
            NoteTh = note,
            Params = values,
            Pins =
            [
                P("1", "B", PinKind.Input, PinSide.Left, 1, "เบส"),
                P("2", "C", PinKind.Passive, PinSide.Top, 0, "คอลเลกเตอร์"),
                P("3", "E", PinKind.Passive, PinSide.Bottom, 0, "อิมิตเตอร์"),
            ],
        };

        void Set(ParamKey key, double? value)
        {
            if (value is { } v) values[key] = v;
        }
    }
}
