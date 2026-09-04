using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SimBoard.Spice;

// Control inherits Layoutable.Measure(Size), which shadows the static Measure class.
// Short common nouns as type names collide with something in every Control subclass -
// Path, Dock and Theme have each done it in this project already.
using SpiceMeasure = SimBoard.Spice.Measure;

namespace SimBoard.App.Controls;

/// <summary>One traced signal on the scope.</summary>
public sealed record Trace(string Label, SpiceVector Signal, Color Colour);

/// <summary>
/// The oscilloscope, drawing real simulation output.
///
/// The mock version of this screen drew a decorative sine. This one plots the vectors
/// ngspice returned, on a time axis taken from the run — so what it shows is the circuit
/// on the sheet, and the measurements underneath are computed from the same samples
/// rather than typed in.
///
/// SPICE uses an adaptive timestep, so samples bunch where the signal moves fast. The
/// plot walks them in time order and never assumes even spacing; the measurements come
/// from <see cref="Measure"/>, which integrates over real time for the same reason.
/// </summary>
public class ScopeView : Control
{
    private static readonly Color[] Palette =
    [
        Color.Parse("#5fd0a8"),   // CH1 — the spec's net-class A
        Color.Parse("#e8b04a"),   // CH2 — selection amber
        Color.Parse("#6fd3e0"),
        Color.Parse("#ff7a5f"),
    ];

    private SpiceVector? _time;
    private readonly List<Trace> _traces = [];
    private double? _cursorFraction;

    public ScopeView()
    {
        ClipToBounds = true;
        Height = 184;
    }

    public IReadOnlyList<Trace> Traces => _traces;

    public static Color ColourFor(int index) => Palette[index % Palette.Length];

    public void SetTime(SpiceVector? time)
    {
        _time = time;
        InvalidateVisual();
    }

    public bool Toggle(string label, SpiceVector signal)
    {
        var existing = _traces.FindIndex(t => t.Label == label);
        if (existing >= 0)
        {
            _traces.RemoveAt(existing);
            InvalidateVisual();
            return false;
        }

        _traces.Add(new Trace(label, signal, ColourFor(_traces.Count)));
        InvalidateVisual();
        return true;
    }

    public void Clear()
    {
        _traces.Clear();
        InvalidateVisual();
    }

    /// <summary>Re-points existing traces at a fresh run, dropping any net that vanished.</summary>
    public void Rebind(Func<string, SpiceVector?> lookup)
    {
        for (int i = _traces.Count - 1; i >= 0; i--)
        {
            var found = lookup(_traces[i].Label);
            if (found is null) _traces.RemoveAt(i);
            else _traces[i] = _traces[i] with { Signal = found };
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext ctx)
    {
        var bounds = new Rect(Bounds.Size);
        var plot = bounds.Deflate(new Thickness(0, 0, 172, 0));

        ctx.FillRectangle(new SolidColorBrush(Color.Parse("#04160f")), plot);
        DrawGraticule(ctx, plot);

        if (_time is null || _traces.Count == 0 || _time.Count < 2)
        {
            var hint = SymbolRenderer.Text(
                _time is null ? "กดรันซิมก่อน" : "เลือกเนตด้วยเครื่องมือจับสัญญาณ (⌖) เพื่อดูรูปคลื่น",
                11, new SolidColorBrush(Color.Parse("#3f7d68")));
            ctx.DrawText(hint, new Point(plot.Center.X - hint.Width / 2, plot.Center.Y - hint.Height / 2));
            DrawMeasurements(ctx, bounds);
            return;
        }

        var (lo, hi) = VerticalRange();
        foreach (var t in _traces) DrawTrace(ctx, plot, t, lo, hi);

        DrawAxisLabels(ctx, plot, lo, hi);
        if (_cursorFraction is { } f) DrawCursor(ctx, plot, f, lo, hi);
        DrawMeasurements(ctx, bounds);
    }

    private (double Lo, double Hi) VerticalRange()
    {
        double lo = double.MaxValue, hi = double.MinValue;
        foreach (var t in _traces)
            foreach (var v in t.Signal.Values)
            {
                if (v < lo) lo = v;
                if (v > hi) hi = v;
            }

        if (lo > hi) return (0, 1);
        if (Math.Abs(hi - lo) < 1e-9) { lo -= 0.5; hi += 0.5; }

        double pad = (hi - lo) * 0.12;
        return (lo - pad, hi + pad);
    }

    private void DrawGraticule(DrawingContext ctx, Rect plot)
    {
        var pen = new Pen(new SolidColorBrush(Color.Parse("#1d4a3c")), 1);
        for (int i = 1; i < 10; i++)
            ctx.DrawLine(pen, new Point(plot.X + plot.Width * i / 10, plot.Y),
                              new Point(plot.X + plot.Width * i / 10, plot.Bottom));
        for (int i = 1; i < 8; i++)
            ctx.DrawLine(pen, new Point(plot.X, plot.Y + plot.Height * i / 8),
                              new Point(plot.Right, plot.Y + plot.Height * i / 8));
    }

    private void DrawTrace(DrawingContext ctx, Rect plot, Trace trace, double lo, double hi)
    {
        int n = Math.Min(_time!.Count, trace.Signal.Count);
        if (n < 2) return;

        double t0 = _time.Values[0], t1 = _time.Values[n - 1];
        double span = t1 - t0;
        if (span <= 0) return;

        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(Map(0), false);
            for (int i = 1; i < n; i++) g.LineTo(Map(i));
            g.EndFigure(false);
        }

        var brush = new SolidColorBrush(trace.Colour);
        ctx.DrawGeometry(null, new Pen(brush, 1.6, lineJoin: PenLineJoin.Round), geo);
        return;

        Point Map(int i) => new(
            plot.X + (_time.Values[i] - t0) / span * plot.Width,
            plot.Bottom - (trace.Signal.Values[i] - lo) / (hi - lo) * plot.Height);
    }

    private void DrawAxisLabels(DrawingContext ctx, Rect plot, double lo, double hi)
    {
        var dim = new SolidColorBrush(Color.Parse("#3f7d68"));
        double perDiv = (hi - lo) / 8;
        var v = SymbolRenderer.Text($"{Eng(perDiv, "V")}/div", 9, dim);
        ctx.DrawText(v, new Point(plot.X + 4, plot.Y + 3));

        double span = _time!.Values[^1] - _time.Values[0];
        var t = SymbolRenderer.Text($"{Eng(span / 10, "s")}/div", 9, dim);
        ctx.DrawText(t, new Point(plot.Right - t.Width - 4, plot.Y + 3));

        for (int i = 0; i < _traces.Count; i++)
        {
            var label = SymbolRenderer.Text(_traces[i].Label, 9, new SolidColorBrush(_traces[i].Colour));
            ctx.DrawText(label, new Point(plot.X + 4, plot.Bottom - (i + 1) * (label.Height + 2) - 2));
        }
    }

    private void DrawCursor(DrawingContext ctx, Rect plot, double fraction, double lo, double hi)
    {
        double x = plot.X + plot.Width * fraction;
        var pen = new Pen(new SolidColorBrush(Color.Parse("#c9a227")), 1) { DashStyle = new DashStyle([3, 3], 0) };
        ctx.DrawLine(pen, new Point(x, plot.Y), new Point(x, plot.Bottom));

        int i = IndexAtFraction(fraction);
        foreach (var t in _traces)
        {
            if (i >= t.Signal.Count) continue;
            double y = plot.Bottom - (t.Signal.Values[i] - lo) / (hi - lo) * plot.Height;
            ctx.DrawEllipse(new SolidColorBrush(t.Colour), null, new Point(x, y), 3, 3);
        }
    }

    /// <summary>Live readouts, computed from the samples rather than written down.</summary>
    private void DrawMeasurements(DrawingContext ctx, Rect bounds)
    {
        var panel = new Rect(bounds.Right - 168, bounds.Y, 168, bounds.Height);
        ctx.FillRectangle(new SolidColorBrush(Color.Parse("#0d1418")), panel);

        double y = panel.Y + 5;
        foreach (var t in _traces)
        {
            var head = SymbolRenderer.Text(t.Label, 10, new SolidColorBrush(t.Colour));
            ctx.DrawText(head, new Point(panel.X + 6, y));
            y += head.Height + 1;

            foreach (var (name, value) in Readouts(t))
            {
                var line = SymbolRenderer.Text($"  {name,-6}{value,10}", 9.5, SymbolRenderer.Meta);
                ctx.DrawText(line, new Point(panel.X + 6, y));
                y += line.Height;
            }
            y += 4;
            if (y > panel.Bottom - 20) break;
        }

        if (_traces.Count == 0)
        {
            var hint = SymbolRenderer.Text("ยังไม่มีสัญญาณ", 9.5, SymbolRenderer.Meta);
            ctx.DrawText(hint, new Point(panel.X + 6, panel.Y + 6));
        }
    }

    private IEnumerable<(string, string)> Readouts(Trace t)
    {
        if (_time is null) yield break;

        yield return ("Vpp", Eng(SpiceMeasure.Vpp(t.Signal), "V"));
        yield return ("RMS", Eng(SpiceMeasure.Rms(_time, t.Signal), "V"));
        yield return ("Max", Eng(SpiceMeasure.Max(t.Signal), "V"));

        if (SpiceMeasure.Frequency(_time, t.Signal) is { } f)
        {
            yield return ("ความถี่", Eng(f, "Hz"));
            if (SpiceMeasure.DutyCycle(_time, t.Signal) is { } d)
                yield return ("ดิวตี้", $"{d * 100:0.0} %");
        }
        if (SpiceMeasure.RiseTime(_time, t.Signal) is { } r)
            yield return ("ขาขึ้น", Eng(r, "s"));
    }

    // ── cursor interaction ───────────────────────────────────────────────

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var plot = new Rect(Bounds.Size).Deflate(new Thickness(0, 0, 172, 0));
        if (plot.Width <= 0) return;

        double f = (e.GetPosition(this).X - plot.X) / plot.Width;
        _cursorFraction = f is >= 0 and <= 1 ? f : null;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _cursorFraction = null;
        InvalidateVisual();
    }

    private int IndexAtFraction(double f)
    {
        if (_time is null || _time.Count == 0) return 0;
        double t0 = _time.Values[0], t1 = _time.Values[^1];
        double target = t0 + (t1 - t0) * f;

        // Binary search: the axis is time, and time is monotonic even when the step is not.
        int lo = 0, hi = _time.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_time.Values[mid] < target) lo = mid + 1; else hi = mid;
        }
        return lo;
    }

    private static string Eng(double v, string unit)
    {
        double a = Math.Abs(v);
        return a switch
        {
            >= 1e9 => $"{v / 1e9:0.###} G{unit}",
            >= 1e6 => $"{v / 1e6:0.###} M{unit}",
            >= 1e3 => $"{v / 1e3:0.###} k{unit}",
            >= 1 => $"{v:0.###} {unit}",
            >= 1e-3 => $"{v * 1e3:0.###} m{unit}",
            >= 1e-6 => $"{v * 1e6:0.###} µ{unit}",
            >= 1e-9 => $"{v * 1e9:0.###} n{unit}",
            0 => $"0 {unit}",
            _ => $"{v:G3} {unit}",
        };
    }
}
