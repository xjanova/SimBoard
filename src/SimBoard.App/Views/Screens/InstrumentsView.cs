using System.Text;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;
using Path = Avalonia.Controls.Shapes.Path;

namespace SimBoard.App.Views.Screens;

/// <summary>Screen 6 — Instruments. Spec: README.md section "6 · Instruments — `06-instruments.png`".</summary>
public static class InstrumentsView
{
    // Window widths are load-bearing: every inner column below is derived from them,
    // so the scope screen can be drawn at exact pixel coordinates rather than guessed.
    private const double ScopeWidth = 660;
    private const double DmmWidth = 296;
    private const double LogicWidth = 520;

    // The scope screen is authored in a 520x260 viewBox and painted, without preserving
    // aspect, into the 523x240 well the 660px window leaves for it — the prototype's
    // preserveAspectRatio="none". Every point goes through ScopeSx / ScopeSy.
    private const double ScopeW = 523, ScopeH = 240;
    private const double ScopeSx = ScopeW / 520.0, ScopeSy = ScopeH / 260.0;

    // Same trick for the logic analyzer: a 440x130 viewBox into a 348x130 well.
    private const double LogicW = 348, LogicH = 130;
    private const double LogicSx = LogicW / 440.0;

    /// <summary>The 16px workspace dot field, built once as a single path.</summary>
    private static readonly string DotField = BuildDotField();

    /// <summary>Builds the screen. Caller places the returned control.</summary>
    public static Control Build()
    {
        var canvas = new Canvas { ClipToBounds = true };

        var (scope, scopeGrip) = Oscilloscope();
        var (dmm, dmmGrip) = Multimeter();
        var (logic, logicGrip) = LogicAnalyzer();

        Canvas.SetLeft(scope, 14);
        Canvas.SetTop(scope, 12);
        Canvas.SetRight(dmm, 14);
        Canvas.SetTop(dmm, 12);
        Canvas.SetLeft(logic, 220);
        Canvas.SetBottom(logic, 14);

        scope.ZIndex = 1;
        dmm.ZIndex = 2;
        logic.ZIndex = 3;
        int top = 3;

        // Dragging by the caption is the whole point of this screen, so it is real
        // behaviour and not a hint: press captures the pointer, move rewrites
        // Canvas.Left/Top, release lets go. The counter keeps the grabbed window on top.
        void Draggable(Control window, Control grip)
        {
            var dragging = false;
            var offset = default(Vector);

            grip.Cursor = new Cursor(StandardCursorType.SizeAll);

            grip.PointerPressed += (_, e) =>
            {
                if (!e.GetCurrentPoint(grip).Properties.IsLeftButtonPressed) return;

                // Two of the three windows are laid out from the right/bottom edge.
                // Writing Left while Right is still set would pin both edges and squash
                // the window, so convert to Left/Top before the first move.
                var origin = window.Bounds.Position;
                Canvas.SetRight(window, double.NaN);
                Canvas.SetBottom(window, double.NaN);
                Canvas.SetLeft(window, origin.X);
                Canvas.SetTop(window, origin.Y);

                offset = e.GetPosition(canvas) - origin;
                dragging = true;
                window.ZIndex = ++top;
                e.Pointer.Capture(grip);
                e.Handled = true;
            };

            grip.PointerMoved += (_, e) =>
            {
                if (!dragging) return;
                var p = e.GetPosition(canvas);
                // Whole pixels only — a window on a half pixel smears its 1px bevel.
                Canvas.SetLeft(window, Math.Round(p.X - offset.X));
                Canvas.SetTop(window, Math.Round(p.Y - offset.Y));
            };

            grip.PointerReleased += (_, e) =>
            {
                dragging = false;
                e.Pointer.Capture(null);
            };

            grip.PointerCaptureLost += (_, _) => dragging = false;
        }

        Draggable(scope, scopeGrip);
        Draggable(dmm, dmmGrip);
        Draggable(logic, logicGrip);

        canvas.Children.Add(scope);
        canvas.Children.Add(dmm);
        canvas.Children.Add(logic);

        return new Panel
        {
            Background = Brush("#12161b"),
            ClipToBounds = true,
            Children = { Backdrop(), canvas },
        };
    }

    // ── backdrop ─────────────────────────────────────────────────────────────

    /// <summary>The schematic underneath, dimmed to .35 so the instruments read as modal.</summary>
    private static Control Backdrop()
    {
        var layer = new Canvas { Opacity = 0.35, ClipToBounds = true };

        layer.Children.Add(new Path
        {
            Data = Geometry.Parse(DotField),
            Fill = Brush("#2b3440"),
        });

        layer.Children.Add(new Path
        {
            Data = Geometry.Parse(
                "M80,90H960 M80,610H960 M450,230h140v200h-140z " +
                "M590,265H640 M640,265V90 M450,355H340 M340,355V610"),
            Stroke = Brush("#93a9bd"),
            StrokeThickness = 1.6,
        });

        return layer;
    }

    private static string BuildDotField()
    {
        var sb = new StringBuilder();
        for (int x = 0; x <= 1400; x += 16)
            for (int y = 0; y <= 1000; y += 16)
                sb.Append('M').Append(x - 1).Append(',').Append(y - 1).Append("h2v2h-2z");
        return sb.ToString();
    }

    // ── 1 · MSO-4CH oscilloscope, 660px ──────────────────────────────────────

    private static (Control Win, Control Grip) Oscilloscope()
    {
        var (bar, grip) = Caption(Keys.IScope, " · MSO-4CH", "DialogTitleBar", "TitleBarFg", minimise: true);

        var display = new Bevel
        {
            Classes = { "sunken" },
            Background = Brush("#04160f"),
            Child = ScopeScreen(),
        };
        Grid.SetColumn(display, 0);

        var rail = ScopeRail();
        Grid.SetColumn(rail, 2);

        var upper = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("525,5,118"),
            Margin = new Thickness(5),
            Children = { display, rail },
        };

        var strip = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,4,*,4,*,4,*,4,*"),
            Margin = new Thickness(5, 0, 5, 5),
        };
        var measurements = new (string Key, string Value)[]
        {
            ("CH1 Vpp", "4.98 V"),
            ("ความถี่", "1.442 kHz"),
            ("ดิวตี้", "63.2 %"),
            ("ขาขึ้น", "142 ns"),
            ("RMS", "3.14 V"),
        };
        for (int i = 0; i < measurements.Length; i++)
        {
            var cell = new Bevel
            {
                Classes = { "sunken" },
                Padding = new Thickness(4, 2),
                Child = new StackPanel
                {
                    Children =
                    {
                        Ui(measurements[i].Key, 8, "#555555"),
                        Mono(measurements[i].Value, 11),
                    },
                },
            };
            Grid.SetColumn(cell, i * 2);
            strip.Children.Add(cell);
        }

        var body = new StackPanel { Children = { upper, strip } };
        return (Frame(ScopeWidth, bar, body), grip);
    }

    /// <summary>The 118px right rail: four pressed knob readouts, then Auto / Single.</summary>
    private static Control ScopeRail()
    {
        var knobs = new StackPanel { Spacing = 4 };
        foreach (var (label, value) in new (string, string)[]
                 {
                     ("ฐานเวลา", "50 µs/div"),
                     ("CH1 · แนวตั้ง", "2 V/div"),
                     ("CH2 · แนวตั้ง", "1 V/div"),
                     ("ทริกเกอร์", "CH1 ↑ 2.5 V"),
                 })
        {
            var knob = new Bevel
            {
                Classes = { "raised" },
                Padding = new Thickness(4, 3),
                Child = new StackPanel
                {
                    Children = { Ui(label, 9, "#2a2a2a"), Mono(value, 11, bold: true) },
                },
            };
            knob.Bind(Bevel.BackgroundProperty, new DynamicResourceExtension("Press"));
            knobs.Children.Add(knob);
        }
        Grid.SetRow(knobs, 0);

        var single = SmallButton(Keys.ISingle);
        Grid.SetColumn(single, 2);
        var buttons = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,3,*"),
            Margin = new Thickness(0, 4, 0, 0),
            Children = { SmallButton(Keys.IAuto), single },
        };
        Grid.SetRow(buttons, 2);

        return new Grid
        {
            Width = 118,
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children = { knobs, buttons },
        };
    }

    private static Button SmallButton(string key) => new()
    {
        Height = 19,
        MinHeight = 0,
        Padding = new Thickness(0),
        Content = Bound(key, 9),
    };

    /// <summary>The 240px #04160f display: graticule, CH1, CH2, D0-D7 and the trigger cursor.</summary>
    private static Control ScopeScreen()
    {
        var screen = new Canvas { Width = ScopeW, Height = ScopeH, ClipToBounds = true };

        screen.Children.Add(new Path
        {
            Data = Geometry.Parse(Graticule()),
            Stroke = Brush("#1d4a3c"),
            StrokeThickness = 1,
        });

        double[] ch1 =
        [
            0, 200, 0, 60, 60, 60, 60, 200, 120, 200, 120, 60, 180, 60, 180, 200,
            240, 200, 240, 60, 300, 60, 300, 200, 360, 200, 360, 60, 420, 60, 420, 200,
            480, 200, 480, 60, 520, 60,
        ];
        double[] ch2 =
        [
            0, 170, 26, 120, 52, 104, 78, 126, 104, 170, 130, 120, 156, 104, 182, 126,
            208, 170, 234, 120, 260, 104, 286, 126, 312, 170, 338, 120, 364, 104, 390, 126,
            416, 170, 442, 120, 468, 104, 494, 126, 520, 170,
        ];
        double[] digital =
        [
            0, 232, 40, 232, 40, 222, 80, 222, 80, 232, 120, 232, 120, 222, 160, 222,
            160, 232, 200, 232, 200, 222, 240, 222, 240, 232, 280, 232, 280, 222, 320, 222,
            320, 232, 360, 232, 360, 222, 400, 222, 400, 232, 440, 232, 440, 222, 480, 222,
            480, 232, 520, 232,
        ];

        // The mock glows both analogue traces with a CSS drop-shadow. A wide, low-alpha
        // copy underneath is the same picture without pulling in an effect pipeline.
        screen.Children.Add(Trace(ch1, ScopeSx, ScopeSy, "#445fd0a8", 7.0));
        screen.Children.Add(Trace(ch2, ScopeSx, ScopeSy, "#44e8b04a", 6.6));
        screen.Children.Add(Trace(ch1, ScopeSx, ScopeSy, "#5fd0a8", 2.2));
        screen.Children.Add(Trace(ch2, ScopeSx, ScopeSy, "#e8b04a", 1.8));
        screen.Children.Add(Trace(digital, ScopeSx, ScopeSy, "#6fd3e0", 1.6));

        screen.Children.Add(new Line
        {
            StartPoint = new Point(300 * ScopeSx, 0),
            EndPoint = new Point(300 * ScopeSx, ScopeH),
            Stroke = Brush("#c9a227"),
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double> { 4, 4 },
        });

        ScopeLabel(screen, "CH1 2V/div", 6, 14, "#3f7d68");
        ScopeLabel(screen, "CH2 1V/div", 86, 14, "#8a6420");
        ScopeLabel(screen, "D0-D7", 170, 14, "#2c6f78");
        ScopeLabel(screen, "50µs/div", 446, 14, "#3f7d68");
        ScopeLabel(screen, "T", 304, 252, "#c9a227");

        return screen;
    }

    /// <summary>The 40x40 graticule with its centre tick marks, flattened to one path.</summary>
    private static string Graticule()
    {
        var sb = new StringBuilder();

        for (int k = 1; k * 40 <= 520; k++)
            sb.Append('M').Append(Sx(k * 40)).Append(",0V").Append((int)ScopeH);
        for (int k = 1; k * 40 <= 260; k++)
            sb.Append("M0,").Append(Sy(k * 40)).Append('H').Append((int)ScopeW);

        for (int ox = 0; ox <= 520; ox += 40)
            for (int oy = 0; oy <= 260; oy += 40)
            {
                foreach (int tx in new[] { 8, 18, 28 })
                    sb.Append('M').Append(Sx(ox + tx)).Append(',').Append(Sy(oy + 20)).Append("h2");
                foreach (int ty in new[] { 8, 18, 28 })
                    sb.Append('M').Append(Sx(ox + 20)).Append(',').Append(Sy(oy + ty)).Append("v2");
            }

        return sb.ToString();
    }

    private static int Sx(double v) => (int)Math.Round(v * ScopeSx);

    private static int Sy(double v) => (int)Math.Round(v * ScopeSy);

    private static void ScopeLabel(Canvas host, string text, double x, double y, string hex)
    {
        var tb = Mono(text, 9, hex);
        // SVG places text on its baseline; a 9px face sits roughly 9px below its own top.
        Canvas.SetLeft(tb, x * ScopeSx);
        Canvas.SetTop(tb, y * ScopeSy - 9);
        host.Children.Add(tb);
    }

    // ── 2 · 6½-digit multimeter, 296px ───────────────────────────────────────

    private static (Control Win, Control Grip) Multimeter()
    {
        var (bar, grip) = Caption(Keys.IDmm, " · 6½", "DialogTitleBar", "TitleBarFg", minimise: false);

        var target = Mono("TP1 → GND", 9, "#2f7a55");
        Grid.SetColumn(target, 1);
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { Mono("DC V · AUTO", 9, "#2f7a55"), target },
        };

        var primary = Mono("3.9812", 38, "#48e08a", bold: true);
        primary.HorizontalAlignment = HorizontalAlignment.Right;
        primary.TextAlignment = TextAlignment.Right;
        primary.LineHeight = 42;
        primary.LetterSpacing = 0.76;

        var max = Mono("MAX 4.9903", 10, "#2f7a55");
        Grid.SetColumn(max, 2);
        var unit = Mono("V", 10, "#2f7a55");
        Grid.SetColumn(unit, 4);
        var footer = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,*,Auto"),
            Children = { Mono("MIN 0.0021", 10, "#2f7a55"), max, unit },
        };

        var screen = new Bevel
        {
            Classes = { "sunken" },
            Background = Brush("#0a1a12"),
            Padding = new Thickness(10, 8),
            Child = new StackPanel { Children = { header, primary, footer } },
        };

        var modes = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,3,*,3,*,3,*"),
            RowDefinitions = new RowDefinitions("20,3,20"),
        };
        string[] functions = ["V⎓", "V∼", "A⎓", "A∼", "Ω", "⊣C", "▶|", "Hz"];
        for (int i = 0; i < functions.Length; i++)
        {
            var b = new Button
            {
                Height = 20,
                MinHeight = 0,
                Padding = new Thickness(0),
                Content = Mono(functions[i], 10, bold: i == 0),
            };
            if (i == 0) b.Classes.Add("latched");   // DC volts is the selected function
            Grid.SetColumn(b, i % 4 * 2);
            Grid.SetRow(b, i / 4 * 2);
            modes.Children.Add(b);
        }

        var readout = new Bevel
        {
            Classes = { "sunken" },
            Padding = new Thickness(5, 3),
            Child = new StackPanel
            {
                Children =
                {
                    KeyValue("RANGE", "10 V", 10, 17),
                    KeyValue("RATE", "10 rdg/s", 10, 17),
                    KeyValue("REL", "OFF", 10, 17),
                },
            },
        };

        var body = new StackPanel
        {
            Margin = new Thickness(6),
            Spacing = 6,
            Children = { screen, modes, readout },
        };
        return (Frame(DmmWidth, bar, body), grip);
    }

    // ── 3 · 8-channel logic analyzer, 520px ──────────────────────────────────

    private static (Control Win, Control Grip) LogicAnalyzer()
    {
        var (bar, grip) = Caption(Keys.ILogic, " · 8CH", "PanelCaption", "PanelCaptionFg", minimise: false);

        var legend = new StackPanel { Width = 44, Spacing = 2 };
        foreach (var channel in new[] { "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7" })
        {
            var cell = new Bevel
            {
                Height = 14,
                Padding = new Thickness(3, 0, 0, 0),
                Child = Mono(channel, 9),
            };
            cell.Bind(Bevel.BackgroundProperty, new DynamicResourceExtension("Press"));
            cell.Bind(Bevel.TopBrushProperty, new DynamicResourceExtension("Lite"));
            cell.Bind(Bevel.LeftBrushProperty, new DynamicResourceExtension("Lite"));
            cell.Bind(Bevel.RightBrushProperty, new DynamicResourceExtension("Shad"));
            cell.Bind(Bevel.BottomBrushProperty, new DynamicResourceExtension("Shad"));
            legend.Children.Add(cell);
        }
        Grid.SetColumn(legend, 0);

        var display = new Bevel
        {
            Classes = { "sunken" },
            Background = Brush("#04160f"),
            Child = LogicScreen(),
        };
        Grid.SetColumn(display, 2);

        var info = new Bevel
        {
            Classes = { "sunken" },
            Width = 104,
            Padding = new Thickness(5, 3),
            Child = new StackPanel
            {
                Children =
                {
                    KeyValue("SR", "1 MS/s", 9, 16),
                    KeyValue("DEPTH", "1 M", 9, 16),
                    KeyValue("PROTO", "I²C", 9, 16),
                    KeyValue("ADDR", "0x3C", 9, 16),
                    KeyValue("TRIG", "START", 9, 16),
                },
            },
        };
        Grid.SetColumn(info, 4);

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("44,5,350,5,104"),
            Margin = new Thickness(5),
            Children = { legend, display, info },
        };
        return (Frame(LogicWidth, bar, body), grip);
    }

    private static Control LogicScreen()
    {
        var screen = new Canvas { Width = LogicW, Height = LogicH, ClipToBounds = true };

        double[][] channels =
        [
            [0, 12, 40, 12, 40, 4, 80, 4, 80, 12, 160, 12, 160, 4, 200, 4, 200, 12, 280, 12, 280, 4, 320, 4, 320, 12, 400, 12, 400, 4, 440, 4],
            [0, 28, 20, 28, 20, 20, 60, 20, 60, 28, 100, 28, 100, 20, 140, 20, 140, 28, 220, 28, 220, 20, 260, 20, 260, 28, 340, 28, 340, 20, 380, 20, 380, 28, 440, 28],
            [0, 44, 60, 44, 60, 36, 140, 36, 140, 44, 200, 44, 200, 36, 280, 36, 280, 44, 360, 44, 360, 36, 440, 36],
            [0, 60, 100, 60, 100, 52, 180, 52, 180, 60, 300, 60, 300, 52, 400, 52, 400, 60, 440, 60],
            [0, 76, 30, 76, 30, 68, 70, 68, 70, 76, 130, 76, 130, 68, 170, 68, 170, 76, 250, 76, 250, 68, 300, 68, 300, 76, 440, 76],
            [0, 92, 0, 84, 50, 84, 50, 92, 90, 92, 90, 84, 190, 84, 190, 92, 240, 92, 240, 84, 330, 84, 330, 92, 440, 92],
            [0, 108, 80, 108, 80, 100, 160, 100, 160, 108, 240, 108, 240, 100, 320, 100, 320, 108, 440, 108],
            [0, 124, 110, 124, 110, 116, 220, 116, 220, 124, 330, 124, 330, 116, 440, 116],
        ];
        foreach (var channel in channels)
            screen.Children.Add(Trace(channel, LogicSx, 1, "#48e08a", 1.4));

        screen.Children.Add(new Line
        {
            StartPoint = new Point(150 * LogicSx, 0),
            EndPoint = new Point(150 * LogicSx, LogicH),
            Stroke = Brush("#c9a227"),
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double> { 3, 3 },
        });

        return screen;
    }

    // ── shared window chrome ─────────────────────────────────────────────────

    /// <summary>
    /// A floating instrument: raised frame, theme radius, and the 5px offset drop shadow
    /// that separates it from the dimmed schematic behind.
    /// </summary>
    private static Control Frame(double width, Control caption, Control body)
    {
        var frame = new Bevel
        {
            Classes = { "raised" },
            Width = width,
            Child = new StackPanel { Children = { caption, body } },
        };

        var shell = new Border
        {
            BoxShadow = BoxShadows.Parse("5 5 12 0 #73000000"),
            Child = frame,
        };
        // The shadow needs something opaque to fall from; the frame face is what shows.
        shell.Bind(Border.BackgroundProperty, new DynamicResourceExtension("Face"));
        return shell;
    }

    /// <summary>
    /// The 19px caption. Returned twice: once as the bar, once as the drag grip, because
    /// the caption <em>is</em> the handle on this screen.
    /// </summary>
    private static (Control Bar, Control Grip) Caption(
        string titleKey, string suffix, string backgroundKey, string foregroundKey, bool minimise)
    {
        var name = Bound(titleKey, 11);
        name.FontWeight = FontWeight.Bold;
        name.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension(foregroundKey));

        var tail = new TextBlock
        {
            Text = suffix,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        tail.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension(foregroundKey));

        var title = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { name, tail },
        };

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (minimise) controls.Children.Add(CaptionButton("_", "MinBg", "Min", "WindowButtonFg", 8, true));
        controls.Children.Add(CaptionButton("✕", "CloseBg", "Close", "CloseFg", 9, false));
        Grid.SetColumn(controls, 1);

        var bar = new Bevel
        {
            EdgeThickness = 0,
            Height = 19,
            Padding = new Thickness(5, 0, 3, 0),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children = { title, controls },
            },
        };
        bar.Bind(Bevel.BackgroundProperty, new DynamicResourceExtension(backgroundKey));
        return (bar, bar);
    }

    /// <summary>A 15x13 window button — geometry and colour both come from the theme.</summary>
    private static Control CaptionButton(
        string glyph, string backgroundKey, string edge, string foregroundKey, double size, bool bottomAligned)
    {
        var tb = new TextBlock
        {
            Text = glyph,
            FontSize = size,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = bottomAligned ? VerticalAlignment.Bottom : VerticalAlignment.Center,
            Margin = bottomAligned ? new Thickness(0, 0, 0, 2) : default,
        };
        tb.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension(foregroundKey));

        var box = new Bevel
        {
            Width = 15,
            Height = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Child = tb,
        };
        box.Bind(Bevel.BackgroundProperty, new DynamicResourceExtension(backgroundKey));
        box.Bind(Bevel.TopBrushProperty, new DynamicResourceExtension(edge + "BorderTop"));
        box.Bind(Bevel.RightBrushProperty, new DynamicResourceExtension(edge + "BorderRight"));
        box.Bind(Bevel.BottomBrushProperty, new DynamicResourceExtension(edge + "BorderBottom"));
        box.Bind(Bevel.LeftBrushProperty, new DynamicResourceExtension(edge + "BorderLeft"));
        box.Bind(Bevel.CornerRadiusProperty, new DynamicResourceExtension("WindowButtonRadius"));
        return box;
    }

    // ── small pieces ─────────────────────────────────────────────────────────

    /// <summary>A label / value row with the value pushed to the right edge.</summary>
    private static Control KeyValue(string key, string value, double size, double height)
    {
        var v = Mono(value, size);
        Grid.SetColumn(v, 1);
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Height = height,
            Children = { Mono(key, size), v },
        };
    }

    private static Polyline Trace(double[] xy, double sx, double sy, string hex, double thickness)
    {
        var points = new List<Point>(xy.Length / 2);
        for (int i = 0; i < xy.Length; i += 2)
            points.Add(new Point(xy[i] * sx, xy[i + 1] * sy));

        return new Polyline
        {
            Points = points,
            Stroke = Brush(hex),
            StrokeThickness = thickness,
        };
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));

    private static TextBlock Ui(string text, double size, string? hex = null, bool bold = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = size,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (hex is not null) tb.Foreground = Brush(hex);
        if (bold) tb.FontWeight = FontWeight.Bold;
        return tb;
    }

    private static TextBlock Mono(string text, double size, string? hex = null, bool bold = false)
    {
        var tb = new TextBlock
        {
            Classes = { size <= 9 ? "dense" : "mono" },
            Text = text,
            FontSize = size,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (hex is not null) tb.Foreground = Brush(hex);
        if (bold) tb.FontWeight = FontWeight.Bold;
        return tb;
    }

    private static TextBlock Bound(string key, double size)
    {
        var tb = new TextBlock { FontSize = size, VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }
}
