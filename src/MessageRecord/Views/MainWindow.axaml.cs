using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace MessageRecord.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ApplyPlatformChrome();
        SizeChanged += (_, _) => UpdateCompactLayout();
    }

    private void UpdateCompactLayout()
    {
        var compact = Bounds.Height < 900;
        TodayChart.IsVisible = !compact;
        TodayCard.Padding = compact ? new Thickness(16, 12) : new Thickness(22);
        TodayCard.Margin = new Thickness(0, 0, 0, compact ? 20 : 52);
        ToolNavigation.Margin = new Thickness(0, compact ? 10 : 28, 0, 0);
    }

    /// <summary>
    /// 視窗外框依平台調整：
    /// Windows / Linux 用自繪的標題列按鈕；macOS 交給系統的紅綠燈按鈕，
    /// 並把左上角的品牌區往右讓開。
    /// </summary>
    private void ApplyPlatformChrome()
    {
        if (OperatingSystem.IsMacOS())
        {
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            ExtendClientAreaTitleBarHeightHint = 46;
            CaptionButtons.IsVisible = false;
            BrandBlock.Margin = new Thickness(84, 0, 0, 0);
        }
        else
        {
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        }
    }

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e) => ToggleMaximize();

    private void OnMinimize(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object? sender, RoutedEventArgs e) => ToggleMaximize();

    private void OnClose(object? sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
}
