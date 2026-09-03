using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;

// ImplicitUsings pulls in System.IO, so a bare `Path` is ambiguous with System.IO.Path.
// In a view file it is always the shape.
using Path = Avalonia.Controls.Shapes.Path;

namespace SimBoard.App.Views.Screens;

/// <summary>
/// Screen 4 — Breadboard 2D. Spec: README.md section "4 · Breadboard 2D — `04-breadboard-2d.png`".
///
/// The mock is one hand-authored SVG on a 1120 × 700 viewBox with
/// <c>preserveAspectRatio="xMidYMid meet"</c>. It is rebuilt here at those exact
/// user-space coordinates on a fixed <see cref="Canvas"/>, and a <see cref="Viewbox"/>
/// supplies the "meet" scaling — every number below is straight out of the spec, so
/// nothing has to be re-derived when the drawing is compared against the PNG.
///
/// This is the point of the 2D view: the parts are drawn as physical objects (a DIP with
/// a notch and legs, resistors carrying their real 4-band colour code, a glowing LED),
/// not as schematic symbols. Colours come from the theme-independent workspace palette
/// and are deliberately hard-coded — the spec says the canvas is never restyled.
/// </summary>
public static class BreadboardView
{
    private const double W = 1120;
    private const double H = 700;

    /// <summary>0.1″ pitch of the tie-point pattern, in viewBox units.</summary>
    private const double Pitch = 19;

    /// <summary>Width of the invisible box that gives <c>text-anchor</c> something to align in.</summary>
    private const double AnchorBox = 240;

    // ── workspace palette (theme-independent — never restyled) ───────────────
    private const string Bg = "#12161b";
    private const string GridDot = "#2b3440";
    private const string Meta = "#7f97ab";
    private const string LabelC = "#8fa8bd";
    private const string Current = "#e8b04a";
    private const string PanelText = "#6f8ba1";

    // breadboard
    private const string BoardFace = "#efece4";
    private const string BoardEdge = "#c9c5ba";
    private const string BoardShadow = "#0d1114";
    private const string Channel = "#e4e0d6";
    private const string ChannelLine = "#cfcabd";
    private const string TieSocket = "#2f3134";
    private const string TieInsert = "#8d8f92";
    private const string RailPlus = "#c0473a";
    private const string RailMinus = "#3a5fa0";

    // parts
    private const string Lead = "#9aa0a6";
    private const string DipBody = "#23262a";
    private const string DipEdge = "#3c4046";
    private const string DipMark = "#5c6169";
    private const string DipLeg = "#8a8f96";
    private const string ResBody = "#e0cfa8";
    private const string ResEdge = "#b6a377";

    // instrument panels
    private const string PanelFace = "#d6d3ce";
    private const string PanelEdge = "#404040";
    private const string PanelHeader = "#4a5f7a";
    private const string KnobFace = "#c9c5bf";
    private const string KnobEdge = "#8a8680";
    private const string DmmBg = "#0a1a12";
    private const string DmmEdge = "#2b3a32";
    private const string DmmTrace = "#48e08a";
    private const string ScopeBg = "#04160f";

    // bottom tables
    private const string TableBg = "#0d1418";
    private const string TableEdge = "#2b3440";

    /// <summary>Where the anchor point of a run of text sits relative to the run.</summary>
    private enum TextAnchor { Start, Middle, End }

    /// <summary>Builds the screen. Caller places the returned control.</summary>
    public static Control Build()
    {
        var scene = new Canvas { Width = W, Height = H, Background = B(Bg) };

        DotGrid(scene);
        Board(scene);
        Parts(scene);
        Jumpers(scene);
        PowerSupply(scene);
        FunctionGenerator(scene);
        BottomTables(scene);

        // The SVG's preserveAspectRatio="xMidYMid meet": scale down to fit, keep the
        // aspect, centre what is left over.
        return new Viewbox
        {
            Child = scene,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    // ── workspace backdrop ───────────────────────────────────────────────────

    /// <summary>The 16px dot pattern (<c>pattern#dots</c>): one r=1 dot per tile corner.</summary>
    private static void DotGrid(Canvas c)
    {
        // Thousands of dots, but one Path: a control per dot would be 3 000 visuals for
        // a texture nobody ever hit-tests.
        var sb = new System.Text.StringBuilder();
        for (double x = 0; x <= W; x += 16)
            for (double y = 0; y <= H; y += 16)
                sb.Append(FormattableString.Invariant(
                    $"M{x - 1},{y}a1,1 0 1,0 2,0a1,1 0 1,0 -2,0"));

        Add(c, new Path { Data = Geometry.Parse(sb.ToString()), Fill = B(GridDot) }, 0, 0);
    }

    // ── the 830-point board ──────────────────────────────────────────────────

    private static void Board(Canvas c)
    {
        Rect(c, 34, 54, 812, 418, BoardShadow, radius: 5);
        Rect(c, 30, 50, 812, 418, BoardFace, BoardEdge, radius: 5);

        // top power rails
        Rect(c, 46, 66, 780, 38, BoardFace);
        TieField(c, 46, 66, 779, 38);
        Stroke(c, "M46,60 L826,60", RailPlus, 2);
        Stroke(c, "M46,110 L826,110", RailMinus, 2);
        Text(c, "+", 38, 64, 10, RailPlus);
        Text(c, "−", 38, 114, 10, RailMinus);

        // upper terminal strip, centre channel, lower terminal strip
        TieField(c, 46, 142, 779, 95);
        Rect(c, 46, 237, 780, 44, Channel);
        Stroke(c, "M46,259 L826,259", ChannelLine, 1);
        TieField(c, 46, 281, 779, 95);

        // bottom power rails
        TieField(c, 46, 414, 779, 38);
        Stroke(c, "M46,408 L826,408", RailPlus, 2);
        Stroke(c, "M46,458 L826,458", RailMinus, 2);

        foreach (var (label, x) in new (string, double)[]
                 {
                     ("1", 52), ("6", 147), ("11", 242), ("16", 337), ("21", 432),
                     ("26", 527), ("31", 622), ("36", 717), ("40", 793),
                 })
            Text(c, label, x, 136, 9, TieInsert);

        foreach (var (label, y) in new (string, double)[] { ("J", 160), ("F", 217), ("E", 300), ("A", 357) })
            Text(c, label, 20, y, 9, TieInsert);
    }

    /// <summary>
    /// One rectangle of <c>pattern#tie</c>: a 19 × 19 tile carrying a 5 × 5 socket with a
    /// 4 × 4 insert. The pattern is anchored to the viewBox origin (userSpaceOnUse), not to
    /// the rectangle, so the tiles are laid out in absolute coordinates and clipped —
    /// that is what puts a half-row of sockets at the edge of each power rail.
    /// </summary>
    private static void TieField(Canvas c, double x, double y, double w, double h)
    {
        var field = new Canvas { Width = w, Height = h, ClipToBounds = true };

        for (double tx = Math.Floor(x / Pitch) * Pitch; tx < x + w; tx += Pitch)
            for (double ty = Math.Floor(y / Pitch) * Pitch; ty < y + h; ty += Pitch)
            {
                Rect(field, tx - x + 6.5, ty - y + 6.5, 5, 5, TieSocket, radius: 1);
                Rect(field, tx - x + 7, ty - y + 7, 4, 4, TieInsert, radius: 1);
            }

        Add(c, field, x, y);
    }

    // ── parts, drawn as objects rather than symbols ──────────────────────────

    private static void Parts(Canvas c)
    {
        // DIP-8 · NE555P
        Rect(c, 330, 240, 152, 60, DipBody, DipEdge, radius: 3);
        Stroke(c, "M394,240 a12,12 0 0 0 24,0", DipMark, 1.6);        // notch arc
        Circle(c, 344, 290, 3, DipMark);                              // pin-1 dot
        foreach (double x in new double[] { 341, 379, 417, 455 })
        {
            Rect(c, x, 228, 9, 14, DipLeg);
            Rect(c, x, 298, 9, 14, DipLeg);
        }
        Text(c, "NE555P", 406, 276, 12, "#c8cdd4", anchor: TextAnchor.Middle);

        // 10 kΩ — brown black orange gold
        Resistor(c, 150,
            [(16, 7, "#6b4a2a"), (28, 7, "#111111"), (40, 7, "#c0473a"), (56, 5, "#c9a227")],
            "M150,188 H132 v-27", "M226,188 h18 v-27");

        // 47 kΩ — yellow violet orange gold
        Resistor(c, 560,
            [(16, 7, "#c9a227"), (28, 7, "#5a3fa0"), (40, 7, "#c0473a"), (56, 5, "#c9a227")],
            "M560,188 h-18 v-27", "M636,188 h18 v-27");

        // red LED with its glow and specular highlight
        Oval(c, 704, 330, 17, 20, "#3f7d68", 0.25);
        Circle(c, 704, 330, 13, "#d94f3d", "#8f2f22", 1.4);
        Circle(c, 699, 325, 4, "#ff9c85", opacity: 0.8);
        Stroke(c, "M698,343 v22 M710,343 v22", Lead, 1.8);

        // electrolytic cap
        Rect(c, 238, 300, 20, 34, "#2c3f6b", "#1b2846", radius: 2);
        Stroke(c, "M242,334 v32 M254,334 v32", Lead, 1.8);
        Text(c, "C1", 248, 296, 8, TieInsert, anchor: TextAnchor.Middle);
    }

    /// <summary>An axial resistor: 76 × 16 rx-7 body, four colour bands, two bent leads.</summary>
    private static void Resistor(Canvas c, double x, (double Dx, double W, string Hex)[] bands,
                                 string leadLeft, string leadRight)
    {
        const double y = 180;
        Rect(c, x, y, 76, 16, ResBody, ResEdge, radius: 7);
        foreach (var (dx, w, hex) in bands) Rect(c, x + dx, y, w, 16, hex);
        Stroke(c, leadLeft, Lead, 1.8);
        Stroke(c, leadRight, Lead, 1.8);
    }

    /// <summary>Cubic Béziers in the five colours a real jumper kit ships in.</summary>
    private static void Jumpers(Canvas c)
    {
        foreach (var (data, colour) in new[]
                 {
                     ("M60,420 C120,400 240,402 344,320", "#c0473a"),
                     ("M60,446 C130,470 300,478 460,320", "#1b1d20"),
                     ("M456,240 C470,190 540,170 542,160", "#c9a227"),
                     ("M380,240 C360,200 300,190 244,180", "#3f9d5a"),
                     ("M418,300 C500,340 620,336 692,332", "#3a5fa0"),
                     ("M712,366 C740,420 780,430 800,446", "#1b1d20"),
                     ("M654,160 C700,130 770,140 800,66", "#c0473a"),
                 })
            Stroke(c, data, colour, 3.4, PenLineCap.Round);
    }

    // ── bench instruments ────────────────────────────────────────────────────

    private static void PowerSupply(Canvas c)
    {
        Enclosure(c, 870, 50, 216, 196, "DC POWER SUPPLY · 0-30V");

        Rect(c, 882, 76, 192, 44, DmmBg, DmmEdge);
        Readout(c, 882, 76, 192, 44, "9.00", 26, DmmTrace);
        Text(c, "VOLT", 890, 92, 9, "#2f7a55");

        Rect(c, 882, 126, 192, 34, DmmBg, DmmEdge);
        Readout(c, 882, 126, 192, 34, "0.128", 18, Current);
        Text(c, "AMP", 890, 140, 9, "#8a6420");

        // voltage + current knobs
        Circle(c, 908, 196, 20, KnobFace, KnobEdge, 2);
        Stroke(c, "M908,196 L920,182", "#333333", 2.5);
        Circle(c, 966, 196, 14, KnobFace, KnobEdge, 2);
        Stroke(c, "M966,196 L966,184", "#333333", 2.5);

        // binding posts
        Circle(c, 1020, 190, 8, RailPlus, "#7a2c22", 2);
        Circle(c, 1052, 190, 8, "#1b1d20", "#000000", 2);
        Text(c, "+", 1020, 216, 8, "#333333", anchor: TextAnchor.Middle);
        Text(c, "−", 1052, 216, 8, "#333333", anchor: TextAnchor.Middle);

        Text(c, "CV MODE · OVP 12.0V", 882, 238, 9, "#333333", mono: false);
    }

    private static void FunctionGenerator(Canvas c)
    {
        Enclosure(c, 870, 262, 216, 180, "FUNCTION GENERATOR · 2MHz");

        Rect(c, 882, 288, 192, 52, ScopeBg, DmmEdge);
        var wave = new Polyline
        {
            Stroke = B(DmmTrace),
            StrokeThickness = 1.8,
            Points = new List<Point>
            {
                new(890, 330), new(906, 300), new(922, 330), new(938, 300),
                new(954, 330), new(970, 300), new(986, 330), new(1002, 300),
                new(1018, 330), new(1034, 300), new(1050, 330), new(1066, 300),
            },
        };
        Add(c, wave, 0, 0);

        Rect(c, 882, 346, 192, 24, DmmBg, DmmEdge);
        Readout(c, 882, 346, 192, 24, "1.000 kHz", 14, Current);

        foreach (var (x, w, label, cx) in new (double, double, string, double)[]
                 {
                     (884, 42, "SINE", 905), (932, 42, "SQR", 953),
                     (980, 42, "TRI", 1001), (1028, 46, "SWEEP", 1051),
                 })
        {
            Rect(c, x, 380, w, 18, KnobFace, KnobEdge, radius: 2);
            Text(c, label, cx, 393, 9, "#222222", mono: false, anchor: TextAnchor.Middle);
        }

        Text(c, "AMPL 5.00 Vpp · OFFSET 0.00 V", 882, 420, 9, "#333333", mono: false);
        Text(c, "DUTY 50% · OUT 50Ω", 882, 434, 9, "#333333", mono: false);
    }

    /// <summary>Instrument enclosure: face, 16px header strip, bold white caption.</summary>
    private static void Enclosure(Canvas c, double x, double y, double w, double h, string caption)
    {
        Rect(c, x, y, w, h, PanelFace, PanelEdge, radius: 3);
        Rect(c, x, y, w, 16, PanelHeader);
        Text(c, caption, x + 8, y + 12, 10, "#ffffff", mono: false, bold: true);
    }

    /// <summary>
    /// A large right-aligned 7-seg style value inside its display well. Centred in the
    /// well rather than sat on a baseline: at 26px a baseline guess drifts visibly.
    /// </summary>
    private static void Readout(Canvas c, double x, double y, double w, double h,
                                string value, double size, string colour)
    {
        var host = new Panel { Width = w, Height = h };
        var tb = Label(value, size, colour, mono: true, bold: false);
        tb.HorizontalAlignment = HorizontalAlignment.Right;
        tb.VerticalAlignment = VerticalAlignment.Center;
        tb.Margin = new Thickness(0, 0, 8, 0);
        host.Children.Add(tb);
        Add(c, host, x, y);
    }

    // ── bottom tables ────────────────────────────────────────────────────────

    private static void BottomTables(Canvas c)
    {
        Rect(c, 30, 492, 812, 176, null, TableEdge);

        BoundText(c, Keys.BbLegend, 42, 512, 10, Meta);

        Rect(c, 42, 522, 380, 132, TableBg, TableEdge);
        double y = 540;
        foreach (var row in new[]
                 {
                     "ROW  NET      TIE-POINTS  V(DC)   I(mA)",
                     "J1   +9V_IN   40          9.00    128.4",
                     "F11  TRIG     5           4.21    0.02",
                     "E16  OUT      5           3.98    9.41",
                     "E31  LED_A    5           1.94    9.38",
                     "J-   GND      40          0.00    —",
                 })
        {
            Text(c, row, 52, y, 9, LabelC);
            y += 18;
        }

        Rect(c, 436, 522, 406, 132, TableBg, TableEdge);
        BoundText(c, Keys.BbBoards, 446, 540, 9, LabelC);
        BoardList(c);
    }

    /// <summary>
    /// The installed-board list. Exactly one board is in use, and clicking a row moves
    /// that state: the glyph, the amber colour and the <c>(กำลังใช้)</c> suffix all follow
    /// the selection, which is the only behaviour this screen owns.
    /// </summary>
    private static void BoardList(Canvas c)
    {
        string[] bodies =
        [
            "830-PT  BB-830   ",
            "400-PT  BB-400 ",
            "PERF     PB-9X15  2.54mm ",
            "PCB 2L   PCB-100X80 ",
            "DEV      UNO / ESP32 / STM32F4 ",
        ];

        var glyphs = new List<TextBlock>();
        var suffixes = new List<TextBlock>();

        for (int i = 0; i < bodies.Length; i++)
        {
            bool active = i == 0;

            var body = Label((active ? "▣ " : "▢ ") + bodies[i], 9, active ? Current : PanelText,
                mono: true, bold: false);
            var suffix = Bound(Keys.BbActive, 9, Current);
            suffix.IsVisible = active;

            glyphs.Add(body);
            suffixes.Add(suffix);

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = Brushes.Transparent,   // so the whole row is hit-testable
                Children = { body, suffix },
            };

            int index = i;
            row.PointerPressed += (_, _) =>
            {
                for (int j = 0; j < glyphs.Count; j++)
                {
                    bool on = j == index;
                    glyphs[j].Text = (on ? "▣ " : "▢ ") + bodies[j];
                    glyphs[j].Foreground = B(on ? Current : PanelText);
                    suffixes[j].IsVisible = on;
                }
            };

            Add(c, row, 446, Baseline(560 + i * 18, 9, mono: true));
        }
    }

    // ── primitives ───────────────────────────────────────────────────────────

    private static SolidColorBrush B(string hex) => new(Color.Parse(hex));

    private static void Add(Canvas c, Control child, double x, double y)
    {
        Canvas.SetLeft(child, x);
        Canvas.SetTop(child, y);
        c.Children.Add(child);
    }

    private static void Rect(Canvas c, double x, double y, double w, double h,
                             string? fill, string? stroke = null, double thickness = 1, double radius = 0)
    {
        var r = new Rectangle { Width = w, Height = h, RadiusX = radius, RadiusY = radius };
        if (fill is not null) r.Fill = B(fill);
        if (stroke is not null) { r.Stroke = B(stroke); r.StrokeThickness = thickness; }
        Add(c, r, x, y);
    }

    private static void Circle(Canvas c, double cx, double cy, double r,
                               string? fill, string? stroke = null, double thickness = 1, double opacity = 1)
        => Oval(c, cx, cy, r, r, fill, opacity, stroke, thickness);

    private static void Oval(Canvas c, double cx, double cy, double rx, double ry,
                             string? fill, double opacity = 1, string? stroke = null, double thickness = 1)
    {
        var e = new Ellipse { Width = rx * 2, Height = ry * 2, Opacity = opacity };
        if (fill is not null) e.Fill = B(fill);
        if (stroke is not null) { e.Stroke = B(stroke); e.StrokeThickness = thickness; }
        Add(c, e, cx - rx, cy - ry);
    }

    /// <summary>
    /// A stroked path in viewBox coordinates. Shapes default to <c>Stretch.None</c>, so
    /// the geometry draws where its numbers say it does — the same absolute space the
    /// SVG uses — as long as the path sits at the canvas origin.
    /// </summary>
    private static void Stroke(Canvas c, string data, string colour, double thickness,
                               PenLineCap cap = PenLineCap.Flat)
        => Add(c, new Path
        {
            Data = Geometry.Parse(data),
            Stroke = B(colour),
            StrokeThickness = thickness,
            StrokeLineCap = cap,
            StrokeJoin = PenLineJoin.Round,
        }, 0, 0);

    // ── text ─────────────────────────────────────────────────────────────────

    private static TextBlock Label(string text, double size, string colour, bool mono, bool bold)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = size,
            Foreground = B(colour),
            FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
        };
        if (mono) tb.Classes.Add("mono");   // the class carries the family; the size is local
        return tb;
    }

    /// <summary>SVG places text on its baseline; Avalonia places it by its top edge.</summary>
    private static double Baseline(double baseline, double size, bool mono)
        => baseline - size * (mono ? 0.95 : 1.0);

    private static void Text(Canvas c, string text, double x, double baseline, double size, string colour,
                             bool mono = true, bool bold = false, TextAnchor anchor = TextAnchor.Start)
    {
        var tb = Label(text, size, colour, mono, bold);
        double top = Baseline(baseline, size, mono);

        if (anchor == TextAnchor.Start)
        {
            Add(c, tb, x, top);
            return;
        }

        // text-anchor without measuring: align inside a fixed box and shift the box.
        tb.Width = AnchorBox;
        tb.TextAlignment = anchor == TextAnchor.End ? TextAlignment.Right : TextAlignment.Center;
        Add(c, tb, anchor == TextAnchor.End ? x - AnchorBox : x - AnchorBox / 2, top);
    }

    /// <summary>A label that re-reads itself when the language changes.</summary>
    private static TextBlock Bound(string key, double size, string colour)
    {
        var tb = Label(string.Empty, size, colour, mono: true, bold: false);
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }

    private static void BoundText(Canvas c, string key, double x, double baseline, double size, string colour)
        => Add(c, Bound(key, size, colour), x, Baseline(baseline, size, mono: true));
}
