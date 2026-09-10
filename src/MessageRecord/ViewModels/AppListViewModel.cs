using System.Collections.ObjectModel;
using System.Windows.Input;
using MessageRecord.Core;

namespace MessageRecord.ViewModels;

/// <summary>中欄：依程式名稱分類的清單。</summary>
public sealed class AppListViewModel : ObservableObject
{
    private static readonly string[] SortLabels = { "按通知數量", "按最新時間", "按程式名稱" };

    private readonly List<AppChannelViewModel> _source;

    private string _searchText = "";
    private int _sortIndex;
    private AppChannelViewModel? _selected;

    public AppListViewModel(List<AppChannelViewModel> source)
    {
        _source = source;
        CycleSortCommand = new RelayCommand(() => SortIndex = (SortIndex + 1) % SortLabels.Length);
        Apply();
        _selected = Apps.FirstOrDefault();
    }

    public ObservableCollection<AppChannelViewModel> Apps { get; } = new();

    public ICommand CycleSortCommand { get; }

    public string HeaderText => $"應用程式 ({Apps.Count})";

    public string SortLabel => SortLabels[_sortIndex];

    public bool IsEmpty => Apps.Count == 0;

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

    private void Apply()
    {
        IEnumerable<AppChannelViewModel> query = _source;

        var keyword = _searchText.Trim();
        if (keyword.Length > 0)
        {
            query = query.Where(a =>
                a.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                a.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase));
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

        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
