using MessageRecord.Models;

namespace MessageRecord.Data;

/// <summary>把資料庫的聚合結果轉成畫面已經在用的 AppChannel。</summary>
public static class ChannelMap
{
    private static readonly string[] Palette =
    {
        "#FF2B7FFF", "#FF06C755", "#FF4285F4", "#FF1877F2", "#FFE4405F",
        "#FFEA4335", "#FF5865F2", "#FF20D45A", "#FF9146FF", "#FF25D366",
        "#FFF59E0B", "#FF34D399"
    };

    public static List<AppChannel> ToChannels(SqliteRecordStore store)
    {
        var fallback = store.DefaultBlocking;
        var apps = new List<AppChannel>();
        foreach (var summary in store.AppSummaries())
        {
            var channel = new AppChannel
            {
                Key = summary.PackageName,
                DisplayName = string.IsNullOrWhiteSpace(summary.AppLabel) ? summary.PackageName : summary.AppLabel,
                Badge = Badge(summary.AppLabel),
                Accent = Palette[StableIndex(summary.PackageName)],
                BlockEnabled = summary.Blocking ?? fallback
            };

            foreach (var record in store.Records(summary.PackageName, null))
                channel.Records.Add(ToRecord(record));

            apps.Add(channel);
        }

        return apps;
    }

    public static NotificationRecord ToRecord(StoredNotification record) => new()
    {
        Id = record.Id,
        AppKey = record.PackageName,
        Title = record.Title,
        Body = record.Text,
        ArrivalTime = DateTimeOffset.FromUnixTimeMilliseconds(record.PostedAt).LocalDateTime,
        Blocked = record.Blocked,
        Uid = record.Uid,
        Category = record.Category ?? "",
        Source = record.Source,
        Ongoing = record.Ongoing,
        RemovedAt = record.RemovedAt is long removed
            ? DateTimeOffset.FromUnixTimeMilliseconds(removed).LocalDateTime
            : null
    };

    private static string Badge(string label)
    {
        var text = string.IsNullOrWhiteSpace(label) ? "?" : label.Trim();
        return text[..1];
    }

    private static int StableIndex(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in value) hash = hash * 31 + ch;
            var index = hash % Palette.Length;
            return index < 0 ? index + Palette.Length : index;
        }
    }
}
