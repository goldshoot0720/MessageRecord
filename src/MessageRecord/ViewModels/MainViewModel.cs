using System.ComponentModel;
using System.Windows.Input;
using MessageRecord.Core;
using MessageRecord.Capture;

namespace MessageRecord.ViewModels;

/// <summary>外框：頂列搜尋、側欄導覽與統計卡，以及右欄要顯示哪一頁。</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly List<AppChannelViewModel> _apps;
    private readonly StatsViewModel _stats;
    private readonly SettingsViewModel _settings;

    private object? _currentPane;
    private AppDetailViewModel? _detail;
    private NavItemViewModel _selectedNav;
    private string _searchText = "";

    public MainViewModel()
    {
        Source = NotificationSourceFactory.Create();

        _apps = Source.Load()
            .Select(a => new AppChannelViewModel(a))
            .ToList();

        TotalRecords = _apps.Sum(a => a.Count);
        BlockedRecords = _apps.Sum(a => a.BlockedCount);
        AllowedRecords = _apps.Sum(a => a.AllowedCount);

        NavItems = new List<NavItemViewModel>
        {
            new("apps", "應用程式", "IconHome", _apps.Count),
            new("all", "全部記錄", "IconList", TotalRecords),
            new("blocked", "已攔截", "IconCheck", BlockedRecords),
            new("allowed", "已允許", "IconCheck", AllowedRecords)
        };

        ToolItems = new List<NavItemViewModel>
        {
            new("stats", "統計", "IconChart"),
            new("settings", "設定", "IconGear")
        };

        _selectedNav = NavItems[0];

        AppList = new AppListViewModel(_apps);
        AppList.PropertyChanged += OnAppListChanged;

        _stats = new StatsViewModel(_apps);
        _settings = new SettingsViewModel();

        OpenSettingsCommand = new RelayCommand(() => SelectedNav = ToolItems[1]);

        BuildTodayStats();
        BuildDetail("all");
    }

    /// <summary>目前使用的通知來源（本版本是示範資料）。</summary>
    public INotificationSource Source { get; }

    public AppListViewModel AppList { get; }

    public ICommand OpenSettingsCommand { get; }

    public IReadOnlyList<NavItemViewModel> NavItems { get; }

    public IReadOnlyList<NavItemViewModel> ToolItems { get; }

    public int TotalRecords { get; }

    public int BlockedRecords { get; }

    public int AllowedRecords { get; }

    public object? CurrentPane
    {
        get => _currentPane;
        private set => Set(ref _currentPane, value);
    }

    public NavItemViewModel SelectedNav
    {
        get => _selectedNav;
        set
        {
            if (!Set(ref _selectedNav, value)) return;
            OnPropertyChanged(nameof(SelectedTool));
            OnPropertyChanged(nameof(SelectedSection));
            ApplyNav();
        }
    }

    /// <summary>統計 / 設定 兩個項目與上面的導覽共用一組選取狀態。</summary>
    public NavItemViewModel? SelectedTool
    {
        get => ToolItems.Contains(_selectedNav) ? _selectedNav : null;
        set { if (value is not null) SelectedNav = value; }
    }

    public NavItemViewModel? SelectedSection
    {
        get => NavItems.Contains(_selectedNav) ? _selectedNav : null;
        set { if (value is not null) SelectedNav = value; }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value)) AppList.SearchText = value;
        }
    }

    // ---------- 側欄統計卡 ----------

    public IReadOnlyList<double> TodayPoints { get; private set; } = Array.Empty<double>();

    public string TodayBlockedText { get; private set; } = "0";

    public string DeltaText { get; private set; } = "";

    public bool DeltaUp { get; private set; }

    public IReadOnlyList<string> HourLabels { get; private set; } = new[] { "00", "06", "12", "18", "24" };

    public string VersionText => "NotifBlock v1.0";

    public string TaglineText => "簡單・專注・不打擾";

    public string SourceText => $"{NotificationSourceFactory.PlatformName} · {Source.Name}";

    private void OnAppListChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppListViewModel.Selected))
            BuildDetail(_detail?.SelectedTab.Key ?? "all");
    }

    private void ApplyNav()
    {
        switch (_selectedNav.Key)
        {
            case "stats":
                CurrentPane = _stats;
                break;
            case "settings":
                CurrentPane = _settings;
                break;
            case "blocked":
            case "allowed":
            case "all":
                BuildDetail(_selectedNav.Key);
                break;
            default:
                BuildDetail(_detail?.SelectedTab.Key ?? "all");
                break;
        }
    }

    private void BuildDetail(string tab)
    {
        _detail?.Detach();

        var app = AppList.Selected;
        if (app is null)
        {
            _detail = null;
            CurrentPane = null;
            return;
        }

        _detail = new AppDetailViewModel(app, tab);
        CurrentPane = _detail;
    }

    private void BuildTodayStats()
    {
        var today = DateTime.Today;
        var hourly = new double[25];
        var todayBlocked = 0;
        var yesterdayBlocked = 0;

        foreach (var app in _apps)
        {
            foreach (var r in app.Records)
            {
                if (!r.Blocked) continue;

                if (r.ArrivalTime.Date == today)
                {
                    todayBlocked++;
                    hourly[Math.Clamp(r.ArrivalTime.Hour, 0, 24)]++;
                }
                else if (r.ArrivalTime.Date == today.AddDays(-1))
                {
                    yesterdayBlocked++;
                }
            }
        }

        // 只畫到目前這個小時，右半邊才不會是一條平線。
        var last = Math.Max(3, Math.Min(24, DateTime.Now.Hour + 1));

        // 讓曲線有起伏又不失真：以實際小時分佈為底，做一次輕微平滑。
        var smoothed = new double[last + 1];
        for (int i = 0; i < smoothed.Length; i++)
        {
            var a = hourly[Math.Max(0, i - 1)];
            var b = hourly[i];
            var c = hourly[Math.Min(hourly.Length - 1, i + 1)];
            smoothed[i] = (a + b * 2 + c) / 4 + 0.4;
        }

        TodayPoints = smoothed;
        HourLabels = Enumerable.Range(0, 5)
            .Select(i => (last * i / 4).ToString("00"))
            .ToList();
        TodayBlockedText = todayBlocked.ToString("N0");

        if (yesterdayBlocked == 0)
        {
            DeltaUp = todayBlocked > 0;
            DeltaText = todayBlocked > 0 ? "新增" : "持平";
        }
        else
        {
            var delta = (todayBlocked - yesterdayBlocked) * 100.0 / yesterdayBlocked;
            DeltaUp = delta >= 0;
            DeltaText = (DeltaUp ? "↑ " : "↓ ") + Math.Abs(Math.Round(delta)) + "%";
        }
    }
}
