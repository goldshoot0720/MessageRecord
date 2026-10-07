using MessageRecord.Capture;
using MessageRecord.Models;

namespace MessageRecord.Data;

/// <summary>桌面版的資料入口。畫面只透過這裡讀寫紀錄、規則與匯入匯出。</summary>
public sealed class AppSession : IDisposable
{
    private readonly SqliteRecordStore _store;
    private CaptureHost? _capture;

    private AppSession(SqliteRecordStore store) => _store = store;

    public event Action? Changed;

    public event Action? StatusChanged;

    public string DataDirectory => _store.DataDirectory;

    public string StatusText { get; private set; } = "正在檢查通知來源";

    public string ShortStatus { get; private set; } = "正在檢查通知來源";

    public bool MasterEnabled
    {
        get => _store.MasterEnabled;
        set => _store.MasterEnabled = value;
    }

    public bool DefaultBlocking
    {
        get => _store.DefaultBlocking;
        set => _store.DefaultBlocking = value;
    }

    public bool KeepFullText
    {
        get => _store.KeepFullText;
        set => _store.KeepFullText = value;
    }

    public static AppSession OpenLive()
    {
        var store = SqliteRecordStore.Open(SqliteRecordStore.DefaultDirectory());
        var session = new AppSession(store);
        store.Changed += () => session.Changed?.Invoke();
        session._capture = new CaptureHost(store, session.UpdateStatus);
        session._capture.Start();
        return session;
    }

    public List<AppChannel> LoadChannels() => ChannelMap.ToChannels(_store);

    public void SetBlocking(string packageName, string appLabel, bool blocking) =>
        _store.SetBlocking(packageName, appLabel, blocking);

    public void Forget(string packageName) => _store.Forget(packageName);

    public string ExportJson(string? packageName) =>
        RecordExporter.ToJson(_store.ExportRecords(packageName), RecordExporter.DeviceId());

    public string ExportFileName(string? appLabel) => RecordExporter.FileName(appLabel);

    public int ImportJson(string json) => _store.ImportJson(json);

    public void RevealDataFolder()
    {
        Directory.CreateDirectory(DataDirectory);
        if (OperatingSystem.IsMacOS())
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "open", ArgumentList = { DataDirectory } });
        else if (OperatingSystem.IsWindows())
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", ArgumentList = { DataDirectory } });
        else
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "xdg-open", ArgumentList = { DataDirectory } });
    }

    public void Dispose()
    {
        _capture?.Dispose();
        _store.Dispose();
    }

    private void UpdateStatus(string shortStatus, string longStatus)
    {
        if (ShortStatus == shortStatus && StatusText == longStatus) return;
        ShortStatus = shortStatus;
        StatusText = longStatus;
        StatusChanged?.Invoke();
    }
}
