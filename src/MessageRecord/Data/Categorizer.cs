using System.Text.RegularExpressions;

namespace MessageRecord.Data;

/// <summary>
/// 通知類型。優先採用系統給的 category，取不到才從內容推導。
/// 推導只做分組，不影響是否攔截。邏輯與手機版 Categorizer 相同。
/// </summary>
public static class Categorizer
{
    private static readonly Regex Promo = new("優惠|折扣|特賣|限時|活動|推薦|為你|снова|sale|deal|off", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Transaction = new("支出|存入|轉帳|交易|帳單|應繳|刷卡|扣款", RegexOptions.Compiled);
    private static readonly Regex Security = new("登入|驗證碼|安全|密碼|授權|login|verify", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Group = new("群組|社團|頻道|group|channel", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Of(string? systemCategory, string title, string text)
    {
        switch (systemCategory)
        {
            case "msg": return "一般訊息";
            case "email": return "一般郵件";
            case "call":
            case "missed_call": return "通話通知";
            case "promo": return "推廣訊息";
            case "transport":
            case "progress": return "系統通知";
            case "event":
            case "reminder": return "行事曆通知";
            case "err":
            case "sys":
            case "service": return "系統通知";
        }

        var blob = $"{title} {text}";
        if (Transaction.IsMatch(blob)) return "交易通知";
        if (Security.IsMatch(blob)) return "安全性通知";
        if (Promo.IsMatch(blob)) return "推廣訊息";
        if (Group.IsMatch(blob)) return "群組訊息";
        return "一般訊息";
    }
}
