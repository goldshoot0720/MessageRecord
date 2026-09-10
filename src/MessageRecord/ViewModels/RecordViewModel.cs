using Avalonia.Media;
using MessageRecord.Core;
using MessageRecord.Models;

namespace MessageRecord.ViewModels;

/// <summary>一筆通知紀錄在右欄清單裡的樣貌。</summary>
public sealed class RecordViewModel : ObservableObject
{
    private static readonly IBrush BlockedFg = SolidColorBrush.Parse("#FF5AA0FF");
    private static readonly IBrush BlockedBg = SolidColorBrush.Parse("#FF16294A");
    private static readonly IBrush AllowedFg = SolidColorBrush.Parse("#FF4ADE80");
    private static readonly IBrush AllowedBg = SolidColorBrush.Parse("#FF14301F");

    private readonly NotificationRecord _model;

    public RecordViewModel(NotificationRecord model) => _model = model;

    public long Id => _model.Id;

    public string Title => string.IsNullOrWhiteSpace(_model.Title) ? "（無標題）" : _model.Title;

    public string Body => string.IsNullOrWhiteSpace(_model.Body) ? "（無內容）" : _model.Body;

    public DateTime ArrivalTime => _model.ArrivalTime;

    public string TimeText => TimeLabel(_model.ArrivalTime);

    public bool Blocked => _model.Blocked;

    public string StatusText => _model.Blocked ? "已攔截" : "已允許";

    public IBrush StatusForeground => _model.Blocked ? BlockedFg : AllowedFg;

    public IBrush StatusBackground => _model.Blocked ? BlockedBg : AllowedBg;

    public IBrush StatusBorder => SolidColorBrush.Parse(_model.Blocked ? "#195990" : "#17613D");

    public Geometry AvatarIcon => Geometry.Parse(Title switch
    {
        var t when t.Contains("群") || t.Contains("同學") => "M12 2 A3 3 0 1 1 11.99 2 M4 5 A2 2 0 1 1 3.99 5 M20 5 A2 2 0 1 1 19.99 5 M5 21 L5 18 C5 10 19 10 19 18 L19 21 Z M0 17 C0 12 4 11 6 13 L3 17 Z M24 17 L21 17 L18 13 C20 11 24 12 24 17 Z",
        var t when t.Contains("提醒") => "M12 1 C7 1 5 5 5 10 L5 15 L2 19 L22 19 L19 15 L19 10 C19 5 17 1 12 1 Z M9 21 A3 3 0 0 0 15 21 Z",
        var t when t.Contains("官方") || t.Contains("公告") => "M2 9 L10 9 L21 3 L21 21 L10 15 L2 15 Z M5 16 L9 16 L11 23 L7 23 Z",
        var t when t.Contains("Pay") || t.Contains("交易") => "M5 1 L15 1 L21 7 L21 23 L5 23 Z M15 2 L15 8 L20 8 Z",
        var t when t.Contains("GAME") => "M7 4 Q12 1 17 4 Q22 8 23 19 Q19 23 16 18 L8 18 Q5 23 1 19 Q2 8 7 4 Z",
        _ => "M12 1 A5 5 0 1 1 11.99 1 M2 22 L2 20 C2 10 22 10 22 20 L22 22 Q12 25 2 22 Z"
    });

    /// <summary>頭像方塊裡的字，取標題第一個字。</summary>
    public string Avatar => Title[..1];

    public static string TimeLabel(DateTime t)
    {
        var day = t.Date;
        if (day == DateTime.Today) return "今天 " + t.ToString("HH:mm");
        if (day == DateTime.Today.AddDays(-1)) return "昨天 " + t.ToString("HH:mm");
        if (day > DateTime.Today.AddDays(-7)) return t.ToString("ddd HH:mm");
        return t.ToString("MM/dd HH:mm");
    }
}
