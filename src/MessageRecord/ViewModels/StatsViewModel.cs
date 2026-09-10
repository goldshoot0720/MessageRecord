using Avalonia.Media;
using MessageRecord.Core;

namespace MessageRecord.ViewModels;

/// <summary>統計頁裡的一列長條。</summary>
public sealed class AppBarViewModel
{
    public required string Name { get; init; }
    public required string Badge { get; init; }
    public required IBrush Accent { get; init; }
    public required int Count { get; init; }
    public required int BlockedCount { get; init; }
    public required double BarWidth { get; init; }
    public string CountText => $"{Count} 則";
    public string BlockedText => $"攔截 {BlockedCount}";
}

/// <summary>側欄「統計」頁：近 7 天趨勢與前幾名程式。</summary>
public sealed class StatsViewModel : ObservableObject
{
    private const double MaxBarWidth = 230;

    public StatsViewModel(IReadOnlyList<AppChannelViewModel> apps)
    {
        var today = DateTime.Today;

        var daily = new double[7];
        foreach (var app in apps)
        {
            foreach (var r in app.Records)
            {
                var offset = (int)(today - r.ArrivalTime.Date).TotalDays;
                if (offset is >= 0 and < 7 && r.Blocked) daily[6 - offset]++;
            }
        }

        WeekPoints = daily;
        DayLabels = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6).ToString("MM/dd")).ToList();

        WeekTotalText = ((int)daily.Sum()).ToString("N0");
        WeekPeakText = ((int)daily.Max()).ToString("N0");
        WeekAverageText = ((int)Math.Round(daily.Average())).ToString("N0");

        var max = Math.Max(1, apps.Count == 0 ? 1 : apps.Max(a => a.Count));
        TopApps = apps
            .OrderByDescending(a => a.Count)
            .Take(8)
            .Select(a => new AppBarViewModel
            {
                Name = a.DisplayName,
                Badge = a.Badge,
                Accent = a.AccentBrush,
                Count = a.Count,
                BlockedCount = a.BlockedCount,
                BarWidth = MaxBarWidth * a.Count / max
            })
            .ToList();
    }

    public IReadOnlyList<double> WeekPoints { get; }

    public IReadOnlyList<string> DayLabels { get; }

    public string WeekTotalText { get; }

    public string WeekPeakText { get; }

    public string WeekAverageText { get; }

    public IReadOnlyList<AppBarViewModel> TopApps { get; }
}
