using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SimBoard.Document;

namespace SimBoard.App.Controls;

/// <summary>
/// The breadboard, drawn from the shared document rather than from a stored picture.
///
/// <see cref="BreadboardLayout.Build"/> decides which hole every pin occupies and which
/// jumpers the circuit needs; this control only puts those tie-points on screen. Nothing
/// here re-derives connectivity, so the board and the sheet cannot drift apart — which is
/// the entire point of having one document behind four views.
///
/// Board geometry and every colour come from <c>Views/Screens/BreadboardView.cs</c>, the
/// hand-measured mock of this screen. Two numbers could not be carried over verbatim and
/// are re-derived here, both deliberately:
///
/// • Width. The mock's tie field is <c>779 / 19 = 41</c> columns and its column ruler
///   stops at 40, but <see cref="BreadboardLayout.Columns"/> is 63. The layout is the
///   authority for where a pin goes, so the field is widened to 63 columns at the mock's
///   pitch and the board grows with it. Every vertical number is untouched. The rails are
///   drawn as 63 continuous holes rather than a BB-830's ten groups of five, so what is
///   on screen is 882 sockets and the header counts columns instead of naming a point
///   total the drawing does not support.
///
/// • Row letters. The mock prints J/F above the channel and E/A below, the physical
///   convention. <see cref="TiePoint.IsUpperBank"/> says the opposite — A-E above, F-J
///   below — and that is what <see cref="TiePoint.Node"/> keys on. Labels follow the
///   model, because a label that contradicts the node key makes the board unverifiable.
/// </summary>
public class BreadboardCanvas : Control
{
    // ── board geometry ───────────────────────────────────────────────────────
    // Vertical coordinates are lifted unchanged from BreadboardView.Board(). Horizontal
    // ones are the same left edge and pitch carried out to 63 columns.

    /// <summary>0.1″ pitch of the tie-point pattern, in scene units.</summary>
    private const double Pitch = 19;

    private const double FieldLeft = 46;
    private const double FieldRight = FieldLeft + BreadboardLayout.Columns * Pitch;   // 1243

    private const double BoardX = 30, BoardY = 50, BoardH = 418;
    private const double BoardW = FieldRight + 16 - BoardX;                           // 1229
    private const double BoardRight = BoardX + BoardW;                                // 1259

    // A rail band is 2 rows deep and a terminal strip 5 — the mock's 38 and 95, which the
    // row-centre loops below reproduce rather than restate.
    private const double TopRailTop = 66;
    private const double TopPlusLine = 60, TopMinusLine = 110;
    private const double UpperTop = 142;
    private const double ChannelTop = 237, ChannelH = 44, ChannelLineY = 259;
    private const double LowerTop = 281;
    private const double BottomRailTop = 414;
    private const double BottomPlusLine = 408, BottomMinusLine = 458;
    private const double ColumnRulerBottom = 136;

    /// <summary>Scene box the fit works against. Grows if the layout runs off the board.</summary>
    private const double SceneMinW = BoardRight + 30;                                 // 1289
    private const double SceneH = BoardY + BoardH + 30;                               // 498

    // ── palette, verbatim from BreadboardView ────────────────────────────────
    // The workspace canvas is never restyled, so these are hard-coded exactly as the mock
    // has them. Chrome still goes through theme tokens; none of this is chrome.

    private static readonly IBrush BoardFace = Ink("#efece4");
    private static readonly IBrush BoardEdge = Ink("#c9c5ba");
    private static readonly IBrush BoardShadow = Ink("#0d1114");
    private static readonly IBrush Channel = Ink("#e4e0d6");
    private static readonly IBrush ChannelLine = Ink("#cfcabd");
    private static readonly IBrush TieSocket = Ink("#2f3134");
    private static readonly IBrush TieInsert = Ink("#8d8f92");
    private static readonly IBrush RailPlus = Ink("#c0473a");
    private static readonly IBrush RailMinus = Ink("#3a5fa0");

    private static readonly IBrush Lead = Ink("#9aa0a6");
    private static readonly IBrush DipBody = Ink("#23262a");
    private static readonly IBrush DipEdge = Ink("#3c4046");
    private static readonly IBrush DipCaption = Ink("#c8cdd4");
    private static readonly IBrush ResBody = Ink("#e0cfa8");
    private static readonly IBrush ResEdge = Ink("#b6a377");
    private static readonly IBrush CapBody = Ink("#2c3f6b");
    private static readonly IBrush CapEdge = Ink("#1b2846");
    private static readonly IBrush LedBody = Ink("#d94f3d");
    private static readonly IBrush LedEdge = Ink("#8f2f22");

    private static readonly IBrush TableBg = Ink("#0d1418");
    private static readonly IBrush TableEdge = Ink("#2b3440");

    /// <summary>Text sitting on the light board face. The mock's on-face ink.</summary>
    private static readonly IBrush FaceInk = Ink("#333333");

    // The five colours a jumper kit ships in, which is what JumperColour names. Taken from
    // the mock's own jumper set, one per enum member, so nothing had to be invented.
    private static readonly IBrush JumperPower = Ink("#c0473a");        // red
    private static readonly IBrush JumperGround = Ink("#1b1d20");       // black
    private static readonly IBrush JumperSignal = Ink("#c9a227");       // yellow
    private static readonly IBrush JumperSignalAlt = Ink("#3f9d5a");    // green
    private static readonly IBrush JumperSignalThird = Ink("#3a5fa0");  // blue

    // ── typefaces ────────────────────────────────────────────────────────────

    /// <summary>
    /// Every note out of <see cref="BreadboardLayout"/> is Thai, and
    /// <see cref="SymbolRenderer.Text"/> is pinned to Lucida Console, which has no Thai
    /// glyphs. Notes therefore use the prose stack from Styles/Chrome.axaml; designators
    /// and values stay monospaced like the rest of the board.
    /// </summary>
    private static readonly Typeface ProseFace = new("Tahoma, Leelawadee UI, Noto Sans Thai, Segoe UI");

    // ── the socket texture, built once ───────────────────────────────────────
    // 63 columns × 14 rows is 882 sockets. As two cached geometries that is two draw calls
    // a frame instead of 1 764; as individual rectangles it showed up in the profile.

    private static readonly Geometry SocketField = TieGeometry(2.5);
    private static readonly Geometry InsertField = TieGeometry(2.0);

    private const double NoteSize = 11, NotePad = 8;

    private CircuitDocument? _document;
    private BreadboardLayout.Result? _layout;
    private IReadOnlyList<string> _notes = [];
    private readonly List<TiePoint> _shorted = [];

    private double _sceneWidth = SceneMinW;
    private double _scale;                 // 0 until the first fit; Render copes either way
    private Point _origin;

    // Shaping a FormattedText is not free and Render runs on every frame of a resize drag,
    // where Fit has already shaped the identical set a moment earlier. Both go through
    // these two caches: the shaped lines depend only on the width, the strip that survives
    // the height cap on both. Refresh drops them, because the text itself has changed.
    private IReadOnlyList<FormattedText> _shaped = [];
    private double? _shapedFor;

    private IReadOnlyList<FormattedText> _stripLines = [];
    private double _stripHeight;
    private Size? _stripFor;

    public BreadboardCanvas()
    {
        ClipToBounds = true;

        // Fit needs a real Bounds, and AttachedToVisualTree has none — see house rule 4
        // and the same comment on SchematicCanvas. SizeChanged runs after layout, where
        // invalidating is also legal.
        SizeChanged += (_, _) => Fit();

        // Subscribe holds the handler only while this control is in a live tree and
        // fires it once on attach, so the board is current even if the document changed
        // while this tab was off-screen. The return value is deliberately dropped: the
        // token is owned by the control's own events and dies with it.
        _ = Workspace.Subscribe(this, (_, _) => Refresh());

        // Once more here, so Notes and BoardLayout answer for the current document from
        // construction rather than from the first attach. That repeats one layout build;
        // a control whose documented warning list is empty until it happens to be shown
        // would be the worse trade.
        Refresh();
    }

    /// <summary>
    /// The document to draw. Null — the default — follows <see cref="Workspace.Document"/>
    /// live, which is what every screen wants. Set it to pin one document, for a preview
    /// or a test.
    /// </summary>
    public CircuitDocument? Document
    {
        get => _document;
        set
        {
            _document = value;
            Refresh();
        }
    }

    /// <summary>The document actually on screen.</summary>
    public CircuitDocument Source => _document ?? Workspace.Document;

    /// <summary>
    /// The layout behind the current drawing, for a side panel that wants the same
    /// tie-points. Null only if <see cref="BreadboardLayout.Build"/> failed, which is
    /// itself reported in <see cref="Notes"/>.
    /// </summary>
    public BreadboardLayout.Result? BoardLayout => _layout;

    /// <summary>
    /// Everything the user has to be told: the layout's own warnings plus what this
    /// control found it could not draw faithfully. Every one of these reaches the screen —
    /// in full where the strip has room, and otherwise counted into a "+N more" line, but
    /// never dropped in silence. A swallowed warning here is a board that gets built wrong.
    /// </summary>
    public IReadOnlyList<string> Notes => _notes;

    /// <summary>Rebuilds the layout from <see cref="Source"/> and repaints.</summary>
    public void Refresh()
    {
        var notes = new List<string>();
        _shorted.Clear();

        try
        {
            _layout = BreadboardLayout.Build(Source);
        }
        catch (OverflowException)
        {
            // BreadboardLayout picks a signal colour with Math.Abs(name.GetHashCode()),
            // and Math.Abs(int.MinValue) throws. Rare, but this runs off a document-changed
            // event, so letting it escape takes the app down instead of the board.
            _layout = null;
            // TODO: localise
            notes.Add("จัดวางบอร์ดไม่สำเร็จ (การเลือกสีจัมเปอร์ล้นค่า) — ยังไม่มีผังให้แสดง");
        }

        double rightmost = FieldRight;

        if (_layout is { } built)
        {
            notes.AddRange(built.Notes);
            notes.AddRange(Audit(built));

            // A pin past column 63 is drawn where the layout put it, hanging off the end
            // of the board, rather than clamped back on. Clamping would show a board that
            // could be built; this shows the one the layout actually produced.
            foreach (var placed in built.Parts)
                foreach (var (_, at) in placed.Pins)
                    rightmost = Math.Max(rightmost, ColumnX(at.Column) + Pitch);
        }

        _notes = notes;
        _sceneWidth = Math.Max(SceneMinW, rightmost + 30);
        _shapedFor = null;
        _stripFor = null;

        Fit();
        InvalidateVisual();
    }

    // ── audit ────────────────────────────────────────────────────────────────

    /// <summary>
    /// What the layout produced that this control cannot draw honestly. Everything here
    /// is read off <see cref="BreadboardLayout.Result"/> — nothing is assumed about the
    /// parts themselves.
    /// </summary>
    private IEnumerable<string> Audit(BreadboardLayout.Result built)
    {
        var notes = new List<string>();

        // The net each pin belongs to, straight from the extracted netlist.
        var netOfPin = new Dictionary<(string Part, string Pin), string>();
        foreach (var net in built.Nets)
            foreach (var (part, pin) in net.Connections)
                netOfPin[(part.Id, pin.Number)] = net.Name;

        var occupants = new Dictionary<string, NodeUse>();
        var unknownRows = new SortedSet<char>();
        var pinless = new SortedSet<string>(StringComparer.Ordinal);
        int widest = 0;
        bool usesRail = false;

        foreach (var placed in built.Parts)
        {
            if (placed.Pins.Count == 0)
            {
                pinless.Add(placed.Part.Designator);
                continue;
            }

            foreach (var (pin, at) in placed.Pins)
            {
                widest = Math.Max(widest, at.Column);
                usesRail |= at.IsRail;

                if (!at.IsRail && !at.IsUpperBank && at.Row is < 'F' or > 'J')
                    unknownRows.Add(at.Row);

                // A pin with no net carries nothing the netlist asserts, so sharing a hole
                // with something else is not a claim this control can call a short.
                if (!netOfPin.TryGetValue((placed.Part.Id, pin.Number), out var netName)) continue;

                if (!occupants.TryGetValue(at.Node, out var use))
                    occupants[at.Node] = use = new NodeUse { At = at };

                use.Nets.Add(netName);
                use.Parts.Add(placed.Part.Designator);
            }
        }

        // Two different nets in one hole is a short the board would really have. It is
        // reachable: a 4-pin part's last pin lands on column c+6 and the advance is also
        // 6, so the next part's first pin takes the same hole.
        foreach (var use in occupants.Values)
        {
            if (use.Nets.Count < 2) continue;
            _shorted.Add(use.At);
            // TODO: localise
            notes.Add($"รู {use.At} รับสองเน็ตพร้อมกัน ({string.Join(", ", use.Nets)}) จาก {string.Join(", ", use.Parts)} — บนบอร์ดจริงจะลัดถึงกัน");
        }

        if (pinless.Count > 0)
            // TODO: localise
            notes.Add($"{string.Join(", ", pinless)} ไม่มีขาในนิยามชิ้นส่วน จึงไม่มีรูให้ลง — ไม่ได้วาดไว้บนบอร์ด");

        if (widest > BreadboardLayout.Columns)
            // TODO: localise
            notes.Add($"มีขาเลยคอลัมน์ {BreadboardLayout.Columns} (ไปถึงคอลัมน์ {widest}) — วาดพ้นขอบบอร์ดตามที่ผังคำนวณ ไม่ได้ดันกลับเข้ามา");

        if (unknownRows.Count > 0)
            // TODO: localise
            notes.Add($"ผังส่งแถวที่ไม่รู้จักมา: {string.Join(", ", unknownRows)} — วางไว้กลางร่องบอร์ดแทน");

        if (usesRail)
            // TODO: localise
            notes.Add("ผังมีรางไฟขั้วละโหนดเดียว (rail+ / rail−) โดยไม่สนคอลัมน์ — ภาพนี้จึงลงขาที่รางคู่บนเท่านั้น รางคู่ล่างวาดตามบอร์ดจริงแต่ผังยังไม่ได้สร้างสะพานเชื่อมให้");

        // A jumper colour the palette has no arm for is drawn in the error ink, and saying
        // so is the whole point: the colour is how a board this picture gets built from is
        // read, and a wire shown in a colour that is not its own is a wiring instruction
        // that is simply wrong.
        var unpainted = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var jumper in built.Jumpers)
            if (JumperInk(jumper.Colour) is null)
                unpainted.Add(jumper.Colour.ToString());

        if (unpainted.Count > 0)
            // TODO: localise
            notes.Add($"สีจัมเปอร์ที่ยังไม่มีในชุดสีของภาพนี้: {string.Join(", ", unpainted)} — วาดด้วยสีแจ้งเตือน ไม่ใช่สีจริงของสาย");

        return notes;
    }

    /// <summary>
    /// What is plugged into one electrical group while the audit walks the board. A class
    /// rather than a tuple so the "look it up or create it" step above stays a plain
    /// reference the compiler can prove non-null.
    /// </summary>
    private sealed class NodeUse
    {
        public required TiePoint At { get; init; }
        public SortedSet<string> Nets { get; } = new(StringComparer.Ordinal);
        public SortedSet<string> Parts { get; } = new(StringComparer.Ordinal);
    }

    // ── scene coordinates ────────────────────────────────────────────────────

    private static double ColumnX(int column) => FieldLeft + (column - 0.5) * Pitch;

    private static double RowY(char row) => row switch
    {
        >= 'A' and <= 'E' => UpperTop + (row - 'A' + 0.5) * Pitch,
        >= 'F' and <= 'J' => LowerTop + (row - 'F' + 0.5) * Pitch,
        '+' => TopRailTop + 0.5 * Pitch,
        '-' => TopRailTop + 1.5 * Pitch,
        _ => ChannelLineY,                 // reported by Audit rather than guessed at
    };

    private static Point Hole(TiePoint at) => new(ColumnX(at.Column), RowY(at.Row));

    /// <summary>Centre line of every row that carries sockets, top to bottom.</summary>
    private static IEnumerable<double> SocketRows()
    {
        yield return TopRailTop + 0.5 * Pitch;
        yield return TopRailTop + 1.5 * Pitch;
        for (int i = 0; i < 5; i++) yield return UpperTop + (i + 0.5) * Pitch;
        for (int i = 0; i < 5; i++) yield return LowerTop + (i + 0.5) * Pitch;
        yield return BottomRailTop + 0.5 * Pitch;
        yield return BottomRailTop + 1.5 * Pitch;
    }

    /// <summary>
    /// One square per hole, all in a single geometry. The mock rounds these by 1 unit;
    /// at any fit scale that radius is well under a device pixel, so it is dropped rather
    /// than paid for 882 times a frame.
    /// </summary>
    private static Geometry TieGeometry(double half)
    {
        var geo = new StreamGeometry();
        using (var g = geo.Open())
            foreach (var y in SocketRows())
                for (int c = 1; c <= BreadboardLayout.Columns; c++)
                {
                    double x = ColumnX(c);
                    g.BeginFigure(new Point(x - half, y - half), true);
                    g.LineTo(new Point(x + half, y - half));
                    g.LineTo(new Point(x + half, y + half));
                    g.LineTo(new Point(x - half, y + half));
                    g.EndFigure(true);
                }
        return geo;
    }

    // ── fit ──────────────────────────────────────────────────────────────────

    private void Fit()
    {
        if (Bounds.Width <= 1 || Bounds.Height <= 1) return;

        var (_, strip) = NoteStrip(Bounds.Size);
        (_scale, _origin) = Fitting(Bounds.Size, strip);
        InvalidateVisual();
    }

    private (double Scale, Point Origin) Fitting(Size size, double strip)
    {
        double usable = Math.Max(1, size.Height - strip);
        double scale = Math.Max(0.02, Math.Min(size.Width / _sceneWidth, usable / SceneH));
        return (scale, new Point(
            (size.Width - _sceneWidth * scale) / 2,
            (usable - SceneH * scale) / 2));
    }

    // ── rendering ────────────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var bounds = new Rect(Bounds.Size);
        ctx.FillRectangle(SymbolRenderer.Body, bounds);
        if (bounds.Width < 1 || bounds.Height < 1) return;

        var (lines, strip) = NoteStrip(bounds.Size);

        // _scale is 0 only on the very first frame, before SizeChanged has run. Computing
        // the same fit into locals keeps that frame correct without writing a field or
        // invalidating from inside a render pass, which throws.
        double scale = _scale;
        var origin = _origin;
        if (scale <= 0) (scale, origin) = Fitting(bounds.Size, strip);

        using (ctx.PushClip(new Rect(0, 0, bounds.Width, Math.Max(0, bounds.Height - strip))))
        using (ctx.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(origin.X, origin.Y)))
        {
            DrawBoard(ctx);

            if (_layout is { } built)
            {
                foreach (var placed in built.Parts) DrawPart(ctx, placed);
                foreach (var jumper in built.Jumpers) DrawJumper(ctx, jumper);
                // Captions last: a jumper arcs over the part it feeds, and the designator
                // is the one thing that must stay readable underneath it.
                foreach (var placed in built.Parts) DrawCaption(ctx, placed);
                DrawShorts(ctx);
            }
        }

        DrawNotes(ctx, bounds, lines, strip);
    }

    /// <summary>The empty board: this is what an empty document renders, and it is complete.</summary>
    private static void DrawBoard(DrawingContext ctx)
    {
        ctx.DrawRectangle(BoardShadow, null, new Rect(BoardX + 4, BoardY + 4, BoardW, BoardH), 5, 5);
        ctx.DrawRectangle(BoardFace, new Pen(BoardEdge, 1), new Rect(BoardX, BoardY, BoardW, BoardH), 5, 5);

        ctx.DrawRectangle(Channel, null, new Rect(FieldLeft, ChannelTop, FieldRight - FieldLeft, ChannelH));
        ctx.DrawLine(new Pen(ChannelLine, 1), new Point(FieldLeft, ChannelLineY), new Point(FieldRight, ChannelLineY));

        ctx.DrawGeometry(TieSocket, null, SocketField);
        ctx.DrawGeometry(TieInsert, null, InsertField);

        var plus = new Pen(RailPlus, 2);
        var minus = new Pen(RailMinus, 2);
        foreach (var (y, pen) in new[]
                 {
                     (TopPlusLine, plus), (TopMinusLine, minus),
                     (BottomPlusLine, plus), (BottomMinusLine, minus),
                 })
            ctx.DrawLine(pen, new Point(FieldLeft, y), new Point(FieldRight, y));

        DrawRulers(ctx);
    }

    private static void DrawRulers(DrawingContext ctx)
    {
        // Column numbers every five, plus 63 itself so the far end is readable.
        for (int c = 1; c <= BreadboardLayout.Columns; c++)
        {
            if (c != 1 && c != BreadboardLayout.Columns && c % 5 != 0) continue;
            var text = SymbolRenderer.Text(c.ToString(System.Globalization.CultureInfo.InvariantCulture), 9, TieInsert);
            ctx.DrawText(text, new Point(ColumnX(c) - text.Width / 2, ColumnRulerBottom - text.Height));
        }

        // A-E above the channel and F-J below, per TiePoint.IsUpperBank. See the class
        // remarks: the mock letters the strips the other way round, and the model wins.
        for (int i = 0; i < 5; i++)
        {
            RowLabel(ctx, (char)('A' + i), UpperTop + (i + 0.5) * Pitch, TieInsert);
            RowLabel(ctx, (char)('F' + i), LowerTop + (i + 0.5) * Pitch, TieInsert);
        }

        foreach (var (mark, top, brush) in new (string, double, IBrush)[]
                 {
                     ("+", TopRailTop, RailPlus), ("−", TopRailTop + Pitch, RailMinus),
                     ("+", BottomRailTop, RailPlus), ("−", BottomRailTop + Pitch, RailMinus),
                 })
            RowLabel(ctx, mark, top + 0.5 * Pitch, brush);
    }

    private static void RowLabel(DrawingContext ctx, char row, double y, IBrush brush) =>
        RowLabel(ctx, row.ToString(), y, brush);

    private static void RowLabel(DrawingContext ctx, string label, double y, IBrush brush)
    {
        var text = SymbolRenderer.Text(label, 9, brush);
        ctx.DrawText(text, new Point(FieldLeft - 6 - text.Width, y - text.Height / 2));
        ctx.DrawText(text, new Point(FieldRight + 6, y - text.Height / 2));
    }

    // ── parts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// The body box comes from the part's own tie-points, never from
    /// <c>Definition.Package</c>. The catalogue is entirely Provenance.Unverified and no
    /// part is guaranteed to carry package dimensions, so drawing a "DIP-8" at a
    /// millimetre size would be a measurement this program never made. What the layout
    /// does know is which holes the pins occupy, and that is what is drawn.
    /// </summary>
    private static void DrawPart(DrawingContext ctx, PlacedOnBoard placed)
    {
        if (placed.Pins.Count == 0) return;   // reported by Audit

        var box = BodyBox(HoleSpan(placed));
        var (fill, edge) = BodyInk(placed.Part.Definition.Symbol);
        var leadPen = new Pen(Lead, 1.8, lineCap: PenLineCap.Round);

        // Body first, legs and sockets over it. Every fill in the palette is opaque, so
        // whatever goes down before the rectangle is simply erased by it.
        ctx.DrawRectangle(fill, new Pen(edge, 1.2), box, 3, 3);

        foreach (var (_, at) in placed.Pins)
        {
            var hole = Hole(at);
            // Out of the nearest point of the body to the socket. The box lies inside the
            // hole span, so this clamp lands on a real edge and the lead has real length —
            // including the long one to a rail, which BodyBox keeps outside the body.
            var exit = new Point(
                Math.Clamp(hole.X, box.X, box.Right),
                Math.Clamp(hole.Y, box.Y, box.Bottom));
            ctx.DrawLine(leadPen, exit, hole);
            ctx.DrawEllipse(DipEdge, null, hole, 2.2, 2.2);
        }
    }

    /// <summary>
    /// The holes the body has to sit between. Rail pins are excluded: a part with one pin
    /// on the rail and one in a strip is a lead reaching across, not a body that long.
    /// </summary>
    private static Rect HoleSpan(PlacedOnBoard placed)
    {
        var holes = placed.Pins.Where(p => !p.At.IsRail).Select(p => Hole(p.At)).ToList();
        if (holes.Count == 0) holes = placed.Pins.Select(p => Hole(p.At)).ToList();

        double x0 = holes.Min(p => p.X), x1 = holes.Max(p => p.X);
        double y0 = holes.Min(p => p.Y), y1 = holes.Max(p => p.Y);
        return new Rect(x0, y0, x1 - x0, y1 - y0);
    }

    /// <summary>
    /// The body sits between the holes, never over them.
    ///
    /// The box on the schematic sheet runs the other way round: there it is the part's own
    /// footprint and the pins sit a grid step outside it, so <see cref="SymbolRenderer"/>
    /// can clamp a pin to it and get a lead. Here the box is derived FROM the holes, so
    /// inflating it — which is what this used to do — puts every hole strictly inside. The
    /// clamp then returns the hole itself, every lead comes out zero-length, and the opaque
    /// body is painted over the tie-points the part is plugged into, which is the one thing
    /// this view exists to show. The span is therefore deflated instead, along the axis the
    /// legs run: across the rows for a two-bank package, along the row for an axial part.
    /// </summary>
    private static Rect BodyBox(Rect span)
    {
        double left, top, right, bottom;

        if (span.Height > 0.5)
        {
            // Pins in two banks: legs drop out of both rows. The body keeps the column
            // span and a little past it, the way a package overhangs its own pin rows.
            (top, bottom) = LegRun(span.Y, span.Bottom);
            (left, right) = (span.X - Pitch * 0.25, span.Right + Pitch * 0.25);
        }
        else
        {
            // One row: an axial part, legs out along the row.
            (left, right) = LegRun(span.X, span.Right);
            (top, bottom) = (span.Y - Pitch * 0.36, span.Bottom + Pitch * 0.36);
        }

        return new Rect(left, top, right - left, bottom - top);
    }

    /// <summary>Lead long enough to still read as a leg at the fit scale.</summary>
    private const double LeadReach = Pitch * 0.42;

    /// <summary>
    /// The body's extent along the axis the legs run: the hole span pulled in at both ends
    /// so each end has a lead worth seeing. Parts squeezed onto neighbouring columns have
    /// no room for the full reach, so the bite is capped at a third of the span each side
    /// rather than letting the box invert — a thin body with visible legs is the honest
    /// picture of a tight placement. A part on a single hole has nothing to reach across:
    /// it keeps a body of its own, and its hole marker is drawn back over the top.
    /// </summary>
    private static (double Lo, double Hi) LegRun(double lo, double hi)
    {
        double span = hi - lo;
        if (span < 0.5) return (lo - Pitch * 0.36, hi + Pitch * 0.36);

        double bite = Math.Min(LeadReach, span / 3);
        return (lo + bite, hi - bite);
    }

    private static (IBrush Fill, IBrush Edge) BodyInk(SymbolShape shape) => shape switch
    {
        SymbolShape.Box => (ResBody, ResEdge),
        SymbolShape.CapacitorPolarised or SymbolShape.CapacitorNonPolar => (CapBody, CapEdge),
        SymbolShape.Led => (LedBody, LedEdge),
        _ => (DipBody, DipEdge),
    };

    /// <summary>
    /// Designator above, value below. A part with no value gets no value line — the
    /// catalogue leaves <see cref="PartInstance.Value"/> null for anything without a
    /// single figure, and printing a dash or a zero there would read as a measurement.
    /// </summary>
    private static void DrawCaption(DrawingContext ctx, PlacedOnBoard placed)
    {
        if (placed.Pins.Count == 0) return;

        var span = HoleSpan(placed);
        var box = BodyBox(span);
        var designator = SymbolRenderer.Text(placed.Part.Designator, 11, FaceInk);

        // Clear of the whole part, not just of the body. The body now sits between the
        // sockets, so a caption hung off its top edge would land on the row the legs go
        // into and hide the very holes it names.
        double left = Math.Min(box.X, span.X);
        double top = Math.Min(box.Y, span.Y) - 3;
        double bottom = Math.Max(box.Bottom, span.Bottom) + 3;

        // Above by default; below if the part is up against the top rail, so the caption
        // never lands off the board.
        double above = top - designator.Height - 2;
        bool flip = above < BoardY + 4;
        ctx.DrawText(designator, new Point(left, flip ? bottom + 2 : above));

        if (placed.Part.Value is { Length: > 0 } value)
        {
            var text = SymbolRenderer.Text(value, 10, FaceInk);
            ctx.DrawText(text, new Point(left, flip
                ? bottom + 2 + designator.Height
                : bottom + 2));
        }

        // An IC has no value; its part name is the thing printed on the package.
        if (placed.Part.Definition.Symbol == SymbolShape.IcBody)
        {
            var name = SymbolRenderer.Text(placed.Part.Definition.Name, 10, DipCaption);
            if (name.Width < box.Width - 4)
                ctx.DrawText(name, new Point(box.Center.X - name.Width / 2, box.Center.Y - name.Height / 2));
        }
    }

    // ── jumpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The kit colour for one jumper, or null for a <see cref="JumperColour"/> this
    /// palette has no arm for.
    ///
    /// Null rather than a fallback colour, and a switch rather than the index arithmetic
    /// this used to be: <c>(int)Colour % Length</c> wrapped a sixth enum member round onto
    /// Power red, which is a wiring instruction that is confidently wrong. C# cannot make
    /// the missing arm a compile error — a switch expression over an enum still needs a
    /// discard or CS8509 fails the build — so the gap is caught at run time instead, drawn
    /// in the error ink and named in <see cref="Notes"/>.
    /// </summary>
    private static IBrush? JumperInk(JumperColour colour) => colour switch
    {
        JumperColour.Power => JumperPower,
        JumperColour.Ground => JumperGround,
        JumperColour.Signal => JumperSignal,
        JumperColour.SignalAlt => JumperSignalAlt,
        JumperColour.SignalThird => JumperSignalThird,
        _ => null,
    };

    private static void DrawJumper(DrawingContext ctx, Jumper jumper)
    {
        var a = Hole(jumper.From);
        var b = Hole(jumper.To);

        double dx = b.X - a.X, dy = b.Y - a.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001) return;   // DistinctBy(Node) upstream makes this unreachable

        var brush = JumperInk(jumper.Colour) ?? SymbolRenderer.Error;
        var pen = new Pen(brush, 3.4, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        // A bow off the straight line, so two jumpers between neighbouring columns stay
        // distinguishable instead of overprinting each other.
        double bow = Math.Min(Pitch * 2.2, length * 0.18);
        var mid = new Point((a.X + b.X) / 2 - dy / length * bow, (a.Y + b.Y) / 2 + dx / length * bow);

        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(a, false);
            g.QuadraticBezierTo(mid, b);
            g.EndFigure(false);
        }

        ctx.DrawGeometry(null, pen, geo);
        ctx.DrawEllipse(brush, null, a, 2.6, 2.6);
        ctx.DrawEllipse(brush, null, b, 2.6, 2.6);
    }

    /// <summary>Holes the layout gave two different nets. Ringed, because they will short.</summary>
    private void DrawShorts(DrawingContext ctx)
    {
        var pen = new Pen(SymbolRenderer.Error, 2);
        foreach (var at in _shorted)
            ctx.DrawEllipse(null, pen, Hole(at), Pitch * 0.45, Pitch * 0.45);
    }

    // ── notes ────────────────────────────────────────────────────────────────

    /// <summary>
    /// The header and every note, shaped for one strip width.
    ///
    /// Shaped in device pixels below the board rather than inside the scaled scene, so a
    /// warning stays legible however far the board has been scaled down. These are the
    /// honest ones — circuit wider than the board, no ground, no supply — and losing them
    /// to a zoom level would be worse than losing the drawing.
    /// </summary>
    private IReadOnlyList<FormattedText> ShapedLines(double width)
    {
        if (_shapedFor == width) return _shaped;

        double inner = Math.Max(60, width - NotePad * 2);

        // Columns, not "830 จุด": the rails here are 63 continuous holes, so the field
        // drawn is 882 sockets and no part of this program ever counted 830 of anything.
        // The part count is the count actually drawn — DrawPart skips a pin-less part, and
        // Audit already names those separately.
        var header = Prose(
            _layout is { } built
                // TODO: localise
                ? $"เบรดบอร์ด {BreadboardLayout.Columns} คอลัมน์ · {built.Parts.Count(p => p.Pins.Count > 0)} ชิ้นบนบอร์ด · {built.Jumpers.Count} สายจัมเปอร์ · {built.Nets.Count} เน็ต"
                : "ยังไม่มีผังบอร์ด",
            NoteSize, SymbolRenderer.Meta);
        header.MaxTextWidth = inner;

        var lines = new List<FormattedText>(_notes.Count + 1) { header };
        foreach (var note in _notes)
        {
            var line = Prose("• " + note, NoteSize, SymbolRenderer.Error);
            line.MaxTextWidth = inner;
            lines.Add(line);
        }

        _shaped = lines;
        _shapedFor = width;
        return lines;
    }

    /// <summary>
    /// The lines the strip will show, and the height it needs for them.
    ///
    /// A note may be counted into a "+N more" line but never dropped without trace. The
    /// strip used to take the natural height, cap it, and clip — so a document with more
    /// warnings than fit lost the tail of them with nothing on screen saying so, which is
    /// exactly the swallowed warning this list exists to prevent.
    /// </summary>
    private (IReadOnlyList<FormattedText> Lines, double Height) NoteStrip(Size size)
    {
        if (_stripFor is { } cached && cached == size) return (_stripLines, _stripHeight);

        var all = ShapedLines(size.Width);

        double natural = NotePad * 2;
        foreach (var line in all) natural += line.Height + 2;

        // Never let the warnings crowd the board out entirely.
        double cap = Math.Max(NoteSize * 2, size.Height * 0.45);

        if (natural <= cap || all.Count == 1)
        {
            _stripLines = all;
            _stripHeight = natural;
        }
        else
        {
            double inner = Math.Max(60, size.Width - NotePad * 2);
            int keep = all.Count;
            FormattedText more;
            double height;

            // Give up whole lines from the end until the line that counts them fits too.
            // The header always survives — it is the only line that says which board this
            // is — so at the very smallest the strip runs past the cap rather than show a
            // bare count with nothing to attach it to.
            do
            {
                keep--;
                // TODO: localise
                more = Prose($"• อีก {all.Count - keep} รายการ — พื้นที่ด้านล่างไม่พอแสดงทั้งหมด",
                    NoteSize, SymbolRenderer.Error);
                more.MaxTextWidth = inner;

                height = NotePad * 2 + more.Height + 2;
                for (int i = 0; i < keep; i++) height += all[i].Height + 2;
            }
            while (keep > 1 && height > cap);

            var shown = new List<FormattedText>(keep + 1);
            for (int i = 0; i < keep; i++) shown.Add(all[i]);
            shown.Add(more);

            _stripLines = shown;
            _stripHeight = height;
        }

        _stripFor = size;
        return (_stripLines, _stripHeight);
    }

    private static void DrawNotes(DrawingContext ctx, Rect bounds, IReadOnlyList<FormattedText> lines, double strip)
    {
        var panel = new Rect(0, bounds.Height - strip, bounds.Width, strip);
        ctx.DrawRectangle(TableBg, new Pen(TableEdge, 1), panel);

        using (ctx.PushClip(panel))
        {
            double y = panel.Y + NotePad;
            foreach (var line in lines)
            {
                ctx.DrawText(line, new Point(NotePad, y));
                y += line.Height + 2;
            }
        }
    }

    // ── primitives ───────────────────────────────────────────────────────────

    private static IBrush Ink(string hex) => new SolidColorBrush(Color.Parse(hex));

    private static FormattedText Prose(string s, double size, IBrush brush) =>
        new(s, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            ProseFace, size, brush);
}
