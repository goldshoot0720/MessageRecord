namespace MessageRecord.Data;

/// <summary>與手機版 Room 的 records 表對齊的一筆通知。</summary>
public sealed class StoredNotification
{
    public long Id { get; init; }
    public string Uid { get; init; } = "";
    public string PackageName { get; init; } = "";
    public string AppLabel { get; init; } = "";
    public string Title { get; init; } = "";
    public string Text { get; init; } = "";
    public string? ChannelId { get; init; }
    public string? Category { get; init; }
    public long PostedAt { get; init; }
    public long? RemovedAt { get; init; }
    public bool Blocked { get; init; }
    public bool Ongoing { get; init; }
    public string Source { get; init; } = RecordSources.DesktopListener;
}

public sealed class StoredRule
{
    public string PackageName { get; init; } = "";
    public string AppLabel { get; init; } = "";
    public bool Blocking { get; init; } = true;
    public long UpdatedAt { get; init; }
}

public sealed class StoredSummary
{
    public string PackageName { get; init; } = "";
    public string AppLabel { get; init; } = "";
    public int Total { get; init; }
    public int BlockedCount { get; init; }
    public long LastPostedAt { get; init; }
    public bool? Blocking { get; init; }
}

/// <summary>shared/notiguard-record.schema.json 的 source，另加桌面端兩個值。</summary>
public static class RecordSources
{
    public const string AndroidListener = "android_listener";
    public const string IosServiceExtension = "ios_service_extension";
    public const string IosDelivered = "ios_delivered";
    public const string CompanionSync = "companion_sync";
    public const string Seed = "seed";
    public const string DesktopListener = "desktop_listener";
    public const string DesktopHistory = "desktop_history";
}

public enum RecordFilter
{
    All,
    Blocked,
    Allowed
}
