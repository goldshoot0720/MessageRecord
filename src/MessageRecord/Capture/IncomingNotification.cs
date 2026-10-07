namespace MessageRecord.Capture;

/// <summary>各平台通知來源正規化之後的一則事件，還沒寫進資料庫。</summary>
public sealed class IncomingNotification
{
    public string Key { get; init; } = "";
    public string PackageName { get; init; } = "";
    public string AppLabel { get; init; } = "";
    public string Title { get; init; } = "";
    public string Text { get; init; } = "";
    public string BigText { get; init; } = "";
    public string SubText { get; init; } = "";
    public string? ChannelId { get; init; }
    public string? SystemCategory { get; init; }
    public long PostedAt { get; init; }
    public long When { get; init; }
    public bool IsSummary { get; init; }
    public bool Ongoing { get; init; }
    public List<IncomingMessage> Messages { get; init; } = new();
}

public sealed class IncomingMessage
{
    public long Timestamp { get; init; }
    public string Text { get; init; } = "";
    public string Sender { get; init; } = "";
    public string Mime { get; init; } = "";
    public string Uri { get; init; } = "";
}

public readonly record struct IngestResult(bool Inserted, bool Dismiss);

public sealed class FeedSnapshot
{
    public bool Accessible { get; init; }
    public bool Recognized { get; init; }
    public string ShortStatus { get; init; } = "";
    public string LongStatus { get; init; } = "";
    public IReadOnlyList<IncomingNotification> Items { get; init; } = Array.Empty<IncomingNotification>();

    public static FeedSnapshot Failed(string shortStatus, string longStatus) => new()
    {
        Accessible = false,
        Recognized = false,
        ShortStatus = shortStatus,
        LongStatus = longStatus
    };
}
