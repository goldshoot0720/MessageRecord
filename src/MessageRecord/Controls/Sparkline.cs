using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MessageRecord.Controls;

/// <summary>
/// 側欄統計卡用的面積折線圖：把一串數值畫成平滑曲線，下方填漸層。
/// </summary>
public class Sparkline : Control
{
    public static readonly StyledProperty<IReadOnlyList<double>?> PointsProperty =
        AvaloniaProperty.Register<Sparkline, IReadOnlyList<double>?>(nameof(Points));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<IBrush?> AreaFillProperty =
        AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(AreaFill));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(StrokeThickness), 2d);

    static Sparkline()
    {
        AffectsRender<Sparkline>(PointsProperty, StrokeProperty, AreaFillProperty, StrokeThicknessProperty);
    }

    public Sparkline() => IsHitTestVisible = false;

    public IReadOnlyList<double>? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public IBrush? AreaFill
    {
        get => GetValue(AreaFillProperty);
        set => SetValue(AreaFillProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var data = Points;
        if (data is null || data.Count < 2) return;

        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 1 || h <= 1) return;

        var pad = StrokeThickness + 1;
        var min = data.Min();
        var max = data.Max();
        var span = Math.Max(0.0001, max - min);

        var pts = new Point[data.Count];
        for (int i = 0; i < data.Count; i++)
        {
            var x = w * i / (data.Count - 1);
            var y = h - pad - (h - pad * 2) * (data[i] - min) / span;
            pts[i] = new Point(x, y);
        }

        var line = BuildSmoothLine(pts);

        if (AreaFill is { } fill)
        {
            var area = BuildSmoothLine(pts, closeTo: h);
            context.DrawGeometry(fill, null, area);
        }

        if (Stroke is { } stroke)
            context.DrawGeometry(null, new Pen(stroke, StrokeThickness, lineCap: PenLineCap.Round,
                lineJoin: PenLineJoin.Round), line);
    }

    /// <summary>用中點二次貝茲把折線磨圓，closeTo 有值時往下收成封閉面積。</summary>
    private static StreamGeometry BuildSmoothLine(Point[] pts, double? closeTo = null)
    {
        var geo = new StreamGeometry();
        using var ctx = geo.Open();

        ctx.BeginFigure(pts[0], closeTo.HasValue);

        for (int i = 1; i < pts.Length - 1; i++)
        {
            var mid = new Point((pts[i].X + pts[i + 1].X) / 2, (pts[i].Y + pts[i + 1].Y) / 2);
            ctx.QuadraticBezierTo(pts[i], mid);
        }

        ctx.LineTo(pts[^1]);

        if (closeTo is { } bottom)
        {
            ctx.LineTo(new Point(pts[^1].X, bottom));
            ctx.LineTo(new Point(pts[0].X, bottom));
            ctx.EndFigure(true);
        }
        else
        {
            ctx.EndFigure(false);
        }

        return geo;
    }
}
