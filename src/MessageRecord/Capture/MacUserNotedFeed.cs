using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MessageRecord.Capture;

/// <summary>
/// 讀 macOS 通知中心資料庫。新版系統把這個檔案放在「完整磁碟取用權」後面。
/// 資料表各版本不同，讀得到就依 record/app 解析，讀不到就把原因顯示在設定頁。
/// </summary>
public sealed class MacUserNotedFeed
{
    public string Name => "macos-usernoted";

    public static string DefaultPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "Group Containers", "group.com.apple.usernoted", "db2", "db");

    public FeedSnapshot Poll() => Read(DefaultPath());

    public static FeedSnapshot Read(string path)
    {
        if (!File.Exists(path))
        {
            return FeedSnapshot.Failed(
                "找不到通知中心",
                "這台 Mac 沒有通知中心資料庫。可以先從設定匯入手機版 JSON。");
        }

        var temp = WindowsWpnFeed.CopyDatabase(path, out var error);
        if (temp is null)
        {
            return FeedSnapshot.Failed(
                "需要完整磁碟取用權",
                "macOS 拒絕讀取通知中心資料庫。請到「系統設定 → 隱私權與安全性 → 完整磁碟取用權」加入 MessageRecord，然後重新開啟。也可以先從設定匯入手機版 JSON。（" + error + "）");
        }

        try
        {
            return ReadCopy(temp);
        }
        finally
        {
            WindowsWpnFeed.TryDelete(temp);
        }
    }

    public static FeedSnapshot ReadCopy(string path)
    {
        try
        {
            using var connection = Open(path);
            var tables = TableNames(connection);
            var items = TryQuery(connection);
            if (items is null)
            {
                return new FeedSnapshot
                {
                    Accessible = true,
                    Recognized = false,
                    ShortStatus = "無法辨識通知中心",
                    LongStatus = "已開啟通知中心資料庫，但資料表與已知格式不同：" + string.Join(", ", tables)
                };
            }

            return new FeedSnapshot
            {
                Accessible = true,
                Recognized = true,
                ShortStatus = "已連接通知中心",
                LongStatus = $"已從 macOS 通知中心讀到 {items.Count} 則。新通知會依攔截開關寫進本機紀錄。桌面系統沒有公開 API 可把其他程式的通知移出通知中心。",
                Items = items
            };
        }
        catch (SqliteException ex) when (ex.Message.Contains("authoriz", StringComparison.OrdinalIgnoreCase)
                                          || ex.SqliteErrorCode == 23)
        {
            return FeedSnapshot.Failed(
                "需要完整磁碟取用權",
                "macOS 拒絕讀取通知中心資料庫。請到「系統設定 → 隱私權與安全性 → 完整磁碟取用權」加入 MessageRecord，然後重新開啟。（" + ex.Message + "）");
        }
        catch (Exception ex)
        {
            return FeedSnapshot.Failed("無法讀取通知中心", ex.Message);
        }
    }

    private static List<IncomingNotification>? TryQuery(SqliteConnection connection)
    {
        string[] statements =
        {
            """
            SELECT rec.rec_id, app.identifier, rec.data, rec.delivered_date
            FROM record rec
            LEFT JOIN app ON app.app_id = rec.app_id
            """,
            """
            SELECT rec.rec_id, app.bundleid, rec.data, rec.delivered_date
            FROM record rec
            LEFT JOIN app ON app.app_id = rec.app_id
            """
        };

        foreach (var statement in statements)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = statement;
                using var reader = command.ExecuteReader();
                var list = new List<IncomingNotification>();
                while (reader.Read())
                {
                    var id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
                    var identifier = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var data = ReadCell(reader, 2);
                    var delivered = reader.IsDBNull(3) ? 0 : Convert.ToDouble(reader.GetValue(3));
                    var text = PlistText.Read(data);
                    var label = string.IsNullOrWhiteSpace(text.AppName)
                        ? AppNames.FromIdentifier(identifier)
                        : text.AppName;
                    if (string.IsNullOrWhiteSpace(identifier)) identifier = label;
                    var posted = CocoaToUnixMs(delivered);
                    list.Add(new IncomingNotification
                    {
                        Key = $"{identifier}:{id}",
                        PackageName = identifier,
                        AppLabel = label,
                        Title = text.Title,
                        Text = text.Body,
                        PostedAt = posted,
                        When = posted
                    });
                }

                return list;
            }
            catch (SqliteException)
            {
                // 這版系統的欄位名稱不同，換下一個查詢。
            }
        }

        return null;
    }

    public static long CocoaToUnixMs(double value)
    {
        if (value <= 0) return 0;
        if (value > 1_000_000_000_000) return (long)value;
        if (value > 1_000_000_000) return (long)(value * 1000);
        var epoch = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return epoch.AddSeconds(value).ToUnixTimeMilliseconds();
    }

    private static List<string> TableNames(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read()) names.Add(reader.GetString(0));
        return names;
    }

    private static byte[] ReadCell(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return Array.Empty<byte>();
        var value = reader.GetValue(ordinal);
        return value switch
        {
            byte[] bytes => bytes,
            string text => Encoding.UTF8.GetBytes(text),
            _ => Array.Empty<byte>()
        };
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
}

/// <summary>從 plist（XML 或 bplist）取出通知標題與內文。</summary>
public static class PlistText
{
    public readonly record struct Content(string Title, string Body, string AppName);

    public static Content Read(byte[] data)
    {
        if (data.Length == 0) return new Content("", "", "");
        var xml = Encoding.UTF8.GetString(data);
        if (data.Length > 6 && data[0] == (byte)'b' && data[1] == (byte)'p')
            xml = ConvertBinary(data) ?? xml;

        if (xml.Contains("<key>", StringComparison.Ordinal))
        {
            var title = Value(xml, "title") ?? Value(xml, "titl") ?? "";
            var body = Value(xml, "body") ?? Value(xml, "informativeText") ?? Value(xml, "subtitle") ?? "";
            var app = Value(xml, "appName") ?? Value(xml, "displayName") ?? Value(xml, "app") ?? "";
            if (title.Length > 0 || body.Length > 0 || app.Length > 0)
                return new Content(title, body, app);
        }

        var strings = ScanUtf16(data).Where(Useful).Take(3).ToList();
        var fallbackTitle = strings.Count > 0 ? strings[0] : "";
        var fallbackBody = strings.Count > 1 ? strings[1] : "";
        return new Content(fallbackTitle, fallbackBody, "");
    }

    private static string? ConvertBinary(byte[] data)
    {
        var path = Path.Combine(Path.GetTempPath(), "messagerecord-" + Guid.NewGuid().ToString("N") + ".plist");
        try
        {
            File.WriteAllBytes(path, data);
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "plutil",
                ArgumentList = { "-convert", "xml1", "-o", "-", path },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            WindowsWpnFeed.TryDelete(path);
        }
    }

    private static string? Value(string xml, string key)
    {
        var match = Regex.Match(xml, $"<key>{Regex.Escape(key)}</key>\\s*<string>(.*?)</string>", RegexOptions.Singleline);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
    }

    private static bool Useful(string value) =>
        value.Length >= 2
        && !value.StartsWith("NS", StringComparison.Ordinal)
        && !value.StartsWith('$')
        && !value.Contains("NSObject", StringComparison.Ordinal);

    private static List<string> ScanUtf16(byte[] data)
    {
        var found = new List<string>();
        var chars = new List<char>();
        void Flush()
        {
            if (chars.Count >= 2) found.Add(new string(chars.ToArray()));
            chars.Clear();
        }

        for (var i = 0; i + 1 < data.Length; i += 2)
        {
            var c = (char)(data[i] | (data[i + 1] << 8));
            if (!char.IsControl(c) && data[i + 1] < 0xD8) chars.Add(c);
            else Flush();
        }

        Flush();
        return found;
    }
}
