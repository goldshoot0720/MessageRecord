using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MessageRecord.Controls;

/// <summary>Resolution-independent application marks, shared by the list and detail header.</summary>
public sealed class AppIcon : Control
{
    public static readonly StyledProperty<string> AppNameProperty =
        AvaloniaProperty.Register<AppIcon, string>(nameof(AppName), "");
    public string AppName { get => GetValue(AppNameProperty); set => SetValue(AppNameProperty, value); }
    static AppIcon() => AffectsRender<AppIcon>(AppNameProperty);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        using var scale = context.PushTransform(Matrix.CreateScale(Bounds.Width / 64, Bounds.Height / 64));
        void Shape(string path, string color, double stroke = 0) => context.DrawGeometry(
            stroke == 0 ? Brush.Parse(color) : null,
            stroke == 0 ? null : new Pen(Brush.Parse(color), stroke, lineCap: PenLineCap.Round), Geometry.Parse(path));
        void Tile(string color) => context.DrawRectangle(Brush.Parse(color), null, new Rect(0, 0, 64, 64), 15, 15);
        void Circle(double x, double y, double radius, string color) => context.DrawEllipse(Brush.Parse(color), null, new Point(x, y), radius, radius);
        void Label(string text, double size, string color, double y)
        {
            var label = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold), size, Brush.Parse(color));
            context.DrawText(label, new Point((64 - label.Width) / 2, y));
        }
        switch (AppName)
        {
            case "LINE":
                Tile("#08C522");
                Shape("M32 11 C13 11 6 28 14 39 C18 45 26 47 30 47 L29 54 C29 56 44 47 49 41 C64 26 51 11 32 11 Z", "White");
                Label("LINE", 14, "#08B82A", 21);
                break;
            case "YouTube":
                context.DrawRectangle(Brush.Parse("#FF0808"), null, new Rect(1, 10, 62, 44), 13, 13);
                Shape("M26 21 L43 32 L26 43 Z", "White"); break;
            case "Chrome":
                Circle(32, 32, 31, "#E94335");
                Shape("M32 32 L59 18 A31 31 0 0 1 30 63 Z", "#F9CE45");
                Shape("M32 32 L30 63 A31 31 0 0 1 5 17 Z", "#24AC65");
                Circle(32, 32, 16, "White"); Circle(32, 32, 13, "#3187EF"); break;
            case "Facebook":
                Tile("#1489FF"); Shape("M37 64 L25 64 L25 37 L18 37 L18 27 L25 27 L25 20 Q25 9 38 9 L46 9 L46 19 L40 19 Q37 19 37 23 L37 27 L46 27 L44 37 L37 37 Z", "White"); break;
            case "Instagram":
                var gradient = new LinearGradientBrush { StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative) };
                gradient.GradientStops.Add(new GradientStop(Color.Parse("#FFD044"), 0));
                gradient.GradientStops.Add(new GradientStop(Color.Parse("#FF285A"), .45));
                gradient.GradientStops.Add(new GradientStop(Color.Parse("#B71BCB"), .72));
                gradient.GradientStops.Add(new GradientStop(Color.Parse("#6046EF"), 1));
                context.DrawRectangle(gradient, null, new Rect(0, 0, 64, 64), 15, 15);
                context.DrawRectangle(null, new Pen(Brushes.White, 4), new Rect(11, 11, 42, 42), 12, 12);
                context.DrawEllipse(null, new Pen(Brushes.White, 4), new Point(32, 32), 10, 10);
                Circle(45, 19, 3, "White"); break;
            case "Gmail":
                Tile("#FAFCFF");
                Shape("M13 48 L13 20 L32 34 L51 20 L51 48", "#4285F4", 9);
                Shape("M13 20 L32 34 L51 20", "#EA4335", 9);
                Shape("M51 24 L51 48", "#34A853", 9); break;
            case "Discord":
                Tile("#5865F2");
                Shape("M17 18 L25 15 L27 19 Q32 17 37 19 L39 15 L47 18 Q54 29 54 44 L43 49 L40 44 Q32 47 24 44 L21 49 L10 44 Q10 29 17 18 Z", "White");
                context.DrawEllipse(Brush.Parse("#5865F2"), null, new Point(24, 33), 4, 5);
                context.DrawEllipse(Brush.Parse("#5865F2"), null, new Point(40, 33), 4, 5); break;
            case "Slack":
                Tile("#FAFCFF");
                Shape("M27 13 L27 27 M13 27 L18 27", "#36C5F0", 9);
                Shape("M37 13 L37 18 M37 27 L51 27", "#2EB67D", 9);
                Shape("M51 37 L46 37 M37 37 L37 51", "#ECB22E", 9);
                Shape("M27 37 L13 37 M27 46 L27 51", "#E01E5A", 9); break;
            case "Spotify":
                Tile("#060D0B"); Circle(32, 32, 27, "#20D45A");
                Shape("M15 24 Q32 17 49 27 M18 33 Q32 27 46 35 M21 41 Q32 37 42 43", "#062619", 4); break;
            case "Microsoft Teams":
                Circle(39, 14, 9, "#7B83EB"); Circle(54, 19, 7, "#5059C9");
                context.DrawRectangle(Brush.Parse("#5059C9"), null, new Rect(40, 29, 23, 24), 6, 6);
                context.DrawRectangle(Brush.Parse("#7B83EB"), null, new Rect(23, 27, 28, 34), 8, 8);
                context.DrawRectangle(Brush.Parse("#454FD3"), null, new Rect(1, 21, 33, 33), 4, 4);
                Shape("M9 30 L26 30 M17 30 L17 46", "White", 4); break;
            case "NotifBlock":
                Shape("M32 3 Q20 10 9 8 Q6 8 6 14 L6 42 Q7 52 32 61 Q57 52 58 42 L58 14 Q58 8 55 8 Q44 10 32 3 Z", "#082943");
                Shape("M32 3 Q20 10 9 8 Q6 8 6 14 L6 42 Q7 52 32 61 Q57 52 58 42 L58 14 Q58 8 55 8 Q44 10 32 3 Z", "#1698FF", 2);
                Shape("M32 17 Q21 17 21 30 L17 42 L47 42 L43 30 Q43 17 32 17 Z", "#36AEFF"); Circle(32, 46, 4, "#90D7FF"); break;
            default:
                Tile("#26374D"); Label(string.IsNullOrEmpty(AppName) ? "?" : AppName[..1], 30, "#D9E9FF", 11); break;
        }
    }
}
