using MessageRecord.Data;

namespace MessageRecord.Capture;

/// <summary>每隔一段時間向目前平台的通知來源要新資料，寫進本機資料庫。</summary>
public sealed class CaptureHost : IDisposable
{
    private readonly SqliteRecordStore _store;
    private readonly Action<string, string> _report;
    private readonly NotificationIngestorHost _ingestor = new();
    private readonly Timer _timer;
    private readonly LinuxDBusFeed? _linux;
    private int _polling;

    public CaptureHost(SqliteRecordStore store, Action<string, string> report)
    {
        _store = store;
        _report = report;
        _linux = OperatingSystem.IsLinux() ? new LinuxDBusFeed() : null;
        _timer = new Timer(_ => Tick(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        _linux?.Start();
        _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(15));
    }

    public void Tick()
    {
        if (Interlocked.Exchange(ref _polling, 1) == 1) return;
        try
        {
            Poll();
        }
        catch (Exception ex)
        {
            _report("通知來源中斷", ex.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _polling, 0);
        }
    }

    private void Poll()
    {
        FeedSnapshot snapshot;
        string feedName;
        if (OperatingSystem.IsWindows())
        {
            feedName = "windows-wpn";
            snapshot = new WindowsWpnFeed().Poll();
        }
        else if (OperatingSystem.IsMacOS())
        {
            feedName = "macos-usernoted";
            snapshot = new MacUserNotedFeed().Poll();
        }
        else if (_linux is not null)
        {
            feedName = _linux.Name;
            snapshot = _linux.Poll();
        }
        else
        {
            _report("沒有通知來源", "這個作業系統目前沒有可讀的通知來源。可以從設定匯入 JSON。");
            return;
        }

        _report(snapshot.ShortStatus, snapshot.LongStatus);
        if (!snapshot.Accessible || !snapshot.Recognized) return;

        var history = !_store.GetFlag("baseline:" + feedName);
        _store.BeginBulk();
        try
        {
            foreach (var item in snapshot.Items)
                _ingestor.Accept(_store, item, history);
            if (history) _store.SetFlag("baseline:" + feedName, true);
        }
        finally
        {
            _store.EndBulk();
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _linux?.Dispose();
    }

    /// <summary>讓測試可以呼叫與正式環境相同的寫入規則。</summary>
    private sealed class NotificationIngestorHost
    {
        public IngestResult Accept(SqliteRecordStore store, IncomingNotification notice, bool history) =>
            NotificationIngestor.Accept(store, notice, history);
    }
}
