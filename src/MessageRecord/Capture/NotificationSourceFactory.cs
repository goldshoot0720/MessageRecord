using MessageRecord.Mock;
using MessageRecord.Models;

namespace MessageRecord.Capture;

/// <summary>示範資料來源：三個平台都能用，因為它根本不碰系統。</summary>
public sealed class SampleNotificationSource : INotificationSource
{
    public string Name => "示範資料集";

    public bool IsAvailable => true;

    public List<AppChannel> Load() => SampleData.Build();
}

/// <summary>
/// 依作業系統挑選通知來源。
///
/// 設計預覽與 UiCheck 使用示範資料。
/// 正式視窗改走 AppSession：各平台來源在 CaptureHost，寫入 SqliteRecordStore。
///
///
///   Windows → 讀 %LOCALAPPDATA%\Microsoft\Windows\Notifications\wpndatabase.db（SQLite），
///             或封裝成 MSIX 後用 UserNotificationListener 收即時事件。
///   macOS   → 讀 ~/Library/Group Containers/group.com.apple.usernoted/db2/db（SQLite），
///             或用 UNUserNotificationCenter / 輔助使用權限監看通知中心。
///   Linux   → 在 D-Bus session bus 上監看 org.freedesktop.Notifications 的 Notify 呼叫
///             （eavesdrop / become monitor），即可拿到 app_name、summary、body。
/// </summary>
public static class NotificationSourceFactory
{
    public static INotificationSource Create()
    {
        foreach (var source in Candidates())
        {
            if (source.IsAvailable) return source;
        }

        return new SampleNotificationSource();
    }

    private static IEnumerable<INotificationSource> Candidates()
    {
        // 各平台的真實實作日後插在這裡，例如：
        //   if (OperatingSystem.IsWindows()) yield return new WindowsWpnSource();
        //   if (OperatingSystem.IsMacOS())   yield return new MacUserNotedSource();
        //   if (OperatingSystem.IsLinux())   yield return new LinuxDBusSource();
        yield return new SampleNotificationSource();
    }

    /// <summary>目前平台的名稱，狀態列或記錄檔可以用。</summary>
    public static string PlatformName =>
        OperatingSystem.IsWindows() ? "Windows" :
        OperatingSystem.IsMacOS() ? "macOS" :
        OperatingSystem.IsLinux() ? "Linux" : "未知平台";
}
