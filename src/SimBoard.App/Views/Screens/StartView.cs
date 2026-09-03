using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;

namespace SimBoard.App.Views.Screens;

/// <summary>Screen 1 — Start / Project picker. Spec: README.md section "1 · Start / Project picker — `01-start-project-picker.png`".</summary>
public static class StartView
{
    /// <summary>Recent projects, verbatim from the spec. The product rename turns .ebp into .sbp.</summary>
    private static readonly (string Name, string KindTh, string KindEn, string Date)[] Recents =
    [
        ("555-astable-reg.sbp", "ผัง + PCB", "Sch + PCB", "03-09-2026"),
        ("smps-12v-3a.sbp", "ผัง + PCB", "Sch + PCB", "01-09-2026"),
        ("esp32-sensor-node.sbp", "บอร์ดทดลอง", "Breadboard", "28-08-2026"),
        ("audio-preamp-ne5532.sbp", "ผังวงจร", "Schematic", "26-08-2026"),
        ("h-bridge-bts7960.sbp", "ผัง + PCB", "Sch + PCB", "22-08-2026"),
        ("7seg-counter-4026.sbp", "ผังวงจร", "Schematic", "19-08-2026"),
        ("lab-psu-0-30v.sbp", "ผัง + PCB", "Sch + PCB", "15-08-2026"),
        ("i2c-oled-driver.sbp", "บอร์ดทดลอง", "Breadboard", "11-08-2026"),
    ];

    /// <summary>The six template cards. No Keys constant exists for these labels, so the Thai stands.</summary>
    private static readonly (string Tag, string Label)[] Templates =
    [
        ("555", "ตั้งเวลา 555 astable"),
        ("OPA", "ออปแอมป์ขยายเสียง"),
        ("PSU", "ภาคจ่ายไฟเรกูเลต"),
        ("MCU", "บอร์ด MCU ขั้นต่ำ"),
        ("74x", "วงจรนับลอจิก"),
        ("···", "ผังเปล่า A3"),
    ];

    /// <summary>Builds the screen. Caller places the returned control.</summary>
    public static Control Build()
    {
        // The 1060px window is the spec's fixed size. A ScrollViewer sits under it so a
        // host narrower than the design canvas pans instead of silently cropping the
        // templates column off the right edge.
        var scroller = new ScrollViewer
        {
            Background = Brushes.Transparent,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = WelcomeWindow(),
        };

        var root = new Panel
        {
            ClipToBounds = true,
            Children =
            {
                // repeating-linear-gradient(0deg, rgba(255,255,255,.025) 0 1px, transparent 1px 3px)
                new Rectangle { Fill = Scanlines(), IsHitTestVisible = false },
                scroller,
            },
        };
        Themed(root, Panel.BackgroundProperty, "Desktop");
        return root;
    }

    // ── the 1060px welcome window ────────────────────────────────────────────

    private static Control WelcomeWindow()
    {
        var window = new Bevel
        {
            Classes = { "raised" },
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    Docked(TitleBar(), Dock.Top),
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("250,*"),
                        Children = { Column(LeftRail(), 0), Column(Body(), 1) },
                    },
                },
            },
        };

        // box-shadow: var(--raiseSh), 6px 6px 14px rgba(0,0,0,.35) — the bevel half of
        // that pair belongs to the Bevel, so the drop shadow rides on a wrapper.
        return new Border
        {
            Width = 1060,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12),
            BoxShadow = BoxShadows.Parse("6 6 14 0 #59000000"),
            Child = window,
        };
    }

    private static Control TitleBar()
    {
        var ring = new Ellipse { Width = 6, Height = 6, StrokeThickness = 1.5 };
        Themed(ring, Shape.StrokeProperty, "Selection");

        var icon = new Border
        {
            Width = 14,
            Height = 14,
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = ring,
        };
        Themed(icon, Border.BackgroundProperty, "Face");
        Themed(icon, Border.BorderBrushProperty, "Dark");

        var title = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(5, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children = { new TextBlock { Text = "SimBoard —" }, Bound(Keys.STitle) },
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                WindowButton("Min", "_", 9, "WindowButtonFg", new Thickness(0, -3, 0, 0)),
                WindowButton("Max", "□", 8, "WindowButtonFg", default),
                WindowButton("Close", "✕", 8, "CloseFg", default),
            },
        };

        var bar = new Bevel
        {
            Classes = { "titlebar" },
            Padding = new Thickness(4, 0, 3, 0),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("14,*,Auto"),
                Children = { Column(icon, 0), Column(title, 1), Column(buttons, 2) },
            },
        };
        Themed(bar, Bevel.CornerRadiusProperty, "WindowRadius");
        return bar;
    }

    /// <summary>Decorative only — this window's chrome is a picture of the shell's, not a copy of its behaviour.</summary>
    private static Control WindowButton(string token, string glyph, double size, string fgKey, Thickness nudge)
    {
        var text = new TextBlock
        {
            Text = glyph,
            FontSize = size,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = nudge == default ? VerticalAlignment.Center : VerticalAlignment.Top,
            Margin = nudge,
        };
        Themed(text, TextBlock.ForegroundProperty, fgKey);

        var b = new Bevel { Width = 16, Height = 14, Child = text };
        Themed(b, Bevel.BackgroundProperty, token + "Bg");
        Themed(b, Bevel.TopBrushProperty, token + "BorderTop");
        Themed(b, Bevel.RightBrushProperty, token + "BorderRight");
        Themed(b, Bevel.BottomBrushProperty, token + "BorderBottom");
        Themed(b, Bevel.LeftBrushProperty, token + "BorderLeft");
        Themed(b, Bevel.CornerRadiusProperty, "WindowButtonRadius");
        return b;
    }

    // ── left rail · 250px ────────────────────────────────────────────────────

    private static Control LeftRail()
    {
        // "Sim" white, "Board" amber — 700 22px/1.15 Tahoma, letter-spacing -.01em.
        var wordmark = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                new TextBlock
                {
                    Text = "Sim",
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    LetterSpacing = -0.22,
                    LineHeight = 25.3,
                    Foreground = Brushes.White,
                },
                new TextBlock
                {
                    Text = "Board",
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    LetterSpacing = -0.22,
                    LineHeight = 25.3,
                    Foreground = Paint("#e8b04a"),
                },
            },
        };

        var subtitle = Bound(Keys.SSub);
        subtitle.FontSize = 11;
        subtitle.LineHeight = 17.05;
        subtitle.TextWrapping = TextWrapping.Wrap;
        subtitle.Foreground = Paint("#9fbccd");
        subtitle.VerticalAlignment = VerticalAlignment.Top;
        subtitle.Margin = new Thickness(0, 10, 0, 0);

        var licence = Bound(Keys.SVer);
        licence.Classes.Add("dense");
        licence.LineHeight = 13.5;
        licence.TextWrapping = TextWrapping.Wrap;
        licence.Foreground = Paint("#6f93a8");
        licence.VerticalAlignment = VerticalAlignment.Top;

        var footer = new StackPanel
        {
            Children =
            {
                FooterLine("v4.2.1 · build 20260903"),
                FooterLine("Simulation core: SPICE3f5 / Xyce"),
                licence,
            },
        };

        var stack = new DockPanel
        {
            LastChildFill = false,
            Children = { Docked(wordmark, Dock.Top), Docked(subtitle, Dock.Top), Docked(footer, Dock.Bottom) },
        };

        return new Border
        {
            Width = 250,
            Padding = new Thickness(16, 20),
            Background = new LinearGradientBrush
            {
                // linear-gradient(160deg, …): mostly downward with a slight rightward lean.
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.34, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#1d3b52"), 0),
                    new GradientStop(Color.Parse("#0e1f2c"), 1),
                },
            },
            Child = stack,
        };
    }

    private static TextBlock FooterLine(string text) => new()
    {
        Classes = { "dense" },
        Text = text,
        LineHeight = 13.5,
        Foreground = Paint("#6f93a8"),
    };

    // ── right-hand body: recent + templates over the footer bar ──────────────

    private static Control Body()
    {
        var columns = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,250"),
            Children = { Column(RecentColumn(), 0), Column(TemplatesColumn(), 1) },
        };

        return new Border
        {
            Padding = new Thickness(18, 16, 18, 18),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children = { Docked(Footer(), Dock.Bottom), columns },
            },
        };
    }

    /// <summary>The 16px gutter is taken out of the flexible column, so templates keep their full 250px.</summary>
    private static Control RecentColumn() => new StackPanel
    {
        Margin = new Thickness(0, 0, 16, 0),
        Children = { Heading(Keys.SRecent), RecentTable() },
    };

    private static Control RecentTable()
    {
        var rows = new StackPanel();

        // Row selection is the one live behaviour on this screen: the highlight is a
        // themed band behind the cells, so it follows a chrome swap like everything else.
        var picked = new List<(Border Band, TextBlock[] Cells, IBrush[] Resting)>();

        void Select(int index)
        {
            for (int i = 0; i < picked.Count; i++)
            {
                var (band, cells, resting) = picked[i];
                bool on = i == index;
                band.IsVisible = on;
                for (int c = 0; c < cells.Length; c++)
                    cells[c].Foreground = on ? Brushes.White : resting[c];
            }
        }

        for (int i = 0; i < Recents.Length; i++)
        {
            int index = i;
            var (name, kindTh, kindEn, date) = Recents[i];

            var chip = new Border
            {
                Width = 13,
                Height = 13,
                Background = Paint("#e8b04a"),
                BorderBrush = Paint("#8a6420"),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var nameText = new TextBlock
            {
                Text = name,
                Foreground = Paint("#1a1a1a"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var kindText = new TextBlock
            {
                Text = L.I.Lang == Lang.Th ? kindTh : kindEn,
                Foreground = Paint("#3c3c3c"),
                Margin = new Thickness(6, 4),
            };
            var dateText = new TextBlock
            {
                Text = date,
                FontSize = 10,
                Foreground = Paint("#6a6a6a"),
                Margin = new Thickness(6, 4),
            };

            var cells = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,92,78"),
                Children =
                {
                    Column(new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        Margin = new Thickness(6, 4),
                        Children = { chip, nameText },
                    }, 0),
                    Column(kindText, 1),
                    Column(dateText, 2),
                },
            };

            var band = new Border { IsVisible = false };
            Themed(band, Border.BackgroundProperty, "Selection");

            var row = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = Paint("#efedea"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = new Panel { Children = { band, cells } },
            };
            row.PointerPressed += (_, _) => Select(index);

            picked.Add((
                band,
                new[] { nameText, kindText, dateText },
                new IBrush[] { Paint("#1a1a1a"), Paint("#3c3c3c"), Paint("#6a6a6a") }));
            rows.Children.Add(row);
        }

        var body = new StackPanel
        {
            Children = { RecentHeader(), rows },
        };

        return new Bevel
        {
            Classes = { "sunken" },
            Height = 262,
            ClipToBounds = true,
            Child = body,
        };
    }

    private static Control RecentHeader()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,92,78"),
            Children =
            {
                Column(HeaderCell(Keys.SColName, divider: true), 0),
                Column(HeaderCell(Keys.SColKind, divider: true), 1),
                Column(HeaderCell(Keys.SColDate, divider: false), 2),
            },
        };

        var header = new Border { BorderThickness = new Thickness(0, 0, 0, 1), Child = grid };
        Themed(header, Border.BackgroundProperty, "Face");
        Themed(header, Border.BorderBrushProperty, "Shad");
        return header;
    }

    private static Control HeaderCell(string key, bool divider)
    {
        var label = Bound(key);
        label.FontSize = 10;
        label.Margin = new Thickness(6, 3);

        var cell = new Border
        {
            BorderThickness = divider ? new Thickness(0, 0, 1, 0) : default,
            Child = label,
        };
        if (divider) Themed(cell, Border.BorderBrushProperty, "Shad");
        return cell;
    }

    // ── templates · 250px, two columns of raised cards ───────────────────────

    private static Control TemplatesColumn()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        for (int r = 0; r < (Templates.Length + 1) / 2; r++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < Templates.Length; i++)
        {
            int column = i % 2, row = i / 2;
            var card = TemplateCard(Templates[i].Tag, Templates[i].Label);
            card.Margin = new Thickness(0, 0, column == 0 ? 6 : 0, row < (Templates.Length / 2) - 1 ? 6 : 0);
            Grid.SetColumn(card, column);
            Grid.SetRow(card, row);
            grid.Children.Add(card);
        }

        return new StackPanel
        {
            Children = { Heading(Keys.STemplates), grid },
        };
    }

    private static Control TemplateCard(string tag, string label)
    {
        // The 44px tile is the placeholder the spec reserves for a real part symbol.
        var thumb = new Border
        {
            Height = 44,
            Background = Paint("#12161b"),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Classes = { "dense" },
                Text = tag,
                Foreground = Paint("#7f97ab"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        Themed(thumb, Border.BorderBrushProperty, "Dark");

        return new Bevel
        {
            Classes = { "raised" },
            Padding = new Thickness(6),
            Child = new StackPanel
            {
                Children =
                {
                    thumb,
                    new TextBlock
                    {
                        Text = label,
                        FontSize = 10,
                        Height = 26,
                        LineHeight = 13,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 0),
                    },
                },
            },
        };
    }

    // ── footer · checkbox and the three buttons ──────────────────────────────

    private static Control Footer()
    {
        var shadRule = new Rectangle { Height = 1, HorizontalAlignment = HorizontalAlignment.Stretch };
        Themed(shadRule, Shape.FillProperty, "Shad");
        var liteRule = new Rectangle { Height = 1, HorizontalAlignment = HorizontalAlignment.Stretch };
        Themed(liteRule, Shape.FillProperty, "Lite");

        // border-top: 1px solid var(--shad) + box-shadow: 0 -1px 0 #fff inset.
        var rule = new StackPanel
        {
            Margin = new Thickness(0, 16, 0, 0),
            Children = { shadRule, liteRule },
        };

        var check = new CheckBox
        {
            IsChecked = true,
            Content = Bound(Keys.SShowStart),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children =
            {
                new Button { Classes = { "default" }, MinWidth = 96, Padding = new Thickness(12, 5), Content = Bound(Keys.BNew) },
                new Button { MinWidth = 96, Padding = new Thickness(12, 5), Content = Bound(Keys.BOpen) },
                new Button { MinWidth = 96, Padding = new Thickness(12, 5), Content = Bound(Keys.BImport) },
            },
        };

        var row = new Grid
        {
            Margin = new Thickness(0, 13, 0, 0),
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { Column(check, 0), Column(buttons, 1) },
        };

        return new StackPanel { Children = { rule, row } };
    }

    // ── shared pieces ────────────────────────────────────────────────────────

    private static TextBlock Heading(string key)
    {
        var tb = Bound(key);
        tb.FontWeight = FontWeight.Bold;
        tb.Margin = new Thickness(0, 0, 0, 5);
        return tb;
    }

    /// <summary>A label that re-reads itself when the language changes.</summary>
    private static TextBlock Bound(string key)
    {
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }

    /// <summary>Follows a chrome swap: the token is resolved live, never captured once.</summary>
    private static void Themed(Control target, AvaloniaProperty property, string key) =>
        target.Bind(property, target.GetResourceObservable(key));

    private static SolidColorBrush Paint(string hex) => new(Color.Parse(hex));

    private static Control Column(Control c, int column) { Grid.SetColumn(c, column); return c; }

    private static Control Docked(Control c, Dock dock) { DockPanel.SetDock(c, dock); return c; }

    /// <summary>The desktop scanline overlay: a 1px white line every 3px, 2.5% alpha.</summary>
    private static IBrush Scanlines() => new DrawingBrush
    {
        Drawing = new GeometryDrawing
        {
            Brush = Paint("#06FFFFFF"),
            Geometry = new RectangleGeometry(new Rect(0, 0, 64, 1)),
        },
        TileMode = TileMode.Tile,
        Stretch = Stretch.Fill,
        SourceRect = new RelativeRect(0, 0, 64, 3, RelativeUnit.Absolute),
        DestinationRect = new RelativeRect(0, 0, 64, 3, RelativeUnit.Absolute),
    };
}
