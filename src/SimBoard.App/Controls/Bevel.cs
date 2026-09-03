using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SimBoard.App.Controls;

/// <summary>
/// The primitive the whole retro chrome is built from: a one-pixel border whose four
/// edges can each carry a different brush, plus the inset shadows that finish the bevel.
///
/// Avalonia's <see cref="Border"/> has a single BorderBrush, which cannot express
/// "light on top and left, dark on bottom and right" — and that two-tone edge *is*
/// the Windows ME and Classic look. Every raised button, sunken field, panel and tab
/// in the product is one of these.
///
/// Edges are snapped to device pixels: a 1px bevel that lands on a half pixel turns
/// into a 2px grey smear at 125% and 150% scaling, which is most Windows laptops.
/// </summary>
public class Bevel : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<Bevel, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> TopBrushProperty =
        AvaloniaProperty.Register<Bevel, IBrush?>(nameof(TopBrush));

    public static readonly StyledProperty<IBrush?> RightBrushProperty =
        AvaloniaProperty.Register<Bevel, IBrush?>(nameof(RightBrush));

    public static readonly StyledProperty<IBrush?> BottomBrushProperty =
        AvaloniaProperty.Register<Bevel, IBrush?>(nameof(BottomBrush));

    public static readonly StyledProperty<IBrush?> LeftBrushProperty =
        AvaloniaProperty.Register<Bevel, IBrush?>(nameof(LeftBrush));

    /// <summary>The inset highlight/shadow pair that gives the edge its depth.</summary>
    public static readonly StyledProperty<BoxShadows> ShadowProperty =
        AvaloniaProperty.Register<Bevel, BoxShadows>(nameof(Shadow));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<Bevel, CornerRadius>(nameof(CornerRadius));

    /// <summary>Set to 0 for a panel that only wants a background and a shadow.</summary>
    public static readonly StyledProperty<double> EdgeThicknessProperty =
        AvaloniaProperty.Register<Bevel, double>(nameof(EdgeThickness), 1.0);

    static Bevel()
    {
        AffectsRender<Bevel>(BackgroundProperty, TopBrushProperty, RightBrushProperty,
            BottomBrushProperty, LeftBrushProperty, ShadowProperty, CornerRadiusProperty,
            EdgeThicknessProperty);
        AffectsMeasure<Bevel>(PaddingProperty, EdgeThicknessProperty);
    }

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? TopBrush { get => GetValue(TopBrushProperty); set => SetValue(TopBrushProperty, value); }
    public IBrush? RightBrush { get => GetValue(RightBrushProperty); set => SetValue(RightBrushProperty, value); }
    public IBrush? BottomBrush { get => GetValue(BottomBrushProperty); set => SetValue(BottomBrushProperty, value); }
    public IBrush? LeftBrush { get => GetValue(LeftBrushProperty); set => SetValue(LeftBrushProperty, value); }
    public BoxShadows Shadow { get => GetValue(ShadowProperty); set => SetValue(ShadowProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double EdgeThickness { get => GetValue(EdgeThicknessProperty); set => SetValue(EdgeThicknessProperty, value); }

    private Thickness Inset => Padding + new Thickness(EdgeThickness);

    protected override Size MeasureOverride(Size availableSize)
    {
        var inset = Inset;
        if (Child is null) return new Size(inset.Left + inset.Right, inset.Top + inset.Bottom);

        Child.Measure(availableSize.Deflate(inset));
        return Child.DesiredSize.Inflate(inset);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize).Deflate(Inset));
        return finalSize;
    }

    public override void Render(DrawingContext ctx)
    {
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var bounds = SnapRect(new Rect(Bounds.Size), scale);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var radius = CornerRadius;
        if (Background is { } bg)
            ctx.DrawRectangle(bg, null, new RoundedRect(bounds, radius), Shadow);

        double t = EdgeThickness <= 0 ? 0 : Math.Max(1.0 / scale, EdgeThickness / scale * scale);
        if (t <= 0) return;

        // Rounded themes (Aqua, Sonoma) get a single stroked outline — four separate
        // straight edges cannot follow a 9px corner, and a mitre gap there is far more
        // visible than the loss of the two-tone effect the rounding already hides.
        if (radius.TopLeft > 1 || radius.TopRight > 1)
        {
            var outline = TopBrush ?? LeftBrush ?? BottomBrush ?? RightBrush;
            if (outline is not null)
                ctx.DrawRectangle(null, new Pen(outline, t), new RoundedRect(bounds.Deflate(t / 2), radius));
            return;
        }

        var (x, y, w, h) = (bounds.X, bounds.Y, bounds.Width, bounds.Height);
        if (TopBrush is { } top) ctx.FillRectangle(top, new Rect(x, y, w, t));
        if (BottomBrush is { } bot) ctx.FillRectangle(bot, new Rect(x, y + h - t, w, t));
        if (LeftBrush is { } left) ctx.FillRectangle(left, new Rect(x, y, t, h));
        if (RightBrush is { } right) ctx.FillRectangle(right, new Rect(x + w - t, y, t, h));
    }

    /// <summary>Aligns to whole device pixels so a 1px edge stays 1px at 125 % and 150 %.</summary>
    private static Rect SnapRect(Rect r, double scale)
    {
        static double Snap(double v, double s) => Math.Round(v * s, MidpointRounding.AwayFromZero) / s;
        double x = Snap(r.X, scale), y = Snap(r.Y, scale);
        return new Rect(x, y, Snap(r.Right, scale) - x, Snap(r.Bottom, scale) - y);
    }
}
