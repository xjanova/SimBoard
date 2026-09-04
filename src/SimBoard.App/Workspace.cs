using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using SimBoard.Document;

namespace SimBoard.App;

/// <summary>
/// What a <see cref="Workspace.Changed"/> notification is about.
///
/// Without a discriminator a handler cannot tell "the user nudged a part" from "a
/// different file is open now", and both answers it can guess are wrong. Rebind the
/// canvas on every notification and <c>SchematicCanvas.Document</c>'s setter throws away
/// the undo stack, drops the selection and re-fits the view on every keystroke — and,
/// because that setter also raises <c>DocumentChanged</c>, a screen that forwards
/// <c>DocumentChanged</c> to <see cref="Workspace.NotifyChanged"/> loops back into itself.
/// Never rebind and a file opened from disk never reaches the screen at all, which leaves
/// the user editing an object nobody reads.
/// </summary>
public sealed class WorkspaceChangedEventArgs : EventArgs
{
    /// <summary>An edit made inside the document that is already current.</summary>
    public static readonly WorkspaceChangedEventArgs Edit = new(false);

    /// <summary>A different <see cref="CircuitDocument"/> instance is now current.</summary>
    public static readonly WorkspaceChangedEventArgs Replacement = new(true);

    private WorkspaceChangedEventArgs(bool replaced) => Replaced = replaced;

    /// <summary>
    /// True when <see cref="Workspace.Document"/> is a different instance than the one the
    /// receiver was last told about — a new project, or a file just opened — so whatever
    /// holds the old instance must be rebound. False is an edit inside the same instance:
    /// redraw, and keep the undo history and the zoom.
    ///
    /// Through <see cref="Workspace.Subscribe"/> this is answered per subscriber, against
    /// the instance that subscriber was last handed, so it is still true on the catch-up
    /// call after a screen re-attaches having missed a Replace, and still false when a
    /// screen re-attaches having missed nothing. Read straight off <see cref="Workspace.Changed"/>
    /// it is answered per notification: true only for a <see cref="Workspace.Replace"/>
    /// that actually swapped the instance.
    /// </summary>
    public bool Replaced { get; }
}

/// <summary>
/// The one circuit the whole app is looking at.
///
/// Before this existed each mode tab built its own document: <c>ScreenHost.Workspace</c>
/// calls <c>EditorView.Build()</c>, which opened by constructing the sample circuit, so
/// clicking Breadboard and then Schematic silently threw the user's drawing away and
/// handed back the demo. A drawing must survive a context switch, so the document is
/// owned here, above the screens, and every mode tab is a projection of it rather than a
/// separate copy of it.
///
/// Naming note for whoever edits <c>ScreenHost</c>: that class already has a member
/// called <c>Workspace</c>, and member lookup beats namespace lookup, so inside
/// <c>ScreenHost.cs</c> the simple name <c>Workspace</c> binds to that method and
/// <c>Workspace.Document</c> fails to compile (CS0119). Write
/// <c>SimBoard.App.Workspace.Document</c> there. Everywhere else the simple name is fine.
///
/// Thread affinity: every mutation in this app happens on the UI thread (edits come out
/// of pointer and key handlers on <c>SchematicCanvas</c>), so <see cref="Changed"/> is
/// raised on the caller's thread with no dispatcher marshalling, and neither the lazy
/// seed of <see cref="Document"/> nor the re-entrancy state below is locked. If a
/// background loader ever calls <see cref="Replace"/>, it must post to the UI thread first.
///
/// Re-entrancy, stated next to the thread note because the two are the same question
/// asked twice: a handler that announces a change of its own does not recurse. The nested
/// notification is coalesced and replayed once after the outer fan-out unwinds — see
/// <see cref="Raise"/> for why that rather than dropping it.
/// </summary>
public static class Workspace
{
    // Seeded on first read rather than in a field initializer. PartCatalog.Require throws
    // KeyNotFoundException for a key that is not in the catalogue, and a throw out of a
    // static field initializer comes back as TypeInitializationException with the real
    // stack buried — and poisons the type for the life of the process, so every later
    // read of Workspace.Document rethrows the same wrapper rather than the real fault.
    // Lazily, the throw lands on whoever asked for the document, with its own stack, once.
    private static CircuitDocument? _document;

    /// <summary>The document every screen reads. Never null; a "new project" is an empty one.</summary>
    public static CircuitDocument Document => _document ??= SampleProject();

    /// <summary>
    /// Raised after the document changes — replaced wholesale, or edited in place and
    /// announced through <see cref="NotifyChanged"/>.
    ///
    /// The <c>EventArgs</c> is always a <see cref="WorkspaceChangedEventArgs"/>; read
    /// <see cref="WorkspaceChangedEventArgs.Replaced"/> to tell those two apart:
    /// <code>
    /// Workspace.Changed += (_, e) =>
    /// {
    ///     if (e is WorkspaceChangedEventArgs { Replaced: true }) Rebind(); else Redraw();
    /// };
    /// </code>
    /// The event keeps the non-generic <see cref="EventHandler"/> shape only so a caller
    /// that unhooks by hand can hold one delegate instance and hand it back to <c>-=</c>.
    /// No screen does that today — every one of them goes through <see cref="Subscribe"/>,
    /// which is typed and which unhooks itself. New code should use Subscribe; this event
    /// is the escape hatch for a listener that is not a <see cref="Control"/>.
    /// </summary>
    public static event EventHandler? Changed;

    /// <summary>
    /// Installs a different document — a new project, or one just opened from disk.
    ///
    /// Reseeds the id counter because a loaded document arrives with ids this counter
    /// never issued, and the next placed part would otherwise take an id that already
    /// belongs to something, which quietly breaks undo. It is idempotent, so calling it
    /// on a document <c>ProjectFile.Load</c> already reseeded costs nothing.
    ///
    /// The <see cref="Changed"/> that follows carries
    /// <see cref="WorkspaceChangedEventArgs.Replaced"/> = true whenever this really did
    /// swap the instance — that is a screen's signal to rebind its canvas, and so to reset
    /// its undo history, since the old commands address parts that are no longer here.
    /// The guard to write, exactly:
    /// <code>
    /// Workspace.Subscribe(canvas, (_, e) =>
    /// {
    ///     if (e.Replaced &amp;&amp; !ReferenceEquals(canvas.Document, SimBoard.App.Workspace.Document))
    ///         canvas.Document = SimBoard.App.Workspace.Document;
    ///     else
    ///         canvas.InvalidateVisual();
    /// });
    /// </code>
    /// The ReferenceEquals half is not belt-and-braces: <c>SchematicCanvas.Document</c>'s
    /// setter does not check for the value it already holds, so assigning the current
    /// instance back still allocates a fresh <c>EditHistory</c>, clears the selection,
    /// re-fits and raises <c>DocumentChanged</c>.
    /// </summary>
    public static void Replace(CircuitDocument doc)
    {
        // A null document would reach the canvas and fault it on the next render, far
        // from whoever passed it in.
        ArgumentNullException.ThrowIfNull(doc);

        // Compared before the assignment so Replaced means what it says. Re-installing the
        // instance that is already current is a legitimate way to reseed ids and ask for a
        // redraw, and there is nothing for anyone to rebind in that case.
        var replaced = !ReferenceEquals(_document, doc);

        doc.ReseedIds();
        _document = doc;
        Raise(replaced);
    }

    /// <summary>
    /// Announces an edit made in place, so the other projections redraw. The notification
    /// carries <see cref="WorkspaceChangedEventArgs.Replaced"/> = false: nobody rebinds,
    /// so nobody loses an undo stack to a keystroke.
    /// </summary>
    public static void NotifyChanged() => Raise(replaced: false);

    /// <summary>Starts a genuinely empty sheet — no sample, nothing placed.</summary>
    public static void NewProject(string title = "untitled") =>
        Replace(new CircuitDocument { Title = title });

    /// <summary>
    /// Subscribes <paramref name="onChanged"/> for exactly as long as
    /// <paramref name="owner"/> is in a live visual tree.
    ///
    /// Why not a plain <c>Changed += …</c>: the screens are rebuilt on every mode-tab
    /// switch and the old control is simply dropped (<c>MainWindow.ShowInWorkspace</c>
    /// assigns <c>host.Child</c>). A static event holding a handler that closes over a
    /// discarded screen keeps that whole control tree alive forever, and after a few tab
    /// switches one edit fans out to a crowd of dead handlers rebuilding panels nobody
    /// can see.
    ///
    /// Why the visual tree rather than an <see cref="IDisposable"/> the caller must
    /// remember: nothing in this app disposes a screen — there is no disposal path at
    /// all — so a token that only unhooks on Dispose would leak in practice. Binding to
    /// Attached/Detached means the correct thing happens when the caller does nothing,
    /// and ignoring the return value is safe. Dispose still works, ends the subscription
    /// permanently, and releases the owner's hold on the token.
    ///
    /// The handler runs once on each attach, so a screen that was off-screen while the
    /// document changed comes back current — and once inside this call if
    /// <paramref name="owner"/> is already attached, which is what makes subscribing from
    /// a post-load hook such as <c>Window.Opened</c> work rather than silently hook
    /// nothing. Either way it can run before the first layout pass, so read the model in
    /// it, never <c>Bounds</c>; and when it runs from inside this call, whatever the
    /// caller was going to do with the return value has not happened yet.
    /// </summary>
    public static IDisposable Subscribe(Control owner, EventHandler<WorkspaceChangedEventArgs> onChanged)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(onChanged);
        return new VisualLifetime(owner, onChanged);
    }

    /// <summary>
    /// The LED driver the editor has always opened with: a starting sample, not user
    /// data. Kept as a factory so "new project" can be genuinely empty and this stays
    /// reachable. A missing catalogue key therefore throws where this is called rather
    /// than inside a type initializer — a property the lazy <see cref="Document"/> seed
    /// above is what actually delivers; as a field initializer it would have been the type
    /// initializer.
    /// </summary>
    public static CircuitDocument SampleProject()
    {
        var doc = new CircuitDocument { Title = "led-driver" };
        var v1 = doc.Place(PartCatalog.Require("VPULSE"), new GridPoint(2, 6));
        var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(10, 3));
        var led = doc.Place(PartCatalog.Require("LED"), new GridPoint(20, 3));
        var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(10, 16));
        // A pulse rather than a flat supply: the scope has to have something to draw,
        // and a blinking LED is the circuit everyone builds first anyway.
        v1.Value = "PULSE(0 5 0 10u 10u 400u 1m)";
        r1.Value = "330";

        SampleWire(doc, v1, "1", r1, "1");
        SampleWire(doc, r1, "2", led, "1");
        SampleWire(doc, led, "2", gnd, "1");
        SampleWire(doc, v1, "2", gnd, "1");
        return doc;
    }

    private static void SampleWire(CircuitDocument doc, PartInstance a, string aPin, PartInstance b, string bPin)
    {
        var pinA = a.Definition.PinByNumber(aPin)!;
        var pinB = b.Definition.PinByNumber(bPin)!;
        var (pa, pb) = (a.PinAt(pinA), b.PinAt(pinB));
        var ea = Escape(pa, pinA.Side);
        var eb = Escape(pb, pinB.Side);
        var corner = pinA.Side is PinSide.Left or PinSide.Right
            ? new GridPoint(ea.X, eb.Y) : new GridPoint(eb.X, ea.Y);

        // All four segments unconditionally, including any that comes out zero-length:
        // Connect issues an id per call, and skipping one would renumber every wire
        // after it — which is what saved probes and net names are pinned to.
        doc.Connect(pa, ea);
        doc.Connect(ea, corner);
        doc.Connect(corner, eb);
        doc.Connect(eb, pb);

        static GridPoint Escape(GridPoint p, PinSide s) => s switch
        {
            PinSide.Left => p.Offset(-2, 0),
            PinSide.Right => p.Offset(2, 0),
            PinSide.Top => p.Offset(0, -2),
            _ => p.Offset(0, 2),
        };
    }

    // ── notification ─────────────────────────────────────────────────────

    private static bool _raising;
    private static bool _pending;
    private static bool _pendingReplaced;

    // A handler that announces a change every time it is notified is an infinite loop by
    // construction. Stop after this many replays and say so, rather than spinning forever
    // or swallowing the tail of the burst in silence.
    private const int MaxReplays = 8;

    /// <summary>
    /// Fans the notification out to <see cref="Changed"/>, once, however deeply the
    /// handlers announce changes of their own.
    ///
    /// Without the guard a handler that normalises the document and announces it — an
    /// auto-labelling refresh, say, or a screen that maps <c>SchematicCanvas.DocumentChanged</c>
    /// back to <see cref="NotifyChanged"/> — re-enters here and recurses until the process
    /// dies of a StackOverflowException, which cannot be caught, logged or reported.
    /// <c>AppState</c>, the house style this file follows, never hits this because
    /// <c>Set&lt;T&gt;</c> short-circuits on an equal value; there is no value to compare
    /// here, so the break in the cycle has to be explicit.
    ///
    /// Nested notifications are coalesced and replayed once after the outer fan-out
    /// unwinds rather than dropped: a nested <see cref="Replace"/> that got dropped would
    /// leave every screen bound to the document that was just closed, which is exactly the
    /// silent data loss this file exists to prevent. <c>Replaced</c> is sticky across the
    /// burst, because a replacement anywhere in it means everyone must rebind.
    /// </summary>
    private static void Raise(bool replaced)
    {
        if (_raising)
        {
            _pending = true;
            _pendingReplaced |= replaced;
            return;
        }

        _raising = true;
        try
        {
            for (var replay = 0; ; replay++)
            {
                // Read the field once, so a handler that unsubscribes while the list is
                // being walked cannot null it out underneath us.
                Changed?.Invoke(null, replaced
                    ? WorkspaceChangedEventArgs.Replacement
                    : WorkspaceChangedEventArgs.Edit);

                if (!_pending) break;
                if (replay == MaxReplays)
                    throw new InvalidOperationException(
                        $"Workspace.Changed did not settle after {MaxReplays} replays: a handler " +
                        "announces a change every time it is notified.");

                _pending = false;
                replaced = _pendingReplaced;
                _pendingReplaced = false;
            }
        }
        finally
        {
            // Cleared in finally as well as on the way out of the loop: a handler that
            // throws must not leave the event permanently muted for the rest of the run.
            _raising = false;
            _pending = false;
            _pendingReplaced = false;
        }
    }

    /// <summary>
    /// Holds one handler on <see cref="Changed"/> only while its owner is attached.
    /// Until Dispose the token is referenced solely by the owner's own events, so when the
    /// owner is dropped both go together without anyone calling Dispose.
    /// </summary>
    private sealed class VisualLifetime : IDisposable
    {
        private readonly EventHandler<WorkspaceChangedEventArgs> _handler;

        // The three delegates are kept in fields because -= only removes the same instance,
        // and Dispose has to be able to remove all three. A caller that disposes to stop
        // listening while deliberately keeping the control alive would otherwise leave this
        // token — and everything _handler closes over — reachable from the owner's own
        // event lists for the owner's whole life: the subscription would end, the memory
        // would not. Naming VisualTreeAttachmentEventArgs is the price of that — an
        // anonymous lambda cannot be handed back to -=.
        private readonly EventHandler<VisualTreeAttachmentEventArgs> _onAttached;
        private readonly EventHandler<VisualTreeAttachmentEventArgs> _onDetached;
        private readonly EventHandler _bridge;

        private Control? _owner;

        // The document instance this owner was last told about. Replaced is answered
        // against this rather than against the raising call, because the question a
        // subscriber actually has is "is this a different document than the one I am bound
        // to". A screen detached across a Replace has to be told to rebind on re-attach
        // even though the Replace itself was raised while it was not listening; a screen
        // that missed nothing must not be told to rebind, or a mode-tab switch would cost
        // it its undo history.
        private CircuitDocument? _seen;

        private bool _live;
        private bool _disposed;

        internal VisualLifetime(Control owner, EventHandler<WorkspaceChangedEventArgs> handler)
        {
            _owner = owner;
            _handler = handler;
            _onAttached = (_, _) => Start();
            _onDetached = (_, _) => Stop();
            _bridge = (_, _) => Deliver();

            owner.AttachedToVisualTree += _onAttached;
            owner.DetachedFromVisualTree += _onDetached;

            // AttachedToVisualTree fires on the *next* attach, so an owner that is already
            // in a tree would hook nothing at all and never update — no exception, no
            // warning, and a call site that looks identical to a correct one. Subscribing
            // from a post-load hook (a Window's Opened, or the late wiring ImportDialogView
            // does) is exactly that case. Start is idempotent through _live, so the real
            // attach later costs nothing.
            if (owner.GetVisualRoot() is not null) Start();
        }

        private void Start()
        {
            if (_disposed || _live) return;   // _live guards a re-attach without a detach
            _live = true;
            Changed += _bridge;
            Deliver();                        // catch up on anything missed while hidden
        }

        private void Stop()
        {
            if (!_live) return;
            _live = false;
            Changed -= _bridge;
        }

        private void Deliver()
        {
            var doc = Document;
            var replaced = !ReferenceEquals(_seen, doc);
            _seen = doc;
            _handler(null, replaced
                ? WorkspaceChangedEventArgs.Replacement
                : WorkspaceChangedEventArgs.Edit);
        }

        public void Dispose()
        {
            _disposed = true;
            Stop();

            if (_owner is { } owner)
            {
                owner.AttachedToVisualTree -= _onAttached;
                owner.DetachedFromVisualTree -= _onDetached;
                _owner = null;   // so a second Dispose is a no-op rather than a double -=
            }

            GC.SuppressFinalize(this);   // no finalizer; this is only to satisfy CA1816
        }
    }
}
