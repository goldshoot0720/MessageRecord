using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MessageRecord.Data;

/// <summary>
/// 跨平台 JSON。欄位對齊 shared/notiguard-record.schema.json，時間是 Unix epoch 毫秒。
/// 手機版匯出的檔案可以直接匯入。
/// </summary>
public static class RecordExporter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string ToJson(IReadOnlyList<StoredNotification> records, string deviceId)
    {
        var file = new ExportFile
        {
            Version = 1,
            ExportedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            DeviceId = deviceId,
            Records = records.Select(record => ToExport(record, deviceId)).ToList()
        };
        return JsonSerializer.Serialize(file, WriteOptions);
    }

    public static IReadOnlyList<StoredNotification> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        JsonElement records;
        if (root.ValueKind == JsonValueKind.Array)
            records = root;
        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("records", out var nested))
            records = nested;
        else
            throw new FormatException("匯入檔需要 records 陣列。");

        if (records.ValueKind != JsonValueKind.Array)
            throw new FormatException("records 必須是陣列。");

        var list = new List<StoredNotification>();
        foreach (var item in records.EnumerateArray())
        {
            var parsed = item.Deserialize<ExportRecord>(ReadOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Uid) || string.IsNullOrWhiteSpace(parsed.PackageName))
                continue;
            list.Add(new StoredNotification
            {
                Uid = parsed.Uid,
                PackageName = parsed.PackageName,
                AppLabel = string.IsNullOrWhiteSpace(parsed.AppLabel) ? parsed.PackageName : parsed.AppLabel,
                Title = parsed.Title ?? "",
                Text = parsed.Text ?? "",
                ChannelId = parsed.ChannelId,
                Category = parsed.Category,
                PostedAt = parsed.PostedAt,
                RemovedAt = parsed.RemovedAt,
                Blocked = parsed.Blocked,
                Ongoing = parsed.Ongoing,
                Source = string.IsNullOrWhiteSpace(parsed.Source) ? RecordSources.CompanionSync : parsed.Source
            });
        }

        return list;
    }

    public static string FileName(string? appLabel)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        var scope = string.IsNullOrWhiteSpace(appLabel)
            ? "all"
            : Regex.Replace(appLabel, @"[^\p{L}\p{N}]", "");
        if (string.IsNullOrEmpty(scope)) scope = "all";
        return $"notiguard-{scope}-{stamp}.json";
    }

    public static string DeviceId()
    {
        var os = OperatingSystem.IsMacOS() ? "macos"
            : OperatingSystem.IsWindows() ? "windows"
            : OperatingSystem.IsLinux() ? "linux"
            : "desktop";
        var machine = Environment.MachineName.ToLowerInvariant().Replace(' ', '-');
        return $"desktop-{os}-{machine}";
    }

    private static ExportRecord ToExport(StoredNotification record, string deviceId) => new()
    {
        Uid = record.Uid,
        PackageName = record.PackageName,
        AppLabel = record.AppLabel,
        Title = record.Title,
        Text = record.Text,
        ChannelId = record.ChannelId,
        Category = record.Category,
        PostedAt = record.PostedAt,
        RemovedAt = record.RemovedAt,
        Blocked = record.Blocked,
        Ongoing = record.Ongoing,
        Source = record.Source,
        DeviceId = deviceId
    };

    private sealed class ExportFile
    {
        [JsonPropertyName("version")] public int Version { get; set; } = 1;
        [JsonPropertyName("exportedAt")] public long ExportedAt { get; set; }
        [JsonPropertyName("deviceId")] public string DeviceId { get; set; } = "";
        [JsonPropertyName("records")] public List<ExportRecord> Records { get; set; } = new();
    }

    private sealed class ExportRecord
    {
        [JsonPropertyName("uid")] public string Uid { get; set; } = "";
        [JsonPropertyName("packageName")] public string PackageName { get; set; } = "";
        [JsonPropertyName("appLabel")] public string AppLabel { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("text")] public string Text { get; set; } = "";
        [JsonPropertyName("channelId")] public string? ChannelId { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("postedAt")] public long PostedAt { get; set; }
        [JsonPropertyName("removedAt")] public long? RemovedAt { get; set; }
        [JsonPropertyName("blocked")] public bool Blocked { get; set; }
        [JsonPropertyName("ongoing")] public bool Ongoing { get; set; }
        [JsonPropertyName("source")] public string Source { get; set; } = "";
        [JsonPropertyName("deviceId")] public string? DeviceId { get; set; }
    }
}
