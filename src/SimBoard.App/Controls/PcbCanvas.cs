using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SimBoard.Document;

namespace SimBoard.App.Controls;

/// <summary>Which lead arrangement the declared package name actually specifies.</summary>
public enum PcbFootprintKind
{
    /// <summary>DIP / SOIC / SOP — two rows, pin 1 at the top left, numbering counter-clockwise.</summary>
    DualRow,
    /// <summary>TO / SOT — every lead in one row.</summary>
    Inline,
    /// <summary>DO / SOD — a two-lead body with the leads leaving opposite ends.</summary>
    Axial,
    /// <summary>A can with its leads underneath — crystals, radial electrolytics.</summary>
    Radial,
    /// <summary>
    /// The package is absent, or it is prose ("module", "breakout", "3-wire") that names
    /// no lead arrangement at all. Drawn dashed and marked, never as a real footprint.
    /// </summary>
    Generic,
}

/// <summary>One pad, in grid steps relative to the board origin.</summary>
public sealed record PcbPad(string PinNumber, string PinName, double X, double Y, bool IsFirst);

/// <summary>One part's footprint, where the placement put it.</summary>
public sealed record PcbPlacement(
    PartInstance Part,
    PcbFootprintKind Kind,
    /// <summary>The catalogue's own package string, verbatim. Null when it declares none.</summary>
    string? DeclaredPackage,
    IReadOnlyList<PcbPad> Pads,
    /// <summary>Silkscreen body box in grid steps. A drawing, not a datasheet dimension.</summary>
    Rect Outline);

/// <summary>
/// One ratsnest line: two pads that must end up on the same net and are not joined yet.
/// It carries the owning parts so the view can highlight what a selected part reaches
/// without matching floating-point coordinates back to their pads.
/// </summary>
public sealed record RatsnestLink(
    string Net, bool IsGround, string FromPartId, Point From, string ToPartId, Point To);

/// <summary>
/// The board view: the shared <see cref="CircuitDocument"/> drawn as a PCB <b>placement</b>
/// with a ratsnest. There is no routed copper here, and the view says so on its face.
///
/// Placement is parts in document order onto shelves, left to right, wrapping at a width
/// picked from the total footprint area. That is deliberate, and it is the same decision
/// <see cref="BreadboardLayout"/> already made and documented: minimising ratsnest length
/// is the combinatorial problem, and a solver that half-does it produces a board that
/// looks authoritative and is not. What this does produce is a placement whose every pad
/// and every connection is exactly what the document says, which a person can then move.
///
/// The ratsnest itself IS computed, and computed properly — a minimum spanning tree over
/// each net's pads, which is what a ratsnest is defined to be. That is an exact,
/// polynomial answer, so it is honest to show; placement is not, so it is not attempted.
///
/// Nothing here is written back to the document. A pad position is this view's own
/// arithmetic, not a stored fact: <see cref="PartInstance.Position"/> is where the part
/// sits on the schematic, and dragging a footprint would drag the symbol with it.
///
/// Geometry is in grid steps, and a step is a drawing unit rather than a physical pitch.
/// The catalogue declares no pitch, no row span and no body size for any part, so naming
/// the step 0.1 inch would state a millimetre dimension the document does not hold — and
/// would be wrong for the parts it actually holds: ATMEGA328P is a 600-mil DIP-28 and
/// W25Q32 is a "SOIC-8 208-mil" on 1.27 mm legs, both drawn here on the same one step.
/// Every artwork constant is PcbView's own number divided by that screen's 24 px step,
/// so at <see cref="MidStep"/> this canvas is pixel-for-pixel the mock's board.
/// </summary>
public class PcbCanvas : Control
{
    // ── PcbView's artwork, as ratios of one drawing step ─────────────────
    private const double Unit = 24;                      // px per step in the mock's 840 × 700 space
    private const double MidStep = Unit;                 // the zoom at which this canvas IS the mock
    private const double MinStep = 3, MaxStep = 56;

    private const double PadW = 14 / Unit;               // the mock's 14 × 16 copper pad
    private const double PadH = 16 / Unit;
    private const double DrillR = 3.4 / Unit;            // its via hole
    private const double FieldPadR = 4 / Unit;           // the .22-opacity pad field
    private const double FieldHoleR = 1.6 / Unit;
    private const double SilkStroke = 1.6 / Unit;
    private const double RatsStroke = 1.4 / Unit;
    private const double EdgeStroke = 2 / Unit;
    private const double BoardRadius = 6 / Unit;
    private const double DotR = 1 / Unit;                // the well's 2 × 2 grid dot
    private const double DotPitch = 16 / Unit;
    private const double DesignatorSize = 12 / Unit;
    private const double HandleSize = 8 / Unit;
    private const double MarginX = 38 / Unit;            // bare substrate outside the hole field
    private const double MarginY = 40 / Unit;

    /// <summary>The mock's own drilled field, 27 × 21 holes — the empty board.</summary>
    private const int MinFieldCols = 27;
    private const int MinFieldRows = 21;

    private const double Gutter = 2;                     // steps of clearance between footprints
    private const int FieldMargin = 2;                   // steps of bare field around the placement

    // Body spacings, in drawing steps — not dimensions. Nothing in the catalogue states a
    // row span or a lead pitch, so every part gets the same ones and CollectNotes says so
    // rather than letting the pin-1 notch and the printed package name imply otherwise.
    private const double DualRowGap = 3;
    private const double AxialSpan = 3;

    // Artwork colours, straight from PcbView. Three of the mock's are already the
    // theme-independent palette, so those come from SymbolRenderer rather than a second
    // copy of the same hex: ratsnest cyan is PinDot, selection amber is Selected.
    private static readonly IBrush Well = new SolidColorBrush(Color.Parse("#0e1013"));
    private static readonly IBrush GridDot = new SolidColorBrush(Color.Parse("#2b3440"));
    private static readonly IBrush Substrate = new SolidColorBrush(Color.Parse("#12311f"));
    private static readonly IBrush SubstrateEdge = new SolidColorBrush(Color.Parse("#2f6b47"));
    private static readonly IBrush FieldCopper = new SolidColorBrush(Color.Parse("#c69a5c"));
    private static readonly IBrush FieldHole = new SolidColorBrush(Color.Parse("#101215"));
    private static readonly IBrush Copper = new SolidColorBrush(Color.Parse("#c98b4b"));
    private static readonly IBrush Drill = new SolidColorBrush(Color.Parse("#0e1013"));
    private static readonly IBrush Silk = new SolidColorBrush(Color.Parse("#e6e6e0"));
    private static readonly IBrush SilkText = new SolidColorBrush(Color.Parse("#f0efe9"));
    private static readonly IBrush KeepOut = new SolidColorBrush(Color.Parse("#8fd0a8"));
    private static readonly IBrush Footer = new SolidColorBrush(Color.Parse("#6f8ba1"));

    // Package-name families. Prefix matching, not a lookup table: the catalogue's package
    // strings carry suffixes ("DIP-14 half-can", "SOIC-8 208-mil"), and a table would
    // imply the 40-odd values are all covered when most of them are prose.
    private static readonly string[] DualRowFamilies =
        ["DIP-", "PDIP-", "SOIC-", "SOP-", "SSOP-", "TSSOP-", "MSOP-"];
    private static readonly string[] InlineFamilies = ["TO-", "SOT-", "SC-"];
    private static readonly string[] AxialFamilies = ["DO-", "SOD-", "Axial"];
    private static readonly string[] RadialFamilies = ["HC-49", "Radial"];

    private readonly List<PcbPlacement> _placements = [];
    private readonly List<RatsnestLink> _links = [];
    private readonly List<string> _notes = [];

    private int _fieldCols = MinFieldCols;
    private int _fieldRows = MinFieldRows;
    private int _netCount;

    private double _step = MidStep;
    private Point _offset = new(40, 40);
    private Point? _panFrom;
    private PcbPlacement? _selected;

    /// <summary>
    /// Set when the placement wants a fit but Bounds is not real yet. Workspace.Subscribe
    /// replays on attach, which is before the first layout pass, and fitting there divides
    /// by a zero Bounds; SizeChanged is the first moment the size is true.
    /// </summary>
    private bool _needsFit = true;

    /// <summary>The document the current view was framed for, so only a new one re-fits.</summary>
    private CircuitDocument? _framed;

    /// <summary>Raised after every rebuild, so a host panel can re-read the counts.</summary>
    public event EventHandler? PlacementChanged;

    /// <summary>Raised when the clicked footprint changes.</summary>
    public event EventHandler<PcbPlacement?>? SelectionChanged;

    public PcbCanvas()
    {
        Focusable = true;
        ClipToBounds = true;

        SizeChanged += (_, _) =>
        {
            if (!_needsFit || Bounds.Width <= 1 || Bounds.Height <= 1) return;
            _needsFit = false;
            ZoomToFit();
        };

        // Workspace.Subscribe, not Changed += : the mode tabs rebuild their screens and
        // drop the old control, and a static event holding a handler over a discarded
        // canvas keeps the whole tree alive. It also replays on attach, so a board that
        // was off-screen while the document changed comes back current.
        Workspace.Subscribe(this, (_, _) => Rebuild());

        Rebuild();
    }

    /// <summary>
    /// The document being drawn. Read through, never cached: Workspace.Replace swaps the
    /// instance on new/open, and a held copy would keep drawing the closed project.
    /// </summary>
    public CircuitDocument Document => Workspace.Document;

    public IReadOnlyList<PcbPlacement> Placements => _placements;
    public IReadOnlyList<RatsnestLink> Ratsnest => _links;

    /// <summary>
    /// Honest caveats about this placement, in the shape BreadboardLayout uses. Drawn in
    /// the footer as well as offered here — a host panel that reads them is optional, a
    /// reader seeing them is not.
    /// </summary>
    public IReadOnlyList<string> Notes => _notes;

    // Every figure below is counted from what was actually placed. There is deliberately
    // no trace count, via count, DRC result or completion percentage: nothing routed
    // anything, so there is no number to report and none is invented.
    public int PlacedCount => _placements.Count;
    public int PadCount => _placements.Sum(p => p.Pads.Count);
    public int RatsnestCount => _links.Count;
    public int NetCount => _netCount;
    public int GenericFootprintCount => _placements.Count(p => p.Kind == PcbFootprintKind.Generic);

    public PcbPlacement? Selected
    {
        get => _selected;
        private set
        {
            if (ReferenceEquals(_selected, value)) return;
            _selected = value;
            SelectionChanged?.Invoke(this, value);
            InvalidateVisual();
        }
    }

    // ── placement ────────────────────────────────────────────────────────

    /// <summary>
    /// Re-extracts the nets and lays the parts out again. Cheap enough to run on every
    /// document change, which is what keeps this view the same circuit as the sheet
    /// rather than a second drawing of it.
    /// </summary>
    public void Rebuild()
    {
        var doc = Document;
        var nets = doc.ExtractNets();

        _placements.Clear();
        _links.Clear();
        _notes.Clear();
        _netCount = nets.Count;

        // Ground symbols and net labels are schematic notation with no body to solder.
        // The same filter BreadboardLayout applies, so the two physical views agree about
        // what exists — note it deliberately keeps connector headers, which are
        // SpiceKind.None and very much physical.
        var physical = doc.Parts.Where(p =>
            !(p.Definition.Spice == SpiceKind.None && p.Definition.Symbol == SymbolShape.Ground)).ToList();
        int symbolsOnly = doc.Parts.Count - physical.Count;

        var built = physical
            .Select(part =>
            {
                var (kind, pads) = Layout(part.Definition);
                return (Part: part, Kind: kind, Pads: pads, Local: BodyBox(pads));
            })
            .ToList();

        ShelvePlacements(built);
        BuildRatsnest(nets);
        CollectNotes(built, symbolsOnly);

        if (!ReferenceEquals(_framed, doc))
        {
            _framed = doc;
            if (Bounds.Width > 1 && Bounds.Height > 1) ZoomToFit();
            else _needsFit = true;
        }

        // Re-point the selection at the part's new placement, or drop it if the part is
        // gone. A local, not the field: the field's null state does not flow into the
        // lambda and reading it there is a possible-null deref.
        if (_selected is { } previous)
            Selected = _placements.FirstOrDefault(p => ReferenceEquals(p.Part, previous.Part));

        _footer = null;                         // the counts and the notes both just moved
        InvalidateVisual();
        PlacementChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shelf packing, in document order: fill a row until the next footprint will not fit,
    /// then start another. It is the simplest rule that produces a readable board, and it
    /// is one the user can predict — which is the whole point of not pretending to
    /// optimise. The row width comes from the total footprint area so the result is
    /// roughly square instead of one long ribbon.
    /// </summary>
    private void ShelvePlacements(
        List<(PartInstance Part, PcbFootprintKind Kind, List<PcbPad> Pads, Rect Local)> built)
    {
        double area = built.Sum(b => (b.Local.Width + Gutter) * (b.Local.Height + Gutter));
        double rowWidth = Math.Max(MinFieldCols - 2 * FieldMargin, Math.Ceiling(Math.Sqrt(area * 1.35)));

        double x = FieldMargin, y = FieldMargin, rowHeight = 0;

        foreach (var b in built)
        {
            if (x > FieldMargin && x - FieldMargin + b.Local.Width > rowWidth)
            {
                x = FieldMargin;
                y += rowHeight + Gutter;
                rowHeight = 0;
            }

            double dx = x - b.Local.X, dy = y - b.Local.Y;
            var moved = b.Pads.Select(p => p with { X = p.X + dx, Y = p.Y + dy }).ToList();

            _placements.Add(new PcbPlacement(
                b.Part, b.Kind, b.Part.Definition.Package, moved,
                new Rect(x, y, b.Local.Width, b.Local.Height)));

            x += b.Local.Width + Gutter;
            rowHeight = Math.Max(rowHeight, b.Local.Height);
        }

        double right = _placements.Count == 0 ? 0 : _placements.Max(p => p.Outline.Right);
        double bottom = _placements.Count == 0 ? 0 : _placements.Max(p => p.Outline.Bottom);
        _fieldCols = Math.Max(MinFieldCols, (int)Math.Ceiling(right) + FieldMargin + 1);
        _fieldRows = Math.Max(MinFieldRows, (int)Math.Ceiling(bottom) + FieldMargin + 1);
    }

    /// <summary>
    /// The ratsnest: for each net, the minimum spanning tree over its pads. Every line is
    /// a pair of pads that has to end up connected and is not connected yet — the exact,
    /// complete statement of what routing still owes this board.
    /// </summary>
    private void BuildRatsnest(IReadOnlyList<Net> nets)
    {
        // (part id, pin number) is the only stable identity a Net connection carries.
        var padOf = new Dictionary<(string PartId, string PinNumber), Point>();
        foreach (var placement in _placements)
            foreach (var pad in placement.Pads)
                padOf[(placement.Part.Id, pad.PinNumber)] = new Point(pad.X, pad.Y);

        foreach (var net in nets)
        {
            var pads = new List<(Point At, string PartId)>();
            foreach (var (part, pin) in net.Connections)
                if (padOf.TryGetValue((part.Id, pin.Number), out var at))
                    pads.Add((at, part.Id));

            if (pads.Count < 2) continue;

            foreach (var (a, b) in SpanningTree(pads.Select(p => p.At).ToList()))
                _links.Add(new RatsnestLink(
                    net.Name, net.IsGround, pads[a].PartId, pads[a].At, pads[b].PartId, pads[b].At));
        }
    }

    /// <summary>
    /// Prim's algorithm, O(n²) over the pads of one net. Ties break on index, and the
    /// index order comes from the extractor's own ordering, so the same document always
    /// produces the same ratsnest rather than one that reshuffles between runs.
    /// </summary>
    private static IEnumerable<(int A, int B)> SpanningTree(IReadOnlyList<Point> points)
    {
        int n = points.Count;
        if (n == 0) yield break;

        var inTree = new bool[n];
        var best = new double[n];
        var from = new int[n];

        for (int i = 0; i < n; i++) best[i] = double.MaxValue;
        best[0] = 0;

        for (int done = 0; done < n; done++)
        {
            int pick = -1;
            for (int i = 0; i < n; i++)
                if (!inTree[i] && (pick < 0 || best[i] < best[pick])) pick = i;
            if (pick < 0) yield break;

            inTree[pick] = true;
            if (done > 0) yield return (from[pick], pick);

            for (int j = 0; j < n; j++)
            {
                if (inTree[j]) continue;
                double dx = points[pick].X - points[j].X, dy = points[pick].Y - points[j].Y;
                double d = dx * dx + dy * dy;          // squared: the tree is the same, the sqrt is not
                if (d >= best[j]) continue;
                best[j] = d;
                from[j] = pick;
            }
        }
    }

    private void CollectNotes(
        List<(PartInstance Part, PcbFootprintKind Kind, List<PcbPad> Pads, Rect Local)> built,
        int symbolsOnly)
    {
        // TODO: localise — no key exists for any of these and Keys.g.cs is generated.
        int generic = built.Count(b => b.Kind == PcbFootprintKind.Generic);
        if (generic > 0)
            _notes.Add($"ไม่ทราบแพ็กเกจ {generic} ตัว — วาดเป็นกรอบเส้นประขนาดตามจำนวนขา ไม่ใช่ฟุตพรินต์จริง");

        if (symbolsOnly > 0)
            _notes.Add($"สัญลักษณ์กราวด์ {symbolsOnly} ตัวไม่มีตัวถังจริง จึงไม่มีแพดบนแผ่น");

        // A package name whose number IS the pin count and disagrees with the library.
        // Only the dual-row families are checked: the 92 in TO-92 is not a pin count.
        var mismatched = built
            .Where(b => DeclaredPinCount(b.Part.Definition.Package) is { } n && n != b.Part.Definition.Pins.Count)
            .Select(b => b.Part.Designator)
            .ToList();
        if (mismatched.Count > 0)
            _notes.Add($"ชื่อแพ็กเกจระบุจำนวนขาไม่ตรงกับขาที่ไลบรารีมี ({string.Join(", ", mismatched)}) — " +
                       "จำนวนแพดยึดตามไลบรารี");

        // Every recognised footprint is drawn on one uniform step in both axes. The pad
        // count is the library's; the spacing is this canvas's own, because no part in the
        // catalogue declares a pitch or a row span — and on these kinds the pin-1 notch,
        // the square pin-1 pad and the package name printed underneath make the drawing
        // look measured. Counted rather than listed: it applies to all of them, so naming
        // each designator would be a list of the whole board.
        int spaced = built.Count(b => b.Kind != PcbFootprintKind.Generic);
        if (spaced > 0)
            _notes.Add($"ผังตัวถัง {spaced} ตัววาดด้วยระยะห่างขาคงที่บนกริดเขียนแบบ — ช่วงแถวและพิตช์จริง " +
                       "(เช่น DIP 300/600 mil, SOIC-8 208-mil ที่ 1.27 mm) ไม่มีอยู่ในแคตตาล็อก");

        // The counter-clockwise dual-row order, the notch and the square pad all assert an
        // orientation as fact, and the pin numbers are the only place that fact can come
        // from. Where they are not ordinals the list order stands in — say so.
        var assumedOrder = built
            .Where(b => b.Part.Definition.Pins.Count > 1 && ByPinNumber(b.Part.Definition.Pins) is null)
            .Select(b => b.Part.Designator)
            .ToList();
        if (assumedOrder.Count > 0)
            _notes.Add($"หมายเลขขาของ {string.Join(", ", assumedOrder)} ไม่ใช่ตัวเลขล้วน — " +
                       "ลำดับขาและตำแหน่งขา 1 ยึดตามลำดับที่ไลบรารีเรียงไว้ ไม่ได้อ่านจากหมายเลขขา");

        if (_placements.Count > 0 && _links.Count == 0)
            _notes.Add("ยังไม่มีเนตที่ถึงแพดตั้งแต่สองจุดขึ้นไป จึงยังไม่มีเส้นโยง");

        _notes.Add("ข้อมูลตัวถังทั้งแคตตาล็อกเป็น Provenance.Unverified — ไม่มีมิติจากดาต้าชีต " +
                   "ผังนี้บอกลำดับและการเชื่อมต่อ ไม่ได้บอกขนาดจริงเป็นมิลลิเมตร");
    }

    // ── footprints ───────────────────────────────────────────────────────

    /// <summary>
    /// What the declared package name says about the lead arrangement — and nothing more.
    /// PartDefinition.Package is free text with no pitch, no body size and no pad count,
    /// and roughly a third of the catalogue's values ("module", "breakout", "3-wire",
    /// "EI core, chassis mount") name no footprint at all. Those fall through to Generic
    /// rather than being bent into the nearest-looking outline.
    /// </summary>
    private static PcbFootprintKind Classify(string? package, int pinCount)
    {
        if (string.IsNullOrWhiteSpace(package)) return PcbFootprintKind.Generic;
        var head = package.Trim();

        if (StartsAny(head, DualRowFamilies)) return PcbFootprintKind.DualRow;
        if (StartsAny(head, InlineFamilies)) return PcbFootprintKind.Inline;
        if (StartsAny(head, AxialFamilies))
            return pinCount == 2 ? PcbFootprintKind.Axial : PcbFootprintKind.Inline;
        if (StartsAny(head, RadialFamilies)) return PcbFootprintKind.Radial;

        return PcbFootprintKind.Generic;
    }

    private static bool StartsAny(string s, string[] prefixes) =>
        prefixes.Any(p => s.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>The pin count a dual-row package name states, when it states one.</summary>
    private static int? DeclaredPinCount(string? package)
    {
        if (string.IsNullOrWhiteSpace(package)) return null;
        var head = package.Trim();

        foreach (var prefix in DualRowFamilies)
        {
            if (!head.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var digits = new string(head[prefix.Length..].TakeWhile(char.IsAsciiDigit).ToArray());
            return int.TryParse(digits, out var n) && n > 0 ? n : null;
        }
        return null;
    }

    /// <summary>
    /// The pins in package order, when their numbers are what says so: every number an
    /// integer, sorted ascending. Null when even one is not — "VCC" and "A0" state no
    /// position, and ordering by them anyway would be the same guess this file refuses to
    /// make about package names.
    /// </summary>
    private static IReadOnlyList<Pin>? ByPinNumber(IReadOnlyList<Pin> pins)
    {
        var numbered = new List<(int Number, Pin Pin)>(pins.Count);
        foreach (var pin in pins)
        {
            if (!int.TryParse(pin.Number, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var number))
                return null;
            numbered.Add((number, pin));
        }

        // OrderBy, not List.Sort: it is stable, so two pins sharing a number keep the
        // order the library gave them instead of swapping between runs.
        return numbered.OrderBy(n => n.Number).Select(n => n.Pin).ToList();
    }

    /// <summary>
    /// Pads for one part, in grid steps with pin 1 at the origin.
    ///
    /// The pad COUNT always comes from Definition.Pins, never from the number in the
    /// package name — those disagree constantly (a TO-220 regulator declares three pins,
    /// a "module" declares six) and the library is the side that knows.
    ///
    /// The spacings are drawing conventions, not datasheet figures, and they are held to
    /// the same standard as the count: a through-hole DIP is on a 0.1-inch pitch but its
    /// row span is 300 or 600 mil depending on the part, and a SOIC-8 208-mil is on
    /// 1.27 mm legs entirely. The catalogue states none of that, so one uniform step
    /// draws all of them and <see cref="CollectNotes"/> names the parts it applied to.
    /// </summary>
    private static (PcbFootprintKind Kind, List<PcbPad> Pads) Layout(PartDefinition def)
    {
        // Package order read off the pin numbers rather than assumed from the list. Every
        // arm below places pads by index and marks index 0 as pin 1, and the notch and the
        // square pad then present that as the part's real orientation — so a definition
        // that happened to list its pins by function would produce a confidently wrong
        // footprint. When the numbers cannot be read the list order stands in, and
        // CollectNotes says which parts that happened to.
        var pins = ByPinNumber(def.Pins) ?? def.Pins;
        var kind = Classify(def.Package, pins.Count);
        var pads = new List<PcbPad>(pins.Count);

        switch (kind)
        {
            case PcbFootprintKind.DualRow:
            {
                // Counter-clockwise from pin 1: down one row, back along the other.
                int half = (pins.Count + 1) / 2;
                for (int i = 0; i < pins.Count; i++)
                    pads.Add(i < half
                        ? new PcbPad(pins[i].Number, pins[i].Name, i, 0, i == 0)
                        : new PcbPad(pins[i].Number, pins[i].Name, half - 1 - (i - half), DualRowGap, false));
                break;
            }

            case PcbFootprintKind.Axial when pins.Count == 2:
                pads.Add(new PcbPad(pins[0].Number, pins[0].Name, 0, 0, true));
                pads.Add(new PcbPad(pins[1].Number, pins[1].Name, AxialSpan, 0, false));
                break;

            case PcbFootprintKind.Generic:
            {
                // Sized by pin count and nothing else. Two rows past eight pins purely so
                // a 30-pin module is not a metre-wide strip; both rows are marked dashed,
                // so the shape claims no arrangement.
                int rows = pins.Count > 8 ? 2 : 1;
                int cols = Math.Max(1, (pins.Count + rows - 1) / rows);
                for (int i = 0; i < pins.Count; i++)
                    pads.Add(new PcbPad(pins[i].Number, pins[i].Name, i % cols, i / cols, i == 0));
                break;
            }

            default:
                for (int i = 0; i < pins.Count; i++)
                    pads.Add(new PcbPad(pins[i].Number, pins[i].Name, i, 0, i == 0));
                break;
        }

        return (kind, pads);
    }

    /// <summary>Silkscreen box around a set of pads. A part with no pins still needs a body.</summary>
    private static Rect BodyBox(IReadOnlyList<PcbPad> pads)
    {
        if (pads.Count == 0) return new Rect(0, 0, 1.4, 1.4);

        double x0 = pads.Min(p => p.X), x1 = pads.Max(p => p.X);
        double y0 = pads.Min(p => p.Y), y1 = pads.Max(p => p.Y);
        return new Rect(x0, y0, x1 - x0, y1 - y0).Inflate(0.7);
    }

    // ── coordinates ──────────────────────────────────────────────────────

    private Point Px(double x, double y) => new(x * _step + _offset.X, y * _step + _offset.Y);

    private Point Px(Point p) => Px(p.X, p.Y);

    private Rect PxRect(Rect r) => new(Px(r.X, r.Y), new Size(r.Width * _step, r.Height * _step));

    /// <summary>The drilled hole field, from the first hole centre to the last.</summary>
    private Rect FieldRect() => new(0, 0, _fieldCols - 1, _fieldRows - 1);

    /// <summary>The substrate: the field plus PcbView's own bare margin.</summary>
    private Rect BoardRect()
    {
        var field = FieldRect();
        return new Rect(
            field.X - MarginX, field.Y - MarginY,
            field.Width + 2 * MarginX, field.Height + 2 * MarginY);
    }

    /// <summary>Frames the whole board. Never called from Render — invalidating there throws.</summary>
    public void ZoomToFit()
    {
        if (Bounds.Width <= 1 || Bounds.Height <= 1) return;

        var board = BoardRect();
        _step = Math.Clamp(
            Math.Min(Bounds.Width / (board.Width + 2), Bounds.Height / (board.Height + 2)),
            MinStep, MaxStep);
        _offset = new Point(
            (Bounds.Width - board.Width * _step) / 2 - board.X * _step,
            (Bounds.Height - board.Height * _step) / 2 - board.Y * _step);

        InvalidateVisual();
    }

    // ── rendering ────────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var bounds = new Rect(Bounds.Size);
        ctx.FillRectangle(Well, bounds);

        DrawDotGrid(ctx, bounds);
        DrawBoard(ctx);
        DrawHoleField(ctx, bounds);
        DrawFootprints(ctx);
        DrawRatsnest(ctx);
        DrawPads(ctx);
        if (_selected is { } picked) DrawSelection(ctx, picked);
        if (_placements.Count == 0) DrawEmptyHint(ctx);

        // The footer is laid out before the legend so the legend knows where it must stop.
        // The two used to be sized independently, each against its own height threshold.
        var footer = FooterFit(FooterText(bounds.Width), Math.Max(0, bounds.Height - 16));
        double top = Math.Max(4, bounds.Height - footer.Sum(t => t.Height + 2) - 8);

        DrawLegend(ctx, bounds, top - 6);
        DrawFooter(ctx, bounds, footer, top);
    }

    /// <summary>
    /// The well's grid, as one geometry per zoom level rather than one draw call per dot.
    /// A 1920 × 1080 pane at the densest admitted spacing is about 83 000 dots, and
    /// OnPointerMoved invalidates on every step of a pan, so the per-dot version paid that
    /// for every frame of every drag. PcbView.DotGrid renders the identical field as a
    /// single Path for the same reason.
    ///
    /// The geometry is built at its own origin and translated into place, so the count and
    /// the spacing are the only things it depends on: panning reuses it and only a zoom or
    /// a resize rebuilds it. Written from Render, which is safe — it is a cache, and
    /// nothing here invalidates.
    /// </summary>
    private Geometry? _dots;
    private (int Cols, int Rows, double Spacing, double Radius) _dotsFor;

    private void DrawDotGrid(DrawingContext ctx, Rect bounds)
    {
        double spacing = _step * DotPitch;
        if (spacing < 5) return;               // any denser and the dots are a haze, not a grid

        double r = Math.Max(0.5, _step * DotR);
        int cols = (int)Math.Ceiling(bounds.Width / spacing) + 1;
        int rows = (int)Math.Ceiling(bounds.Height / spacing) + 1;

        var key = (cols, rows, spacing, r);
        var dots = _dots;                      // a local: the field's null state does not
        if (dots is null || _dotsFor != key)   // survive the calls between here and the draw
        {
            dots = DotField(cols, rows, spacing, r);
            (_dots, _dotsFor) = (dots, key);
        }

        // Down to the first dot at or before the left/top edge, so the field lands on the
        // same absolute positions the per-dot loop used.
        var origin = new Point(
            Math.Floor(-_offset.X / spacing) * spacing + _offset.X,
            Math.Floor(-_offset.Y / spacing) * spacing + _offset.Y);

        using (ctx.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
            ctx.DrawGeometry(GridDot, null, dots);
    }

    /// <summary>
    /// Square dots, not round ones: the mock draws this field as 2 × 2 rects, and at the
    /// one pixel these are across, a square and a circle are the same picture.
    /// </summary>
    private static Geometry DotField(int cols, int rows, double spacing, double r)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
            for (int x = 0; x <= cols; x++)
                for (int y = 0; y <= rows; y++)
                {
                    double cx = x * spacing, cy = y * spacing;
                    g.BeginFigure(new Point(cx - r, cy - r), true);
                    g.LineTo(new Point(cx + r, cy - r));
                    g.LineTo(new Point(cx + r, cy + r));
                    g.LineTo(new Point(cx - r, cy + r));
                    g.EndFigure(true);
                }
        return geometry;
    }

    private void DrawBoard(DrawingContext ctx)
    {
        var board = PxRect(BoardRect());
        var radius = new CornerRadius(_step * BoardRadius);

        ctx.DrawRectangle(
            Substrate, new Pen(SubstrateEdge, Math.Max(1, _step * EdgeStroke)),
            new RoundedRect(board, radius));

        // The edge repeated as a keep-out, exactly as the mock draws it.
        double t = Math.Max(0.7, _step / Unit);
        using (ctx.PushOpacity(0.5))
            ctx.DrawRectangle(null, new Pen(KeepOut, t) { DashStyle = Dash(t, _step * 8 / Unit, _step * 5 / Unit) },
                new RoundedRect(board, radius));
    }

    private void DrawHoleField(DrawingContext ctx, Rect bounds)
    {
        if (_step < 6) return;                 // the holes merge into a smear below this

        double pad = Math.Max(0.6, _step * FieldPadR), hole = Math.Max(0.3, _step * FieldHoleR);

        // Culled to the viewport: a large circuit drills tens of thousands of holes, and
        // every one off-screen is a per-frame cost that buys nothing.
        int c0 = Math.Max(0, (int)Math.Floor(-_offset.X / _step));
        int c1 = Math.Min(_fieldCols - 1, (int)Math.Ceiling((bounds.Width - _offset.X) / _step));
        int r0 = Math.Max(0, (int)Math.Floor(-_offset.Y / _step));
        int r1 = Math.Min(_fieldRows - 1, (int)Math.Ceiling((bounds.Height - _offset.Y) / _step));

        using (ctx.PushOpacity(0.22))
        {
            for (int c = c0; c <= c1; c++)
                for (int r = r0; r <= r1; r++)
                {
                    var at = Px(c, r);
                    ctx.DrawEllipse(FieldCopper, null, at, pad, pad);
                    ctx.DrawEllipse(FieldHole, null, at, hole, hole);
                }
        }
    }

    private void DrawFootprints(DrawingContext ctx)
    {
        double t = Math.Max(0.8, _step * SilkStroke);
        var solid = new Pen(Silk, t);
        var dashed = new Pen(Silk, t) { DashStyle = Dash(t, _step * 4 / Unit, _step * 3 / Unit) };

        using (ctx.PushOpacity(0.85))
        {
            foreach (var placement in _placements)
            {
                var box = PxRect(placement.Outline);
                var pen = placement.Kind == PcbFootprintKind.Generic ? dashed : solid;

                if (placement.Kind == PcbFootprintKind.Radial)
                    ctx.DrawEllipse(null, pen, box.Center, box.Width / 2, box.Height / 2);
                else
                    ctx.DrawRectangle(null, pen, new RoundedRect(box, new CornerRadius(_step * 3 / Unit)));

                switch (placement.Kind)
                {
                    case PcbFootprintKind.DualRow when _step >= 8:
                        DrawPinOneNotch(ctx, box, pen);
                        break;

                    case PcbFootprintKind.Axial:
                        // The body sits between the two leads, which is what "axial" means.
                        ctx.DrawRectangle(null, pen, new Rect(
                            box.X + box.Width * 0.28, box.Y + box.Height * 0.18,
                            box.Width * 0.44, box.Height * 0.64));
                        break;

                    case PcbFootprintKind.Generic when _step >= 10:
                    {
                        // The mark. A generic outline must never be mistaken for a footprint.
                        var q = Prose("?", _step * 0.7, Silk);
                        ctx.DrawText(q, new Point(box.Center.X - q.Width / 2, box.Center.Y - q.Height / 2));
                        break;
                    }
                }
            }
        }

        DrawFootprintText(ctx);
    }

    /// <summary>The half-circle at the pin-1 end of a dual-row body — the mock's own notch.</summary>
    private void DrawPinOneNotch(DrawingContext ctx, Rect box, Pen pen)
    {
        double r = Math.Min(_step * 0.28, box.Width / 3);
        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(new Point(box.Center.X - r, box.Y), false);
            g.ArcTo(new Point(box.Center.X + r, box.Y), new Size(r, r), 0, false, SweepDirection.CounterClockwise);
            g.EndFigure(false);
        }
        ctx.DrawGeometry(null, pen, geo);
    }

    private void DrawFootprintText(DrawingContext ctx)
    {
        if (_step < 10) return;                // below this the silkscreen is noise, not information

        foreach (var placement in _placements)
        {
            var box = PxRect(placement.Outline);

            var designator = SymbolRenderer.Text(placement.Part.Designator, _step * DesignatorSize, SilkText);
            ctx.DrawText(designator, new Point(
                box.Center.X - designator.Width / 2, box.Y - designator.Height - _step * 0.12));

            if (_step < 16) continue;

            // The declared package printed verbatim, with no dimension attached to it —
            // the catalogue gives a name, not a size, so a name is all that is shown.
            // TODO: localise
            string label = placement.Kind == PcbFootprintKind.Generic
                ? "ไม่ทราบแพ็กเกจ"
                : placement.DeclaredPackage ?? "";
            if (label.Length == 0) continue;

            var text = Prose(label, _step * 0.36, Footer);
            ctx.DrawText(text, new Point(box.Center.X - text.Width / 2, box.Bottom + _step * 0.08));
        }
    }

    /// <summary>
    /// The ratsnest. Ground runs dimmer than the rest: it touches nearly every part, and at
    /// full strength it buries the signal nets it is drawn on top of.
    /// </summary>
    private void DrawRatsnest(DrawingContext ctx)
    {
        if (_links.Count == 0) return;

        double t = Math.Max(0.7, _step * RatsStroke);
        var pen = new Pen(SymbolRenderer.PinDot, t)
        {
            DashStyle = Dash(t, _step * 6 / Unit, _step * 4 / Unit),
        };

        var lit = _selected?.Part.Id;
        var reached = new Pen(SymbolRenderer.Selected, t) { DashStyle = pen.DashStyle };

        foreach (var link in _links)
        {
            bool touches = lit is not null && (link.FromPartId == lit || link.ToPartId == lit);
            using (ctx.PushOpacity(touches ? 1.0 : link.IsGround ? 0.45 : 0.75))
                ctx.DrawLine(touches ? reached : pen, Px(link.From), Px(link.To));
        }
    }

    private void DrawPads(DrawingContext ctx)
    {
        double w = Math.Max(1.5, _step * PadW), h = Math.Max(1.5, _step * PadH);
        double drill = _step * DrillR;

        foreach (var placement in _placements)
            foreach (var pad in placement.Pads)
            {
                var at = Px(pad.X, pad.Y);
                var rect = new Rect(at.X - w / 2, at.Y - h / 2, w, h);

                // Square pad = pin 1. It is the convention every assembler already reads,
                // and it costs nothing to be right about.
                if (pad.IsFirst) ctx.FillRectangle(Copper, rect);
                else ctx.DrawRectangle(Copper, null, new RoundedRect(rect, new CornerRadius(Math.Min(w, h) / 2)));

                if (drill >= 1) ctx.DrawEllipse(Drill, null, at, drill, drill);
            }
    }

    private void DrawSelection(DrawingContext ctx, PcbPlacement placement)
    {
        var box = PxRect(placement.Outline).Inflate(_step * 0.3);
        var pen = new Pen(SymbolRenderer.Selected, 1.4) { DashStyle = Dash(1.4, 4, 3) };
        ctx.DrawRectangle(null, pen, box);

        double s = Math.Max(5, _step * HandleSize);
        foreach (var corner in new[] { box.TopLeft, box.TopRight, box.BottomLeft, box.BottomRight })
            ctx.FillRectangle(SymbolRenderer.Selected,
                new Rect(corner.X - s / 2, corner.Y - s / 2, s, s));
    }

    private void DrawEmptyHint(DrawingContext ctx)
    {
        var board = PxRect(BoardRect());
        // TODO: localise — the phrase NetlistBuilder already uses for the same condition.
        var text = Prose("ยังไม่มีอุปกรณ์บนผัง", 12, Footer);
        ctx.DrawText(text, new Point(
            board.Center.X - text.Width / 2, board.Center.Y - text.Height / 2));
    }

    /// <summary>
    /// Names the four things actually on screen. It is a key, not a layer switcher: the
    /// mock's TOP CU / BOT CU / MASK / DRILL chips describe copper that does not exist
    /// here, and offering them would promise a board this view has not produced.
    ///
    /// Rows are drawn while there is room for them, instead of the whole key disappearing
    /// below a size threshold. Two of the four are themselves caveats — the dashed generic
    /// outline and "ยังไม่ได้เดินลาย" — and a caveat with a minimum window size is not one.
    /// Labels wrap to the pane rather than running off it, for the same reason.
    /// </summary>
    private static void DrawLegend(DrawingContext ctx, Rect bounds, double limit)
    {
        // TODO: localise
        (IBrush Swatch, bool Dashed, string Label)[] rows =
        [
            (Copper, false, "แพดทองแดง (พิน 1 = เหลี่ยม)"),
            (Silk, false, "ผังตัวถังจากชื่อแพ็กเกจ"),
            (Silk, true, "รูปทรงทั่วไป — ไม่ทราบแพ็กเกจ"),
            (SymbolRenderer.PinDot, false, "เส้นโยงเนต — ยังไม่ได้เดินลาย"),
        ];

        const double x = 8, size = 9;
        double y = 8;

        foreach (var (swatch, dashed, label) in rows)
        {
            var text = Prose(label, size, SymbolRenderer.Label);
            text.MaxTextWidth = Math.Max(60, bounds.Width - x - 28);

            double height = Math.Max(13, text.Height + 3);
            if (y + height > limit) break;

            var box = new Rect(x, y + 2, 14, 8);
            if (dashed)
                ctx.DrawRectangle(null, new Pen(swatch, 1) { DashStyle = Dash(1, 2, 2) }, box);
            else
                ctx.FillRectangle(swatch, box);

            ctx.DrawText(text, new Point(x + 20, y));
            y += height;
        }
    }

    // ── the footer ───────────────────────────────────────────────────────

    /// <summary>
    /// One footer line, and how hard it fights for space when the pane is short.
    /// Rank 0 is the sentence that survives any size.
    /// </summary>
    private readonly record struct FooterLine(FormattedText Text, int Rank);

    /// <summary>The shaped footer, and the wrapping width it was shaped for.</summary>
    private List<FooterLine>? _footer;
    private double _footerWidth = -1;

    /// <summary>
    /// What this board is, stated on the board, followed by every caveat the placement
    /// actually computed. It replaces PcbView's two footer lines, both of which reported
    /// figures nothing had computed — a 100 × 80 mm outline the artwork did not match, and
    /// "DRC 0 / 0" for a check that never ran. Every number printed here was counted from
    /// what was placed, and every note under it was derived from the same pass.
    ///
    /// Shaped once per width: Render runs on every step of a pan, and this is a dozen
    /// lines of Thai text shaping. <see cref="Rebuild"/> drops the cache, which is the
    /// only thing that changes either the counts or the notes.
    /// </summary>
    private IReadOnlyList<FooterLine> FooterText(double width)
    {
        double inner = Math.Max(80, width - 24);
        if (_footer is { } cached && Math.Abs(_footerWidth - inner) < 0.5) return cached;

        var lines = new List<FooterLine>();

        void Add(string s, IBrush brush, int rank)
        {
            var text = Prose(s, 10, brush);
            text.MaxTextWidth = inner;         // wrap into the pane rather than off it
            lines.Add(new FooterLine(text, rank));
        }

        // TODO: localise — no key exists for any of this and Keys.g.cs is generated.
        Add("วางอุปกรณ์ + เส้นโยงเนต (ratsnest) — ยังไม่ได้เดินลายทองแดง", Footer, 0);
        Add("เส้นสีฟ้าคือคู่แพดที่ต้องต่อถึงกัน ไม่ใช่ลายทองแดง · ยังไม่มีการเดินลาย เจาะเวีย หรือตรวจ DRC", Footer, 3);
        Add("เรียงตามลำดับในเอกสาร ไม่ได้ผ่านตัวจัดวางอัตโนมัติ", Footer, 4);
        Add("ขนาดตัวถังจริงยังไม่ทราบ — ผังนี้วัดเป็นช่องกริดสำหรับเขียนแบบ " +
            "ไม่ใช่ระยะพิตช์จริงหรือมิลลิเมตรจากดาต้าชีต", Footer, 3);
        Add($"อุปกรณ์ {PlacedCount} · แพด {PadCount} · เนต {NetCount} · เส้นโยง {RatsnestCount} · " +
            $"ไม่ทราบแพ็กเกจ {GenericFootprintCount}", Footer, 2);

        // The caveats CollectNotes computed, drawn where the reader is. They used to reach
        // only the Notes property, which nothing reads — so the count line above could say
        // "อุปกรณ์ 3" against the sheet's 4 with the note explaining the gap thrown away.
        // They rank directly under the opening sentence: they are the part of this footer
        // that is about this document rather than about the view in general.
        foreach (var note in _notes) Add("• " + note, SymbolRenderer.Error, 1);

        _footer = lines;
        _footerWidth = inner;
        return lines;
    }

    /// <summary>
    /// The lines that fit, in reading order. Space goes out by rank, so a short pane keeps
    /// the sentence that matters and the computed caveats rather than whichever five lines
    /// come first; one line is always kept, even when there is no room for it. The previous
    /// version drew nothing at all below about 85 px, which left a short pane showing
    /// copper pads and cyan dashes with nothing on it saying this is placement.
    /// </summary>
    private static List<FormattedText> FooterFit(IReadOnlyList<FooterLine> lines, double available)
    {
        var keep = new bool[lines.Count];
        double used = 0;

        foreach (int i in Enumerable.Range(0, lines.Count).OrderBy(n => lines[n].Rank))
        {
            double height = lines[i].Text.Height + 2;
            // continue, not break: a long note that wraps to three lines must not also
            // cost the one-line count that ranks below it.
            if (used > 0 && used + height > available) continue;
            keep[i] = true;
            used += height;
        }

        return Enumerable.Range(0, lines.Count).Where(i => keep[i]).Select(i => lines[i].Text).ToList();
    }

    private static void DrawFooter(DrawingContext ctx, Rect bounds, List<FormattedText> lines, double top)
    {
        if (lines.Count == 0) return;

        double height = lines.Sum(t => t.Height + 2);
        double backing = Math.Max(0, Math.Min(lines.Max(t => t.Width) + 12, bounds.Width - 8));

        using (ctx.PushOpacity(0.82))
            ctx.FillRectangle(Well, new Rect(4, top - 4, backing, height + 8));

        // ClipToBounds does the rest: in a pane too short even for the one kept line, it is
        // cut off at the bottom edge, which still says more than a bare green board does.
        foreach (var text in lines)
        {
            ctx.DrawText(text, new Point(10, top));
            top += text.Height + 2;
        }
    }

    // ── interaction ──────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var pt = e.GetCurrentPoint(this);

        if (pt.Properties.IsMiddleButtonPressed)
        {
            _panFrom = pt.Position;
            e.Handled = true;
            return;
        }

        if (!pt.Properties.IsLeftButtonPressed) return;

        // Last match wins, so the footprint drawn on top is the one that answers.
        Selected = _placements.LastOrDefault(p => PxRect(p.Outline).Contains(pt.Position));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_panFrom is not { } from) return;
        var p = e.GetPosition(this);
        _offset += p - from;
        _panFrom = p;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e) => _panFrom = null;

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // Zoom about the pointer, so whatever is under it stays under it.
        var at = e.GetPosition(this);
        double before = _step;
        _step = Math.Clamp(_step * (e.Delta.Y > 0 ? 1.15 : 1 / 1.15), MinStep, MaxStep);

        var anchor = new Point((at.X - _offset.X) / before, (at.Y - _offset.Y) / before);
        _offset = new Point(at.X - anchor.X * _step, at.Y - anchor.Y * _step);

        InvalidateVisual();
        e.Handled = true;
    }

    // ── shared pieces ────────────────────────────────────────────────────

    /// <summary>Avalonia dashes are multiples of the stroke width; artwork dashes are absolute.</summary>
    private static DashStyle Dash(double thickness, double on, double off) =>
        new([on / thickness, off / thickness], 0);

    /// <summary>
    /// Prose face for Thai. The measured-value face is Lucida Console — deliberately, per
    /// the spec's type rule — but a monospace fallback renders a Thai sentence badly, so
    /// sentences take the UI face and only designators keep the mono one.
    ///
    /// The face is named rather than left to <see cref="FontFamily.Default"/>: the app
    /// registers Inter, which carries no Thai glyphs, so the default face draws every
    /// caption on this canvas as empty boxes. This is the stack Styles/Chrome.axaml and
    /// <see cref="BreadboardCanvas"/> already pin for exactly that reason.
    /// </summary>
    private static readonly Typeface ProseFace = new("Tahoma, Leelawadee UI, Noto Sans Thai, Segoe UI");

    private static FormattedText Prose(string s, double size, IBrush brush) =>
        new(s, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            ProseFace, size, brush);
}
