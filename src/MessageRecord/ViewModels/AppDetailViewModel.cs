using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MessageRecord.Core;

namespace MessageRecord.ViewModels;

/// <summary>右欄分頁籤：所有記錄 / 已攔截 / 已允許。</summary>
public sealed class RecordTab
{
    public RecordTab(string key, string name, int count)
    {
        Key = key;
        Name = name;
        Count = count;
    }

    public string Key { get; }
    public string Name { get; }
    public int Count { get; }
    public string Label => $"{Name} ({Count})";
}

/// <summary>右欄：某個程式底下的所有攔截紀錄。</summary>
public sealed class AppDetailViewModel : ObservableObject
{
    private readonly string _recordQuery;
    private RecordTab _selectedTab;
    private bool _newestFirst;

    public AppDetailViewModel(AppChannelViewModel app, string initialTab = "all", string recordQuery = "", bool newestFirst = true)
    {
        App = app;
        _recordQuery = recordQuery.Trim();
        _newestFirst = newestFirst;

        Tabs = new ObservableCollection<RecordTab>
        {
            new("all", "所有記錄", app.Count),
            new("blocked", "已攔截", app.BlockedCount),
            new("allowed", "已允許", app.AllowedCount)
        };

        _selectedTab = Tabs.FirstOrDefault(t => t.Key == initialTab) ?? Tabs[0];

        ToggleOrderCommand = new RelayCommand(() => NewestFirst = !NewestFirst);

        App.PropertyChanged += OnAppChanged;
        Apply();
    }

    public AppChannelViewModel App { get; }

    public ObservableCollection<RecordTab> Tabs { get; }

    public ObservableCollection<RecordViewModel> Records { get; } = new();

    public ICommand ToggleOrderCommand { get; }

    public Func<(string FileName, string Json)>? ExportFactory { get; init; }

    public Action? ForgetApp { get; init; }

    public bool CanExport => ExportFactory is not null;

    public bool CanForget => ForgetApp is not null;

    public RecordTab SelectedTab
    {
        get => _selectedTab;
        set { if (Set(ref _selectedTab, value)) Apply(); }
    }

    public bool NewestFirst
    {
        get => _newestFirst;
        set
        {
            if (Set(ref _newestFirst, value))
            {
                OnPropertyChanged(nameof(OrderLabel));
                Apply();
            }
        }
    }

    public string OrderLabel => _newestFirst ? "最新在上" : "最舊在上";

    public bool IsEmpty => Records.Count == 0;

    public string EmptyText => SelectedTab.Key switch
    {
        "blocked" => "這個程式目前沒有被攔截的通知",
        "allowed" => "這個程式目前沒有放行的通知",
        _ => "這個程式還沒有任何通知紀錄"
    };

    /// <summary>切到別的程式時解除掛勾，避免舊的 ViewModel 被留住。</summary>
    public void Detach() => App.PropertyChanged -= OnAppChanged;

    public void SelectTab(string key)
    {
        var tab = Tabs.FirstOrDefault(t => t.Key == key);
        if (tab is not null) SelectedTab = tab;
    }

    private void OnAppChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppChannelViewModel.IsBlocking))
            OnPropertyChanged(nameof(App));
    }

    private void Apply()
    {
        IEnumerable<RecordViewModel> query = _selectedTab.Key switch
        {
            "blocked" => App.Records.Where(r => r.Blocked),
            "allowed" => App.Records.Where(r => !r.Blocked),
            _ => App.Records
        };

        if (_recordQuery.Length > 0)
        {
            query = query.Where(record =>
                record.Title.Contains(_recordQuery, StringComparison.OrdinalIgnoreCase) ||
                record.Body.Contains(_recordQuery, StringComparison.OrdinalIgnoreCase));
        }

        query = _newestFirst
            ? query.OrderByDescending(r => r.ArrivalTime)
            : query.OrderBy(r => r.ArrivalTime);

        Records.Clear();
        foreach (var r in query) Records.Add(r);

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }
}
