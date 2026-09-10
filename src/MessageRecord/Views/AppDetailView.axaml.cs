using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using MessageRecord.ViewModels;

namespace MessageRecord.Views;

public partial class AppDetailView : UserControl
{
    public AppDetailView()
    {
        InitializeComponent();
    }

    private void OnRecordMenu(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: RecordViewModel record } button) return;
        var content = new StackPanel { Spacing = 12, Width = 320, Margin = new Thickness(8) };
        content.Children.Add(new TextBlock { Text = record.Title, FontSize = 20, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = $"{record.ArrivalTime:yyyy/MM/dd HH:mm} · {record.StatusText}", Foreground = record.StatusForeground });
        content.Children.Add(new TextBlock { Text = record.Body, TextWrapping = TextWrapping.Wrap });
        var copy = new Button { Content = "複製通知內容" };
        copy.Click += async (_, _) =>
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
            try
            {
                await clipboard.SetTextAsync($"{record.Title}\n{record.Body}");
                copy.Content = "已複製";
            }
            catch { copy.Content = "無法複製，請再試一次"; }
        };
        content.Children.Add(copy);
        new Flyout { Content = content }.ShowAt(button);
    }
}
