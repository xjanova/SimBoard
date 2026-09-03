namespace SimBoard.Parts;

public enum Lang { Th, En }

/// <summary>
/// Renders findings in the user's language. Part numbers, packages, pinout letters and
/// SI units stay English in both — that is the standard the spec fixes, and technicians
/// read them off the component itself.
/// </summary>
public static class FindingText
{
    public static string Describe(Finding f, Lang lang) => lang == Lang.Th ? Thai(f) : English(f);

    private static string Thai(Finding f) => f.Code switch
    {
        FindingCode.WrongPolarity =>
            $"ผิดขั้ว — ตัวนี้เป็น {f.Args[0]} แต่ของเดิมเป็น {f.Args[1]}",
        FindingCode.BelowRating =>
            $"{f.Args[0]} ต่ำกว่าของเดิม ({f.Args[1]} เทียบกับ {f.Args[2]}) — ใส่แล้วพัง",
        // Scale the advice to the size of the gap. Telling someone to avoid RF over a
        // 17 % difference teaches them to skip the warnings that matter.
        FindingCode.SlowerSwitching when f.Severity == Severity.Caution =>
            $"{f.Args[0]} ต่ำกว่าของเดิมเล็กน้อย ({f.Args[1]} เทียบกับ {f.Args[2]}) — งานทั่วไปไม่มีผล",
        FindingCode.SlowerSwitching =>
            $"{f.Args[0]} แย่กว่าของเดิมมาก ({f.Args[1]} เทียบกับ {f.Args[2]}) — สวิตช์ช้าลง อย่าใช้ในภาค RF หรือสวิตชิ่งความถี่สูง",
        FindingCode.HigherLoss when f.Severity == Severity.Caution =>
            $"{f.Args[0]} สูงกว่าของเดิมเล็กน้อย ({f.Args[1]} เทียบกับ {f.Args[2]}) — สูญเสียมากขึ้นนิดหน่อย งานทั่วไปไม่มีผล",
        FindingCode.HigherLoss =>
            $"{f.Args[0]} สูงกว่าของเดิมมาก ({f.Args[1]} เทียบกับ {f.Args[2]}) — ร้อนขึ้น ประสิทธิภาพลด ต้องเช็คฮีตซิงก์",
        FindingCode.MarginThin =>
            $"{f.Args[0]} เผื่อไว้แค่ {f.Args[1]}% — ใช้ได้แต่ไม่มีที่ให้พลาด",
        FindingCode.HeavilyOverRated =>
            $"{f.Args[0]} สูงกว่าของเดิม {f.Args[1]} เท่า — เกินความจำเป็น อาจเปลี่ยนพฤติกรรมวงจร",
        FindingCode.GainLower =>
            $"อัตราขยายต่ำกว่า (hFE ต่ำสุด {f.Args[0]} เทียบกับ {f.Args[1]}) — วงจรสวิตช์อาจอิ่มตัวไม่พอ ร้อนขึ้น",
        FindingCode.GainHigher =>
            $"อัตราขยายสูงกว่ามาก (hFE สูงสุด {f.Args[0]} เทียบกับ {f.Args[1]}) — ภาคขยายอาจแกว่ง ต้องปรับไบแอส",
        FindingCode.DifferentPinout =>
            $"⚠ ขาไม่เหมือนกัน — ตัวนี้เป็น {f.Args[0]} ของเดิมเป็น {f.Args[1]} ต้องบิดขาก่อนใส่ ไม่งั้นพังทันที",
        FindingCode.DifferentPackage =>
            $"แพ็กเกจต่างกัน — {f.Args[0]} เทียบกับ {f.Args[1]} ลงบอร์ดเดิมไม่ได้ตรง ๆ",
        FindingCode.MissingData =>
            $"ไม่มีข้อมูล {f.Args[0]} ในคลัง — เทียบข้อนี้ไม่ได้",
        FindingCode.UnverifiedData =>
            "ข้อมูลยังไม่ได้ทานกับดาต้าชีต — ตรวจก่อนบัดกรีจริง",
        _ => f.Code.ToString(),
    };

    private static string English(Finding f) => f.Code switch
    {
        FindingCode.WrongPolarity =>
            $"Wrong polarity — this is {f.Args[0]}, the original is {f.Args[1]}",
        FindingCode.BelowRating =>
            $"{f.Args[0]} is below the original ({f.Args[1]} vs {f.Args[2]}) — it will fail",
        FindingCode.SlowerSwitching when f.Severity == Severity.Caution =>
            $"{f.Args[0]} is slightly below the original ({f.Args[1]} vs {f.Args[2]}) — no effect in general-purpose use",
        FindingCode.SlowerSwitching =>
            $"{f.Args[0]} is far worse than the original ({f.Args[1]} vs {f.Args[2]}) — slower switching; keep it out of RF and high-frequency switching",
        FindingCode.HigherLoss when f.Severity == Severity.Caution =>
            $"{f.Args[0]} is slightly higher than the original ({f.Args[1]} vs {f.Args[2]}) — marginally more loss, no effect in general use",
        FindingCode.HigherLoss =>
            $"{f.Args[0]} is much higher than the original ({f.Args[1]} vs {f.Args[2]}) — it will run hotter and less efficiently; check the heatsink",
        FindingCode.MarginThin =>
            $"{f.Args[0]} has only {f.Args[1]}% headroom — workable, with no room for error",
        FindingCode.HeavilyOverRated =>
            $"{f.Args[0]} is {f.Args[1]}× the original — more than needed, and it may change how the circuit behaves",
        FindingCode.GainLower =>
            $"Lower gain (hFE min {f.Args[0]} vs {f.Args[1]}) — a switching stage may not saturate fully and will run hotter",
        FindingCode.GainHigher =>
            $"Much higher gain (hFE max {f.Args[0]} vs {f.Args[1]}) — an amplifier stage may oscillate; rebias it",
        FindingCode.DifferentPinout =>
            $"⚠ Different pinout — this is {f.Args[0]}, the original is {f.Args[1]}. Bend the legs before fitting, or it dies on power-up",
        FindingCode.DifferentPackage =>
            $"Different package — {f.Args[0]} vs {f.Args[1]}; it will not drop into the existing board",
        FindingCode.MissingData =>
            $"No {f.Args[0]} figure in the library — this one could not be compared",
        FindingCode.UnverifiedData =>
            "Figures not yet checked against the datasheet — verify before soldering",
        _ => f.Code.ToString(),
    };
}
