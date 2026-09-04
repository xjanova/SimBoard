using SimBoard.App;
using SimBoard.Document;

// ─────────────────────────────────────────────────────────────────────────────
// S8 — the shared document.
//
// Written against a defect that shipped: every mode tab rebuilt its screen, and
// EditorView.Build() opened with `var doc = SampleCircuit();`. Clicking Breadboard
// and clicking back therefore threw the user's circuit away and replaced it with
// the demo — silent data loss on an ordinary context switch, with no dialog, no
// warning and nothing in the undo stack to get it back.
//
// These checks fail if a screen ever owns a document again.
// No Avalonia control is constructed here; Subscribe needs one, and the visual-tree
// lifetime it manages is not what this spike is about.
// ─────────────────────────────────────────────────────────────────────────────

int failures = 0;

Section("A · one document, however many times it is read");
{
    var a = Workspace.Document;
    var b = Workspace.Document;
    Report(ReferenceEquals(a, b), "two reads return the same instance");
}

Section("B · an edit survives what used to destroy it");
{
    var before = Workspace.Document.Parts.Count;
    var added = Workspace.Document.Place(PartCatalog.Require("R"), new GridPoint(40, 40));
    added.Value = "4k7";

    // What a mode-tab click does now: the screen is rebuilt and asks for the document
    // again. Before the workspace owned it, this is the point at which the part above
    // stopped existing.
    var afterRebuild = Workspace.Document;

    Report(afterRebuild.Parts.Count == before + 1,
        $"a placed part is still there after the screen is rebuilt ({before} → {afterRebuild.Parts.Count})");
    Report(afterRebuild.Parts.Any(p => p.Id == added.Id && p.Value == "4k7"),
        "and it kept its identity and its value");

    Workspace.Document.Remove(added);
}

Section("C · every view reads the same circuit");
{
    var doc = Workspace.Document;
    var nets = doc.ExtractNets();
    var board = BreadboardLayout.Build(doc);
    var deck = NetlistBuilder.Build(doc, Analysis.OperatingPoint());

    // Ground is a schematic symbol with no body, so the board legitimately places
    // one fewer part than the sheet holds. Everything else must agree exactly.
    int physical = doc.Parts.Count(p =>
        !(p.Definition.Spice == SpiceKind.None && p.Definition.Symbol == SymbolShape.Ground));

    Report(board.Nets.Count == nets.Count,
        $"breadboard and schematic agree on net count ({board.Nets.Count} = {nets.Count})");
    Report(board.Parts.Count == physical,
        $"breadboard places every physical part ({board.Parts.Count} of {doc.Parts.Count}, ground excluded)");
    Report(deck.Nets.Count == nets.Count,
        $"the netlist agrees too ({deck.Nets.Count})");
}

Section("D · replacing the document tells everyone, and only then");
{
    // Listening through the raw Changed event: Subscribe is what the screens use, but it
    // binds its lifetime to a Control and this spike deliberately builds no UI.
    bool sawReplacement = false;
    EventHandler watch = (_, _) => sawReplacement = true;
    Workspace.Changed += watch;

    var replacement = new CircuitDocument { Title = "opened-file" };
    replacement.Place(PartCatalog.Require("C"), new GridPoint(0, 0));

    Workspace.Replace(replacement);
    Report(ReferenceEquals(Workspace.Document, replacement),
        "after Replace, every screen reads the opened file");
    Report(Workspace.Document.Title == "opened-file", "including its title");
    Report(sawReplacement, "and the swap was announced rather than done silently");

    Workspace.Changed -= watch;
}

Section("E · a handler that announces a change does not take the process down");
{
    // The trap: Changed -> handler edits and calls NotifyChanged -> Changed -> ...
    // A StackOverflowException cannot be caught, logged or reported; the app simply
    // vanishes. The guard has to break the cycle without dropping the nested event.
    int seen = 0;
    EventHandler? reentrant = null;
    reentrant = (_, _) =>
    {
        seen++;
        if (seen < 3) Workspace.NotifyChanged();   // the recursive case
    };

    Workspace.Changed += reentrant;
    Workspace.NotifyChanged();
    Workspace.Changed -= reentrant;

    Report(seen >= 2, $"the nested notification was delivered, not dropped (handler ran {seen}×)");
    Report(seen < 50, $"and it terminated instead of recursing ({seen} deliveries)");
}

Section("F · a fresh project is empty, not the demo");
{
    Workspace.NewProject("blank");
    Report(Workspace.Document.Parts.Count == 0,
        $"New starts with nothing on the sheet ({Workspace.Document.Parts.Count} parts)");
    Report(Workspace.Document.Title == "blank", "and takes the name it was given");

    var nets = Workspace.Document.ExtractNets();
    Report(nets.Count == 0, "an empty sheet extracts zero nets rather than throwing");

    var deck = NetlistBuilder.Build(Workspace.Document, Analysis.OperatingPoint());
    Report(deck.Deck.Contains(".end"), "and still produces a valid, if empty, deck");
    Report(!deck.CanSimulate, "which correctly reports that there is nothing to simulate");
}

Console.WriteLine();
Console.WriteLine(failures == 0
    ? "S8 — the document survives every screen rebuild."
    : $"S8 — {failures} check(s) failed.");
return failures == 0 ? 0 : 1;

void Section(string name)
{
    Console.WriteLine();
    Console.WriteLine($"── {name} ".PadRight(78, '─'));
}

void Report(bool ok, string message)
{
    if (!ok) failures++;
    Console.WriteLine($"  [{(ok ? "ok  " : "FAIL")}] {message}");
}
