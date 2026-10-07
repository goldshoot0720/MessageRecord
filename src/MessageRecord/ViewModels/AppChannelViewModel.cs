using Avalonia.Media;
using MessageRecord.Core;
using MessageRecord.Models;

namespace MessageRecord.ViewModels;

/// <summary>中欄的一列「程式名稱」，也是右欄紀錄的來源。</summary>
public sealed class AppChannelViewModel : ObservableObject
{
    private bool _isBlocking;

    public AppChannelViewModel(AppChannel model)
    {
        Model = model;
        _isBlocking = model.BlockEnabled;
        AccentBrush = SolidColorBrush.Parse(model.Accent);

        Records = model.Records
            .OrderByDescending(r => r.ArrivalTime)
            .Select(r => new RecordViewModel(r))
            .ToList();

        BlockedCount = Records.Count(r => r.Blocked);
        AllowedCount = Records.Count - BlockedCount;
    }

    public AppChannel Model { get; }

    public string Key => Model.Key;

    public string DisplayName => Model.DisplayName;

    public string Badge => Model.Badge;

    public IBrush AccentBrush { get; }

    public List<RecordViewModel> Records { get; }

    public int Count => Records.Count;

    public int BlockedCount { get; }

    public int AllowedCount { get; }

    public string CountText => $"{Count} 則通知";

    public string SummaryText => $"共 {Count} 則通知・已攔截 {BlockedCount} 則";

    public DateTime LastArrival => Records.Count == 0 ? DateTime.MinValue : Records[0].ArrivalTime;

    public int TodayCount => Records.Count(r => r.ArrivalTime.Date == DateTime.Today);

    public int TodayBlockedCount => Records.Count(r => r.Blocked && r.ArrivalTime.Date == DateTime.Today);

    /// <summary>這個程式是否啟用攔截；中欄與右欄的開關綁的是同一個值。</summary>
    public bool IsBlocking
    {
        get => _isBlocking;
        set
        {
            if (!Set(ref _isBlocking, value)) return;
            BlockingChanged?.Invoke(value);
        }
    }

    /// <summary>即時模式才會接上，把開關寫進資料庫。</summary>
    public Action<bool>? BlockingChanged { get; set; }
}
