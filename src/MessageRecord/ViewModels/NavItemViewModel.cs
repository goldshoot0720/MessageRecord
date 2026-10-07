using Avalonia;
using Avalonia.Media;
using MessageRecord.Core;

namespace MessageRecord.ViewModels;

/// <summary>側欄的一列導覽項目。</summary>
public sealed class NavItemViewModel : ObservableObject
{
    private int? _count;

    public NavItemViewModel(string key, string label, string iconKey, int? count = null)
    {
        Key = key;
        Label = label;
        Icon = FindIcon(iconKey);
        _count = count;
    }

    public string Key { get; }

    public string Label { get; }

    public Geometry? Icon { get; }

    public int? Count => _count;

    public bool HasCount => _count.HasValue;

    public string CountText => _count?.ToString("N0") ?? "";

    public void SetCount(int count)
    {
        if (_count == count) return;
        _count = count;
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(HasCount));
    }

    private static Geometry? FindIcon(string key)
    {
        if (Application.Current is { } app &&
            app.TryGetResource(key, null, out var value))
        {
            return value as Geometry;
        }

        return null;
    }
}
