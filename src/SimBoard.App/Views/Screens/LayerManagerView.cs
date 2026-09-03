using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;

namespace SimBoard.App.Views.Screens;

/// <summary>
/// Screen 9 — Layer manager. Spec: README.md section "9 · Layer manager — `09-layer-manager.png`".
///
/// Returns the 700px modal's <em>content</em> only: the dialog frame, caption and scrim are
/// the host's job (see <c>Views/Dialogs/ModalHost.cs</c>), so this file never opens a Window.
///
/// Layout is the mock's, to the pixel: a 280px layer table over five equal buttons on the
/// left, a fixed 238px property column on the right, 10px between them.
/// </summary>
public static class LayerManagerView
{
    /// <summary>One table row exactly as the mock's fixture has it.</summary>
    private readonly record struct LayerRow(
        string Name, string Objects, string Opacity, bool Selected, bool Locked, bool Hidden);

    /// <summary>The nine rows from the spec table — Thai copy as the prototype renders it.</summary>
    private static readonly LayerRow[] Layers =
    [
        new("วงจรหลัก · Schematic",      "148", "100%", false, false, false),
        new("ราง +5V / +9V",             "22",  "100%", false, false, false),
        new("สัญญาณ · Signal",           "61",  "100%", true,  false, false),
        new("กราวด์ · Ground",           "18",  "100%", false, false, false),
        new("ป้ายเนต · Net labels",      "14",  "80%",  false, false, false),
        new("หมายเหตุ & มิติ",           "9",   "65%",  false, true,  false),
        new("ทองแดงด้านบน · Top Cu",     "96",  "100%", false, false, false),
        new("ทองแดงด้านล่าง · Bottom Cu", "74",  "55%",  false, false, true),
        new("ซิลค์สกรีน · Silkscreen",   "41",  "90%",  false, false, false),
    ];

    /// <summary>Buttons under the table. No Keys constant exists for these, so Thai is literal.</summary>
    private static readonly string[] TableButtons =
        ["เพิ่ม", "ลบ", "จัดกลุ่ม", "รวมเลเยอร์", "นำเข้าชุดเลเยอร์"];

    /// <summary>The five layer-property fields, label then value.</summary>
    private static readonly (string Label, string Value)[] Fields =
    [
        ("ชนิด", "สัญญาณ / ทองแดง"),
        ("โหมดผสาน", "ปกติ"),
        ("ใช้กับซิมูเลชัน", "ใช้"),
        ("พิมพ์ออก", "พิมพ์"),
        ("สแนป", "2.54 mm"),
    ];

    /// <summary>
    /// The layer palette the spec's implementation note supplies — the prototype left these
    /// 16 cells empty, and the note says to fill them from this list, in this order.
    /// </summary>
    private static readonly string[] Palette =
    [
        "#e8b04a", "#6fd3e0", "#5fd0a8", "#d76a5a", "#93a9bd", "#c98b4b", "#5f7fb0", "#b79ad4",
        "#48e08a", "#ff7a5f", "#8fa8bd", "#c9a227", "#3f9d5a", "#2c3f6b", "#8a6420", "#e6e6e0",
    ];

    // Literals the spec fixes for the table itself; these are workspace/list colours, not
    // chrome tokens, so they are the same under every theme — exactly as in the mock.
    private static readonly IBrush LayerSwatch = new SolidColorBrush(Color.Parse("#8fa8bd"));
    private static readonly IBrush LayerSwatchEdge = new SolidColorBrush(Color.Parse("#55666f"));
    private static readonly IBrush SelectedSwatch = new SolidColorBrush(Color.Parse("#e8b04a"));
    private static readonly IBrush RowRule = new SolidColorBrush(Color.Parse("#f1efec"));
    private static readonly IBrush SelectedRowRule = new SolidColorBrush(Color.Parse("#08204f"));
    private static readonly IBrush CellDim = new SolidColorBrush(Color.Parse("#555555"));
    private static readonly IBrush FieldLabelFg = new SolidColorBrush(Color.Parse("#2a2a2a"));
    private static readonly IBrush HintFg = new SolidColorBrush(Color.Parse("#333333"));
    private static readonly IBrush SwatchEdge = new SolidColorBrush(Color.Parse("#6a6a6a"));
    private static readonly IBrush TrackFill = new SolidColorBrush(Color.Parse("#c2beb8"));

    /// <summary>columns: eye · lock · colour · name · objects · opacity.</summary>
    private const string TableColumns = "26,26,26,*,62,54";

    /// <summary>10px cells at line-height 2.1.</summary>
    private const double RowHeight = 21;

    /// <summary>Builds the screen. Caller places the returned control.</summary>
    public static Control Build()
    {
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("*,238") };

        var left = TableColumn();
        Grid.SetColumn(left, 0);

        var right = PropertyColumn();
        right.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(right, 1);

        body.Children.Add(left);
        body.Children.Add(right);
        return body;
    }

    // ── left column: the 280px table and the five buttons under it ────────────

    private static Control TableColumn()
    {
        var list = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        list.Children.Add(TableHeader());
        foreach (var layer in Layers) list.Children.Add(TableRow(layer));

        var table = new Bevel
        {
            Classes = { "sunken" },
            Height = 280,
            ClipToBounds = true,
            Child = list,
        };

        return new StackPanel { Children = { table, TableButtonRow() } };
    }

    private static Control TableHeader()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(TableColumns) };
        Cell(grid, 0, HeaderCell(Glyph("👁", 9), centre: true, divider: true));
        Cell(grid, 1, HeaderCell(Glyph("🔒", 9), centre: true, divider: true));
        Cell(grid, 2, HeaderCell(Glyph("■", 9), centre: true, divider: true));
        Cell(grid, 3, HeaderCell(HeaderLabel(Keys.LmName), centre: false, divider: true));
        Cell(grid, 4, HeaderCell(HeaderLabel(Keys.LmObjects), centre: false, divider: true));
        Cell(grid, 5, HeaderCell(HeaderLabel(Keys.LmOpacity), centre: false, divider: false));

        // The header sits on the control face with a single shadow-coloured rule beneath it.
        var face = new Bevel { Classes = { "flat" }, Child = grid };
        return new StackPanel { Children = { face, Hairline() } };
    }

    private static Control HeaderCell(Control content, bool centre, bool divider)
    {
        // EdgeThickness stays 1 on every cell so the text baseline does not shift by a pixel
        // between the cells that carry a divider and the one that does not.
        var cell = new Bevel
        {
            EdgeThickness = 1,
            Padding = centre ? new Thickness(0, 1) : new Thickness(4, 1),
            Child = content,
        };
        if (divider) Token(cell, Bevel.RightBrushProperty, "Shad");
        return cell;
    }

    private static TextBlock HeaderLabel(string key)
    {
        var tb = Bound(key);
        tb.FontSize = 9;
        return tb;
    }

    private static Control TableRow(LayerRow layer)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(TableColumns), Height = RowHeight };
        IBrush? fg = layer.Selected ? Brushes.White : null;

        Cell(grid, 0, ToggleCell(!layer.Hidden, "👁", "·", fg));
        Cell(grid, 1, ToggleCell(layer.Locked, "🔒", "·", fg));
        Cell(grid, 2, SwatchDot(layer.Selected));
        Cell(grid, 3, RowText(layer.Name, mono: false, fg));
        Cell(grid, 4, RowText(layer.Objects, mono: true, fg ?? CellDim));
        Cell(grid, 5, RowText(layer.Opacity, mono: true, fg ?? CellDim));

        var row = new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = layer.Selected ? SelectedRowRule : RowRule,
            Child = grid,
        };
        if (layer.Selected) Token(row, Border.BackgroundProperty, "Selection");
        return row;
    }

    /// <summary>
    /// The eye and lock cells are the only live controls in the table: clicking one flips its
    /// glyph. The state lives in the closure because a screen builder owns no model.
    /// </summary>
    private static Control ToggleCell(bool on, string onGlyph, string offGlyph, IBrush? fg)
    {
        bool state = on;
        var glyph = Glyph(on ? onGlyph : offGlyph, 10);
        if (fg is not null) glyph.Foreground = fg;

        var cell = new Border { Background = Brushes.Transparent, Child = glyph };
        cell.PointerPressed += (_, _) =>
        {
            state = !state;
            glyph.Text = state ? onGlyph : offGlyph;
        };
        return cell;
    }

    private static Control SwatchDot(bool selected) => new Border
    {
        Width = 10,
        Height = 10,
        Background = selected ? SelectedSwatch : LayerSwatch,
        BorderThickness = new Thickness(1),
        BorderBrush = selected ? Brushes.White : LayerSwatchEdge,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static Control RowText(string text, bool mono, IBrush? fg)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 10,
            Margin = new Thickness(5, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (mono) tb.Classes.Add("mono");
        if (fg is not null) tb.Foreground = fg;
        return tb;
    }

    private static Control TableButtonRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*"),
            Margin = new Thickness(0, 6, 0, 0),
        };

        for (int i = 0; i < TableButtons.Length; i++)
        {
            var button = new Button
            {
                Content = new TextBlock { Text = TableButtons[i], FontSize = 10 },
                Height = 21,
                MinHeight = 0,
                FontSize = 10,
                Padding = new Thickness(2, 0),
                Margin = new Thickness(i == 0 ? 0 : 4, 0, 0, 0),
            };
            Cell(grid, i, button);
        }
        return grid;
    }

    // ── right column: 238px of layer properties, colours and the footer ───────

    private static Control PropertyColumn()
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };

        var props = PropertyBox();
        Grid.SetRow(props, 0);

        var colours = ColourBox();
        colours.Margin = new Thickness(0, 7, 0, 0);
        Grid.SetRow(colours, 1);

        var footer = Footer();
        footer.Margin = new Thickness(0, 7, 0, 0);
        Grid.SetRow(footer, 2);

        grid.Children.Add(props);
        grid.Children.Add(colours);
        grid.Children.Add(footer);
        return grid;
    }

    private static Control PropertyBox()
    {
        var stack = new StackPanel();
        stack.Children.Add(BoxTitle(Keys.LmProps));
        foreach (var (label, value) in Fields) stack.Children.Add(FieldRow(label, value));
        stack.Children.Add(OpacityRow());

        return new Bevel { Classes = { "sunken", "face" }, Padding = new Thickness(8, 7), Child = stack };
    }

    private static Control FieldRow(string label, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("66,*"),
            Margin = new Thickness(0, 0, 0, 4),
        };

        var caption = new TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = FieldLabelFg,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Cell(grid, 0, caption);

        // A hand-built combo, not the ComboBox style: the spec pins this field at 18px with
        // a 14x16 stub, where the shared control is 19px with a 15px stub.
        var inner = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        Cell(inner, 0, new TextBlock
        {
            Classes = { "mono" },
            Text = value,
            FontSize = 10,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        Cell(inner, 1, new Bevel
        {
            Classes = { "raised" },
            Width = 14,
            Height = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Child = Glyph("▼", 6),
        });

        var field = new Bevel
        {
            Classes = { "sunken" },
            Height = 18,
            Margin = new Thickness(6, 0, 0, 0),
            Child = inner,
        };
        Cell(grid, 1, field);
        return grid;
    }

    private static Control OpacityRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("66,*,30"),
            Margin = new Thickness(0, 6, 0, 0),
        };

        var label = Bound(Keys.LmOpacity);
        label.FontSize = 10;
        Cell(grid, 0, label);

        var track = new Bevel
        {
            Classes = { "sunken" },
            Height = 4,
            Background = TrackFill,
            VerticalAlignment = VerticalAlignment.Center,
        };

        // 76 : 24 star columns put the 9px thumb's left edge at 76% of the track, which is
        // what `left:76%` does in the mock without needing a measured pixel width.
        var rail = new Grid { ColumnDefinitions = new ColumnDefinitions("76*,9,24*") };
        Cell(rail, 1, new Bevel
        {
            Classes = { "raised" },
            Height = 16,
            VerticalAlignment = VerticalAlignment.Center,
        });

        Cell(grid, 1, new Panel
        {
            Height = 18,
            Margin = new Thickness(6, 0, 6, 0),
            Children = { track, rail },
        });

        Cell(grid, 2, new TextBlock
        {
            Classes = { "mono" },
            Text = "80%",
            FontSize = 10,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return grid;
    }

    private static Control ColourBox()
    {
        var stack = new StackPanel();
        stack.Children.Add(BoxTitle(Keys.LmColor));

        var swatches = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };

        for (int i = 0; i < Palette.Length; i++)
        {
            // The amber is the selected layer's colour, so it carries the mock's current-swatch ring.
            bool current = i == 0;
            var cell = new Border
            {
                Height = 15,
                Background = new SolidColorBrush(Color.Parse(Palette[i])),
                BorderThickness = new Thickness(current ? 2 : 1),
                BorderBrush = current ? Brushes.Black : SwatchEdge,
                Margin = new Thickness(i % 8 == 0 ? 0 : 3, i < 8 ? 0 : 3, 0, 0),
            };
            Grid.SetColumn(cell, i % 8);
            Grid.SetRow(cell, i / 8);
            swatches.Children.Add(cell);
        }
        stack.Children.Add(swatches);

        var hint = Bound(Keys.LmHint);
        hint.Classes.Add("dense");
        hint.Foreground = HintFg;
        hint.TextWrapping = TextWrapping.Wrap;
        hint.LineHeight = 14.4;
        hint.VerticalAlignment = VerticalAlignment.Top;
        hint.Margin = new Thickness(0, 8, 0, 0);
        stack.Children.Add(hint);

        return new Bevel { Classes = { "sunken", "face" }, Padding = new Thickness(8, 7), Child = stack };
    }

    private static Control Footer()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        Cell(grid, 0, new Button
        {
            Classes = { "default" },
            Height = 23,
            MinHeight = 0,
            Margin = new Thickness(0, 0, 5, 0),
            Content = Bound(Keys.BOk),
        });
        Cell(grid, 1, new Button
        {
            Height = 23,
            MinHeight = 0,
            Content = Bound(Keys.BCancel),
        });
        return grid;
    }

    // ── shared pieces ────────────────────────────────────────────────────────

    private static Control BoxTitle(string key)
    {
        var tb = Bound(key);
        tb.FontSize = 10;
        tb.FontWeight = FontWeight.Bold;
        tb.VerticalAlignment = VerticalAlignment.Top;
        tb.Margin = new Thickness(0, 0, 0, 5);
        return tb;
    }

    private static TextBlock Glyph(string text, double size) => new()
    {
        Text = text,
        FontSize = size,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>A one-pixel rule in the theme's shadow colour.</summary>
    private static Control Hairline()
    {
        var rule = new Bevel { EdgeThickness = 0, Height = 1 };
        Token(rule, Bevel.BackgroundProperty, "Shad");
        return rule;
    }

    private static void Cell(Grid grid, int column, Control content)
    {
        Grid.SetColumn(content, column);
        grid.Children.Add(content);
    }

    /// <summary>
    /// Binds one property to a chrome token. Chrome.axaml keeps DynamicResource in XAML, but
    /// the two tokens this screen needs — the divider shadow and the selection fill — have no
    /// style class of their own, and a literal here would survive a live theme swap.
    /// </summary>
    private static void Token(Control target, AvaloniaProperty property, string key)
    {
        IObservable<object?> token = target.GetResourceObservable(key);
        target.Bind(property, token);
    }

    private static TextBlock Bound(string key)
    {
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }
}
