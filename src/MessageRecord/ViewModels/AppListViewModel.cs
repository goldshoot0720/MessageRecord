using System.Collections.ObjectModel;
using System.Windows.Input;
using MessageRecord.Core;

namespace MessageRecord.ViewModels;

/// <summary>中欄：依程式名稱分類的清單。</summary>
public sealed class AppListViewModel : ObservableObject
{
    private static readonly string[] SortLabels = { "按通知數量", "按最新時間", "按程式名稱" };

    private readonly List<AppChannelViewModel> _source;
    private readonly string? _noDataTitle;
    private readonly string? _noDataHint;

    private string _searchText = "";
    private int _sortIndex;
    private AppChannelViewModel? _selected;
    private string _emptyTitle = "找不到符合的應用程式";
    private string _emptyHint = "換個關鍵字試試";

    public AppListViewModel(List<AppChannelViewModel> source, string? noDataTitle = null, string? noDataHint = null)
    {
        _source = source;
        _noDataTitle = noDataTitle;
        _noDataHint = noDataHint;
        CycleSortCommand = new RelayCommand(() => SortIndex = (SortIndex + 1) % SortLabels.Length);
        Apply();
        _selected = Apps.FirstOrDefault();
    }

    public ObservableCollection<AppChannelViewModel> Apps { get; } = new();

    public ICommand CycleSortCommand { get; }

    public string HeaderText => $"應用程式 ({Apps.Count})";

    public string SortLabel => SortLabels[_sortIndex];

    public bool IsEmpty => Apps.Count == 0;

    public string EmptyTitle => _emptyTitle;

    public string EmptyHint => _emptyHint;

    public string SearchText
    {
        get => _searchText;
        set { if (Set(ref _searchText, value)) Apply(); }
    }

    public int SortIndex
    {
        get => _sortIndex;
        set
        {
            if (Set(ref _sortIndex, value))
            {
                OnPropertyChanged(nameof(SortLabel));
                Apply();
            }
        }
    }

    public AppChannelViewModel? Selected
    {
        get => _selected;
        set => Set(ref _selected, value);
    }

    public void Reset(IEnumerable<AppChannelViewModel> source, string? selectedKey)
    {
        _source.Clear();
        _source.AddRange(source);
        Apply();
        if (selectedKey is null) return;
        var match = Apps.FirstOrDefault(app => app.Key == selectedKey);
        if (match is not null) Selected = match;
    }

    private void Apply()
    {
        IEnumerable<AppChannelViewModel> query = _source;

        var keyword = _searchText.Trim();
        if (keyword.Length > 0)
        {
            query = query.Where(app =>
                app.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                app.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                app.Records.Any(record =>
                    record.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    record.Body.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        query = _sortIndex switch
        {
            1 => query.OrderByDescending(a => a.LastArrival),
            2 => query.OrderBy(a => a.DisplayName, StringComparer.CurrentCulture),
            _ => query.OrderByDescending(a => a.Count)
        };

        Apps.Clear();
        foreach (var app in query) Apps.Add(app);

        if (_selected is null || !Apps.Contains(_selected))
            Selected = Apps.FirstOrDefault();

        if (Apps.Count == 0 && _source.Count == 0 && keyword.Length == 0 && _noDataTitle is not null)
        {
            _emptyTitle = _noDataTitle;
            _emptyHint = _noDataHint ?? "";
        }
        else
        {
            _emptyTitle = "找不到符合的應用程式";
            _emptyHint = "換個關鍵字試試";
        }

        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyHint));
    }
}
