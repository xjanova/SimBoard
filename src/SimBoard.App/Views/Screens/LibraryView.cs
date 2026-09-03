using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;

// ImplicitUsings pulls in System.IO, whose Path would otherwise make the shape ambiguous.
using Path = Avalonia.Controls.Shapes.Path;

namespace SimBoard.App.Views.Screens;

/// <summary>Screen 3 — component library (full view). Spec: README.md section
/// "### 3 · Component library (full view) — `03-component-library.png`".</summary>
public static class LibraryView
{
    /// <summary>Data/technical face. Prose is Tahoma, numbers and part numbers are never.</summary>
    private static readonly FontFamily Mono = new("Lucida Console, Consolas, monospace");

    /// <summary>
    /// The six filter chips. No Keys constant exists for these, so the Thai from the
    /// spec is hard-coded rather than inventing generated-file entries.
    /// </summary>
    private static readonly string[] ChipLabels =
        ["ทั้งหมด", "ในสต็อก", "มีโมเดล SPICE", "ผ่านการรับรอง", "ทะลุแผ่น", "SMD"];

    /// <summary>The 24 seeded parts: category tag · MPN · description · package · placeholder glyph.</summary>
    private static readonly (string Tag, string Mpn, string Desc, string Pkg, string Sym)[] Parts =
    [
        ("IC", "NE555P", "ไทเมอร์ 555 แรงดัน 4.5-16V", "DIP-8", "555"),
        ("IC", "TL074CN", "ออปแอมป์ JFET 4 ตัว", "DIP-14", "▷"),
        ("IC", "LM358N", "ออปแอมป์คู่ ทั่วไป", "DIP-8", "▷"),
        ("IC", "LM7805", "เร็กกูเลเตอร์ +5V 1A", "TO-220", "⎓"),
        ("IC", "LM317T", "เร็กกูเลเตอร์ปรับค่าได้", "TO-220", "⎓"),
        ("IC", "ULN2003A", "ไดรฟ์เวอร์ดาร์ลิงตัน 7 ช่อง", "DIP-16", "⊳"),
        ("Q", "2N3904", "ทรานซิสเตอร์ NPN 200mA", "TO-92", "Q"),
        ("Q", "BD139", "NPN กำลังกลาง 1.5A", "TO-126", "Q"),
        ("Q", "IRFZ44N", "MOSFET N 49A 55V", "TO-220", "Q"),
        ("D", "1N4007", "ไดโอดเรียงกระแส 1A", "DO-41", "▶"),
        ("D", "1N4148", "ไดโอดสวิตชิง", "DO-35", "▶"),
        ("D", "LED-5R", "LED แดง 5mm 20mA", "RAD-5", "☀"),
        ("R", "R-0805", "ตัวต้านทาน SMD 1%", "0805", "▭"),
        ("C", "C-ELEC", "อิเล็กโทรไลต์ 470µF 25V", "RAD-8", "⊣"),
        ("L", "L-100U", "ขดลวด 100µH 3A", "RAD-10", "∿"),
        ("&", "74HC00", "NAND 2 อินพุต 4 ชุด", "DIP-14", "&"),
        ("&", "74HC595", "ชิฟต์รีจิสเตอร์ 8 บิต", "DIP-16", "&"),
        ("µ", "ATMEGA328P", "MCU 8 บิต 16MHz", "DIP-28", "µ"),
        ("µ", "ESP32-WROOM", "MCU ไวไฟ + บลูทูธ", "MOD-38", "µ"),
        ("µ", "STM32F411", "MCU Cortex-M4 100MHz", "LQFP-48", "µ"),
        ("S", "DS18B20", "เซนเซอร์อุณหภูมิ 1-Wire", "TO-92", "S"),
        ("S", "MPU-6050", "เกียโร + แอคเซล 6 แกน", "QFN-24", "S"),
        ("M", "SRD-05VDC", "รีเลย์ 5V 10A", "THT", "M"),
        ("7", "SSD1306", "จอ OLED 0.96\" I²C", "MOD-4", "▢"),
    ];

    /// <summary>NE555P datasheet rows, verbatim from the spec.</summary>
    private static readonly (string Label, string Value)[] Specs =
    [
        ("แรงดันใช้งาน", "4.5 – 16 V"),
        ("กระแสจ่ายออก", "±200 mA"),
        ("ความถี่สูงสุด", "500 kHz"),
        ("ความผิดพลาดคาบ", "1.0 %"),
        ("อุณหภูมิใช้งาน", "0 – 70 °C"),
        ("แพ็กเกจ", "DIP-8 / SO-8"),
        ("โมเดล SPICE", "NE555.LIB"),
        ("ฟุตพรินต์", "DIP254P762X508-8"),
        ("สถานะ", "ยังผลิตอยู่"),
    ];

    /// <summary>DIP-8 pins: x/y of the 14×6 lead, and the label's left edge and text.</summary>
    private static readonly (double PinX, double LabelX, double Y, string Name)[] Pins =
    [
        (82, 64, 42, "1 GND"), (82, 64, 62, "2 TRG"), (82, 64, 82, "3 OUT"), (82, 64, 102, "4 RST"),
        (184, 202, 42, "8 VCC"), (184, 202, 62, "7 DIS"), (184, 202, 82, "6 THR"), (184, 202, 102, "5 CTL"),
    ];

    /// <summary>Builds the screen. Caller places the returned control.</summary>
    public static Control Build()
    {
        var filterBar = FilterBar();
        DockPanel.SetDock(filterBar, Dock.Top);

        // border-bottom 1px --shad plus box-shadow 0 1px 0 --lite: the two-tone rule,
        // horizontal. Bevel already draws exactly that when only two edges are set.
        var underline = new Bevel { Height = 2 };
        Themed(underline, Bevel.TopBrushProperty, "Shad");
        Themed(underline, Bevel.BottomBrushProperty, "Lite");
        DockPanel.SetDock(underline, Dock.Top);

        var split = new Grid { ColumnDefinitions = new ColumnDefinitions("*,290") };
        var browser = Browser();
        Grid.SetColumn(browser, 0);
        var datasheet = DatasheetPane();
        Grid.SetColumn(datasheet, 1);
        split.Children.Add(browser);
        split.Children.Add(datasheet);

        return new Bevel
        {
            Classes = { "flat" },
            Child = new DockPanel
            {
                LastChildFill = true,
                Children = { filterBar, underline, split },
            },
        };
    }

    // ── filter bar ───────────────────────────────────────────────────────

    private static Control FilterBar()
    {
        // 280px sunken field carrying the typed query and a 1×12 black caret.
        var query = new Bevel
        {
            Classes = { "sunken" },
            Width = 280,
            Height = 20,
            Padding = new Thickness(6, 0),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = "555", FontSize = 11, VerticalAlignment = VerticalAlignment.Center },
                    new Rectangle
                    {
                        Width = 1, Height = 12, Fill = Brushes.Black,
                        Margin = new Thickness(1, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                },
            },
        };

        var search = new Button
        {
            Height = 20,
            MinHeight = 0,
            Padding = new Thickness(9, 0),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Content = Bound(Keys.LibSearchBtn),
        };

        var sortLabel = Bound(Keys.LibSort);
        sortLabel.FontSize = 11;
        sortLabel.Foreground = B("#3a3a3a");
        sortLabel.Margin = new Thickness(10, 0, 0, 0);

        var sortValue = Bound(Keys.LibSortVal);
        sortValue.FontSize = 10;
        sortValue.Margin = new Thickness(7, 0);

        var stub = new Bevel
        {
            Classes = { "raised" },
            Width = 16,
            Height = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "▼",
                FontSize = 7,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var sortCombo = new Bevel
        {
            Classes = { "sunken" },
            Height = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { sortValue, stub },
            },
        };

        var left = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { query, search, sortLabel, sortCombo },
        };

        var toggles = ViewToggle();
        DockPanel.SetDock(toggles, Dock.Right);

        return new Bevel
        {
            Classes = { "flat" },
            Padding = new Thickness(6, 5),
            Child = new DockPanel { LastChildFill = true, Children = { toggles, left } },
        };
    }

    /// <summary>24 × 20 grid/list pair; grid starts pressed. Clicking moves the latch.</summary>
    private static Control ViewToggle()
    {
        var gridIcon = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto"),
            ColumnSpacing = 2,
            RowSpacing = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        gridIcon.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        gridIcon.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int i = 0; i < 4; i++)
        {
            var cell = new Rectangle { Width = 4, Height = 4, Fill = B("#333333") };
            Grid.SetColumn(cell, i % 2);
            Grid.SetRow(cell, i / 2);
            gridIcon.Children.Add(cell);
        }

        var listIcon = new StackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Rectangle { Width = 11, Height = 2, Fill = B("#333333") },
                new Rectangle { Width = 11, Height = 2, Fill = B("#333333") },
                new Rectangle { Width = 11, Height = 2, Fill = B("#333333") },
            },
        };

        var gridBtn = new Button
        {
            Classes = { "latched" },
            Width = 24, Height = 20, MinHeight = 0,
            Padding = new Thickness(0),
            Content = gridIcon,
        };
        var listBtn = new Button
        {
            Width = 24, Height = 20, MinHeight = 0,
            Padding = new Thickness(0),
            Content = listIcon,
        };

        gridBtn.Click += (_, _) =>
        {
            if (!gridBtn.Classes.Contains("latched")) gridBtn.Classes.Add("latched");
            listBtn.Classes.Remove("latched");
        };
        listBtn.Click += (_, _) =>
        {
            if (!listBtn.Classes.Contains("latched")) listBtn.Classes.Add("latched");
            gridBtn.Classes.Remove("latched");
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { gridBtn, listBtn },
        };
    }

    // ── chips + part grid ────────────────────────────────────────────────

    private static Control Browser()
    {
        var chips = ChipRow();
        chips.Margin = new Thickness(0, 0, 0, 6);
        DockPanel.SetDock(chips, Dock.Top);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*"),
            ColumnSpacing = 8,
            RowSpacing = 8,
            VerticalAlignment = VerticalAlignment.Top,
        };
        for (int r = 0; r < (Parts.Length + 5) / 6; r++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < Parts.Length; i++)
        {
            var (tag, mpn, desc, pkg, sym) = Parts[i];
            var card = PartCard(tag, mpn, desc, pkg, sym);
            Grid.SetColumn(card, i % 6);
            Grid.SetRow(card, i / 6);
            grid.Children.Add(card);
        }

        var well = new Bevel
        {
            Classes = { "sunken" },
            Padding = new Thickness(8),
            ClipToBounds = true,
            Child = grid,
        };

        return new Bevel
        {
            Classes = { "flat" },
            Padding = new Thickness(7),
            Child = new DockPanel { LastChildFill = true, Children = { chips, well } },
        };
    }

    private static Panel ChipRow()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var setters = new List<Action<bool>>();
        for (int i = 0; i < ChipLabels.Length; i++)
        {
            var label = new TextBlock
            {
                Text = ChipLabels[i],
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var body = new Bevel { Padding = new Thickness(9, 2), Child = label };

            // The active chip paints --sel, which is a live theme token, so it has to be
            // a binding rather than a colour resolved once at construction.
            IDisposable? selection = null;
            void Set(bool on)
            {
                selection?.Dispose();
                selection = null;
                if (on)
                {
                    selection = body.Bind(Bevel.BackgroundProperty, new DynamicResourceExtension("Selection"));
                    var edge = B("#051239");
                    body.TopBrush = edge;
                    body.RightBrush = edge;
                    body.BottomBrush = edge;
                    body.LeftBrush = edge;
                    label.Foreground = Brushes.White;
                }
                else
                {
                    body.Background = B("#e8e6e2");
                    body.TopBrush = B("#ffffff");
                    body.RightBrush = B("#9a9691");
                    body.BottomBrush = B("#9a9691");
                    body.LeftBrush = B("#ffffff");
                    label.Foreground = B("#2a2a2a");
                }
            }

            Set(i == 0);
            setters.Add(Set);

            // A transparent Panel is what actually takes the click: Bevel paints through
            // its own Render, which is not a hit-testable background.
            var hit = new Panel { Background = Brushes.Transparent, Children = { body } };
            int index = i;
            hit.PointerPressed += (_, _) =>
            {
                for (int k = 0; k < setters.Count; k++) setters[k](k == index);
            };
            row.Children.Add(hit);
        }

        return row;
    }

    private static Control PartCard(string tag, string mpn, string desc, string pkg, string sym)
    {
        var thumb = new Panel
        {
            Height = 58,
            Background = B("#12161b"),
            Children =
            {
                new TextBlock
                {
                    Text = sym,
                    FontFamily = Mono,
                    FontSize = 15,
                    FontWeight = FontWeight.Bold,
                    Foreground = B("#8fb4cf"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                new TextBlock
                {
                    Text = tag,
                    FontFamily = Mono,
                    FontSize = 8,
                    Foreground = B("#e8b04a"),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(3, 3, 0, 0),
                },
                new Rectangle
                {
                    Height = 1,
                    Fill = B("#cfcdc8"),
                    VerticalAlignment = VerticalAlignment.Bottom,
                },
            },
        };

        var body = new StackPanel
        {
            Margin = new Thickness(5, 4),
            Children =
            {
                new TextBlock
                {
                    Text = mpn,
                    FontFamily = Mono,
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = B("#12161b"),
                    LineHeight = 13,
                    ClipToBounds = true,
                },
                new TextBlock
                {
                    Text = desc,
                    FontSize = 9,
                    LineHeight = 12,
                    Height = 24,
                    Foreground = B("#5a5a5a"),
                    TextWrapping = TextWrapping.Wrap,
                    ClipToBounds = true,
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
                    Margin = new Thickness(0, 3, 0, 0),
                    Children = { Badge(pkg), Badge("SPICE") },
                },
            },
        };

        return new Bevel
        {
            Background = B("#f8f8f6"),
            TopBrush = B("#cfcdc8"),
            RightBrush = B("#cfcdc8"),
            BottomBrush = B("#cfcdc8"),
            LeftBrush = B("#cfcdc8"),
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Top,
            Child = new StackPanel { Children = { thumb, body } },
        };
    }

    private static Control Badge(string text) => new Bevel
    {
        Background = B("#e4ebf1"),
        TopBrush = B("#c3cfd9"),
        RightBrush = B("#c3cfd9"),
        BottomBrush = B("#c3cfd9"),
        LeftBrush = B("#c3cfd9"),
        Padding = new Thickness(3, 1),
        VerticalAlignment = VerticalAlignment.Top,
        Child = new TextBlock
        {
            Text = text,
            FontFamily = Mono,
            FontSize = 8,
            Foreground = B("#4a6a80"),
        },
    };

    // ── datasheet pane ───────────────────────────────────────────────────

    private static Control DatasheetPane()
    {
        var caption = new Bevel
        {
            Classes = { "caption" },
            Height = 18,
            Padding = new Thickness(5, 0),
            Child = Bound(Keys.LibDatasheet),
        };
        Grid.SetRow(caption, 0);

        // "sunken" paints white; the pin-out well is the workspace dark, so the
        // background is a local value that overrides the class setter.
        var drawing = new Bevel
        {
            Classes = { "sunken" },
            Background = B("#12161b"),
            Height = 150,
            ClipToBounds = true,
            Child = Pinout(),
        };
        Grid.SetRow(drawing, 1);

        var rows = new StackPanel();
        foreach (var (label, value) in Specs) rows.Children.Add(SpecRow(label, value));

        var table = new Bevel
        {
            Classes = { "sunken" },
            ClipToBounds = true,
            Child = rows,
        };
        Grid.SetRow(table, 2);

        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 4 };
        var place = new Button
        {
            Classes = { "default" },
            Height = 22,
            MinHeight = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = Bound(Keys.LibPlace),
        };
        Grid.SetColumn(place, 0);
        var pdf = new Button
        {
            Height = 22,
            MinHeight = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = Bound(Keys.LibPdf),
        };
        Grid.SetColumn(pdf, 1);
        buttons.Children.Add(place);
        buttons.Children.Add(pdf);
        Grid.SetRow(buttons, 3);

        var stack = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            RowSpacing = 6,
        };
        stack.Children.Add(caption);
        stack.Children.Add(drawing);
        stack.Children.Add(table);
        stack.Children.Add(buttons);

        var divider = new Bevel { Classes = { "vdivider" } };
        DockPanel.SetDock(divider, Dock.Left);

        return new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                divider,
                new Bevel { Classes = { "flat" }, Padding = new Thickness(7), Child = stack },
            },
        };
    }

    private static Control SpecRow(string label, string value)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Height = 17 };

        var k = new TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = B("#4a4a4a"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(k, 0);

        var v = new TextBlock
        {
            Text = value,
            FontFamily = Mono,
            FontSize = 10,
            Foreground = B("#12161b"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        Grid.SetColumn(v, 1);

        row.Children.Add(k);
        row.Children.Add(v);

        return new Bevel
        {
            BottomBrush = B("#eeece8"),
            Padding = new Thickness(6, 0),
            Child = row,
        };
    }

    /// <summary>
    /// The DIP-8 pin-out, drawn at the mock's own 280 × 150 coordinates so every
    /// number in the spec is literally in the source.
    /// </summary>
    private static Control Pinout()
    {
        var canvas = new Canvas
        {
            Width = 280,
            Height = 148,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        // 16px dot pattern, r=1, #2b3440 — the same grid as the workspace canvas.
        var dot = B("#2b3440");
        for (double y = 0; y <= 144; y += 16)
            for (double x = 0; x <= 272; x += 16)
            {
                var d = new Ellipse { Width = 2, Height = 2, Fill = dot };
                Canvas.SetLeft(d, x - 1);
                Canvas.SetTop(d, y - 1);
                canvas.Children.Add(d);
            }

        var outline = B("#8fb4cf");

        var package = new Rectangle
        {
            Width = 88,
            Height = 90,
            Fill = B("#1b2027"),
            Stroke = outline,
            StrokeThickness = 1.4,
        };
        Canvas.SetLeft(package, 96);
        Canvas.SetTop(package, 30);
        canvas.Children.Add(package);

        var notch = new Path
        {
            Data = Geometry.Parse("M128,30 A12,12 0 0 0 152,30"),
            Stroke = outline,
            StrokeThickness = 1.4,
        };
        Canvas.SetLeft(notch, 0);
        Canvas.SetTop(notch, 0);
        canvas.Children.Add(notch);

        var lead = B("#6f8ba1");
        var meta = B("#7f97ab");
        foreach (var (pinX, labelX, y, name) in Pins)
        {
            var pin = new Rectangle { Width = 14, Height = 6, Fill = lead };
            Canvas.SetLeft(pin, pinX);
            Canvas.SetTop(pin, y);
            canvas.Children.Add(pin);

            var text = new TextBlock
            {
                Text = name,
                FontFamily = Mono,
                FontSize = 7,
                Foreground = meta,
            };
            Canvas.SetLeft(text, labelX);
            Canvas.SetTop(text, y);
            canvas.Children.Add(text);
        }

        var partName = new TextBlock
        {
            Text = "NE555P",
            FontFamily = Mono,
            FontSize = 10,
            Foreground = B("#cfdce6"),
            Width = 88,
            TextAlignment = TextAlignment.Center,
        };
        Canvas.SetLeft(partName, 96);
        Canvas.SetTop(partName, 71);
        canvas.Children.Add(partName);

        var footer = new TextBlock
        {
            Text = "DIP-8 · 9.81 × 6.35 mm",
            FontFamily = Mono,
            FontSize = 7,
            Foreground = B("#4a6a80"),
        };
        Canvas.SetLeft(footer, 8);
        Canvas.SetTop(footer, 135);
        canvas.Children.Add(footer);

        return canvas;
    }

    // ── shared pieces ────────────────────────────────────────────────────

    private static SolidColorBrush B(string hex) => new(Color.Parse(hex));

    /// <summary>Points a property at a live chrome token, so a theme swap repaints it.</summary>
    private static void Themed(AvaloniaObject target, AvaloniaProperty property, string key)
        => target.Bind(property, new DynamicResourceExtension(key));

    private static TextBlock Bound(string key)
    {
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }
}
