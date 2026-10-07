namespace MessageRecord.Capture;

/// <summary>由套件識別或 AUMID 推出畫面上的程式名稱。</summary>
public static class AppNames
{
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["com.apple.MobileSMS"] = "訊息",
        ["com.apple.mail"] = "郵件",
        ["com.apple.iCal"] = "行事曆",
        ["com.apple.reminders"] = "提醒事項",
        ["com.apple.Safari"] = "Safari",
        ["com.apple.Notes"] = "備忘錄",
        ["com.tencent.xinWeChat"] = "微信",
        ["com.tencent.qq"] = "QQ",
        ["jp.naver.line"] = "LINE",
        ["jp.naver.line.mac"] = "LINE",
        ["jp.naver.line.android"] = "LINE",
        ["com.google.android.gm"] = "Gmail",
        ["com.apple.mobilecal"] = "行事曆"
    };

    private static readonly HashSet<string> Skip = new(StringComparer.OrdinalIgnoreCase)
    {
        "android", "app", "ios", "macos", "osx", "desktop", "mobile", "www"
    };

    public static string FromIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return "未知程式";
        if (Known.TryGetValue(identifier, out var known)) return known;

        var head = identifier.Split('!')[0];
        var family = head.Split('_')[0];
        if (Known.TryGetValue(family, out known)) return known;

        var parts = family.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            if (Skip.Contains(parts[i]) || parts[i].Length <= 1) continue;
            return Prettify(parts[i]);
        }

        return family;
    }

    private static string Prettify(string token)
    {
        if (token.Any(char.IsLower) && token.Any(char.IsUpper)) return token;
        if (token.Length == 1) return token.ToUpperInvariant();
        return char.ToUpperInvariant(token[0]) + token[1..];
    }
}
