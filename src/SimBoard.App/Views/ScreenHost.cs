using Avalonia.Controls;
using SimBoard.App.Localization;
using SimBoard.App.Views.Dialogs;
using SimBoard.App.Views.Screens;

namespace SimBoard.App.Views;

/// <summary>
/// One place that knows how every screen is reached. The screens themselves are pure
/// builders returning a <see cref="Control"/>; whether a given one fills the workspace
/// or opens as a modal is a routing decision, and it belongs here rather than smeared
/// across menu handlers.
/// </summary>
public static class ScreenHost
{
    /// <summary>Screens that live inside the centre workspace.</summary>
    public static Control? Workspace(string name) => name.ToLowerInvariant() switch
    {
        // The real editor, not the picture of one. SchematicView stays reachable as
        // "mockup" so the hi-fi reference can still be compared against.
        "schematic" or "editor" => EditorView.Build(),
        "mockup" => SchematicView.Build(),
        "breadboard" => BreadboardView.Build(),
        "pcb" => PcbView.Build(),
        "library" => LibraryView.Build(),
        "start" => StartView.Build(),

        // Two screens are the schematic with something laid over it, not scenes of their
        // own — the spec is explicit that screen 5 is "the same schematic scene plus live
        // overlays" and screen 6 floats its instruments over a schematic dimmed to .35.
        // Composing here keeps each view a single honest layer.
        "sim" => Layer(SchematicView.Build(), SimulationView.Build()),
        // The spec says opacity .35, which was measured in the HTML prototype where the
        // scene composited over a lighter base. Here the schematic's own #12161b ground
        // matches the workspace, so .35 crushes the #93a9bd strokes to almost nothing.
        // .5 reproduces what the mock actually shows: dimmed but still readable.
        "instruments" => Layer(Dim(SchematicView.Build(), 0.5), InstrumentsView.Build()),

        _ => null,
    };

    private static Control Layer(params Control[] layers)
    {
        var panel = new Panel();
        foreach (var l in layers) panel.Children.Add(l);
        return panel;
    }

    private static Control Dim(Control c, double opacity)
    {
        c.Opacity = opacity;
        c.IsHitTestVisible = false;   // the dimmed backdrop must not swallow clicks
        return c;
    }

    /// <summary>Screens the spec defines as modals over the main window.</summary>
    public static Task? Modal(Window owner, string name) => name.ToLowerInvariant() switch
    {
        "import" => ModalHost.Show(owner, Keys.NvImport, ImportDialogView.Build(), 760),
        "layers" => ModalHost.Show(owner, Keys.NvLayers, LayerManagerView.Build(), 700),
        _ => null,
    };

    /// <summary>What the four mode tabs put in the workspace.</summary>
    public static string ForMode(int mode) => mode switch
    {
        1 => "breadboard",
        2 => "pcb",
        3 => "netlist",
        _ => "schematic",
    };

    /// <summary>Every name <c>--open</c> accepts, for the screenshot-verification loop.</summary>
    public static readonly string[] Names =
    [
        "start", "schematic", "editor", "mockup", "library", "breadboard", "sim",
        "instruments", "import", "pcb", "layers", "settings",
    ];
}
