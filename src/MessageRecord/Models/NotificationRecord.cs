namespace MessageRecord.Models;

/// <summary>單一筆攔截到的通知紀錄。</summary>
public sealed class NotificationRecord
{
    public long Id { get; init; }

    /// <summary>所屬程式的識別碼。</summary>
    public string AppKey { get; init; } = "";

    /// <summary>通知標題，多半是發送者或頻道名稱。</summary>
    public string Title { get; init; } = "";

    /// <summary>通知內文。</summary>
    public string Body { get; init; } = "";

    public DateTime ArrivalTime { get; init; }

    /// <summary>true = 已攔截（沒有跳出來），false = 已允許。</summary>
    public bool Blocked { get; init; }
}
