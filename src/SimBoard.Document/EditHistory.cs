namespace SimBoard.Document;

/// <summary>One reversible edit. Every change the user makes goes through one of these.</summary>
public interface IEditCommand
{
    /// <summary>What the undo menu says. Shown to the user, so it is in their language.</summary>
    string Label { get; }
    void Apply(CircuitDocument doc);
    void Revert(CircuitDocument doc);
}

/// <summary>
/// Undo/redo.
///
/// Commands rather than snapshots: a schematic with a thousand parts would make every
/// keystroke copy the whole document, and the interesting part of an edit is what changed, not what
/// stayed. A drag emits one command when the pointer is released, not one per pixel —
/// <see cref="Merge"/> folds a run of moves of the same part into a single undo step,
/// because "undo" should take back the drag, not one frame of it.
/// </summary>
public sealed class EditHistory
{
    private readonly CircuitDocument _doc;
    private readonly List<IEditCommand> _done = [];
    private readonly List<IEditCommand> _undone = [];

    public EditHistory(CircuitDocument doc) => _doc = doc;

    public event EventHandler? Changed;

    public bool CanUndo => _done.Count > 0;
    public bool CanRedo => _undone.Count > 0;
    public string? UndoLabel => _done.Count > 0 ? _done[^1].Label : null;
    public string? RedoLabel => _undone.Count > 0 ? _undone[^1].Label : null;

    /// <summary>Applies a command and records it. Anything redoable is discarded.</summary>
    public void Do(IEditCommand cmd)
    {
        cmd.Apply(_doc);
        _done.Add(cmd);
        _undone.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Records an edit that has already been applied — used by the canvas, which moves a
    /// part live and only knows the full extent of the drag once it ends.
    /// </summary>
    public void Record(IEditCommand cmd)
    {
        if (Merge(cmd)) { Changed?.Invoke(this, EventArgs.Empty); return; }
        _done.Add(cmd);
        _undone.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Folds consecutive moves of the same part so one drag is one undo step.</summary>
    private bool Merge(IEditCommand cmd)
    {
        if (cmd is not MovePart incoming || _done.Count == 0) return false;
        if (_done[^1] is not MovePart previous || previous.PartId != incoming.PartId) return false;

        _done[^1] = new MovePart(incoming.PartId, previous.From, incoming.To);
        return true;
    }

    public void Undo()
    {
        if (_done.Count == 0) return;
        var cmd = _done[^1];
        _done.RemoveAt(_done.Count - 1);
        cmd.Revert(_doc);
        _undone.Add(cmd);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (_undone.Count == 0) return;
        var cmd = _undone[^1];
        _undone.RemoveAt(_undone.Count - 1);
        cmd.Apply(_doc);
        _done.Add(cmd);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _done.Clear();
        _undone.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

// ── the commands ─────────────────────────────────────────────────────────

/// <summary>
/// Placing keeps the instance itself, not a description of it, so undo/redo restores the
/// same object — wires refer to positions, but the properties panel and selection hold
/// references, and resurrecting a copy would strand them.
/// </summary>
public sealed class PlacePart(PartInstance part) : IEditCommand
{
    public string Label => $"วาง {part.Designator}";
    public PartInstance Part => part;

    public void Apply(CircuitDocument doc)
    {
        if (!doc.Parts.Contains(part)) doc.Parts.Add(part);
    }

    public void Revert(CircuitDocument doc) => doc.Parts.Remove(part);
}

public sealed class RemovePart(PartInstance part, IReadOnlyList<Wire> touching) : IEditCommand
{
    public string Label => $"ลบ {part.Designator}";

    public void Apply(CircuitDocument doc)
    {
        doc.Parts.Remove(part);
        foreach (var w in touching) doc.Wires.Remove(w);
    }

    public void Revert(CircuitDocument doc)
    {
        doc.Parts.Add(part);
        foreach (var w in touching) doc.Wires.Add(w);
    }
}

public sealed class MovePart(string partId, GridPoint from, GridPoint to) : IEditCommand
{
    public string PartId => partId;
    public GridPoint From => from;
    public GridPoint To => to;
    public string Label => "ย้ายอุปกรณ์";

    public void Apply(CircuitDocument doc) => Set(doc, to);
    public void Revert(CircuitDocument doc) => Set(doc, from);

    private void Set(CircuitDocument doc, GridPoint p)
    {
        var part = doc.Parts.FirstOrDefault(x => x.Id == partId);
        if (part is not null) part.Position = p;
    }
}

public sealed class RotatePart(string partId, Rotation from, Rotation to) : IEditCommand
{
    public string Label => "หมุนอุปกรณ์";

    public void Apply(CircuitDocument doc) => Set(doc, to);
    public void Revert(CircuitDocument doc) => Set(doc, from);

    private void Set(CircuitDocument doc, Rotation r)
    {
        var part = doc.Parts.FirstOrDefault(x => x.Id == partId);
        if (part is not null) part.Rotation = r;
    }
}

public sealed class AddWires(IReadOnlyList<Wire> wires) : IEditCommand
{
    public string Label => wires.Count > 1 ? $"ต่อสาย {wires.Count} เส้น" : "ต่อสาย";

    public void Apply(CircuitDocument doc)
    {
        foreach (var w in wires) if (!doc.Wires.Contains(w)) doc.Wires.Add(w);
    }

    public void Revert(CircuitDocument doc)
    {
        foreach (var w in wires) doc.Wires.Remove(w);
    }
}

public sealed class RemoveWire(Wire wire) : IEditCommand
{
    public string Label => "ลบสาย";
    public void Apply(CircuitDocument doc) => doc.Wires.Remove(wire);
    public void Revert(CircuitDocument doc) => doc.Wires.Add(wire);
}

public sealed class SetValue(string partId, string? from, string? to) : IEditCommand
{
    public string Label => $"แก้ค่าเป็น {to}";

    public void Apply(CircuitDocument doc) => Set(doc, to);
    public void Revert(CircuitDocument doc) => Set(doc, from);

    private void Set(CircuitDocument doc, string? v)
    {
        var part = doc.Parts.FirstOrDefault(x => x.Id == partId);
        if (part is not null) part.Value = v;
    }
}

public sealed class SetDesignator(string partId, string from, string to) : IEditCommand
{
    public string Label => $"เปลี่ยนชื่อเป็น {to}";

    public void Apply(CircuitDocument doc) => Set(doc, to);
    public void Revert(CircuitDocument doc) => Set(doc, from);

    private void Set(CircuitDocument doc, string v)
    {
        var part = doc.Parts.FirstOrDefault(x => x.Id == partId);
        if (part is not null) part.Designator = v;
    }
}
