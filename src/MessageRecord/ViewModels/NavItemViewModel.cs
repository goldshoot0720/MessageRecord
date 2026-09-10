using Avalonia;
using Avalonia.Media;
using MessageRecord.Core;

namespace MessageRecord.ViewModels;

/// <summary>側欄的一列導覽項目。</summary>
public sealed class NavItemViewModel : ObservableObject
{
    public NavItemViewModel(string key, string label, string iconKey, int? count = null)
    {
        Key = key;
        Label = label;
        Icon = FindIcon(iconKey);
        Count = count;
    }

    public string Key { get; }

    public string Label { get; }

    public Geometry? Icon { get; }

    public int? Count { get; }

    public bool HasCount => Count.HasValue;

    public string CountText => Count?.ToString("N0") ?? "";

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
