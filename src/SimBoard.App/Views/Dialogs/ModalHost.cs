using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.App.Localization;

namespace SimBoard.App.Views.Dialogs;

/// <summary>
/// The retro dialog frame, supplied once so screen content does not each rebuild it.
///
/// Screens 7 (Import) and 9 (Layer manager) are modals over the main window and the spec
/// gives them identical chrome: a caption in the dialog title-bar colour, a close button,
/// a draggable bar. Their content classes return only the panel; this puts the window
/// around it.
///
/// Every themed value comes from a style class rather than a resource lookup in code —
/// DynamicResource is declarative in XAML and follows a live theme switch for free.
/// </summary>
public sealed class ModalHost : Window
{
    private ModalHost(string titleKey, Control content, double width)
    {
        Width = width;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SystemDecorations = SystemDecorations.None;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;

        var title = new TextBlock();
        title.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding($"[{titleKey}]") { Source = L.I });
        Grid.SetColumn(title, 0);

        var close = new Button { Classes = { "windowclose" } };
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);

        var caption = new Bevel
        {
            Classes = { "dialogcaption" },
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children = { title, close },
            },
        };
        DockPanel.SetDock(caption, Dock.Top);
        caption.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };

        Content = new Bevel
        {
            Classes = { "raised", "windowframe" },
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    caption,
                    new Bevel
                    {
                        Classes = { "flat" },
                        Padding = new Thickness(10),
                        Child = content,
                    },
                },
            },
        };

        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }

    /// <summary>Shows <paramref name="content"/> as a modal over <paramref name="owner"/>.</summary>
    public static Task Show(Window owner, string titleKey, Control content, double width) =>
        new ModalHost(titleKey, content, width).ShowDialog(owner);
}
