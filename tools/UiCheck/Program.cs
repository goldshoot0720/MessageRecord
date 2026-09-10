using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MessageRecord;
using MessageRecord.ViewModels;
using MessageRecord.Views;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().SetupWithoutStarting();
        var model = new MainViewModel();
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            Console.WriteLine("PASS " + name);
        }
        Check(model.AppList.Selected?.DisplayName == "LINE", "initial selection");
        var line = model.AppList.Selected!;
        model.SearchText = "chrome";
        Check(model.AppList.Apps.Count == 1 && model.AppList.Selected?.DisplayName == "Chrome", "search and selection");
        model.SearchText = "nothing-matches-this";
        Check(model.AppList.IsEmpty && model.CurrentPane is null, "empty search");
        model.SearchText = "";
        model.AppList.Selected = line;
        var detail = (AppDetailViewModel)model.CurrentPane!;
        detail.SelectTab("blocked");
        Check(detail.Records.Count == line.BlockedCount && detail.Records.All(r => r.Blocked), "blocked filter");
        detail.SelectTab("allowed");
        Check(detail.Records.Count == line.AllowedCount && detail.Records.All(r => !r.Blocked), "allowed filter");
        detail.SelectTab("all");
        detail.ToggleOrderCommand.Execute(null);
        Check(detail.Records.First().ArrivalTime <= detail.Records.Last().ArrivalTime, "oldest first");
        detail.ToggleOrderCommand.Execute(null);
        var previous = line.IsBlocking;
        line.IsBlocking = !previous;
        Check(detail.App.IsBlocking == !previous, "shared blocking toggle");
        line.IsBlocking = previous;
        model.SelectedTool = model.ToolItems[0];
        Check(model.SelectedSection is null && model.CurrentPane is StatsViewModel, "stats navigation");
        model.OpenSettingsCommand.Execute(null);
        Check(model.CurrentPane is SettingsViewModel, "settings navigation");
        model.SelectedSection = model.NavItems[0];

        Directory.CreateDirectory("artifacts");
        var window = new MainWindow { DataContext = model, ShowActivated = false, WindowStartupLocation = WindowStartupLocation.Manual, Position = new PixelPoint(-20000, -20000) };
        window.Show();
        // Keep the native host offscreen while rendering the actual Avalonia visual tree.
        foreach (var size in new[] { new PixelSize(1536, 1024), new PixelSize(1280, 800), new PixelSize(1280, 760) })
        {
            window.Width = size.Width;
            window.Height = size.Height;
            window.Measure(new Size(size.Width, size.Height));
            window.Arrange(new Rect(0, 0, size.Width, size.Height));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
            bitmap.Render(window);
            var suffix = size.Height == 760 ? "1280-min" : size.Width.ToString();
            bitmap.Save($"artifacts/notifblock-{suffix}.png");
            var navigation = window.FindControl<StackPanel>("ToolNavigation")!;
            var card = window.FindControl<Border>("TodayCard")!;
            Check(navigation.Bounds.Bottom <= card.Bounds.Top, $"sidebar does not overlap at {size.Width} x {size.Height}");
            Console.WriteLine($"Rendered {size.Width} x {size.Height}");
        }
        window.Close();
    }
}
