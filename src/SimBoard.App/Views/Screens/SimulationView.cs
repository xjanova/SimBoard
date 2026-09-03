using System.Text;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using SimBoard.App.Controls;
using SimBoard.App.Localization;
using Path = Avalonia.Controls.Shapes.Path;

namespace SimBoard.App.Views.Screens;

/// <summary>
/// Screen 5 — Simulation running. Spec: README.md section
/// "### 5 · Simulation running — `05-simulation-running.png`".
///
/// This is the *overlay* layer only: it carries the live current-flow dashes, the lit
/// LED, the node-voltage tags, the run HUD and the docked oscilloscope. The schematic
/// itself is unchanged from screen 2, so the caller stacks this on top of the schematic
/// scene rather than the two duplicating the same circuit. The background is
/// transparent and the scene is not hit-testable for exactly that reason.
///
/// Both layers live in the same 1100 × 700 scene coordinate space with the SVG's
/// `xMidYMid meet` fit, so overlay geometry lands on the wires it describes.
/// </summary>
public static class SimulationView
{
    // ── the scene coordinate space, straight from the mock's viewBox ──────────
    private const double SceneW = 1100;
    private const double SceneH = 700;

    // ── the scope display's own viewBox (preserveAspectRatio="none") ──────────
    private const double ScopeW = 720;
    private const double ScopeH = 140;
    private const int ScopeTile = 40;

    // ── workspace palette · theme-independent, never restyled ─────────────────
    private const string Current = "#e8b04a";   // selection / current
    private const string NetA = "#5fd0a8";      // net class A · scope CH1
    private const string NetB = "#6fd3e0";      // net class B
    private const string Meta = "#8fa8bd";      // label
    private const string TagBg = "#0d1418";     // HUD + node tag fill
    private const string ScopeBg = "#04160f";
    private const string ScopeGrid = "#1d4a3c";
    private const string ScopeLabel = "#3f7d68";

    /// <summary>Flow dash geometry: 7 on / 5 off at 2.4px, looping over a 24px period.</summary>
    private const double FlowThickness = 2.4;
    private const double DashOn = 7.0 / FlowThickness;
    private const double DashOff = 5.0 / FlowThickness;
    private const double DashTravel = -24.0 / FlowThickness;

    /// <summary>Rough top-of-line-box to baseline ratio, for placing SVG-positioned text.</summary>
    private const double Baseline = 0.8;

    /// <summary>
    /// Builds the screen. Caller places the returned control over the schematic scene.
    /// Pass <paramref name="animate"/> = false to honour prefers-reduced-motion: every
    /// element still renders, it simply holds still.
    /// </summary>
    public static Control Build(bool animate = true)
    {
        var root = new Panel { ClipToBounds = true };

        // ── the 1100 × 700 overlay scene, fitted xMidYMid meet ────────────────
        var sceneScale = new ScaleTransform(1, 1);
        var scene = new Canvas
        {
            Width = SceneW,
            Height = SceneH,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = RelativePoint.Center,
            RenderTransform = sceneScale,
            IsHitTestVisible = false,           // decoration: clicks belong to the schematic
        };
        FillScene(scene);

        root.SizeChanged += (_, e) =>
        {
            double k = Math.Min(e.NewSize.Width / SceneW, e.NewSize.Height / SceneH);
            if (double.IsNaN(k) || double.IsInfinity(k) || k <= 0) return;
            sceneScale.ScaleX = k;
            sceneScale.ScaleY = k;
        };

        root.Children.Add(scene);
        root.Children.Add(DockedScope());

        // Reduced motion keeps every overlay, and drops only the movement.
        if (animate)
        {
            root.Styles.Add(FlowStyle("flow-main", 0.8));
            root.Styles.Add(FlowStyle("flow-led", 0.5));
            root.Styles.Add(BlinkStyle("led-blink"));
        }

        return root;
    }

    // ── scene overlays ────────────────────────────────────────────────────────

    private static void FillScene(Canvas c)
    {
        // Current flow. Speed encodes magnitude, colour encodes net class: the LED
        // branch runs at .5s against the main loop's .8s because more current flows.
        c.Children.Add(Flow(
            "M80,90 L960,90 M640,90 L640,265 M382,90 L382,400 M660,90 L660,310 " +
            "M112,338 L112,140 M112,140 L200,140 M312,140 L360,140 M360,140 L360,90",
            Current, "#E6E8B04A", "flow-main"));

        c.Children.Add(Flow(
            "M450,355 L340,355 M340,355 L340,610",
            NetA, "#CC5FD0A8", "flow-led"));

        // Lit LED — a soft halo behind a hard blinking core.
        c.Children.Add(Dot(340, 533, 16, "#d94f3d", 0.28, null));
        c.Children.Add(Dot(340, 533, 9, "#ff7a5f", 0.7, "led-blink"));

        // Node voltage tags — box stroked in the net colour, value in 10px mono.
        AddTag(c, 368, 98, 52, Current);
        AddTag(c, 596, 240, 58, Current);
        AddTag(c, 596, 330, 58, NetA);
        AddTag(c, 286, 346, 52, NetA);
        AddTag(c, 768, 326, 58, NetB);

        AddText(c, 372, 110, "5.02 V", 10, Current);
        AddText(c, 600, 252, "5.00 V", 10, Current);
        AddText(c, 600, 342, "4.21 V", 10, NetA);
        AddText(c, 290, 358, "3.98 V", 10, NetA);
        AddText(c, 772, 338, "2.14 V", 10, NetB);

        // Run HUD, top-left: 196 × 52.
        AddBox(c, 24, 24, 196, 52, Current);
        AddText(c, 36, 44, "▶ RUN · TRAN 0-20ms", 11, Current);
        AddText(c, 36, 62, "t = 12.480 ms · f = 1.44 kHz", 10, Meta);
    }

    private static Path Flow(string data, string hex, string glowArgb, string cls)
    {
        return new Path
        {
            Data = Geometry.Parse(data),
            Stroke = Paint(hex),
            StrokeThickness = FlowThickness,
            StrokeDashArray = new AvaloniaList<double> { DashOn, DashOff },
            StrokeDashOffset = 0,
            // stands in for the mock's filter: drop-shadow(0 0 3px …)
            Effect = new DropShadowEffect
            {
                BlurRadius = 3,
                OffsetX = 0,
                OffsetY = 0,
                Color = Color.Parse(glowArgb),
            },
            Classes = { cls },
        };
    }

    private static Ellipse Dot(double cx, double cy, double r, string hex, double opacity, string? cls)
    {
        var e = new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Fill = Paint(hex),
            Opacity = opacity,
        };
        if (cls is not null) e.Classes.Add(cls);
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e, cy - r);
        return e;
    }

    private static void AddTag(Canvas c, double x, double y, double w, string stroke)
        => AddBox(c, x, y, w, 15, stroke);

    private static void AddBox(Canvas c, double x, double y, double w, double h, string stroke)
    {
        var r = new Rectangle
        {
            Width = w,
            Height = h,
            Fill = Paint(TagBg),
            Stroke = Paint(stroke),
            StrokeThickness = 1,
        };
        Canvas.SetLeft(r, x);
        Canvas.SetTop(r, y);
        c.Children.Add(r);
    }

    /// <summary>Places monospace text by its SVG baseline, the way the mock positions it.</summary>
    private static void AddText(Canvas c, double x, double baseline, string text, double size, string hex)
    {
        var tb = new TextBlock
        {
            Classes = { "mono" },
            Text = text,
            FontSize = size,
            Foreground = Paint(hex),
        };
        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, baseline - size * Baseline);
        c.Children.Add(tb);
    }

    // ── docked oscilloscope, 184px across the bottom of the workspace ─────────

    private static Control DockedScope()
    {
        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,4,170"),
            Margin = new Thickness(4),
        };

        var display = ScopeDisplay();
        Grid.SetColumn(display, 0);
        body.Children.Add(display);

        var rail = ScopeRail();
        Grid.SetColumn(rail, 2);
        body.Children.Add(rail);

        var shell = new DockPanel { LastChildFill = true };
        shell.Children.Add(ScopeCaption());   // already docked Top by its builder
        shell.Children.Add(body);

        return new Bevel
        {
            Classes = { "sunken", "face" },
            Height = 184,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Child = shell,
        };
    }

    private static Control ScopeCaption()
    {
        var title = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        title.Children.Add(Bound(Keys.IScope));
        title.Children.Add(new TextBlock { Text = " — CH1 OUT · CH2 THR" });

        var controls = new TextBlock
        {
            Text = "▭ ✕",
            FontSize = 9,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var row = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(controls, Dock.Right);
        row.Children.Add(controls);
        row.Children.Add(title);

        var cap = new Bevel
        {
            Classes = { "caption" },
            Height = 17,
            Padding = new Thickness(4, 0),
            Child = row,
        };
        DockPanel.SetDock(cap, Dock.Top);
        return cap;
    }

    private static Control ScopeDisplay()
    {
        var canvas = new Canvas
        {
            Width = ScopeW,
            Height = ScopeH,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransformOrigin = RelativePoint.TopLeft,
        };
        var scale = new ScaleTransform(1, 1);
        canvas.RenderTransform = scale;

        // Graticule — the mock's <pattern id="scopegrid" width=40 height=40>.
        canvas.Children.Add(new Path
        {
            Data = Graticule(),
            Stroke = Paint(ScopeGrid),
            StrokeThickness = 1,
        });

        // CH1 · OUT square wave, 2px.
        canvas.Children.Add(Trace(
            "M0,110 L0,30 L60,30 L60,110 L120,110 L120,30 L180,30 L180,110 L240,110 L240,30 " +
            "L300,30 L300,110 L360,110 L360,30 L420,30 L420,110 L480,110 L480,30 L540,30 " +
            "L540,110 L600,110 L600,30 L660,30 L660,110 L720,110",
            NetA, 2, "#FF5FD0A8"));

        // CH2 · THR charge/discharge ramp, 1.6px.
        canvas.Children.Add(Trace(
            "M0,100 L30,58 L60,44 L90,64 L120,100 L150,58 L180,44 L210,64 L240,100 L270,58 " +
            "L300,44 L330,64 L360,100 L390,58 L420,44 L450,64 L480,100 L510,58 L540,44 " +
            "L570,64 L600,100 L630,58 L660,44 L690,64 L720,100",
            Current, 1.6, "#FFE8B04A"));

        AddText(canvas, 6, 14, "2V/div", 9, ScopeLabel);
        AddText(canvas, 640, 14, "0.2ms/div", 9, ScopeLabel);

        var host = new Panel { ClipToBounds = true, Children = { canvas } };
        host.SizeChanged += (_, e) =>
        {
            if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;
            scale.ScaleX = e.NewSize.Width / ScopeW;
            scale.ScaleY = e.NewSize.Height / ScopeH;
        };

        return new Bevel
        {
            Classes = { "sunken" },
            Background = Paint(ScopeBg),
            Child = host,
        };
    }

    private static Path Trace(string data, string hex, double thickness, string glowArgb)
        => new()
        {
            Data = Geometry.Parse(data),
            Stroke = Paint(hex),
            StrokeThickness = thickness,
            StrokeJoin = PenLineJoin.Miter,
            Effect = new DropShadowEffect
            {
                BlurRadius = 3,
                OffsetX = 0,
                OffsetY = 0,
                Color = Color.Parse(glowArgb),
            },
        };

    /// <summary>The 40 × 40 graticule tile: right/bottom rules plus centre tick marks.</summary>
    private static Geometry Graticule()
    {
        var sb = new StringBuilder();
        for (int x = ScopeTile; x <= (int)ScopeW; x += ScopeTile)
            sb.Append("M").Append(x).Append(",0 V").Append((int)ScopeH).Append(' ');
        for (int y = ScopeTile; y <= (int)ScopeH; y += ScopeTile)
            sb.Append("M0,").Append(y).Append(" H").Append((int)ScopeW).Append(' ');

        for (int tx = 0; tx < (int)ScopeW; tx += ScopeTile)
        {
            for (int ty = 0; ty < (int)ScopeH; ty += ScopeTile)
            {
                sb.Append("M").Append(tx + 8).Append(',').Append(ty + 20).Append(" h2 ");
                sb.Append("M").Append(tx + 18).Append(',').Append(ty + 20).Append(" h2 ");
                sb.Append("M").Append(tx + 28).Append(',').Append(ty + 20).Append(" h2 ");
                sb.Append("M").Append(tx + 20).Append(',').Append(ty + 8).Append(" v2 ");
                sb.Append("M").Append(tx + 20).Append(',').Append(ty + 18).Append(" v2 ");
                sb.Append("M").Append(tx + 20).Append(',').Append(ty + 28).Append(" v2 ");
            }
        }

        return Geometry.Parse(sb.ToString());
    }

    private static Control ScopeRail()
    {
        var rows = new StackPanel();
        foreach (var (label, value, hex) in new[]
                 {
                     ("CH1 Vpp", "4.98 V", "#1c7a3e"),
                     ("CH2 Vpp", "1.72 V", "#8a6420"),
                     ("FREQ", "1.442 kHz", null),
                     ("DUTY", "63.2 %", null),
                     ("RISE", "142 ns", null),
                 })
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Height = 16 };

            var l = new TextBlock
            {
                Classes = { "mono" },
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (hex is not null) l.Foreground = Paint(hex);
            Grid.SetColumn(l, 0);

            var v = new TextBlock
            {
                Classes = { "mono" },
                Text = value,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(v, 1);

            row.Children.Add(l);
            row.Children.Add(v);
            rows.Children.Add(row);
        }

        var box = new Bevel
        {
            Classes = { "sunken" },
            Padding = new Thickness(5, 3),
            Child = rows,
        };

        var buttons = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,3,*"),
            Margin = new Thickness(0, 3, 0, 0),
        };
        var auto = ScopeButton(Keys.IAuto);
        Grid.SetColumn(auto, 0);
        var cursor = ScopeButton(Keys.ICursor);
        Grid.SetColumn(cursor, 2);
        buttons.Children.Add(auto);
        buttons.Children.Add(cursor);

        return new StackPanel { Children = { box, buttons } };
    }

    private static Button ScopeButton(string key) => new()
    {
        Height = 19,
        MinHeight = 0,
        FontSize = 10,
        Padding = new Thickness(2, 0),
        Content = Bound(key),
    };

    // ── animation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The mock's `@keyframes flow { to { stroke-dashoffset: -24 } }`. Avalonia's dash
    /// units are multiples of the stroke thickness, so 24 device px is -24 / 2.4.
    /// </summary>
    private static Style FlowStyle(string cls, double seconds)
    {
        var style = new Style(x => x.OfType<Path>().Class(cls));
        style.Animations.Add(new Animation
        {
            Duration = TimeSpan.FromSeconds(seconds),
            IterationCount = IterationCount.Infinite,
            Easing = new LinearEasing(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(Path.StrokeDashOffsetProperty, 0d) },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(Path.StrokeDashOffsetProperty, DashTravel) },
                },
            },
        });
        return style;
    }

    /// <summary>`animation: blink 1s steps(2) infinite` — a hard two-state flicker.</summary>
    private static Style BlinkStyle(string cls)
    {
        var style = new Style(x => x.OfType<Ellipse>().Class(cls));
        style.Animations.Add(new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            IterationCount = IterationCount.Infinite,
            Easing = new LinearEasing(),
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, 0.7) } },
                new KeyFrame { Cue = new Cue(0.49d), Setters = { new Setter(Visual.OpacityProperty, 0.7) } },
                new KeyFrame { Cue = new Cue(0.5d), Setters = { new Setter(Visual.OpacityProperty, 0.35) } },
                new KeyFrame { Cue = new Cue(0.99d), Setters = { new Setter(Visual.OpacityProperty, 0.35) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, 0.7) } },
            },
        });
        return style;
    }

    // ── shared pieces ─────────────────────────────────────────────────────────

    private static SolidColorBrush Paint(string hex) => new(Color.Parse(hex));

    private static TextBlock Bound(string key)
    {
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }
}
