using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SimBoard.Document;

namespace SimBoard.App.Controls;

public enum EditorTool { Select, Wire, Place, Delete }

/// <summary>
/// The schematic editor surface: draws a <see cref="CircuitDocument"/> and edits it.
///
/// Everything on screen is derived from the document — there is no parallel copy of the
/// drawing. That is what makes moving a part change the netlist: the geometry IS the
/// circuit, and the simulator reads the same object the mouse just moved.
/// </summary>
public class SchematicCanvas : Control
{
    private const double MinStep = 3, MaxStep = 40;

    private CircuitDocument _doc = new();
    private double _step = 10;                 // pixels per grid step
    private Point _offset = new(40, 40);       // pixels

    private PartInstance? _selected;
    private PartInstance? _dragging;
    private GridPoint _dragGrab;               // where in the part the pointer took hold
    private Point? _panFrom;
    private GridPoint? _wireFrom;
    private GridPoint _cursor;
    private bool _pointerInside;

    /// <summary>
    /// Fit is deferred to the first frame that has a real size. Calling it from
    /// AttachedToVisualTree divides by a zero Bounds and throws the view off-screen —
    /// the canvas rendered empty and looked like a drawing bug rather than a layout one.
    /// </summary>
    private bool _needsFit = true;

    /// <summary>Raised whenever the document changes in a way the rest of the UI cares about.</summary>
    public event EventHandler? DocumentChanged;

    /// <summary>Raised when the selected part changes, so the properties panel can follow.</summary>
    public event EventHandler<PartInstance?>? SelectionChanged;

    public SchematicCanvas()
    {
        Focusable = true;
        ClipToBounds = true;

        // SizeChanged, not Render: invalidating from inside a render pass throws
        // "Visual was invalidated during the render pass". This fires after layout, when
        // Bounds is real and invalidation is legal.
        SizeChanged += (_, _) =>
        {
            if (!_needsFit || Bounds.Width <= 1 || Bounds.Height <= 1) return;
            _needsFit = false;
            ZoomToFit();
        };
    }

    public CircuitDocument Document
    {
        get => _doc;
        set { _doc = value; _selected = null; _needsFit = true; InvalidateVisual(); Raise(); }
    }

    public EditorTool Tool { get; set; } = EditorTool.Select;

    /// <summary>The part the Place tool will drop next.</summary>
    public PartDefinition? PendingPart { get; set; }

    public PartInstance? Selected
    {
        get => _selected;
        private set
        {
            if (ReferenceEquals(_selected, value)) return;
            _selected = value;
            SelectionChanged?.Invoke(this, value);
            InvalidateVisual();
        }
    }

    // ── coordinates ──────────────────────────────────────────────────────

    private Point ToPixel(GridPoint g) => new(g.X * _step + _offset.X, g.Y * _step + _offset.Y);

    private GridPoint ToGrid(Point p) => new(
        (int)Math.Round((p.X - _offset.X) / _step),
        (int)Math.Round((p.Y - _offset.Y) / _step));

    // ── rendering ────────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var bounds = new Rect(Bounds.Size);
        ctx.FillRectangle(SymbolRenderer.Body, bounds);

        DrawGrid(ctx, bounds);

        double stroke = Math.Max(1, _step * 0.16);
        var wirePen = new Pen(SymbolRenderer.Wire, stroke, lineCap: PenLineCap.Square);

        foreach (var w in _doc.Wires)
            ctx.DrawLine(wirePen, ToPixel(w.A), ToPixel(w.B));

        DrawJunctions(ctx, stroke);

        foreach (var part in _doc.Parts)
            SymbolRenderer.Draw(ctx, part, ToPixel, _step, ReferenceEquals(part, _selected), stroke);

        if (_selected is not null) DrawSelectionHandles(ctx, _selected);
        if (_wireFrom is { } from) DrawWirePreview(ctx, from, stroke);
        if (Tool == EditorTool.Place && PendingPart is not null && _pointerInside) DrawGhost(ctx, stroke);
        DrawCursorReadout(ctx);
    }

    private void DrawGrid(DrawingContext ctx, Rect bounds)
    {
        if (_step < 6) return;   // denser than this the dots merge into a haze

        var dot = new SolidColorBrush(Color.Parse("#2b3440"));
        double r = Math.Max(0.5, _step * 0.06);
        int x0 = (int)Math.Floor(-_offset.X / _step), x1 = (int)Math.Ceiling((bounds.Width - _offset.X) / _step);
        int y0 = (int)Math.Floor(-_offset.Y / _step), y1 = (int)Math.Ceiling((bounds.Height - _offset.Y) / _step);

        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                ctx.DrawEllipse(dot, null, ToPixel(new GridPoint(x, y)), r, r);
    }

    /// <summary>A filled dot where three or more wire ends meet — the classic junction mark.</summary>
    private void DrawJunctions(DrawingContext ctx, double stroke)
    {
        var count = new Dictionary<GridPoint, int>();
        foreach (var w in _doc.Wires)
        {
            count[w.A] = count.GetValueOrDefault(w.A) + 1;
            count[w.B] = count.GetValueOrDefault(w.B) + 1;
        }

        foreach (var (p, n) in count)
            if (n >= 3)
                ctx.DrawEllipse(SymbolRenderer.Wire, null, ToPixel(p), stroke * 1.6, stroke * 1.6);
    }

    private void DrawSelectionHandles(DrawingContext ctx, PartInstance part)
    {
        var (w, h) = CircuitDocument.Footprint(part);
        var box = new Rect(ToPixel(part.Position), new Size(w * _step, h * _step)).Inflate(_step * 0.35);

        var dashed = new Pen(SymbolRenderer.Selected, 1)
        {
            DashStyle = new DashStyle([3, 2], 0),
        };
        ctx.DrawRectangle(null, dashed, box);

        double s = Math.Max(5, _step * 0.7);
        foreach (var c in new[] { box.TopLeft, box.TopRight, box.BottomLeft, box.BottomRight })
            ctx.FillRectangle(SymbolRenderer.Selected, new Rect(c.X - s / 2, c.Y - s / 2, s, s));
    }

    private void DrawWirePreview(DrawingContext ctx, GridPoint from, double stroke)
    {
        var pen = new Pen(SymbolRenderer.Selected, stroke) { DashStyle = new DashStyle([4, 3], 0) };
        // Orthogonal preview: the leg the wire will actually take.
        var corner = new GridPoint(_cursor.X, from.Y);
        ctx.DrawLine(pen, ToPixel(from), ToPixel(corner));
        ctx.DrawLine(pen, ToPixel(corner), ToPixel(_cursor));
    }

    private void DrawGhost(DrawingContext ctx, double stroke)
    {
        var ghost = new PartInstance
        {
            Id = "ghost",
            Definition = PendingPart!,
            Designator = _doc.NextDesignator(PendingPart!.Prefix),
            Position = _cursor,
        };
        using (ctx.PushOpacity(0.55))
            SymbolRenderer.Draw(ctx, ghost, ToPixel, _step, true, stroke);
    }

    private void DrawCursorReadout(DrawingContext ctx)
    {
        if (!_pointerInside) return;
        // Grid steps are 2.54 mm; the status bar in the spec reads in millimetres.
        var text = SymbolRenderer.Text(
            $"X {_cursor.X * 2.54:0.00}  Y {_cursor.Y * 2.54:0.00} mm", 10, SymbolRenderer.Meta);
        ctx.DrawText(text, new Point(6, Bounds.Height - text.Height - 4));
    }

    // ── interaction ──────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var pt = e.GetCurrentPoint(this);
        _cursor = ToGrid(pt.Position);

        if (pt.Properties.IsMiddleButtonPressed) { _panFrom = pt.Position; e.Handled = true; return; }
        if (pt.Properties.IsRightButtonPressed) { CancelPending(); e.Handled = true; return; }
        if (!pt.Properties.IsLeftButtonPressed) return;

        switch (Tool)
        {
            case EditorTool.Place when PendingPart is not null:
                var placed = _doc.Place(PendingPart, _cursor);
                Selected = placed;
                Raise();
                break;

            case EditorTool.Wire:
                if (_wireFrom is { } from)
                {
                    // Two segments, so the run is orthogonal like a drawn schematic.
                    var corner = new GridPoint(_cursor.X, from.Y);
                    if (corner != from) _doc.Connect(from, corner);
                    if (corner != _cursor) _doc.Connect(corner, _cursor);
                    // Chain from here, so a run of wires is one gesture per corner.
                    _wireFrom = _cursor;
                    Raise();
                }
                else _wireFrom = _cursor;
                break;

            case EditorTool.Delete:
                DeleteAt(_cursor);
                break;

            default:
                var hit = _doc.PartAt(_cursor) ?? _doc.PinAt(_cursor)?.Part;
                Selected = hit;
                if (hit is not null && !hit.Locked)
                {
                    _dragging = hit;
                    _dragGrab = new GridPoint(_cursor.X - hit.Position.X, _cursor.Y - hit.Position.Y);
                }
                break;
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        _pointerInside = true;
        var g = ToGrid(p);

        if (_panFrom is { } from)
        {
            _offset += p - from;
            _panFrom = p;
            InvalidateVisual();
            return;
        }

        if (_dragging is { } part && g != part.Position.Offset(_dragGrab.X, _dragGrab.Y))
        {
            part.Position = new GridPoint(g.X - _dragGrab.X, g.Y - _dragGrab.Y);
            _cursor = g;
            Raise();               // the netlist follows the part while it is still moving
            InvalidateVisual();
            return;
        }

        if (g != _cursor)
        {
            _cursor = g;
            if (_wireFrom is not null || Tool == EditorTool.Place) InvalidateVisual();
            else InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dragging is not null) { _dragging = null; Raise(); }
        _panFrom = null;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _pointerInside = false;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // Zoom about the pointer, so the point under the cursor stays put.
        var before = ToGrid(e.GetPosition(this));
        _step = Math.Clamp(_step * (e.Delta.Y > 0 ? 1.15 : 1 / 1.15), MinStep, MaxStep);
        var after = ToPixel(before);
        _offset += e.GetPosition(this) - after;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Delete or Key.Back when _selected is not null:
                _doc.Remove(_selected);
                Selected = null;
                Raise();
                break;

            case Key.R when _selected is not null:
                _selected.Rotation = (Rotation)(((int)_selected.Rotation + 90) % 360);
                Raise();
                break;

            case Key.Escape:
                CancelPending();
                break;

            default:
                return;
        }

        InvalidateVisual();
        e.Handled = true;
    }

    private void CancelPending()
    {
        _wireFrom = null;
        PendingPart = null;
        if (Tool == EditorTool.Place) Tool = EditorTool.Select;
        InvalidateVisual();
    }

    private void DeleteAt(GridPoint g)
    {
        if (_doc.PartAt(g) is { } part) { _doc.Remove(part); if (ReferenceEquals(part, _selected)) Selected = null; Raise(); return; }
        var wire = _doc.Wires.FirstOrDefault(w => w.Points().Contains(g));
        if (wire is not null) { _doc.Remove(wire); Raise(); }
    }

    /// <summary>Centres the view on everything that is placed.</summary>
    public void ZoomToFit()
    {
        if (_doc.Parts.Count == 0) return;

        int minX = _doc.Parts.Min(p => p.Position.X), maxX = _doc.Parts.Max(p => p.Position.X + CircuitDocument.Footprint(p).W);
        int minY = _doc.Parts.Min(p => p.Position.Y), maxY = _doc.Parts.Max(p => p.Position.Y + CircuitDocument.Footprint(p).H);

        double w = Math.Max(1, maxX - minX + 6), h = Math.Max(1, maxY - minY + 6);
        _step = Math.Clamp(Math.Min(Bounds.Width / w, Bounds.Height / h), MinStep, MaxStep);
        _offset = new Point(
            (Bounds.Width - (maxX - minX) * _step) / 2 - minX * _step,
            (Bounds.Height - (maxY - minY) * _step) / 2 - minY * _step);
        InvalidateVisual();
    }

    private void Raise() => DocumentChanged?.Invoke(this, EventArgs.Empty);
}
