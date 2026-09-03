using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using SimBoard.App.Controls;
using SimBoard.App.Localization;

namespace SimBoard.App.Views.Screens;

/// <summary>
/// Screen 7 — Import layout / netlist. Spec: README.md section
/// "### 7 · Import layout / netlist — `07-import-gerber-netlist.png`".
///
/// The 760px modal's body only: the modal host supplies the frame, the caption in the
/// dialog title-bar colour and the close button, so this returns the panel that sits
/// inside it.
/// </summary>
public static class ImportDialogView
{
    /// <summary>The eight files the mock's Gerber package contains, in mock order.</summary>
    private static readonly (string Name, string Layer, string Size)[] PackageFiles =
    [
        ("amp-v3-F_Cu.gbr", "ทองแดงด้านบน", "84 KB"),
        ("amp-v3-B_Cu.gbr", "ทองแดงด้านล่าง", "71 KB"),
        ("amp-v3-F_Mask.gbr", "โซลเดอร์มาสก์บน", "22 KB"),
        ("amp-v3-F_Silk.gbr", "ซิลค์สกรีนบน", "39 KB"),
        ("amp-v3-Edge.gbr", "ขอบแผ่น", "4 KB"),
        ("amp-v3.drl", "ตำแหน่งเจาะ", "11 KB"),
        ("amp-v3.net", "เนตลิสต์", "18 KB"),
        ("amp-v3-BOM.csv", "รายการอุปกรณ์", "6 KB"),
    ];

    /// <summary>Import options; the spec checks the first four and leaves the fifth clear.</summary>
    private static readonly (string Label, bool On)[] ImportOptions =
    [
        ("จับคู่ฟุตพรินต์กับคลังอุปกรณ์อัตโนมัติ", true),
        ("สร้างผังวงจรย้อนกลับจากเนตลิสต์", true),
        ("แนบโมเดล SPICE ให้อุปกรณ์ที่รู้จัก", true),
        ("รวมเป็นเลเยอร์ใหม่ ไม่ทับของเดิม", true),
        ("หน่วยเป็นมิลลิเมตร (ไม่ใช่ mil)", false),
    ];

    private static readonly string[] FormatLines =
    [
        "GERBER RS-274X · EXCELLON · KICAD",
        "EAGLE .BRD · ALTIUM .PCBDOC",
        "SPICE .CIR / .NET · ALTIUM NETLIST",
    ];

    // The dark preview is authored in the mock's own 276 × 192 SVG frame; keeping those
    // numbers verbatim is what makes the board render match the PNG pad for pad.
    private const double BoardW = 276;
    private const double BoardH = 192;

    // Barber pole: 6 stripes of 8px on / 2px off = the spec's 10px period, 60px total,
    // which is the 30 % of the well the mock uses. It travels -100 % → 320 % of itself.
    private const double StripeSpan = 60;
    private const double StripeTravel = StripeSpan * 4.2;
    private const double StripePeriodMs = 1600;
    private const double StripeTickMs = 40;

    /// <summary>Builds the screen. Caller places the returned control.</summary>
    public static Control Build()
    {
        var root = new StackPanel
        {
            // ModalHost pads 10 all round; the spec's dialog body is 10px 12px 12px.
            Margin = new Thickness(2, 0, 2, 2),
            Children =
            {
                FileRow(),
                ContentsAndPreview(),
                OptionsAndFormats(),
                Footer(),
            },
        };
        return root;
    }

    // ── file row ─────────────────────────────────────────────────────────

    private static Control FileRow()
    {
        var label = Bound(Keys.ImFile);
        label.Margin = new Thickness(0, 0, 6, 0);
        DockPanel.SetDock(label, Dock.Left);

        var browse = new Button
        {
            Width = 84,
            Height = 21,
            MinHeight = 0,
            Padding = new Thickness(0),
            Margin = new Thickness(6, 0, 0, 0),
            Content = Bound(Keys.BBrowse),
        };
        DockPanel.SetDock(browse, Dock.Right);

        var path = new TextBox
        {
            Text = @"D:\Projects\amp-v3\gerber\amp-v3.zip",
            Height = 20,
            MinHeight = 0,
            FontSize = 10,
            FontFamily = Mono,
            Padding = new Thickness(6, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        return new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0, 0, 0, 8),
            Children = { label, browse, path },
        };
    }

    // ── package contents + board preview ─────────────────────────────────

    private static Control ContentsAndPreview()
    {
        var left = new StackPanel
        {
            Children = { Caption(Keys.ImFiles, 3), ContentsTable() },
        };
        Grid.SetColumn(left, 0);

        var right = new StackPanel
        {
            Width = 280,
            Margin = new Thickness(10, 0, 0, 0),
            Children = { Caption(Keys.ImPreview, 3), BoardPreview() },
        };
        Grid.SetColumn(right, 1);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,280") };
        grid.Children.Add(left);
        grid.Children.Add(right);
        return grid;
    }

    /// <summary>The 196px sunken well: a raised header strip over eight hairline rows.</summary>
    private static Control ContentsTable()
    {
        var rows = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        rows.Children.Add(TableHeader());
        foreach (var (name, layer, size) in PackageFiles)
        {
            rows.Children.Add(TableRow(name, layer, size));
            rows.Children.Add(new Rectangle { Height = 1, Fill = Ink("#f2f0ed") });
        }

        return new Bevel
        {
            Classes = { "sunken" },
            Height = 196,
            Padding = new Thickness(0),
            Child = new Panel { ClipToBounds = true, Children = { rows } },
        };
    }

    private static Control TableHeader()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,96,60") };
        var cells = new[]
        {
            HeaderCell(Keys.ImColFile, true),
            HeaderCell(Keys.ImColLayer, true),
            HeaderCell(Keys.ImColSize, false),
        };
        for (int i = 0; i < cells.Length; i++)
        {
            Grid.SetColumn(cells[i], i);
            grid.Children.Add(cells[i]);
        }

        // A raised strip is how a period list header carries the spec's shadow underline
        // without hard-coding --shad, which would survive a theme switch.
        return new Bevel
        {
            Classes = { "raised" },
            CornerRadius = new CornerRadius(0),
            Child = grid,
        };
    }

    private static Control HeaderCell(string key, bool divider)
    {
        var text = Bound(key);
        text.FontSize = 9;
        text.Margin = new Thickness(5, 2);

        var cell = new DockPanel { LastChildFill = true };
        if (divider)
        {
            var rule = new Bevel { Classes = { "vdivider" } };
            DockPanel.SetDock(rule, Dock.Right);
            cell.Children.Add(rule);
        }
        cell.Children.Add(text);
        return cell;
    }

    private static Control TableRow(string name, string layer, string size)
    {
        var chip = new Border
        {
            Width = 9,
            Height = 9,
            Background = Ink("#4f7cb0"),
            BorderBrush = Ink("#24405e"),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var file = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Margin = new Thickness(5, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children = { chip, Dense(name, null) },
        };
        Grid.SetColumn(file, 0);

        var layerCell = Dense(layer, "#3a3a3a");
        layerCell.Margin = new Thickness(5, 0);
        Grid.SetColumn(layerCell, 1);

        var sizeCell = Dense(size, "#6a6a6a");
        sizeCell.Margin = new Thickness(5, 0);
        Grid.SetColumn(sizeCell, 2);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,96,60"),
            Height = 17,   // 9px / 1.9 line-height
        };
        grid.Children.Add(file);
        grid.Children.Add(layerCell);
        grid.Children.Add(sizeCell);
        return grid;
    }

    /// <summary>The dark board render — the mock's own SVG, control for control.</summary>
    private static Control BoardPreview()
    {
        var canvas = new Canvas { Width = BoardW, Height = BoardH };

        var substrate = new Rectangle
        {
            Width = 224,
            Height = 152,
            RadiusX = 3,
            RadiusY = 3,
            Fill = Ink("#12311f"),
            Stroke = Ink("#2f6b47"),
            StrokeThickness = 1,
        };
        Place(canvas, substrate, 26, 20);

        var copper = new Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse(
                "M42 36h192 M42 156h192 M96 60h-38v56 M140 60h52v34 M96 116h24v28h72"),
            Stroke = Ink("#c98b4b"),
            StrokeThickness = 2.4,
        };
        Place(canvas, copper, 0, 0);

        Place(canvas, Silk(new Rectangle { Width = 44, Height = 56 }), 96, 60);
        Place(canvas, Silk(new Rectangle { Width = 34, Height = 20 }), 176, 94);
        Place(canvas, Silk(new Ellipse { Width = 16, Height = 16 }), 208, 132);

        Place(canvas, Via(), 116.6, 42.6);
        Place(canvas, Via(), 188.6, 126.6);

        var footer = new TextBlock
        {
            Classes = { "dense" },
            FontSize = 7,
            Foreground = Ink("#6f8ba1"),
            Text = "100.0 × 80.0 mm · 2L · 44 NETS · 22 PARTS",
        };
        Place(canvas, footer, 30, 179);

        return new Bevel
        {
            Classes = { "workspace" },
            Background = Ink("#0e1013"),
            Height = 196,
            Padding = new Thickness(0),
            Child = new Panel
            {
                ClipToBounds = true,
                Children = { canvas },
            },
        };
    }

    private static Shape Silk(Shape shape)
    {
        shape.Stroke = Ink("#e6e6e0");
        shape.StrokeThickness = 1;
        return shape;
    }

    private static Shape Via() =>
        new Ellipse { Width = 6.8, Height = 6.8, Fill = Ink("#c98b4b") };

    private static void Place(Canvas canvas, Control child, double x, double y)
    {
        Canvas.SetLeft(child, x);
        Canvas.SetTop(child, y);
        canvas.Children.Add(child);
    }

    // ── options + supported formats ──────────────────────────────────────

    private static Control OptionsAndFormats()
    {
        var options = new StackPanel { Children = { Caption(Keys.ImOpts, 4) } };
        foreach (var (label, on) in ImportOptions)
            options.Children.Add(new CheckBox
            {
                IsChecked = on,
                Height = 19,   // 10px / 1.9 line-height
                Content = new TextBlock
                {
                    Text = label,
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });

        var optionBox = new Bevel
        {
            Classes = { "sunken", "face" },
            Padding = new Thickness(8, 6),
            Child = options,
        };
        Grid.SetColumn(optionBox, 0);

        var formatBox = FormatsBox();
        Grid.SetColumn(formatBox, 1);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,280"),
            Margin = new Thickness(0, 9, 0, 0),
        };
        grid.Children.Add(optionBox);
        grid.Children.Add(formatBox);
        return grid;
    }

    private static Control FormatsBox()
    {
        var caption = Caption(Keys.ImFormats, 4);
        DockPanel.SetDock(caption, Dock.Top);

        var progress = ProgressRow();
        DockPanel.SetDock(progress, Dock.Bottom);

        var lines = new StackPanel { Spacing = 5 };   // 9px / 1.8 line-height
        foreach (var line in FormatLines)
            lines.Children.Add(Dense(line, "#2a2a2a"));

        // The last line ends in a translated phrase, so it is two runs, not one string.
        var trace = Bound(Keys.ImTrace);
        trace.Classes.Add("dense");
        trace.Foreground = Ink("#2a2a2a");
        lines.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { Dense("IMAGE TRACE .PNG / .JPG → ", "#2a2a2a"), trace },
        });

        return new Bevel
        {
            Classes = { "sunken", "face" },
            Width = 280,
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(8, 6),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children = { caption, progress, lines },
            },
        };
    }

    /// <summary>
    /// The barber pole. It is deliberately indeterminate: the mock shows a package being
    /// read, and a percentage we cannot compute would be a lie dressed as progress.
    /// </summary>
    private static Control ProgressRow()
    {
        var stripes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        for (int i = 0; i < 6; i++)
            stripes.Children.Add(new Rectangle
            {
                Width = 8,
                Fill = Ink("#0a246a"),
                Margin = new Thickness(0, 0, 2, 0),
            });

        var shift = new TranslateTransform { X = -StripeSpan };
        stripes.RenderTransform = shift;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(StripeTickMs) };
        double phase = 0;
        timer.Tick += (_, _) =>
        {
            phase += StripeTickMs / StripePeriodMs;
            if (phase >= 1) phase -= 1;
            shift.X = -StripeSpan + (phase * StripeTravel);
        };
        // Tied to the visual tree so a closed dialog stops costing frames.
        stripes.AttachedToVisualTree += (_, _) => timer.Start();
        stripes.DetachedFromVisualTree += (_, _) => timer.Stop();

        var well = new Bevel
        {
            Classes = { "sunken" },
            Height = 12,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new Panel { ClipToBounds = true, Children = { stripes } },
        };

        var label = Bound(Keys.ImProgress);
        label.FontSize = 10;
        label.Margin = new Thickness(0, 0, 6, 0);
        DockPanel.SetDock(label, Dock.Left);

        return new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0, 8, 0, 0),
            Children = { label, well },
        };
    }

    // ── footer ───────────────────────────────────────────────────────────

    private static Control Footer() => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Spacing = 6,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 11, 0, 0),
        Children =
        {
            new Button
            {
                Classes = { "default" }, MinWidth = 88, Height = 23,
                Content = Bound(Keys.BImport),
            },
            new Button { MinWidth = 88, Height = 23, Content = Bound(Keys.BCancel) },
        },
    };

    // ── shared pieces ────────────────────────────────────────────────────

    private static FontFamily Mono => new("Lucida Console, Consolas, monospace");

    private static IBrush Ink(string hex) => new SolidColorBrush(Color.Parse(hex));

    private static TextBlock Caption(string key, double gap)
    {
        var tb = Bound(key);
        tb.FontSize = 10;
        tb.FontWeight = FontWeight.Bold;
        tb.Margin = new Thickness(0, 0, 0, gap);
        return tb;
    }

    private static TextBlock Dense(string text, string? hex)
    {
        var tb = new TextBlock
        {
            Classes = { "dense" },
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (hex is not null) tb.Foreground = Ink(hex);
        return tb;
    }

    private static TextBlock Bound(string key)
    {
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }
}
