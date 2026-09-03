using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;
using Path = Avalonia.Controls.Shapes.Path;

namespace SimBoard.App.Views.Screens;

/// <summary>Screen 8 — PCB + AI auto-place &amp; route. Spec: README.md section
/// "8 · PCB + AI auto-place &amp; route — `08-pcb-ai-autoplace.png`".</summary>
public static class PcbView
{
    // ── the board artwork, in the mock's own 840 × 700 user space ─────────
    private const double ArtWidth = 840;
    private const double ArtHeight = 700;

    /// <summary>Measured values never share a face with prose — spec's core type rule.</summary>
    private static readonly FontFamily Mono = new("Lucida Console, Consolas, monospace");

    /// <summary>Copper traces on the top layer, straight from the prototype's path data.</summary>
    private static readonly string[] TopTraces =
    [
        "M120 110H700",
        "M120 570H700",
        "M300 200H210v120",
        "M420 200h140v90",
        "M300 320h60v120h180",
        "M540 380h120v100",
        "M210 440h70v90h180",
    ];

    private static readonly string[] BottomTraces = ["M360 110v56", "M660 200v-40", "M280 530v40"];

    /// <summary>Via centres — copper annulus r=9 over a r=3.4 hole.</summary>
    private static readonly (double X, double Y)[] Vias = [(360, 166), (660, 160), (280, 570)];

    /// <summary>Silkscreen outlines: package bodies, 1.6px, #e6e6e0.</summary>
    private static readonly (double X, double Y, double W, double H)[] SilkBoxes =
    [
        (300, 200, 120, 130),
        (180, 410, 130, 46),
        (520, 270, 80, 40),
        (100, 140, 46, 70),
    ];

    /// <summary>Reference designators, placed on their SVG baseline and centred.</summary>
    private static readonly (double X, double Y, string Text)[] Designators =
    [
        (360, 272, "U2"), (245, 438, "U1"), (560, 295, "J2"), (640, 485, "D2"), (123, 180, "J1"),
    ];

    /// <summary>Pads of the selected footprint, 14 × 16 copper.</summary>
    private static readonly (double X, double Y)[] SelectedPads =
    [
        (316, 192), (352, 192), (388, 192), (404, 192),
        (316, 322), (352, 322), (388, 322), (404, 322),
    ];

    /// <summary>The 8px corner handles on the selection box.</summary>
    private static readonly (double X, double Y)[] Handles =
        [(286, 186), (424, 186), (286, 334), (424, 334)];

    /// <summary>Unrouted nets, drawn as the dashed cyan ratsnest.</summary>
    private static readonly string[] Ratsnest = ["M420 265h100v25", "M310 265H210v175h70"];

    private static readonly string[] LayerChipLabels = ["TOP CU", "BOT CU", "SILK", "MASK", "DRILL", "OUTLINE"];

    /// <summary>Placement goals — 1, 2, 3 and 5 checked, exactly as the mock ships them.</summary>
    private static readonly (string Label, bool On)[] Goals =
    [
        ("ลายสั้นที่สุด", true),
        ("กระจายความร้อน (U1, Q1)", true),
        ("เว้นระยะไฟแรงสูง 2.0 mm", true),
        ("จัดกลุ่มตามบล็อกวงจร", false),
        ("ลดจำนวนเวีย", true),
    ];

    private static readonly string[] LogLines =
    [
        "· PLACEMENT PASS 3/4 · 22 PARTS",
        "· RATSNEST LEN 1,842 → 1,106 mm",
        "· THERMAL: U1 MOVED TO EDGE",
        "· ROUTED 38 / 44 NETS",
    ];

    private static readonly (string Check, string Value)[] Drc =
    [
        ("ระยะลาย-ลาย", "PASS"),
        ("ระยะลาย-แพด", "PASS"),
        ("ขนาดรูเจาะ", "PASS"),
        ("ลายกว้างพอรับกระแส", "PASS"),
        ("ระยะขอบแผ่น 0.5 mm", "PASS"),
        ("ซิลค์ทับแพด", "PASS"),
        ("เนตที่ยังไม่เดิน", "6"),
    ];

    /// <summary>Builds the screen. Caller places the returned control.</summary>
    public static Control Build()
    {
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("*,314") };

        var canvas = BoardCanvas();
        Grid.SetColumn(canvas, 0);
        root.Children.Add(canvas);

        var ai = AiPanel();
        Grid.SetColumn(ai, 1);
        root.Children.Add(ai);

        return root;
    }

    // ── board canvas ──────────────────────────────────────────────────────

    /// <summary>
    /// The dark PCB well with the artwork drawn at the mock's own coordinates. The board
    /// is a fixed 700 × 560 because the spec gives it in millimetre-true pixels — scaling
    /// it to the pane would make every stated dimension a lie.
    /// </summary>
    private static Control BoardCanvas()
    {
        var art = new Canvas
        {
            Width = ArtWidth,
            Height = ArtHeight,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        // 16px dot grid over the whole well
        art.Children.Add(new Path { Data = Geometry.Parse(DotGrid()), Fill = Brush("#2b3440") });

        // substrate
        art.Children.Add(At(new Rectangle
        {
            Width = 700,
            Height = 560,
            RadiusX = 6,
            RadiusY = 6,
            Fill = Brush("#12311f"),
            Stroke = Brush("#2f6b47"),
            StrokeThickness = 2,
        }, 70, 60));

        // pad field — the <pattern id="pads"> tile, at .22
        var (padCopper, padHoles) = PadField();
        art.Children.Add(new Path { Data = Geometry.Parse(padCopper), Fill = Brush("#c69a5c"), Opacity = .22 });
        art.Children.Add(new Path { Data = Geometry.Parse(padHoles), Fill = Brush("#101215"), Opacity = .22 });

        foreach (var d in TopTraces)
            art.Children.Add(new Path
            {
                Data = Geometry.Parse(d),
                Stroke = Brush("#c98b4b"),
                StrokeThickness = 7,
                StrokeLineCap = PenLineCap.Round,
                StrokeJoin = PenLineJoin.Round,
                Opacity = .95,
            });

        foreach (var d in BottomTraces)
            art.Children.Add(new Path
            {
                Data = Geometry.Parse(d),
                Stroke = Brush("#5f7fb0"),
                StrokeThickness = 5,
                StrokeLineCap = PenLineCap.Round,
                Opacity = .8,
            });

        foreach (var (x, y) in Vias)
        {
            art.Children.Add(At(new Ellipse { Width = 18, Height = 18, Fill = Brush("#c98b4b") }, x - 9, y - 9));
            art.Children.Add(At(new Ellipse { Width = 6.8, Height = 6.8, Fill = Brush("#0e1013") }, x - 3.4, y - 3.4));
        }

        foreach (var (x, y, w, h) in SilkBoxes)
            art.Children.Add(At(new Rectangle
            {
                Width = w,
                Height = h,
                RadiusX = 3,
                RadiusY = 3,
                Stroke = Brush("#e6e6e0"),
                StrokeThickness = 1.6,
                Opacity = .85,
            }, x, y));

        // U2's pin-1 notch and D2's body
        art.Children.Add(new Path
        {
            Data = Geometry.Parse("M348 200a12 12 0 0 0 24 0"),
            Stroke = Brush("#e6e6e0"),
            StrokeThickness = 1.6,
            Opacity = .85,
        });
        art.Children.Add(At(new Ellipse
        {
            Width = 36,
            Height = 36,
            Stroke = Brush("#e6e6e0"),
            StrokeThickness = 1.6,
            Opacity = .85,
        }, 622, 462));

        foreach (var (x, y, text) in Designators)
            art.Children.Add(MonoText(text, 12, "#f0efe9", x, y, .9, centred: true));

        foreach (var (x, y) in SelectedPads)
            art.Children.Add(At(new Rectangle { Width = 14, Height = 16, Fill = Brush("#c98b4b") }, x, y));

        // selection: dashed amber box + 8px corner handles
        art.Children.Add(At(new Rectangle
        {
            Width = 136,
            Height = 146,
            Stroke = Brush("#e8b04a"),
            StrokeThickness = 1.4,
            StrokeDashArray = Dashes(1.4, 4, 3),
        }, 292, 192));

        foreach (var (x, y) in Handles)
            art.Children.Add(At(new Rectangle { Width = 8, Height = 8, Fill = Brush("#e8b04a") }, x, y));

        foreach (var d in Ratsnest)
            art.Children.Add(new Path
            {
                Data = Geometry.Parse(d),
                Stroke = Brush("#6fd3e0"),
                StrokeThickness = 1.4,
                StrokeDashArray = Dashes(1.4, 6, 4),
                Opacity = .75,
            });

        art.Children.Add(MonoText("PCB-100X80 · 2 LAYER · 1.6mm FR-4 · 35µm Cu", 10, "#6f8ba1", 76, 646));
        art.Children.Add(MonoText("DRC 0 / 0", 10, "#6f8ba1", 600, 646));

        // the board outline, repeated as a dashed keep-out
        art.Children.Add(At(new Rectangle
        {
            Width = 700,
            Height = 560,
            RadiusX = 6,
            RadiusY = 6,
            Stroke = Brush("#8fd0a8"),
            StrokeThickness = 1,
            StrokeDashArray = Dashes(1, 8, 5),
            Opacity = .5,
        }, 70, 60));

        var well = new Panel { Background = Brush("#0e1013"), ClipToBounds = true };
        well.Children.Add(art);
        well.Children.Add(LayerChips());
        return well;
    }

    /// <summary>The six layer chips overlaid top-left; clicking one makes it the active layer.</summary>
    private static Control LayerChips()
    {
        var strip = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            Margin = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var chips = new List<Border>();
        var labels = new List<TextBlock>();

        void Activate(int active)
        {
            for (int i = 0; i < chips.Count; i++)
            {
                bool on = i == active;
                chips[i].Background = Brush(on ? "#e8b04a" : "#1b2027");
                chips[i].BorderBrush = Brush(on ? "#8a6420" : "#3a4652");
                labels[i].Foreground = Brush(on ? "#2b1f06" : "#8fa8bd");
            }
        }

        for (int i = 0; i < LayerChipLabels.Length; i++)
        {
            int index = i;
            var text = new TextBlock { Classes = { "dense" }, Text = LayerChipLabels[i] };
            var chip = new Border
            {
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 3),
                Child = text,
            };
            chip.PointerPressed += (_, _) => Activate(index);
            chips.Add(chip);
            labels.Add(text);
            strip.Children.Add(chip);
        }

        Activate(0);   // TOP CU, as in the mock
        return strip;
    }

    // ── AI panel ──────────────────────────────────────────────────────────

    /// <summary>The 314px auto-place panel. Violet is reserved for AI and appears nowhere else.</summary>
    private static Control AiPanel()
    {
        var body = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*,Auto,Auto") };

        AddRow(body, 0, AiCaption());
        AddRow(body, 1, Description());
        AddRow(body, 2, GoalsBox());
        AddRow(body, 3, ProgressBox());
        AddRow(body, 4, DrcTable());
        AddRow(body, 5, PrimaryButtons());
        AddRow(body, 6, ExportButtons(), last: true);

        var divider = new Bevel { Classes = { "vdivider" } };
        DockPanel.SetDock(divider, Dock.Left);

        return new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                divider,
                new Bevel { Classes = { "flat" }, Padding = new Thickness(6), Child = body },
            },
        };
    }

    private static void AddRow(Grid host, int row, Control child, bool last = false)
    {
        if (!last) child.Margin = new Thickness(0, 0, 0, 5);
        Grid.SetRow(child, row);
        host.Children.Add(child);
    }

    private static Control AiCaption()
    {
        var star = new TextBlock
        {
            Text = "✦",
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var title = Bound(Keys.PcbAi);
        title.FontSize = 10;
        title.FontWeight = FontWeight.Bold;
        title.Foreground = Brushes.White;

        return new Border
        {
            Height = 18,
            Padding = new Thickness(5, 0),
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#7a5fa8"), 0),
                    new GradientStop(Color.Parse("#b79ad4"), 1),
                },
            },
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { star, title },
            },
        };
    }

    private static Control Description()
    {
        var tb = Bound(Keys.PcbAiDesc);
        tb.FontSize = 10;
        tb.LineHeight = 15.5;                       // 10px × 1.55
        tb.TextWrapping = TextWrapping.Wrap;
        tb.Foreground = Brush("#2a2a2a");
        tb.Margin = new Thickness(2, 0);
        tb.VerticalAlignment = VerticalAlignment.Top;
        return tb;
    }

    private static Control GoalsBox()
    {
        var stack = new StackPanel { Spacing = 5 };

        var head = Bound(Keys.PcbGoal);
        head.FontSize = 10;
        head.FontWeight = FontWeight.Bold;
        stack.Children.Add(head);

        foreach (var (label, on) in Goals)
            stack.Children.Add(new CheckBox
            {
                IsChecked = on,
                Content = new TextBlock { Text = label, FontSize = 10 },
            });

        var trace = FieldRow(Keys.PcbTrace, "0.35 mm");
        trace.Margin = new Thickness(0, 2, 0, 0);
        stack.Children.Add(trace);
        stack.Children.Add(FieldRow(Keys.PcbVia, "0.8 / 0.4"));

        return new Bevel { Classes = { "sunken", "face" }, Padding = new Thickness(6), Child = stack };
    }

    /// <summary>Label + the mock's compact combo: a white sunken field with a 14 × 16 ▼ stub.</summary>
    private static Control FieldRow(string labelKey, string value)
    {
        var label = Bound(labelKey);
        label.FontSize = 10;
        Grid.SetColumn(label, 0);

        var field = new Bevel
        {
            Classes = { "sunken" },
            Height = 18,
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock
                    {
                        Classes = { "mono" },
                        Text = value,
                        Margin = new Thickness(5, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new Bevel
                    {
                        Classes = { "raised" },
                        Width = 14,
                        Height = 16,
                        Child = new TextBlock
                        {
                            Text = "▼",
                            FontSize = 6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        },
                    },
                },
            },
        };
        Grid.SetColumn(field, 1);

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        row.Children.Add(label);
        row.Children.Add(field);
        return row;
    }

    private static Control ProgressBox()
    {
        var caption = Bound(Keys.PcbPlacing);
        caption.FontSize = 10;
        caption.FontWeight = FontWeight.Bold;
        Grid.SetColumn(caption, 0);

        var percent = new TextBlock { Classes = { "mono" }, Text = "68%" };
        Grid.SetColumn(percent, 1);

        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        head.Children.Add(caption);
        head.Children.Add(percent);

        var logs = new StackPanel();
        foreach (var line in LogLines)
            logs.Children.Add(new TextBlock
            {
                Classes = { "dense" },
                Text = line,
                LineHeight = 14.4,                  // 9px × 1.6
                Foreground = Brush("#333333"),
            });

        return new Bevel
        {
            Classes = { "sunken", "face" },
            Padding = new Thickness(6),
            Child = new StackPanel { Spacing = 4, Children = { head, ProgressStrip(), logs } },
        };
    }

    /// <summary>
    /// A segmented 22-cell strip at 68 % — 15 filled. Period-correct: a smooth bar is the
    /// one thing this era of chrome never had.
    /// </summary>
    private static Control ProgressStrip()
    {
        var cells = new Grid();
        for (int i = 0; i < 22; i++) cells.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        for (int i = 0; i < 22; i++)
        {
            var cell = new Rectangle { Margin = new Thickness(0, 0, i == 21 ? 0 : 1, 0) };
            if (i < 15) BindTheme(cell, Shape.FillProperty, "Selection");
            else cell.Fill = Brush("#e6e4e0");
            Grid.SetColumn(cell, i);
            cells.Children.Add(cell);
        }

        return new Bevel { Classes = { "sunken" }, Height = 14, Padding = new Thickness(1), Child = cells };
    }

    private static Control DrcTable()
    {
        var check = Bound(Keys.PcbCheck);
        check.FontSize = 9;
        check.Margin = new Thickness(5, 2);
        Grid.SetColumn(check, 0);

        var divider = new Bevel { Classes = { "vdivider" } };
        Grid.SetColumn(divider, 1);

        var result = Bound(Keys.PcbVal);
        result.FontSize = 9;
        result.Margin = new Thickness(5, 2);
        Grid.SetColumn(result, 2);

        var headRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,2,54") };
        headRow.Children.Add(check);
        headRow.Children.Add(divider);
        headRow.Children.Add(result);

        var rule = new Rectangle { Height = 1 };
        BindTheme(rule, Shape.FillProperty, "Shad");

        var table = new StackPanel();
        table.Children.Add(new Bevel { Classes = { "flat" }, Child = headRow });
        table.Children.Add(rule);

        foreach (var (key, value) in Drc)
        {
            var k = new TextBlock
            {
                Classes = { "dense" },
                Text = key,
                LineHeight = 15.75,                 // 9px × 1.75
                Foreground = Brush("#2a2a2a"),
            };
            Grid.SetColumn(k, 0);

            var v = new TextBlock
            {
                Classes = { "dense" },
                Text = value,
                LineHeight = 15.75,
                Foreground = Brush("#1c7a3e"),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(v, 1);

            var cells = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(5, 0) };
            cells.Children.Add(k);
            cells.Children.Add(v);

            table.Children.Add(new Border
            {
                BorderBrush = Brush("#f0eeeb"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = cells,
            });
        }

        return new Bevel { Classes = { "sunken" }, ClipToBounds = true, Child = table };
    }

    private static Control PrimaryButtons()
    {
        var label = Bound(Keys.PcbAuto);
        var auto = new Button
        {
            Classes = { "default" },
            Height = 24,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children = { new TextBlock { Text = "✦", VerticalAlignment = VerticalAlignment.Center }, label },
            },
        };
        Grid.SetColumn(auto, 0);

        var cancel = new Button { Height = 24, Content = Bound(Keys.BCancel) };
        Grid.SetColumn(cancel, 2);

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,4,78") };
        row.Children.Add(auto);
        row.Children.Add(cancel);
        return row;
    }

    private static Control ExportButtons()
    {
        var gerber = new Button { Height = 22, Content = Bound(Keys.PcbGerber) };
        Grid.SetColumn(gerber, 0);

        var print = new Button { Height = 22, Content = Bound(Keys.PcbPrint) };
        Grid.SetColumn(print, 2);

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,4,*") };
        row.Children.Add(gerber);
        row.Children.Add(print);
        return row;
    }

    // ── shared pieces ─────────────────────────────────────────────────────

    /// <summary>
    /// The filled progress cells and the table rule are the two values here that are
    /// theme-owned (`--sel`, `--shad`) and have no style class of their own, so they read
    /// the resource directly — a live observable, so a theme switch still repaints them.
    /// </summary>
    private static void BindTheme(Control target, AvaloniaProperty property, string key)
        => target.Bind(property, target.GetResourceObservable(key));

    private static TextBlock Bound(string key)
    {
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));

    /// <summary>Avalonia dashes are multiples of the stroke width; the spec's are absolute.</summary>
    private static Avalonia.Collections.AvaloniaList<double> Dashes(double thickness, double on, double off)
        => new() { on / thickness, off / thickness };

    private static Control At(Control shape, double left, double top)
    {
        Canvas.SetLeft(shape, left);
        Canvas.SetTop(shape, top);
        return shape;
    }

    /// <summary>Places monospace artwork text on its SVG baseline (Lucida Console sits at .79em).</summary>
    private static Control MonoText(string text, double size, string hex, double x, double baseline,
        double opacity = 1, bool centred = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontFamily = Mono,
            FontSize = size,
            Foreground = Brush(hex),
            Opacity = opacity,
        };
        return At(tb, centred ? x - text.Length * size * 0.6 / 2 : x, baseline - size * 0.79);
    }

    /// <summary>The 16px `#dots` pattern, as one path so 2 332 dots cost one visual.</summary>
    private static string DotGrid()
    {
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y <= ArtHeight; y += 16)
            for (int x = 0; x <= ArtWidth; x += 16)
                sb.Append($"M{x - 1},{y - 1}h2v2h-2z");
        return sb.ToString();
    }

    /// <summary>
    /// The `#pads` pattern (24px tile, r=4 pad over a r=1.6 hole) clipped to the mock's
    /// 652 × 512 pad field — again two paths rather than a thousand shapes.
    /// </summary>
    private static (string Copper, string Holes) PadField()
    {
        var copper = new System.Text.StringBuilder();
        var holes = new System.Text.StringBuilder();
        for (int y = 108; y <= 588; y += 24)
            for (int x = 108; x <= 732; x += 24)
            {
                copper.Append($"M{x},{y}m-4,0a4,4 0 1,0 8,0a4,4 0 1,0 -8,0");
                holes.Append($"M{x},{y}m-1.6,0a1.6,1.6 0 1,0 3.2,0a1.6,1.6 0 1,0 -3.2,0");
            }
        return (copper.ToString(), holes.ToString());
    }
}
