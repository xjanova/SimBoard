using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;

namespace SimBoard.App.Views;

public partial class MainWindow : Window
{
    private static readonly string[] MenuKeys =
        [
            Keys.MFile, Keys.MEdit, Keys.MView, Keys.MPlace, Keys.MSim,
            Keys.MTools, Keys.MBoard, Keys.MWin, Keys.MHelp,
        ];

    // select · wire · junction · bus · net-label · place-part · probe · text · dimension
    private static readonly string[] ToolGlyphs = ["✛", "⌁", "⊹", "⌗", "⎋", "⧉", "⌖", "T", "⟂"];

    private static readonly string[] ModeKeys = [Keys.TbSchem, Keys.TbBread, Keys.TbPcb, Keys.TbSpice];

    private readonly List<Button> _tools = [];
    private readonly List<Button> _modeTabs = [];

    public MainWindow()
    {
        // Set before the tree is built: BuildToolbar reads state to decide which tool
        // is latched, and an assignment made by the caller lands too late for that.
        DataContext = AppState.Current;
        AvaloniaXamlLoader.Load(this);
        BuildMenu();
        BuildToolbar();
        BuildModeTabs();
        BuildStatusBar();
        WireWindowButtons();
        KeyDown += OnKeyDown;

        // Deterministic entry for screenshot verification: `--open settings` opens that
        // dialog once the window is up. Hunting menu coordinates from a screen capture
        // breaks every time the window lands somewhere new, and silently verifies the
        // wrong thing when the click misses.
        var args = Environment.GetCommandLineArgs();
        var openIndex = Array.IndexOf(args, "--open");
        if (openIndex >= 0 && openIndex + 1 < args.Length)
        {
            var what = args[openIndex + 1];
            Opened += (_, _) => Open(what);
        }
        else
        {
            Opened += (_, _) => ShowInWorkspace("schematic");
        }
    }

    /// <summary>Routes a screen name to wherever the spec says that screen lives.</summary>
    private void Open(string name)
    {
        if (name.Equals("settings", StringComparison.OrdinalIgnoreCase)) { OpenSettings(); return; }
        if (ScreenHost.Modal(this, name) is not null) return;
        ShowInWorkspace(name);
    }

    private void ShowInWorkspace(string name)
    {
        var host = this.FindControl<Bevel>("WorkspaceHost");
        if (host is null) return;
        host.Child = ScreenHost.Workspace(name) ?? Placeholder(name);
    }

    private AppState State => (AppState)DataContext!;

    private void BuildMenu()
    {
        var host = this.FindControl<StackPanel>("MenuHost")!;
        foreach (var key in MenuKeys)
        {
            var b = new Button { Classes = { "menu" }, Content = Bound(key) };

            // Until real dropdowns land, each menu opens the screen it owns. Every screen
            // in the spec is reachable from the menu bar, which is what "clickable" means
            // before the behaviour behind it exists.
            var target = key switch
            {
                var k when k == Keys.MFile => "start",
                var k when k == Keys.MView => "library",
                var k when k == Keys.MPlace => "library",
                var k when k == Keys.MSim => "sim",
                var k when k == Keys.MTools => "settings",
                var k when k == Keys.MBoard => "import",
                var k when k == Keys.MWin => "instruments",
                var k when k == Keys.MEdit => "layers",
                _ => null,
            };
            if (target is not null) b.Click += (_, _) => Open(target);
            host.Children.Add(b);
        }
    }

    private void BuildToolbar()
    {
        var host = this.FindControl<StackPanel>("ToolbarHost")!;

        // group 1 — file
        foreach (var (glyph, tip) in new[] { ("🗋", Keys.BNew), ("🗀", Keys.BOpen), ("🖫", Keys.BImport) })
            host.Children.Add(new Button { Classes = { "tool" }, Content = glyph });
        host.Children.Add(Divider());

        // group 2 — the nine editing tools; exactly one is latched at a time
        for (int i = 0; i < ToolGlyphs.Length; i++)
        {
            int index = i;
            var b = new Button { Classes = { "tool" }, Content = ToolGlyphs[i] };
            b.Click += (_, _) => { State.ActiveTool = index; RefreshTools(); };
            _tools.Add(b);
            host.Children.Add(b);
        }
        host.Children.Add(Divider());

        // group 3 — simulation transport
        foreach (var (key, cls) in new[] { (Keys.BRun, "play"), (Keys.BPause, ""), (Keys.BStop, ""), (Keys.BStep, "") })
        {
            var b = new Button { Content = Bound(key), Padding = new Thickness(8, 2) };
            if (cls == "play") b.Click += (_, _) => { State.Running = !State.Running; RefreshTransport(); };
            if (key == Keys.BStop) b.Click += (_, _) => { State.Running = false; RefreshTransport(); };
            b.Tag = key;
            host.Children.Add(b);
        }
        host.Children.Add(Divider());

        // group 4 — grid and zoom
        host.Children.Add(Combo(Keys.TbGrid, "2.54 mm"));
        host.Children.Add(Combo(Keys.TbZoom, "160%"));

        RefreshTools();
        RefreshTransport();
    }

    private Control Combo(string labelKey, string value) => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center,
        Spacing = 4,
        Margin = new Thickness(4, 0),
        Children =
        {
            Bound(labelKey),
            new Bevel
            {
                Classes = { "sunken", "face" },
                Padding = new Thickness(5, 1),
                Child = new TextBlock { Classes = { "mono" }, Text = value },
            },
        },
    };

    private static Control Divider() => new Bevel
    {
        Classes = { "vdivider" },
        Margin = new Thickness(3, 4, 5, 4),
    };

    private void BuildModeTabs()
    {
        var host = this.FindControl<StackPanel>("ModeTabHost")!;
        for (int i = 0; i < ModeKeys.Length; i++)
        {
            int index = i;
            var b = new Button { Classes = { "tab" }, Content = Bound(ModeKeys[i]) };
            b.Click += (_, _) => { State.Mode = index; RefreshModeTabs(); };
            _modeTabs.Add(b);
            host.Children.Add(b);
        }
        RefreshModeTabs();
    }

    private void BuildStatusBar()
    {
        var host = this.FindControl<StackPanel>("StatusHost")!;
        string[] cells =
        [
            "พร้อม", "X 184.15  Y 92.70 mm", "กริด 2.54 mm",
            "เนต 14 · อุปกรณ์ 22", "DRC 0 / ERC 0", "เลือกอยู่: R2", "SPICE3f5 · TRAN",
        ];
        foreach (var text in cells)
            host.Children.Add(new Bevel
            {
                Classes = { "statuscell" },
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
    }

    private void WireWindowButtons()
    {
        this.FindControl<Button>("MinBtn")!.Click += (_, _) => WindowState = WindowState.Minimized;
        this.FindControl<Button>("MaxBtn")!.Click += (_, _) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        this.FindControl<Button>("CloseBtn")!.Click += (_, _) => Close();

        // The retro title bar replaces the OS one, so it has to move the window itself.
        var bar = this.FindControl<Bevel>("TitleBarArea")!;
        bar.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };
    }

    private async void OpenSettings()
    {
        try { await new Dialogs.SettingsDialog().ShowDialog(this); }
        catch (InvalidOperationException) { /* already open */ }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+Shift+L is the language toggle the spec specifies.
        if (e.Key == Key.L && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            State.Lang = State.Lang == Lang.Th ? Lang.En : Lang.Th;
            e.Handled = true;
        }
        else if (e.Key == Key.OemComma && e.KeyModifiers == KeyModifiers.Control)
        {
            OpenSettings();
            e.Handled = true;
        }
        // Still handy while building screens: cycle chrome without opening the dialog.
        else if (e.Key == Key.T && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            var all = Enum.GetValues<ChromeTheme>();
            State.Theme = all[(Array.IndexOf(all, State.Theme) + 1) % all.Length];
            Title = $"SimBoard — {State.Theme}";
            e.Handled = true;
        }
    }

    private void RefreshTools()
    {
        for (int i = 0; i < _tools.Count; i++)
            SetClass(_tools[i], "latched", i == State.ActiveTool);
    }

    private void RefreshModeTabs()
    {
        for (int i = 0; i < _modeTabs.Count; i++)
            SetClass(_modeTabs[i], "active", i == State.Mode);

        // The tabs are four views of one project, so switching them swaps the workspace
        // rather than opening anything — the netlist is shared, per the spec.
        if (IsLoaded) ShowInWorkspace(ScreenHost.ForMode(State.Mode));
    }

    /// <summary>Shown for a screen that has no builder yet — never a blank panel.</summary>
    private static Control Placeholder(string name) => new TextBlock
    {
        Text = $"— {name} —",
        Foreground = new SolidColorBrush(Color.Parse("#7f97ab")),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private void RefreshTransport()
    {
        var host = this.FindControl<StackPanel>("ToolbarHost")!;
        foreach (var b in host.Children.OfType<Button>())
            if (b.Tag is Keys.BRun) SetClass(b, "latched", State.Running);
    }

    private static void SetClass(StyledElement e, string name, bool on)
    {
        if (on) { if (!e.Classes.Contains(name)) e.Classes.Add(name); }
        else e.Classes.Remove(name);
    }

    /// <summary>A label that re-reads itself when the language changes.</summary>
    private static TextBlock Bound(string key)
    {
        var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        tb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{key}]") { Source = L.I });
        return tb;
    }
}
