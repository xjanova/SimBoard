using SimBoard.Parts;

// ─────────────────────────────────────────────────────────────────────────────
// เบอร์แทน / cross-reference — ask what can stand in for a part, and why.
//   dotnet run --project tools/SimBoard.Xref -- 2N3904
//   dotnet run --project tools/SimBoard.Xref -- 1N4148 --en --all
// ─────────────────────────────────────────────────────────────────────────────

var lang = args.Contains("--en") ? Lang.En : Lang.Th;
bool showRejected = args.Contains("--all");
var query = args.FirstOrDefault(a => !a.StartsWith('-'));

if (query is null)
{
    Console.WriteLine(lang == Lang.Th
        ? "ใช้: xref <เบอร์อุปกรณ์> [--en] [--all]"
        : "usage: xref <part number> [--en] [--all]");
    Console.WriteLine($"\n{PartLibrary.All.Count} parts in the library:");
    foreach (var g in PartLibrary.All.GroupBy(p => p.Category))
        Console.WriteLine($"  {g.Key,-8} {string.Join("  ", g.Select(p => p.Mpn))}");
    return 2;
}

var original = PartLibrary.Find(query);
if (original is null)
{
    Console.WriteLine(lang == Lang.Th
        ? $"ไม่พบเบอร์ '{query}' ในคลัง — คลังตอนนี้มี {PartLibrary.All.Count} เบอร์"
        : $"'{query}' is not in the library — it currently holds {PartLibrary.All.Count} parts.");
    return 1;
}

// The warning goes first, not in a footnote. Someone is about to solder.
if (original.Provenance == Provenance.Unverified)
    Console.WriteLine(lang == Lang.Th
        ? "\n⚠  ตัวเลขในคลังนี้ยังไม่ได้ทานกับดาต้าชีต — ใช้เป็นตัวช่วยคัดกรอง ไม่ใช่คำตอบสุดท้าย\n"
        : "\n⚠  These figures have not been checked against datasheets — use them to narrow the field, not to decide.\n");

Console.WriteLine($"── {original.Mpn} · {original.Description}");
Console.WriteLine($"   {original.Package}" +
                  (original.Pinout is { Length: > 0 } pin ? $" · ขา {pin}" : "") +
                  (original.Polarity != Polarity.None ? $" · {original.Polarity}" : ""));
Console.WriteLine($"   {Params(original)}");

var subs = Substitution.Find(original, PartLibrary.All, limit: 8, includeRejected: showRejected);
if (subs.Count == 0)
{
    Console.WriteLine(lang == Lang.Th
        ? "\nไม่มีเบอร์แทนในคลังตอนนี้"
        : "\nNo substitute in the library yet.");
    return 0;
}

Console.WriteLine();
Console.WriteLine(lang == Lang.Th ? "เบอร์ที่แทนได้ เรียงตามความเหมาะสม:" : "Substitutes, best fit first:");

int rank = 0;
foreach (var s in subs)
{
    rank++;
    string mark = !s.Usable ? "✕" : s.NeedsAttention ? "!" : "✓";
    Console.WriteLine();
    Console.WriteLine($"  {mark} {rank}. {s.Part.Mpn,-9} {s.Part.Package,-7} " +
                      $"{(s.Part.Pinout is { Length: > 0 } p ? p : "   "),-4} {s.Part.Description}");
    Console.WriteLine($"       {Params(s.Part)}");

    foreach (var f in s.Findings.Where(f => f.Code != FindingCode.UnverifiedData)
                                .OrderBy(f => f.Severity))
        Console.WriteLine($"       {Bullet(f.Severity)} {FindingText.Describe(f, lang)}");
}

Console.WriteLine();
Console.WriteLine(lang == Lang.Th
    ? "  ✓ ใส่แทนได้เลย   ! ใส่ได้แต่มีข้อควรระวัง   ✕ ห้ามใส่"
    : "  ✓ drop-in   ! usable with caveats   ✕ do not fit");
return 0;

static string Bullet(Severity s) => s switch
{
    Severity.Blocking => "✕",
    Severity.Serious => "!",
    _ => "·",
};

static string Params(Part p) => p.Category switch
{
    PartCategory.Bjt =>
        $"Vceo {Eng.Format(p, ParamKey.Vceo)} · Ic {Eng.Format(p, ParamKey.Ic)} · " +
        $"Ptot {Eng.Format(p, ParamKey.Ptot)} · hFE {p.Get(ParamKey.HfeMin):F0}–{p.Get(ParamKey.HfeMax):F0} · " +
        $"fT {Eng.Format(p, ParamKey.Ft)}",
    PartCategory.Mosfet =>
        $"Vds {Eng.Format(p, ParamKey.Vds)} · Id {Eng.Format(p, ParamKey.Id)} · " +
        $"RdsOn {Eng.Format(p, ParamKey.RdsOn)} · Vgs(th) ≤{Eng.Format(p, ParamKey.VgsThMax)} · " +
        $"Qg {Eng.Format(p, ParamKey.Qg)}",
    PartCategory.Diode =>
        $"Vrrm {Eng.Format(p, ParamKey.Vrrm)} · If {Eng.Format(p, ParamKey.If)} · " +
        $"Vf {Eng.Format(p, ParamKey.Vf)} · trr {Eng.Format(p, ParamKey.Trr)}",
    _ => string.Join(" · ", p.Params.Select(kv => $"{kv.Key} {Eng.Format(kv.Value, Eng.UnitOf(kv.Key))}")),
};
