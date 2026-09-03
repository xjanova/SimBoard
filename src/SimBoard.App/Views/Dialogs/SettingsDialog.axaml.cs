using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;

namespace SimBoard.App.Views.Dialogs;

public partial class SettingsDialog : Window
{
    private static readonly string[] TabKeys =
        [Keys.SetGen, Keys.SetLang, Keys.SetThemeTab, Keys.SetSimTab, Keys.SetBoard, Keys.SetKeys];

    /// <summary>Card order and copy for the seven themes, straight from the spec.</summary>
    private static readonly (ChromeTheme Theme, string Name, string Th, string En)[] Themes =
    [
        (ChromeTheme.Aqua, "Aqua 3D · Liquid", "เจลใส 3 มิติ ปุ่มมันวาว", "Glossy 3D gel"),
        (ChromeTheme.Me, "Windows ME", "ขอบสองโทน คลาสสิก", "Two-tone bevel, classic"),
        (ChromeTheme.Xp, "Windows XP · Luna", "ฟ้าน้ำเงิน ขอบมน", "Luna blue, rounded"),
        (ChromeTheme.Silver, "Windows XP · Silver", "เทาเงิน โทนสุภาพ", "Neutral silver"),
        (ChromeTheme.Mac, "Macintosh · Platinum", "ลายทางละเอียด ตัวอักษรดำ", "Pinstripe, black caption"),
        (ChromeTheme.Macos, "macOS · Sonoma", "เส้นบาง ไฟจราจร", "Hairline, traffic lights"),
        (ChromeTheme.Classic, "Classic 2000", "แบนเรียบ สีเดียว", "Flat, single colour"),
    ];

    private readonly List<Button> _tabs = [];

    /// <summary>Edits are staged here so Cancel really cancels — Apply is what commits.</summary>
    private ChromeTheme _pendingTheme;
    private Lang _pendingLang;
    private readonly ChromeTheme _themeOnOpen;
    private readonly Lang _langOnOpen;

    public SettingsDialog()
    {
        AvaloniaXamlLoader.Load(this);

        _themeOnOpen = _pendingTheme = AppState.Current.Theme;
        _langOnOpen = _pendingLang = AppState.Current.Lang;

        BuildTabs();
        ShowTab(AppState.Current.SettingsTab);

        this.FindControl<Button>("CloseBtn")!.Click += (_, _) => Revert();
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) => Revert();
        this.FindControl<Button>("ApplyBtn")!.Click += (_, _) => Commit();
        this.FindControl<Button>("OkBtn")!.Click += (_, _) => { Commit(); Close(); };

        this.FindControl<Bevel>("CaptionArea")!.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };
    }

    private void Commit()
    {
        AppState.Current.Theme = _pendingTheme;
        AppState.Current.Lang = _pendingLang;
    }

    private void Revert()
    {
        AppState.Current.Theme = _themeOnOpen;
        AppState.Current.Lang = _langOnOpen;
        Close();
    }

    private void BuildTabs()
    {
        var host = this.FindControl<StackPanel>("TabHost")!;
        for (int i = 0; i < TabKeys.Length; i++)
        {
            int index = i;
            var b = new Button { Classes = { "tab" }, Content = Bound(TabKeys[i]) };
            b.Click += (_, _) => { AppState.Current.SettingsTab = index; ShowTab(index); };
            _tabs.Add(b);
            host.Children.Add(b);
        }
    }

    private void ShowTab(int index)
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (i == index) { if (!_tabs[i].Classes.Contains("active")) _tabs[i].Classes.Add("active"); }
            else _tabs[i].Classes.Remove("active");
        }

        this.FindControl<Bevel>("TabBody")!.Child = index switch
        {
            1 => LanguageTab(),
            2 => AppearanceTab(),
            _ => NotBuiltYet(TabKeys[index]),
        };
    }

    // ── Language ─────────────────────────────────────────────────────────

    private Control LanguageTab()
    {
        var left = new StackPanel { Spacing = 7 };
        left.Children.Add(Caption(Keys.SetLangQ));

        foreach (var (lang, label) in new (Lang?, string)[]
                 {
                     (Lang.Th, "ไทย (Thai) — ค่าเริ่มต้น"),
                     (Lang.En, "English (United States)"),
                     (null, "ไทย + English (แสดงคู่กัน)"),
                 })
        {
            var r = new RadioButton
            {
                GroupName = "lang",
                Content = new TextBlock { Text = label },
                IsChecked = lang is not null && lang == _pendingLang,
                IsEnabled = lang is not null,   // the dual-language mode is not wired yet
            };
            if (lang is { } l)
                r.IsCheckedChanged += (_, _) =>
                {
                    if (r.IsChecked == true) { _pendingLang = l; L.I.Lang = l; }
                };
            left.Children.Add(r);
        }

        left.Children.Add(new Control { Height = 4 });
        foreach (var (label, value) in new[]
                 {
                     ("ฟอนต์อินเทอร์เฟซ", "Tahoma 8 pt"), ("หน่วยความยาว", "มิลลิเมตร (mm)"),
                     ("รูปแบบตัวเลข", "1,234.56"), ("คำนำหน้าหน่วย", "k / M / µ / n"),
                     ("ปฏิทิน", "พุทธศักราช (พ.ศ.)"),
                 })
            left.Children.Add(LabelledCombo(label, value));

        left.Children.Add(new Control { Height = 4 });
        foreach (var text in new[]
                 {
                     "ใช้ชื่อเบอร์อุปกรณ์เป็นภาษาอังกฤษเสมอ",
                     "แสดงคำแนะนำเครื่องมือสองภาษา",
                     "สลับภาษาด้วยคีย์ลัด Ctrl+Shift+L",
                 })
            left.Children.Add(new CheckBox { Content = new TextBlock { Text = text }, IsChecked = true });

        return TwoColumn(left, PreviewColumn(Keys.SetNote));
    }

    // ── Appearance ───────────────────────────────────────────────────────

    private Control AppearanceTab()
    {
        var left = new StackPanel { Spacing = 7 };
        left.Children.Add(Caption(Keys.SetThemeQ));

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        for (int i = 0; i < (Themes.Length + 1) / 2; i++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < Themes.Length; i++)
        {
            var (theme, name, th, en) = Themes[i];
            var card = ThemeCard(theme, name, th, en);
            Grid.SetColumn(card, i % 2);
            Grid.SetRow(card, i / 2);
            grid.Children.Add(card);
        }
        left.Children.Add(grid);

        left.Children.Add(LabelledCombo("ขนาดตัวอักษร", "Tahoma 8 pt"));
        foreach (var key in new[] { Keys.SetThemeAnim, Keys.SetThemeShadow, Keys.SetThemeApplyAll })
            left.Children.Add(new CheckBox { Content = Bound(key), IsChecked = true });

        return TwoColumn(left, PreviewColumn(Keys.SetThemeNote));
    }

    private Control ThemeCard(ChromeTheme theme, string name, string th, string en)
    {
        var radio = new RadioButton
        {
            GroupName = "theme",
            IsChecked = theme == _pendingTheme,
            VerticalAlignment = VerticalAlignment.Center,
        };
        // Applying on selection is deliberate: a theme you cannot see is a theme you
        // cannot choose. Cancel puts back whatever was active when the dialog opened.
        radio.IsCheckedChanged += (_, _) =>
        {
            if (radio.IsChecked != true) return;
            _pendingTheme = theme;
            AppState.Current.Theme = theme;
        };

        var text = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Width = 104,
            Children =
            {
                new TextBlock
                {
                    Text = name, FontWeight = FontWeight.Bold, FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Text = L.I.Lang == Lang.Th ? th : en,
                    FontSize = 9, Foreground = new SolidColorBrush(Color.Parse("#5a5a5a")),
                    TextWrapping = TextWrapping.Wrap,
                },
            },
        };

        var card = new Bevel
        {
            Classes = { "sunken", "face" },
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(6),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children = { new ThemeThumbnail { Chrome = theme, Width = 68, Height = 44 }, radio, text },
            },
        };
        card.PointerPressed += (_, _) => radio.IsChecked = true;
        return card;
    }

    // ── shared pieces ────────────────────────────────────────────────────

    private static Control TwoColumn(Control left, Control right)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,244") };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        right.Margin = new Thickness(12, 0, 0, 0);
        g.Children.Add(left);
        g.Children.Add(right);
        return g;
    }

    /// <summary>The live preview window from the mocks — real chrome, not a picture of it.</summary>
    private static Control PreviewColumn(string noteKey)
    {
        var rows = new StackPanel { Spacing = 3 };
        foreach (var (key, value) in new[]
                 {
                     (Keys.SetPvV, "2.86 V"), (Keys.SetPvI, "60.8 µA"), (Keys.SetPvF, "1.442 kHz"),
                 })
        {
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("64,*") };
            var label = Bound(key);
            label.FontSize = 10;
            Grid.SetColumn(label, 0);
            var field = new Bevel
            {
                Classes = { "sunken" },
                Padding = new Thickness(4, 0),
                Child = new TextBlock { Classes = { "mono" }, Text = value, VerticalAlignment = VerticalAlignment.Center },
            };
            Grid.SetColumn(field, 1);
            g.Children.Add(label);
            g.Children.Add(field);
            rows.Children.Add(g);
        }

        var preview = new Bevel
        {
            Classes = { "raised" },
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    Docked(new Bevel
                    {
                        Classes = { "titlebar" },
                        Height = 17,
                        Child = Titled(Keys.SetPvTitle),
                    }, Dock.Top),
                    new Bevel
                    {
                        Classes = { "flat" },
                        Padding = new Thickness(7),
                        Child = new StackPanel
                        {
                            Spacing = 6,
                            Children =
                            {
                                Bound(Keys.SetPvBody),
                                rows,
                                new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    Spacing = 5,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    Children =
                                    {
                                        new Button { Classes = { "default" }, MinWidth = 62, Content = Bound(Keys.BOk) },
                                        new Button { MinWidth = 62, Content = Bound(Keys.BCancel) },
                                    },
                                },
                            },
                        },
                    },
                },
            },
        };

        var note = Bound(noteKey);
        note.FontSize = 9;
        note.TextWrapping = TextWrapping.Wrap;
        note.Foreground = new SolidColorBrush(Color.Parse("#5a5a5a"));

        return new StackPanel
        {
            Spacing = 8,
            Children = { Caption(Keys.SetPreview), preview, note },
        };
    }

    private static Control Docked(Control c, Dock dock) { DockPanel.SetDock(c, dock); return c; }

    private static Control Titled(string key)
    {
        var tb = Bound(key);
        tb.FontWeight = FontWeight.Bold;
        return tb;
    }

    private static Control LabelledCombo(string label, string value)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("150,*"), Margin = new Thickness(0, 0, 0, 2) };
        var t = new TextBlock { Text = label, FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(t, 0);
        var combo = new ComboBox { ItemsSource = new[] { value }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        Grid.SetColumn(combo, 1);
        g.Children.Add(t);
        g.Children.Add(combo);
        return g;
    }

    private static TextBlock Caption(string key)
    {
        var tb = Bound(key);
        tb.FontWeight = FontWeight.Bold;
        tb.Margin = new Thickness(0, 0, 0, 3);
        return tb;
    }

    private static Control NotBuiltYet(string key)
    {
        var tb = Bound(key);
        tb.HorizontalAlignment = HorizontalAlignment.Center;
        tb.VerticalAlignment = VerticalAlignment.Center;
        tb.Foreground = new SolidColorBrush(Color.Parse("#8a8a8a"));
        return tb;
    }

    private static TextBlock Bound(string key)
    {
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }
}
