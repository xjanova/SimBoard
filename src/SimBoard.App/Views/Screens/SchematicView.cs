using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace SimBoard.App.Views.Screens;

/// <summary>Screen 2 — Schematic editor workspace. Spec: README.md section "### 2 · Schematic editor — `02-schematic-editor.png`".</summary>
public static class SchematicView
{
    // ── the workspace palette. Theme-independent by design: the chrome around the
    //    canvas restyles, the canvas never does, so traces and measured values keep
    //    the same meaning whichever of the seven chrome themes is active.
    private static readonly IBrush BgInk = Ink("#12161b");
    private static readonly IBrush GridInk = Ink("#2b3440");
    private static readonly IBrush WireInk = Ink("#93a9bd");
    private static readonly IBrush RailInk = Ink("#b9cbdb");
    private static readonly IBrush LabelInk = Ink("#8fa8bd");
    private static readonly IBrush MetaInk = Ink("#7f97ab");
    private static readonly IBrush SelInk = Ink("#e8b04a");
    private static readonly IBrush NetInk = Ink("#6fd3e0");
    private static readonly IBrush NetBoxInk = Ink("#2c5f68");
    private static readonly IBrush TpInk = Ink("#d76a5a");
    private static readonly IBrush BodyInk = Ink("#cfdce6");

    /// <summary>The scene is authored at 1:1 in these units and scaled by the Viewbox.</summary>
    private const double SceneW = 1100;
    private const double SceneH = 700;

    /// <summary>
    /// SVG anchors text on its baseline; a TextBlock is placed by its top-left corner.
    /// Lucida Console's ascent is ≈0.9 em, so that is the conversion the scene uses.
    /// </summary>
    private const double Ascent = 0.9;

    /// <summary>Builds the screen. Caller places the returned control.</summary>
    public static Control Build()
    {
        var scene = new Canvas
        {
            Width = SceneW,
            Height = SceneH,
            Background = BgInk,
            // The dot pattern and the boxed net labels bleed a fraction past the edge,
            // exactly as the SVG pattern does inside its rect. Clip like the SVG does.
            ClipToBounds = true,
        };

        DotGrid(scene);
        Wires(scene);
        Symbols(scene);
        SelectedR2(scene);
        Junctions(scene);
        Devices(scene);
        NetLabels(scene);
        ReferenceLabels(scene);
        TestPoint(scene);
        TitleBlock(scene);

        return new Bevel
        {
            Classes = { "workspace" },
            Margin = new Thickness(6, 0, 6, 4),   // spec: workspace inset "0 6px 4px"
            Child = new Viewbox
            {
                // preserveAspectRatio="xMidYMid meet" — grow and shrink, never distort.
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = scene,
            },
        };
    }

    // ── background ───────────────────────────────────────────────────────────

    /// <summary>
    /// The 16px dot grid. One Path of 3,036 sub-paths rather than 3,036 Ellipse
    /// controls: the grid is pure decoration and must not cost a visual each.
    /// </summary>
    private static void DotGrid(Canvas c)
    {
        var data = string.Concat(
            from row in Enumerable.Range(0, 44)
            from col in Enumerable.Range(0, 69)
            select $"M{col * 16 - 1},{row * 16} a1,1 0 1 0 2,0 a1,1 0 1 0 -2,0 ");

        Add(c, new ShapePath { Data = Geometry.Parse(data), Fill = GridInk }, 0, 0);
    }

    // ── nets ─────────────────────────────────────────────────────────────────

    private static void Wires(Canvas c)
    {
        // Power rails: +9V_IN at y=90, GND at y=610. Heavier and lighter than a net.
        Seg(c, 80, 90, 960, 90, RailInk, 2, PenLineCap.Square);
        Seg(c, 80, 610, 960, 610, RailInk, 2, PenLineCap.Square);

        Box(c, 70, 320, 42, 70, WireInk, 1.6);                 // J1 DC IN
        W(c, 112, 338, 112, 140);
        W(c, 112, 140, 140, 140);
        W(c, 180, 140, 200, 140);

        Box(c, 200, 108, 112, 64, WireInk, 1.6);               // U1 LM7805
        W(c, 312, 140, 360, 140);
        W(c, 360, 140, 360, 90);
        W(c, 256, 172, 256, 610);
        W(c, 112, 372, 112, 610);
        W(c, 190, 140, 190, 240);
        W(c, 190, 264, 190, 610);
        W(c, 336, 140, 336, 240);
        W(c, 336, 264, 336, 610);

        Box(c, 450, 230, 140, 200, WireInk, 1.6);              // U2 NE555
        W(c, 450, 265, 410, 265);
        W(c, 410, 265, 410, 610);
        W(c, 450, 310, 420, 310);
        W(c, 420, 310, 420, 555);
        W(c, 420, 555, 760, 555);
        W(c, 450, 355, 340, 355);
        W(c, 340, 355, 340, 440);
        W(c, 340, 490, 340, 520);
        W(c, 340, 546, 340, 610);
        W(c, 450, 400, 382, 400);
        W(c, 382, 400, 382, 90);
        W(c, 590, 265, 640, 265);
        W(c, 640, 265, 640, 90);
        W(c, 590, 310, 700, 310);
        W(c, 660, 310, 660, 230);
        W(c, 660, 180, 660, 90);
        W(c, 590, 355, 700, 355);
        W(c, 700, 310, 700, 322);
        W(c, 700, 343, 700, 355);
        W(c, 700, 355, 760, 355);
        W(c, 760, 355, 760, 470);
        W(c, 760, 494, 760, 610);
        W(c, 590, 400, 830, 400);
        W(c, 830, 400, 830, 470);
        W(c, 830, 494, 830, 610);

        Box(c, 960, 300, 46, 112, WireInk, 1.6);               // J2 MCU
        W(c, 960, 326, 920, 326);
        W(c, 960, 356, 920, 356);
        W(c, 960, 386, 920, 386);
    }

    /// <summary>Junction dots — r=3 where three or more nets meet, r=2.5 on header pins.</summary>
    private static void Junctions(Canvas c)
    {
        foreach (var (x, y) in new (double, double)[]
                 {
                     (190, 140), (336, 140), (360, 90), (382, 90), (640, 90), (660, 90),
                     (660, 310), (760, 355), (760, 555), (112, 610), (190, 610), (256, 610),
                     (336, 610), (340, 610), (410, 610), (760, 610), (830, 610),
                 })
            Circle(c, x, y, 3, WireInk);

        Gnd(c, 520, 610, WireInk);
    }

    // ── symbols ──────────────────────────────────────────────────────────────

    private static void Symbols(Canvas c)
    {
        ResH(c, 140, 140, WireInk);    // D1 1N4007
        ECapV(c, 190, 240, WireInk);   // C1 470µ/25V
        CapV(c, 336, 240, WireInk);    // C2 100n
        ResV(c, 340, 440, WireInk);    // R4 470Ω
        LedV(c, 340, 520, WireInk);    // D2 LED RED
        ResV(c, 660, 180, WireInk);    // R1 10k
        CapV(c, 760, 470, WireInk);    // C3 10n
        CapV(c, 830, 470, WireInk);    // C4 10n
    }

    /// <summary>
    /// R2 is the selected object: amber symbol, a 3-2 dashed bounding rect, and four
    /// 7×7 corner handles. Selection only ever changes colour — never geometry.
    /// </summary>
    private static void SelectedR2(Canvas c)
    {
        ResV(c, 700, 293, SelInk, 0.42);
        Box(c, 686, 316, 28, 34, SelInk, 1.6);

        var marquee = Box(c, 678, 300, 44, 66, SelInk, 1);
        marquee.StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 3, 2 };

        foreach (var (x, y) in new (double, double)[] { (674, 296), (719, 296), (674, 363), (719, 363) })
            Box(c, x, y, 7, 7, null, 0, SelInk);
    }

    // ── parts with a body: the box is repainted opaque so nets pass behind it ─

    private static void Devices(Canvas c)
    {
        Box(c, 450, 230, 140, 200, WireInk, 1.6, BgInk);
        Text(c, 520, 316, 13, BodyInk, "NE555", TextAlignment.Center);
        Text(c, 520, 334, 10, MetaInk, "U2 · DIP-8", TextAlignment.Center);

        // DIP-8 pin numbering: 1–4 down the left, 8–5 down the right.
        Text(c, 456, 262, 9, MetaInk, "1 GND");
        Text(c, 456, 307, 9, MetaInk, "2 TRG");
        Text(c, 456, 352, 9, MetaInk, "3 OUT");
        Text(c, 456, 397, 9, MetaInk, "4 RST");
        Text(c, 584, 262, 9, MetaInk, "VCC 8", TextAlignment.Right);
        Text(c, 584, 307, 9, MetaInk, "DIS 7", TextAlignment.Right);
        Text(c, 584, 352, 9, MetaInk, "THR 6", TextAlignment.Right);
        Text(c, 584, 397, 9, MetaInk, "CTL 5", TextAlignment.Right);

        Box(c, 200, 108, 112, 64, WireInk, 1.6, BgInk);
        Text(c, 256, 136, 12, BodyInk, "LM7805", TextAlignment.Center);
        Text(c, 256, 152, 9, MetaInk, "U1 · TO-220", TextAlignment.Center);

        Box(c, 70, 320, 42, 70, WireInk, 1.6, BgInk);
        Box(c, 960, 300, 46, 112, WireInk, 1.6, BgInk);

        Text(c, 76, 342, 9, MetaInk, "J1");
        Text(c, 76, 356, 9, MetaInk, "DC");
        Text(c, 76, 370, 9, MetaInk, "IN");
        Text(c, 966, 318, 9, MetaInk, "J2");
        Text(c, 966, 332, 9, MetaInk, "MCU");

        foreach (var (x, y) in new (double, double)[]
                 { (112, 338), (112, 372), (960, 326), (960, 356), (960, 386) })
            Circle(c, x, y, 2.5, WireInk);
    }

    // ── annotation ───────────────────────────────────────────────────────────

    /// <summary>Net labels: bare text on the rails, boxed on the J2 header pins.</summary>
    private static void NetLabels(Canvas c)
    {
        Text(c, 86, 82, 10, NetInk, "+9V_IN");
        Text(c, 404, 82, 10, NetInk, "+5V");
        Text(c, 86, 628, 10, NetInk, "GND");

        foreach (var (y, name) in new (double, string)[] { (317, "+5V"), (347, "OUT"), (377, "GND") })
        {
            Box(c, 884, y, 34, 14, NetBoxInk, 1, BgInk);
            Text(c, 888, y + 11, 10, NetInk, name);
        }
    }

    /// <summary>Reference designators. R2's own label carries the selection colour.</summary>
    private static void ReferenceLabels(Canvas c)
    {
        Text(c, 152, 126, 10, LabelInk, "D1 1N4007");
        Text(c, 204, 252, 10, LabelInk, "C1 470µ/25V");
        Text(c, 350, 252, 10, LabelInk, "C2 100n");
        Text(c, 354, 470, 10, LabelInk, "R4 470Ω");
        Text(c, 354, 546, 10, LabelInk, "D2 LED RED");
        Text(c, 674, 212, 10, LabelInk, "R1 10k");
        Text(c, 734, 336, 10, SelInk, "R2 47k");
        Text(c, 774, 486, 10, LabelInk, "C3 10n");
        Text(c, 844, 486, 10, LabelInk, "C4 10n");
    }

    private static void TestPoint(Canvas c)
    {
        Circle(c, 340, 380, 4, null, TpInk, 1.6);
        Text(c, 350, 384, 9, TpInk, "TP1");
    }

    /// <summary>The 288×96 drawing-sheet title block, bottom-right of the sheet.</summary>
    private static void TitleBlock(Canvas c)
    {
        Box(c, 800, 596, 288, 96, GridInk, 1);
        Seg(c, 800, 620, 1088, 620, GridInk, 1);
        Seg(c, 800, 644, 1088, 644, GridInk, 1);
        Seg(c, 800, 668, 1088, 668, GridInk, 1);
        Seg(c, 944, 644, 944, 692, GridInk, 1);

        Text(c, 806, 612, 9, MetaInk, "555 ASTABLE + 5V REG · SHEET 1/3");
        Text(c, 806, 636, 9, MetaInk, "SIMBOARD · REV C");
        Text(c, 806, 660, 9, MetaInk, "DATE 03-09-2026");
        Text(c, 950, 660, 9, MetaInk, "SCALE 1:1");
        Text(c, 806, 684, 9, MetaInk, "DRC 0 ERR");
        Text(c, 950, 684, 9, MetaInk, "NETS 14");
    }

    // ── the symbol library, one method per <defs> group ──────────────────────
    //
    // Every symbol is drawn in the ink it is handed, never in a hard-coded colour:
    // that is what lets selection and current-flow recolour a part without a second
    // copy of its geometry. IEC-style rectangular resistors, 1.6px stroke throughout.

    private static void ResV(Canvas c, double ox, double oy, IBrush ink, double sy = 1)
    {
        Box(c, ox - 6.5, oy + 8 * sy, 13, 34 * sy, ink, 1.6);
        Seg(c, ox, oy, ox, oy + 8 * sy, ink, 1.6);
        Seg(c, ox, oy + 42 * sy, ox, oy + 50 * sy, ink, 1.6);
    }

    private static void ResH(Canvas c, double ox, double oy, IBrush ink)
    {
        Box(c, ox + 8, oy - 6.5, 34, 13, ink, 1.6);
        Seg(c, ox, oy, ox + 8, oy, ink, 1.6);
        Seg(c, ox + 42, oy, ox + 50, oy, ink, 1.6);
    }

    private static void CapV(Canvas c, double ox, double oy, IBrush ink)
    {
        Seg(c, ox, oy, ox, oy + 9, ink, 1.6);
        Seg(c, ox - 10, oy + 9, ox + 10, oy + 9, ink, 1.6);
        Seg(c, ox - 10, oy + 15, ox + 10, oy + 15, ink, 1.6);
        Seg(c, ox, oy + 15, ox, oy + 24, ink, 1.6);
    }

    /// <summary>Electrolytic: the curved plate is what marks the polarity.</summary>
    private static void ECapV(Canvas c, double ox, double oy, IBrush ink)
    {
        Seg(c, ox, oy, ox, oy + 9, ink, 1.6);
        Seg(c, ox - 10, oy + 9, ox + 10, oy + 9, ink, 1.6);
        Add(c, new ShapePath
        {
            Data = Geometry.Parse(FormattableString.Invariant(
                $"M{ox - 10},{oy + 17} Q{ox},{oy + 11} {ox + 10},{oy + 17}")),
            Stroke = ink,
            StrokeThickness = 1.6,
        }, 0, 0);
        Seg(c, ox, oy + 15, ox, oy + 24, ink, 1.6);
    }

    private static void LedV(Canvas c, double ox, double oy, IBrush ink)
    {
        Seg(c, ox, oy, ox, oy + 6, ink, 1.6);
        Add(c, new ShapePath
        {
            Data = Geometry.Parse(FormattableString.Invariant(
                $"M{ox - 8},{oy + 6} L{ox + 8},{oy + 6} L{ox},{oy + 20} Z")),
            Fill = ink,
        }, 0, 0);
        Seg(c, ox - 9, oy + 20, ox + 9, oy + 20, ink, 1.6);
        Seg(c, ox, oy + 20, ox, oy + 26, ink, 1.6);
        Seg(c, ox + 10, oy + 4, ox + 16, oy - 2, ink, 1.6);    // emission arrows
        Seg(c, ox + 12, oy + 11, ox + 18, oy + 5, ink, 1.6);
    }

    private static void Gnd(Canvas c, double ox, double oy, IBrush ink)
    {
        Seg(c, ox, oy, ox, oy + 7, ink, 1.6);
        Seg(c, ox - 11, oy + 7, ox + 11, oy + 7, ink, 1.6);
        Seg(c, ox - 7, oy + 11, ox + 7, oy + 11, ink, 1.6);
        Seg(c, ox - 3, oy + 15, ox + 3, oy + 15, ink, 1.6);
    }

    // ── primitives ───────────────────────────────────────────────────────────

    private static SolidColorBrush Ink(string hex) => new(Color.Parse(hex));

    private static void Add(Canvas c, Control child, double left, double top)
    {
        Canvas.SetLeft(child, left);
        Canvas.SetTop(child, top);
        c.Children.Add(child);
    }

    /// <summary>A default net segment: #93a9bd, 1.6px, square cap, as the spec states.</summary>
    private static void W(Canvas c, double x1, double y1, double x2, double y2)
        => Seg(c, x1, y1, x2, y2, WireInk, 1.6, PenLineCap.Square);

    /// <summary>
    /// A line in scene coordinates. Shapes with Stretch.None keep their geometry's
    /// absolute offset, so every segment can sit at Canvas 0,0 and still land where
    /// the SVG puts it — no per-line origin arithmetic, no drift.
    /// </summary>
    private static void Seg(Canvas c, double x1, double y1, double x2, double y2,
        IBrush ink, double w, PenLineCap cap = PenLineCap.Flat)
        => Add(c, new Line
        {
            StartPoint = new Point(x1, y1),
            EndPoint = new Point(x2, y2),
            Stroke = ink,
            StrokeThickness = w,
            StrokeLineCap = cap,
        }, 0, 0);

    /// <summary>
    /// A rectangle whose stroke straddles x,y,w,h the way an SVG rect's does.
    /// Avalonia deflates a Rectangle's geometry by half the stroke, so the control
    /// is inflated by a full stroke and shifted back half of one to compensate.
    /// </summary>
    private static Rectangle Box(Canvas c, double x, double y, double w, double h,
        IBrush? stroke, double sw, IBrush? fill = null)
    {
        var r = new Rectangle
        {
            Width = w + sw,
            Height = h + sw,
            Stroke = stroke,
            StrokeThickness = sw,
            Fill = fill,
        };
        Add(c, r, x - sw / 2, y - sw / 2);
        return r;
    }

    /// <summary>Same correction as <see cref="Box"/>, for a circle given by centre and radius.</summary>
    private static void Circle(Canvas c, double cx, double cy, double r,
        IBrush? fill, IBrush? stroke = null, double sw = 0)
    {
        double d = 2 * r + sw;
        Add(c, new Ellipse { Width = d, Height = d, Fill = fill, Stroke = stroke, StrokeThickness = sw },
            cx - d / 2, cy - d / 2);
    }

    /// <summary>
    /// Mono text placed the way SVG places it: by baseline, with a text anchor.
    /// The fixed width is the anchoring trick — a TextBlock has no anchor, so it is
    /// given a box wider than any string in the scene and aligned inside it.
    /// </summary>
    private static void Text(Canvas c, double x, double baseline, double size, IBrush ink,
        string text, TextAlignment align = TextAlignment.Left)
    {
        const double w = 300;
        var tb = new TextBlock
        {
            Classes = { "mono" },
            Text = text,
            FontSize = size,
            Foreground = ink,
            Width = w,
            TextAlignment = align,
        };
        double left = align switch
        {
            TextAlignment.Center => x - w / 2,
            TextAlignment.Right => x - w,
            _ => x,
        };
        Add(c, tb, left, baseline - size * Ascent);
    }
}
