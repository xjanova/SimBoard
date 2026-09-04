using Avalonia;
using Avalonia.Media;
using SimBoard.Document;

namespace SimBoard.App.Controls;

/// <summary>
/// Draws part symbols. Shapes, not glyphs — a resistor is an IEC rectangle with leads,
/// because that is what the person reading the sheet expects to see.
///
/// Everything is drawn in grid space and scaled by the caller, so a symbol is identical
/// at every zoom level and lines land on whole device pixels where they can.
/// </summary>
public static class SymbolRenderer
{
    public static readonly IBrush Wire = new SolidColorBrush(Color.Parse("#93a9bd"));
    public static readonly IBrush Label = new SolidColorBrush(Color.Parse("#8fa8bd"));
    public static readonly IBrush Meta = new SolidColorBrush(Color.Parse("#7f97ab"));
    public static readonly IBrush Selected = new SolidColorBrush(Color.Parse("#e8b04a"));
    public static readonly IBrush PinDot = new SolidColorBrush(Color.Parse("#6fd3e0"));
    public static readonly IBrush Body = new SolidColorBrush(Color.Parse("#12161b"));
    public static readonly IBrush Error = new SolidColorBrush(Color.Parse("#ff7a5f"));

    /// <summary>
    /// Draws one placed part. <paramref name="px"/> converts a grid point to pixels.
    /// </summary>
    public static void Draw(
        DrawingContext ctx, PartInstance part, Func<GridPoint, Point> px, double step,
        bool selected, double strokeWidth)
    {
        var colour = selected ? Selected : Wire;
        var pen = new Pen(colour, strokeWidth, lineCap: PenLineCap.Square);
        var (w, h) = CircuitDocument.Footprint(part);

        var origin = px(part.Position);
        var box = new Rect(origin, new Size(w * step, h * step));

        DrawBody(ctx, part.Definition.Symbol, box, pen, colour, step);
        DrawLeads(ctx, part, px, pen, step);
        DrawText(ctx, part, box, colour, step);
    }

    private static void DrawBody(
        DrawingContext ctx, SymbolShape shape, Rect box, Pen pen, IBrush colour, double step)
    {
        double cx = box.Center.X, cy = box.Center.Y;

        switch (shape)
        {
            case SymbolShape.Box:
                ctx.DrawRectangle(null, pen, box.Deflate(new Thickness(0, box.Height * 0.22)));
                break;

            case SymbolShape.CapacitorNonPolar:
            case SymbolShape.CapacitorPolarised:
            {
                double gap = step * 0.45, plate = box.Height * 0.42;
                ctx.DrawLine(pen, new Point(cx - gap, cy - plate), new Point(cx - gap, cy + plate));
                if (shape == SymbolShape.CapacitorNonPolar)
                    ctx.DrawLine(pen, new Point(cx + gap, cy - plate), new Point(cx + gap, cy + plate));
                else
                {
                    // The curved plate is the negative side — the half people get wrong.
                    var arc = new StreamGeometry();
                    using (var g = arc.Open())
                    {
                        g.BeginFigure(new Point(cx + gap, cy - plate), false);
                        g.QuadraticBezierTo(new Point(cx + gap + step * 0.5, cy), new Point(cx + gap, cy + plate));
                        g.EndFigure(false);
                    }
                    ctx.DrawGeometry(null, pen, arc);
                }
                break;
            }

            case SymbolShape.Inductor:
            {
                var geo = new StreamGeometry();
                using (var g = geo.Open())
                {
                    double r = box.Width / 8;
                    g.BeginFigure(new Point(box.X, cy), false);
                    for (int i = 0; i < 4; i++)
                        g.ArcTo(new Point(box.X + r * 2 * (i + 1), cy), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    g.EndFigure(false);
                }
                ctx.DrawGeometry(null, pen, geo);
                break;
            }

            case SymbolShape.Diode:
            case SymbolShape.Led:
            case SymbolShape.Zener:
            {
                double s = Math.Min(box.Width, box.Height) * 0.42;
                var tri = new StreamGeometry();
                using (var g = tri.Open())
                {
                    g.BeginFigure(new Point(cx - s, cy - s), true);
                    g.LineTo(new Point(cx + s * 0.55, cy));
                    g.LineTo(new Point(cx - s, cy + s));
                    g.EndFigure(true);
                }
                ctx.DrawGeometry(colour, pen, tri);
                ctx.DrawLine(pen, new Point(cx + s * 0.55, cy - s), new Point(cx + s * 0.55, cy + s));

                if (shape == SymbolShape.Led)
                {
                    // Two arrows leaving the junction — the mark that says "this emits".
                    var arrow = new Pen(colour, pen.Thickness * 0.8);
                    for (int i = 0; i < 2; i++)
                    {
                        double ox = cx - s * 0.2 + i * s * 0.5, oy = cy - s;
                        ctx.DrawLine(arrow, new Point(ox, oy), new Point(ox + s * 0.5, oy - s * 0.6));
                    }
                }
                break;
            }

            case SymbolShape.BjtNpn:
            case SymbolShape.BjtPnp:
            {
                double bar = box.Height * 0.34;
                ctx.DrawLine(pen, new Point(cx - step * 0.3, cy - bar), new Point(cx - step * 0.3, cy + bar));
                ctx.DrawLine(pen, new Point(cx - step * 0.3, cy - bar * 0.5), new Point(box.Right - step * 0.6, box.Y));
                ctx.DrawLine(pen, new Point(cx - step * 0.3, cy + bar * 0.5), new Point(box.Right - step * 0.6, box.Bottom));
                break;
            }

            case SymbolShape.MosfetN:
            case SymbolShape.MosfetP:
            {
                double bar = box.Height * 0.34;
                ctx.DrawLine(pen, new Point(cx - step * 0.55, cy - bar), new Point(cx - step * 0.55, cy + bar));
                for (int i = -1; i <= 1; i++)
                    ctx.DrawLine(pen, new Point(cx - step * 0.2, cy + i * bar * 0.6 - bar * 0.15),
                                      new Point(cx - step * 0.2, cy + i * bar * 0.6 + bar * 0.15));
                ctx.DrawLine(pen, new Point(cx - step * 0.2, cy - bar * 0.6), new Point(box.Right - step * 0.6, box.Y));
                ctx.DrawLine(pen, new Point(cx - step * 0.2, cy + bar * 0.6), new Point(box.Right - step * 0.6, box.Bottom));
                break;
            }

            case SymbolShape.VoltageSource:
            case SymbolShape.CurrentSource:
            {
                double r = Math.Min(box.Width, box.Height) * 0.42;
                ctx.DrawEllipse(null, pen, box.Center, r, r);
                if (shape == SymbolShape.VoltageSource)
                {
                    ctx.DrawLine(pen, new Point(cx - r * 0.4, cy - r * 0.35), new Point(cx + r * 0.4, cy - r * 0.35));
                    ctx.DrawLine(pen, new Point(cx, cy - r * 0.6), new Point(cx, cy - r * 0.1));
                    ctx.DrawLine(pen, new Point(cx - r * 0.4, cy + r * 0.4), new Point(cx + r * 0.4, cy + r * 0.4));
                }
                else
                    ctx.DrawLine(pen, new Point(cx, cy + r * 0.6), new Point(cx, cy - r * 0.6));
                break;
            }

            case SymbolShape.Ground:
            {
                double top = box.Y + box.Height * 0.25;
                for (int i = 0; i < 3; i++)
                {
                    double half = box.Width * (0.45 - i * 0.13);
                    double y = top + i * step * 0.35;
                    ctx.DrawLine(pen, new Point(cx - half, y), new Point(cx + half, y));
                }
                break;
            }

            case SymbolShape.Switch:
                ctx.DrawLine(pen, new Point(box.X, cy), new Point(cx - step * 0.4, cy));
                ctx.DrawLine(pen, new Point(cx - step * 0.4, cy), new Point(cx + step * 0.5, cy - box.Height * 0.35));
                ctx.DrawLine(pen, new Point(cx + step * 0.4, cy), new Point(box.Right, cy));
                break;

            case SymbolShape.Motor:
            {
                double r = Math.Min(box.Width, box.Height) * 0.4;
                ctx.DrawEllipse(null, pen, box.Center, r, r);
                break;
            }

            default:
                // IC bodies, connectors and anything else: a plain rectangle, which is
                // what a real symbol for a multi-pin part is.
                ctx.DrawRectangle(Body, pen, box);
                break;
        }
    }

    /// <summary>Leads from the body edge out to each pin, plus the pin's own dot.</summary>
    private static void DrawLeads(
        DrawingContext ctx, PartInstance part, Func<GridPoint, Point> px, Pen pen, double step)
    {
        var (w, h) = CircuitDocument.Footprint(part);
        var origin = px(part.Position);
        var box = new Rect(origin, new Size(w * step, h * step));

        foreach (var (pin, at) in part.PinPositions())
        {
            var p = px(at);
            var edge = new Point(
                Math.Clamp(p.X, box.X, box.Right),
                Math.Clamp(p.Y, box.Y, box.Bottom));
            ctx.DrawLine(pen, edge, p);
            ctx.DrawEllipse(PinDot, null, p, step * 0.14, step * 0.14);
        }
    }

    private static void DrawText(DrawingContext ctx, PartInstance part, Rect box, IBrush colour, double step)
    {
        if (step < 5) return;   // below this the text is noise, not information

        var designator = Text(part.Designator, step * 1.15, colour);
        ctx.DrawText(designator, new Point(box.X, box.Y - designator.Height - step * 0.15));

        if (part.Value is { Length: > 0 } value && part.Definition.Symbol != SymbolShape.IcBody)
        {
            var v = Text(value, step * 1.05, Meta);
            ctx.DrawText(v, new Point(box.X, box.Bottom + step * 0.1));
        }

        if (part.Definition.Symbol == SymbolShape.IcBody && step >= 7)
        {
            var name = Text(part.Definition.Name, step * 1.05, Meta);
            ctx.DrawText(name, new Point(box.Center.X - name.Width / 2, box.Center.Y - name.Height / 2));
        }
    }

    public static FormattedText Text(string s, double size, IBrush brush) =>
        new(s, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Lucida Console, Consolas, monospace"), size, brush);
}
