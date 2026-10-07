using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Threading;
using MessageRecord.Capture;
using MessageRecord.Core;
using MessageRecord.Data;
using MessageRecord.Models;

namespace MessageRecord.ViewModels;

/// <summary>外框：頂列搜尋、側欄導覽與統計卡，以及右欄要顯示哪一頁。</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly AppSession? _session;
    private readonly List<NavItemViewModel> _navItems;
    private List<AppChannelViewModel> _apps;
    private StatsViewModel _stats = null!;
    private readonly SettingsViewModel _settings;

    private object? _currentPane;
    private AppDetailViewModel? _detail;
    private NavItemViewModel _selectedNav;
    private string _searchText = "";
    private bool _newestFirst = true;
    private int _reloadQueued;

    public MainViewModel()
    {
        Source = NotificationSourceFactory.Create();
        _apps = Source.Load().Select(app => new AppChannelViewModel(app)).ToList();
        _navItems = CreateNav(_apps);
        _selectedNav = _navItems[0];
        _settings = new SettingsViewModel();
        InitializeChrome();
    }

    public MainViewModel(AppSession session)
    {
        _session = session;
        session.Changed += ScheduleReload;
        session.StatusChanged += ScheduleStatus;
        Source = new SampleNotificationSource();
        _apps = Map(session.LoadChannels());
        _navItems = CreateNav(_apps);
        _selectedNav = _navItems[0];
        _settings = new SettingsViewModel(session);
        InitializeChrome();
    }

    /// <summary>設計預覽用的通知來源。即時模式的資料來自 <see cref="AppSession"/>。</summary>
    public INotificationSource Source { get; }

    public AppListViewModel AppList { get; private set; } = null!;

    public ICommand OpenSettingsCommand { get; private set; } = null!;

    public IReadOnlyList<NavItemViewModel> NavItems => _navItems;

    public IReadOnlyList<NavItemViewModel> ToolItems { get; private set; } = Array.Empty<NavItemViewModel>();

    public int TotalRecords { get; private set; }

    public int BlockedRecords { get; private set; }

    public int AllowedRecords { get; private set; }

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
        get => _navItems.Contains(_selectedNav) ? _selectedNav : null;
        set { if (value is not null) SelectedNav = value; }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!Set(ref _searchText, value)) return;
            AppList.SearchText = value;
            if (_selectedNav.Key is not ("stats" or "settings"))
                BuildDetail(VisibleTab());
        }
    }

    public IReadOnlyList<double> TodayPoints { get; private set; } = Array.Empty<double>();

    public string TodayBlockedText { get; private set; } = "0";

    public string DeltaText { get; private set; } = "";

    public bool DeltaUp { get; private set; }

    public IReadOnlyList<string> HourLabels { get; private set; } = new[] { "00", "06", "12", "18", "24" };

    public string VersionText => _session is null ? "NotifBlock v1.0" : "MessageRecord 1.1.0";

    public string TaglineText => _session?.ShortStatus ?? "簡單・專注・不打擾";

    private void InitializeChrome()
    {
        ToolItems = new List<NavItemViewModel>
        {
            new("stats", "統計", "IconChart"),
            new("settings", "設定", "IconGear")
        };

        AppList = new AppListViewModel(
            _apps,
            _session is null ? null : "還沒有通知紀錄",
            _session is null ? null : "授權讀取通知中心，或從設定匯入 JSON");
        AppList.PropertyChanged += OnAppListChanged;
        _stats = new StatsViewModel(_apps);
        OpenSettingsCommand = new RelayCommand(() => SelectedNav = ToolItems[1]);
        PublishCounts();
        BuildTodayStats();
        BuildDetail("all");
    }

    private static List<NavItemViewModel> CreateNav(List<AppChannelViewModel> apps)
    {
        var total = apps.Sum(app => app.Count);
        var blocked = apps.Sum(app => app.BlockedCount);
        var allowed = apps.Sum(app => app.AllowedCount);
        return new List<NavItemViewModel>
        {
            new("apps", "應用程式", "IconHome", apps.Count),
            new("all", "全部記錄", "IconList", total),
            new("blocked", "已攔截", "IconCheck", blocked),
            new("allowed", "已允許", "IconCheck", allowed)
        };
    }

    private List<AppChannelViewModel> Map(IEnumerable<AppChannel> channels)
    {
        var apps = channels.Select(channel => new AppChannelViewModel(channel)).ToList();
        if (_session is null) return apps;
        foreach (var app in apps)
        {
            var current = app;
            current.BlockingChanged = blocking => _session.SetBlocking(current.Key, current.DisplayName, blocking);
        }

        return apps;
    }

    private void ScheduleReload()
    {
        if (Interlocked.Exchange(ref _reloadQueued, 1) == 1) return;
        Dispatcher.UIThread.Post(Reload);
    }

    private void ScheduleStatus()
    {
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(TaglineText));
            _settings.RefreshStatus();
        });
    }

    private void Reload()
    {
        if (_session is null)
        {
            Interlocked.Exchange(ref _reloadQueued, 0);
            return;
        }

        var selectedKey = AppList.Selected?.Key;
        var tab = VisibleTab();
        _apps = Map(_session.LoadChannels());
        AppList.Reset(_apps, selectedKey);
        PublishCounts();
        _stats = new StatsViewModel(_apps);
        BuildTodayStats();
        ApplyNav(tab);
        Interlocked.Exchange(ref _reloadQueued, 0);
    }

    private void PublishCounts()
    {
        TotalRecords = _apps.Sum(app => app.Count);
        BlockedRecords = _apps.Sum(app => app.BlockedCount);
        AllowedRecords = TotalRecords - BlockedRecords;
        _navItems[0].SetCount(_apps.Count);
        _navItems[1].SetCount(TotalRecords);
        _navItems[2].SetCount(BlockedRecords);
        _navItems[3].SetCount(AllowedRecords);
    }

    private void OnAppListChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppListViewModel.Selected)) return;
        if (_selectedNav.Key is "stats" or "settings") return;
        BuildDetail(VisibleTab());
    }

    private string VisibleTab() => _detail?.SelectedTab.Key ?? "all";

    private void ApplyNav(string? tab = null)
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
                BuildDetail(tab ?? VisibleTab());
                break;
        }
    }

    private void BuildDetail(string tab)
    {
        if (_detail is not null) _newestFirst = _detail.NewestFirst;
        _detail?.Detach();

        var app = AppList.Selected;
        if (app is null)
        {
            _detail = null;
            CurrentPane = null;
            return;
        }

        var keyword = _searchText.Trim();
        var nameMatch = keyword.Length == 0
            || app.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || app.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        _detail = new AppDetailViewModel(app, tab, nameMatch ? "" : keyword, _newestFirst)
        {
            ExportFactory = _session is null ? null : () => (_session.ExportFileName(app.DisplayName), _session.ExportJson(app.Key)),
            ForgetApp = _session is null ? null : () => _session.Forget(app.Key)
        };
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
            foreach (var record in app.Records)
            {
                if (!record.Blocked) continue;

                if (record.ArrivalTime.Date == today)
                {
                    todayBlocked++;
                    hourly[Math.Clamp(record.ArrivalTime.Hour, 0, 24)]++;
                }
                else if (record.ArrivalTime.Date == today.AddDays(-1))
                {
                    yesterdayBlocked++;
                }
            }
        }

        var last = Math.Max(3, Math.Min(24, DateTime.Now.Hour + 1));
        var smoothed = new double[last + 1];
        for (var i = 0; i < smoothed.Length; i++)
        {
            var left = hourly[Math.Max(0, i - 1)];
            var mid = hourly[i];
            var right = hourly[Math.Min(hourly.Length - 1, i + 1)];
            smoothed[i] = (left + mid * 2 + right) / 4 + 0.4;
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

        OnPropertyChanged(nameof(TodayPoints));
        OnPropertyChanged(nameof(HourLabels));
        OnPropertyChanged(nameof(TodayBlockedText));
        OnPropertyChanged(nameof(DeltaText));
        OnPropertyChanged(nameof(DeltaUp));
    }
}
