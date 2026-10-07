using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MessageRecord.Capture;

namespace MessageRecord.Data;

/// <summary>
/// 與手機版 NotiGuardListenerService.uidOf 相同的穩定鍵：
/// 通知識別碼、來源訊息時間與內容的 SHA-256。
/// </summary>
public static class RecordIdentity
{
    public static string Uid(IncomingNotification notice)
    {
        var messageTime = ResolveMessageTime(notice);
        var parts = new List<string>
        {
            notice.Key ?? "",
            messageTime.ToString(CultureInfo.InvariantCulture),
            Trim(notice.Title),
            Trim(notice.Text),
            Trim(notice.BigText),
            Trim(notice.SubText)
        };

        foreach (var message in notice.Messages)
        {
            parts.Add(message.Timestamp.ToString(CultureInfo.InvariantCulture));
            parts.Add(message.Text ?? "");
            parts.Add(message.Sender ?? "");
            parts.Add(message.Mime ?? "");
            parts.Add(message.Uri ?? "");
        }

        var fingerprint = string.Concat(parts.Select(part => $"{part.Length}:{part}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
        return "notification-v2:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// postTime 是每次發布或更新的時間，不能當成訊息本身的時間。
    /// 缺少來源時間時退回 postTime，避免把真正的新訊息合併掉。
    /// </summary>
    public static long ResolveMessageTime(IncomingNotification notice)
    {
        if (notice.Messages.Count > 0 && notice.Messages[^1].Timestamp > 0)
            return notice.Messages[^1].Timestamp;
        if (notice.When > 0) return notice.When;
        return notice.PostedAt;
    }

    private static string Trim(string? value) => (value ?? "").Trim();
}
