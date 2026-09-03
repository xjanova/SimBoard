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
        "schematic" => SchematicView.Build(),
        "sim" => SimulationView.Build(),
        "breadboard" => BreadboardView.Build(),
        "pcb" => PcbView.Build(),
        "library" => LibraryView.Build(),
        "instruments" => InstrumentsView.Build(),
        "start" => StartView.Build(),
        _ => null,
    };

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
        "start", "schematic", "library", "breadboard", "sim",
        "instruments", "import", "pcb", "layers", "settings",
    ];
}
