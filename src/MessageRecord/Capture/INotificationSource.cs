using MessageRecord.Models;

namespace MessageRecord.Capture;

/// <summary>
/// 通知來源。介面層只認識這個介面，實際要怎麼攔截由各平台的實作決定。
/// 目前只有示範資料的實作；接真實攔截時新增各平台實作即可，畫面完全不用改。
/// </summary>
public interface INotificationSource
{
    /// <summary>來源名稱，顯示在介面或記錄檔用。</summary>
    string Name { get; }

    /// <summary>這個來源在目前的作業系統上能不能用。</summary>
    bool IsAvailable { get; }

    /// <summary>載入依程式名稱分好類的通知紀錄。</summary>
    List<AppChannel> Load();
}
