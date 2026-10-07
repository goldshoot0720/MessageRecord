using Avalonia.Controls;
using Avalonia.Interactivity;
using MessageRecord.ViewModels;

namespace MessageRecord.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void OnExportAll(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel settings || !settings.ShowFileActions) return;
        if (TopLevel.GetTopLevel(this) is not { } top) return;
        var (fileName, json) = settings.BuildExport();
        await JsonFiles.SaveAsync(top, fileName, json);
    }

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel settings || !settings.ShowFileActions) return;
        if (TopLevel.GetTopLevel(this) is not { } top) return;
        var json = await JsonFiles.OpenAsync(top);
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            settings.Import(json);
        }
        catch (Exception ex)
        {
            settings.Report(ex.Message);
        }
    }

    private void OnOpenFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel settings) settings.OpenDataFolder();
    }
}
