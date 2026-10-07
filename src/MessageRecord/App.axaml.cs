using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MessageRecord.Data;
using MessageRecord.ViewModels;
using MessageRecord.Views;

namespace MessageRecord;

public partial class App : Application
{
    private AppSession? _session;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _session = AppSession.OpenLive();
            desktop.ShutdownRequested += (_, _) => _session.Dispose();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(_session)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
