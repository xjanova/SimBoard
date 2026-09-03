using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using SimBoard.App.Localization;

namespace SimBoard.App;

public enum Screen { Start, Schematic, Library, Breadboard, Sim, Instruments, Import, Pcb, Layers, Settings }

public enum ChromeTheme { Aqua, Me, Xp, Silver, Mac, Macos, Classic }

/// <summary>
/// Live UI strings. Controls bind through the indexer, so switching language rewrites
/// every label in place — the spec requires it without a restart, which rules out
/// resolving strings once at construction.
/// </summary>
public sealed class L : INotifyPropertyChanged
{
    public static L I { get; } = new();
    private Lang _lang = Lang.Th;

    public Lang Lang
    {
        get => _lang;
        set
        {
            if (_lang == value) return;
            _lang = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lang)));
        }
    }

    public string this[string key] => Strings.Get(key, _lang);

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Everything the shell needs to render itself. Language and theme are user preferences,
/// not document state — they survive opening a different project.
/// </summary>
public sealed class AppState : INotifyPropertyChanged
{
    public static AppState Current { get; } = new();

    private Screen _screen = Screen.Start;
    private ChromeTheme _theme = ChromeTheme.Aqua;   // the product default
    private int _mode;                                // 0 schematic · 1 breadboard · 2 pcb · 3 netlist
    private int _activeTool = 1;                      // wire, as in every mock
    private int _settingsTab = 2;                     // Appearance
    private bool _running;
    private bool _netLabels = true;
    private double _zoom = 1.6;
    private double _grid = 2.54;
    private string _selection = "R2";

    public Screen Screen { get => _screen; set => Set(ref _screen, value); }
    public int Mode { get => _mode; set => Set(ref _mode, value); }
    public int ActiveTool { get => _activeTool; set => Set(ref _activeTool, value); }
    public int SettingsTab { get => _settingsTab; set => Set(ref _settingsTab, value); }
    public bool Running { get => _running; set => Set(ref _running, value); }
    public bool NetLabels { get => _netLabels; set => Set(ref _netLabels, value); }
    public double Zoom { get => _zoom; set => Set(ref _zoom, value); }
    public double Grid { get => _grid; set => Set(ref _grid, value); }
    public string Selection { get => _selection; set => Set(ref _selection, value); }

    public Lang Lang
    {
        get => L.I.Lang;
        set { L.I.Lang = value; Raise(); }
    }

    public ChromeTheme Theme
    {
        get => _theme;
        set
        {
            if (_theme == value) return;
            _theme = value;
            ApplyTheme(value);
            Raise();
        }
    }

    /// <summary>
    /// Swapping the chrome is one dictionary swap — no control knows which theme is
    /// active, and none may hard-code a colour. That is the whole point of the token set.
    /// </summary>
    public static void ApplyTheme(ChromeTheme theme)
    {
        if (Application.Current is not { } app) return;
        var uri = new Uri($"avares://SimBoard.App/Themes/{theme}.axaml");
        var dict = new ResourceInclude(uri) { Source = uri };

        // Slot 0 is reserved for the chrome so later dictionaries are never clobbered.
        if (app.Resources.MergedDictionaries.Count == 0)
            app.Resources.MergedDictionaries.Add(dict);
        else
            app.Resources.MergedDictionaries[0] = dict;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Raise(name);
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
