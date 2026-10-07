using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MessageRecord.Views;

internal static class JsonFiles
{
    private static readonly FilePickerFileType JsonType = new("JSON")
    {
        Patterns = new[] { "*.json" },
        MimeTypes = new[] { "application/json" }
    };

    public static async Task SaveAsync(TopLevel top, string fileName, string json)
    {
        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "匯出通知紀錄",
            SuggestedFileName = fileName,
            DefaultExtension = "json",
            FileTypeChoices = new[] { JsonType }
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(json);
    }

    public static async Task<string?> OpenAsync(TopLevel top)
    {
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "匯入通知紀錄",
            AllowMultiple = false,
            FileTypeFilter = new[] { JsonType }
        });
        var file = files.Count > 0 ? files[0] : null;
        if (file is null) return null;
        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
