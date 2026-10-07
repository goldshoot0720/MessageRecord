using MessageRecord.Data;

namespace MessageRecord.Capture;

/// <summary>
/// 手機版監聽服務的寫入規則：
/// 群組摘要不另存，空白通知略過，常駐通知一律放行，重複 uid 不新增。
/// 桌面端讀到的既有通知當成歷史（已允許）；程式啟動後才出現的才依規則標記。
/// </summary>
public static class NotificationIngestor
{
    public static bool ShouldIgnore(string? packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName)) return true;
        return packageName.Equals("MessageRecord", StringComparison.OrdinalIgnoreCase)
            || packageName.Equals("com.notiguard.messagerecord", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("com.notiguard", StringComparison.OrdinalIgnoreCase);
    }

    public static IngestResult Accept(SqliteRecordStore store, IncomingNotification notice, bool history)
    {
        if (ShouldIgnore(notice.PackageName)) return default;

        var wantBlock = !notice.Ongoing && store.MasterEnabled && store.IsBlocking(notice.PackageName);
        if (notice.IsSummary)
            return new IngestResult(false, wantBlock && !history);

        var title = (notice.Title ?? "").Trim();
        var text = FirstText(notice);
        if (title.Length == 0 && text.Length == 0) return default;

        var category = Categorizer.Of(notice.SystemCategory, title, text);
        if (!store.KeepFullText) text = "";

        var record = new StoredNotification
        {
            Uid = RecordIdentity.Uid(notice),
            PackageName = notice.PackageName,
            AppLabel = string.IsNullOrWhiteSpace(notice.AppLabel) ? notice.PackageName : notice.AppLabel,
            Title = title,
            Text = text,
            ChannelId = notice.ChannelId,
            Category = category,
            PostedAt = notice.PostedAt > 0 ? notice.PostedAt : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Blocked = history ? false : wantBlock,
            Ongoing = notice.Ongoing,
            Source = history ? RecordSources.DesktopHistory : RecordSources.DesktopListener
        };

        var inserted = store.InsertIgnore(record, notify: false);
        return new IngestResult(inserted, wantBlock && !history);
    }

    public static void Remove(SqliteRecordStore store, IncomingNotification notice)
    {
        if (ShouldIgnore(notice.PackageName)) return;
        var at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        store.BeginBulk();
        try
        {
            store.MarkRemoved(RecordIdentity.Uid(notice), at, notify: false);
            store.MarkRemoved($"{notice.Key}:{notice.PostedAt}", at, notify: false);
        }
        finally
        {
            store.EndBulk();
        }
    }

    private static string FirstText(IncomingNotification notice)
    {
        var text = (notice.Text ?? "").Trim();
        if (text.Length > 0) return text;
        var big = (notice.BigText ?? "").Trim();
        if (big.Length > 0) return big;
        return (notice.SubText ?? "").Trim();
    }
}
