using System.Text;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;

namespace MessageRecord.Capture;

/// <summary>
/// 讀 Windows 通知資料庫 %LOCALAPPDATA%\Microsoft\Windows\Notifications\wpndatabase.db。
/// 不需要特殊權限，但系統會清掉過期紀錄，所以程式會另存一份。
/// </summary>
public sealed class WindowsWpnFeed
{
    public string Name => "windows-wpn";

    public static string DefaultPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Notifications", "wpndatabase.db");

    public FeedSnapshot Poll() => Read(DefaultPath());

    public static FeedSnapshot Read(string path)
    {
        if (!File.Exists(path))
        {
            return FeedSnapshot.Failed(
                "找不到通知資料庫",
                "沒有找到 Windows 通知資料庫 wpndatabase.db。可以先從設定匯入手機版 JSON。");
        }

        var temp = CopyDatabase(path, out var error);
        if (temp is null)
        {
            return FeedSnapshot.Failed(
                "無法讀取通知資料庫",
                "Windows 通知資料庫目前讀不到。" + error);
        }

        try
        {
            var items = ReadCopy(temp);
            return new FeedSnapshot
            {
                Accessible = true,
                Recognized = true,
                ShortStatus = "已連接通知紀錄",
                LongStatus = $"已從 Windows 通知資料庫讀到 {items.Count} 則。新通知會依攔截開關寫進本機紀錄。",
                Items = items
            };
        }
        catch (Exception ex)
        {
            return FeedSnapshot.Failed("無法辨識通知資料庫", "wpndatabase.db 的資料表與預期不同。" + ex.Message);
        }
        finally
        {
            TryDelete(temp);
        }
    }

    public static IReadOnlyList<IncomingNotification> ReadCopy(string path)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT n.Id, h.PrimaryId, n.Payload, n.ArrivalTime
            FROM Notification n
            JOIN NotificationHandler h ON n.HandlerId = h.RecordId
            """;
        using var reader = command.ExecuteReader();
        var list = new List<IncomingNotification>();
        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            var primary = reader.IsDBNull(1) ? "" : reader.GetString(1);
            if (NotificationIngestor.ShouldIgnore(primary)) continue;
            var payload = ReadPayload(reader, 2);
            var (title, body) = ReadToast(payload);
            var posted = FileTimeToUnixMs(reader.IsDBNull(3) ? 0 : reader.GetInt64(3));
            list.Add(new IncomingNotification
            {
                Key = $"{primary}:{id}",
                PackageName = primary,
                AppLabel = AppNames.FromIdentifier(primary),
                Title = title,
                Text = body,
                PostedAt = posted,
                When = posted
            });
        }

        return list;
    }

    public static long FileTimeToUnixMs(long fileTime)
    {
        if (fileTime <= 0) return 0;
        try
        {
            return new DateTimeOffset(DateTime.FromFileTimeUtc(fileTime)).ToUnixTimeMilliseconds();
        }
        catch (ArgumentOutOfRangeException)
        {
            return 0;
        }
    }

    public static (string Title, string Body) ReadToast(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return ("", "");
        try
        {
            var document = XDocument.Parse(payload);
            var texts = document.Descendants()
                .Where(element => element.Name.LocalName == "text")
                .Select(element => element.Value.Trim())
                .Where(value => value.Length > 0)
                .ToList();
            if (texts.Count == 0) return ("", "");
            var title = texts[0];
            var body = string.Join(" ", texts.Skip(1));
            return (title, body);
        }
        catch (System.Xml.XmlException)
        {
            return ("", "");
        }
    }

    private static string ReadPayload(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return "";
        if (reader.GetFieldType(ordinal) == typeof(string)) return reader.GetString(ordinal);
        var bytes = (byte[])reader.GetValue(ordinal);
        if (bytes.Length >= 2 && bytes[1] == 0) return Encoding.Unicode.GetString(bytes);
        return Encoding.UTF8.GetString(bytes);
    }

    private static SqliteConnection Open(string path)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        return connection;
    }

    internal static string? CopyDatabase(string path, out string error)
    {
        var temp = Path.Combine(Path.GetTempPath(), "messagerecord-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            File.Copy(path, temp, true);
            error = "";
            return temp;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
            TryDelete(temp);
            return null;
        }
    }

    internal static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
