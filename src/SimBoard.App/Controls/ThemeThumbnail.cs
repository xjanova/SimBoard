using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;

namespace SimBoard.App.Controls;

/// <summary>
/// A live 84px preview of one chrome theme: a miniature title bar, a sunken field and a
/// raised button, drawn from <em>that theme's own tokens</em> rather than the active one.
///
/// It works because a control's own Resources shadow the application's for everything
/// beneath it — so merging one theme dictionary here re-points every DynamicResource in
/// the subtree without touching the rest of the window. That also means the card is never
/// a stale screenshot: change a token and every thumbnail updates with the product.
/// </summary>
public class ThemeThumbnail : Decorator
{
    public static readonly StyledProperty<ChromeTheme> ChromeProperty =
        AvaloniaProperty.Register<ThemeThumbnail, ChromeTheme>(nameof(Chrome));

    /// <summary>Named Chrome, not Theme: StyledElement.Theme is Avalonia's ControlTheme.</summary>
    public ChromeTheme Chrome
    {
        get => GetValue(ChromeProperty);
        set => SetValue(ChromeProperty, value);
    }

    public ThemeThumbnail()
    {
        Width = 84;
        Height = 52;
        Child = Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChromeProperty) ApplyOwnTheme();
    }

    private void ApplyOwnTheme()
    {
        var uri = new Uri($"avares://SimBoard.App/Themes/{Chrome}.axaml");
        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(new ResourceInclude(uri) { Source = uri });
    }

    private static Control Build()
    {
        var titleBar = new Bevel
        {
            Classes = { "titlebar" },
            Height = 13,
            Padding = new Thickness(3, 0),
            Child = new TextBlock
            {
                Text = "SimBoard",
                FontSize = 7,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        DockPanel.SetDock(titleBar, Dock.Top);

        var field = new Bevel
        {
            Classes = { "sunken" },
            Height = 12,
            Margin = new Thickness(0, 0, 0, 4),
            Padding = new Thickness(3, 0),
            Child = new TextBlock { Text = "47 k", FontSize = 7, VerticalAlignment = VerticalAlignment.Center },
        };

        var button = new Bevel
        {
            Classes = { "raised" },
            Height = 14,
            Width = 34,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = "OK",
                FontSize = 7,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var body = new Bevel
        {
            Classes = { "flat" },
            Padding = new Thickness(4),
            Child = new StackPanel { Children = { field, button } },
        };

        return new Bevel
        {
            Classes = { "raised" },
            Child = new DockPanel
            {
                LastChildFill = true,
                Children = { titleBar, body },
            },
        };
    }
}
