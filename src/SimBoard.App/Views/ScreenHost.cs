using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
        "mockup" => Mock(SchematicView.Build()),
        "breadboard" => BreadboardView.Build(),
        "pcb" => PcbView.Build(),
        "netlist" => NetlistView.Build(),

        // The design mocks stay reachable so the live screens can be compared against the
        // spec they were drawn from, the same way "mockup" keeps SchematicView.
        "breadboard-mock" => Mock(BreadboardView.BuildMock()),
        "pcb-mock" => Mock(PcbView.BuildMock()),
        "library" => LibraryView.Build(),
        "start" => StartView.Build(),

        // Two screens are the schematic with something laid over it, not scenes of their
        // own — the spec is explicit that screen 5 is "the same schematic scene plus live
        // overlays" and screen 6 floats its instruments over a schematic dimmed to .35.
        // Composing here keeps each view a single honest layer.
        "sim" => Mock(Layer(SchematicView.Build(), SimulationView.Build())),
        // The spec says opacity .35, which was measured in the HTML prototype where the
        // scene composited over a lighter base. Here the schematic's own #12161b ground
        // matches the workspace, so .35 crushes the #93a9bd strokes to almost nothing.
        // .5 reproduces what the mock actually shows: dimmed but still readable.
        "instruments" => Mock(Layer(Dim(SchematicView.Build(), 0.5), InstrumentsView.Build())),

        _ => null,
    };

    /// <summary>
    /// Marks a screen that is still the hi-fi picture of the design rather than a live
    /// view of the circuit.
    ///
    /// These screens were built to the mock first and their readings are the mock's:
    /// SimulationView alone draws eleven fixed voltages, and a value like "4.21 V" is
    /// indistinguishable from a measurement unless something says otherwise. Now that the
    /// schematic, breadboard, PCB and netlist tabs show real computed numbers, an
    /// unmarked screen of invented ones is a trap. The badge is applied here, in the one
    /// place that already knows which screens are which, so no screen has to remember to
    /// declare itself.
    /// </summary>
    private static Control Mock(Control screen)
    {
        var badge = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#3a2a12")),
            BorderBrush = new SolidColorBrush(Color.Parse("#8a6420")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(7, 3),
            Margin = new Thickness(0, 10, 14, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = "ภาพตัวอย่าง · ตัวเลขในหน้านี้เป็นของแบบร่าง ไม่ใช่ค่าที่วัดได้",   // TODO: localise
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#e8b04a")),
            },
        };

        return Layer(screen, badge);
    }

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
        "instruments", "import", "pcb", "netlist", "layers", "settings",
        "breadboard-mock", "pcb-mock",
    ];
}
