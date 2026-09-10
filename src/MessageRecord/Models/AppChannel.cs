namespace MessageRecord.Models;

/// <summary>一個「程式名稱」分類：底下掛著該程式的所有通知紀錄。</summary>
public sealed class AppChannel
{
    /// <summary>唯一鍵（AUMID 或程序名稱）。</summary>
    public string Key { get; init; } = "";

    /// <summary>顯示用的程式名稱，介面上以它分類。</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>圖示方塊中的字。</summary>
    public string Badge { get; init; } = "";

    /// <summary>圖示方塊底色（ARGB 字串）。</summary>
    public string Accent { get; init; } = "#FF2B7FFF";

    /// <summary>是否對這個程式啟用攔截。</summary>
    public bool BlockEnabled { get; init; } = true;

    public List<NotificationRecord> Records { get; } = new();
}
