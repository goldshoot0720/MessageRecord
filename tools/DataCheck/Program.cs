using System.Text;
using MessageRecord.Capture;
using MessageRecord.Data;
using Microsoft.Data.Sqlite;

var failed = 0;
void Check(bool condition, string name)
{
    if (!condition)
    {
        failed++;
        Console.WriteLine("FAIL " + name);
        return;
    }

    Console.WriteLine("PASS " + name);
}

var root = Path.Combine(Path.GetTempPath(), "messagerecord-datacheck-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    SearchChecks(Path.Combine(root, "search"));
    RecordingChecks(Path.Combine(root, "record"));
    ExportChecks(Path.Combine(root, "export"));
    WindowsChecks(Path.Combine(root, "windows"));
    MacChecks(Path.Combine(root, "mac"));
    LinuxChecks();
    Check(Categorizer.Of("msg", "", "") == "一般訊息", "system category");
    Check(Categorizer.Of(null, "轉帳", "完成") == "交易通知", "content category");
    Check(RecordExporter.FileName("LINE!!!") .StartsWith("notiguard-LINE-", StringComparison.Ordinal), "export file name");
    Check(AppNames.FromIdentifier("jp.naver.line.android") == "LINE", "known app name");
}
finally
{
    try { Directory.Delete(root, true); } catch (IOException) { }
}

if (failed > 0)
{
    Console.WriteLine($"{failed} failed");
    Environment.Exit(1);
}

Console.WriteLine("all passed");

void SearchChecks(string directory)
{
    using var store = SqliteRecordStore.Open(directory);
    Insert(store, "a", "jp.line", "LINE", "小明", "明天開會 100%", 1, true);
    Insert(store, "b", "jp.line", "LINE", "開會通知", "O'Brien_a", 3, false);
    Insert(store, "c", "com.mail", "Mail", "工作", "開會資料", 2, true);

    Check(string.Join(',', store.Search(" 開會 ", null, null).Select(r => r.Uid)) == "b,c,a", "chinese search order");
    Check(string.Join(',', store.Search("小明", null, null).Select(r => r.Uid)) == "a", "title search");
    Check(string.Join(',', store.Search("line", null, null).Select(r => r.Uid)) == "b,a", "app label case");
    Check(string.Join(',', store.Search("COM.MAIL", null, null).Select(r => r.Uid)) == "c", "package case");
    Check(string.Join(',', store.Search("開會", "jp.line", true).Select(r => r.Uid)) == "a", "scoped blocked");
    Check(string.Join(',', store.Search("開會", "jp.line", false).Select(r => r.Uid)) == "b", "scoped allowed");
    Check(string.Join(',', store.Search("開會", null, true).Select(r => r.Uid)) == "c,a", "blocked filter");
    Check(store.Search("%", null, null).Single().Uid == "a", "percent is literal");
    Check(store.Search("_", null, null).Single().Uid == "b", "underscore is literal");
    Check(store.Search("O'Brien", null, null).Single().Uid == "b", "quote is literal");
    Check(store.Search("' OR 1=1 --", null, null).Count == 0, "sql text is literal");
    Check(store.Search("  ", null, null).Count == 0, "blank search");
    Check(store.Search("不存在的內容", null, null).Count == 0, "missing search");
    Insert(store, "d", "com.mail", "Mail", "新通知", "內容", 4, false);
    Check(store.Search("新通知", null, null).Single().Uid == "d", "search sees new row");
}

void RecordingChecks(string directory)
{
    using var store = SqliteRecordStore.Open(directory);
    store.MasterEnabled = false;
    Check(AcceptCount(store, Event(1, 2000), Event(2, 2001, summary: true)) == 1, "summary is not stored");
    Check(AcceptCount(store, Event(1, 2000), Event(1, 2001)) == 1, "unchanged update");
    Reset(store);
    Check(AcceptCount(store, Event(1, 2000), Event(1, 2001, messageTime: 1001)) == 2, "new message time");
    Reset(store);
    Check(AcceptCount(store, Event(1, 2000), Event(2, 2001)) == 2, "different id");
    Reset(store);
    Check(AcceptCount(store, Event(1, 2000), Event(1, 2001, text: "(3)(7)")) == 2, "changed content");
    Reset(store);
    Check(AcceptCount(store, Event(1, 2000)) == 1, "first delivery");
    Check(AcceptCount(store, Event(1, 2001)) == 1, "replay is deduplicated");
    Reset(store);
    Check(AcceptCount(store, Event(1, 2000, messageTime: 0), Event(1, 2001, messageTime: 0)) == 2, "missing time does not merge");
    Reset(store);
    Check(AcceptCount(store, Event(1, 2000, embedded: 900), Event(1, 2001, embedded: 901)) == 2, "messaging timestamp");
    Reset(store);
    Check(AcceptCount(store, Event(1, 2000, embedded: 900), Event(1, 2001, messageTime: 1001, embedded: 900)) == 1, "messaging update deduped");
    Reset(store);
    var same = Enumerable.Range(0, 20).Select(i => Event(1, 2000 + i)).ToArray();
    Check(AcceptCount(store, same) == 1, "concurrent updates collapse");
    NotificationIngestor.Remove(store, Event(1, 2019));
    Check(store.Records("test.messages", null).Single().RemovedAt is not null, "removal marks record");

    Reset(store);
    store.MasterEnabled = true;
    var summary = NotificationIngestor.Accept(store, Event(2, 2000, summary: true), history: false);
    Check(!summary.Inserted && summary.Dismiss && store.Records("test.messages", null).Count == 0, "summary can be dismissed");

    Reset(store);
    store.MasterEnabled = true;
    var ongoing = NotificationIngestor.Accept(store, Event(3, 2000, ongoing: true), history: false);
    Check(ongoing.Inserted && !ongoing.Dismiss && !store.Records("test.messages", null).Single().Blocked, "ongoing is allowed");

    Reset(store);
    store.MasterEnabled = false;
    NotificationIngestor.Accept(store, Event(4, 3000), history: false);
    Check(!store.Records("test.messages", null).Single().Blocked, "master off still records as allowed");

    Reset(store);
    store.MasterEnabled = true;
    var ignored = NotificationIngestor.Accept(store, Event(5, 3000, packageName: "com.notiguard.debug"), history: false);
    Check(!ignored.Inserted && store.Records("com.notiguard.debug", null).Count == 0, "own package ignored");

    Reset(store);
    store.MasterEnabled = true;
    NotificationIngestor.Accept(store, Event(6, 4000), history: true);
    Check(store.Records("test.messages", null).Single() is { Blocked: false, Source: RecordSources.DesktopHistory }, "history is not marked blocked");
    NotificationIngestor.Accept(store, Event(7, 4001), history: false);
    var rows = store.Records("test.messages", null);
    Check(rows.Count == 2 && rows.Any(r => r.Source == RecordSources.DesktopListener && r.Blocked), "live row is blocked");
}

void ExportChecks(string directory)
{
    using var store = SqliteRecordStore.Open(directory);
    Insert(store, "uid-1", "jp.line", "LINE", "標題", "內文", 10, true);
    var json = RecordExporter.ToJson(store.ExportRecords("jp.line"), "desktop-test");
    Check(json.Contains("\"uid\": \"uid-1\"", StringComparison.Ordinal), "export uid");
    Check(json.Contains("\"blocked\": true", StringComparison.Ordinal), "export blocked");
    Check(json.Contains("\"deviceId\": \"desktop-test\"", StringComparison.Ordinal), "export device");
    Check(json.Contains("\"source\": \"seed\"", StringComparison.Ordinal), "export keeps stored source");
    using var other = SqliteRecordStore.Open(Path.Combine(directory, "import"));
    Check(other.ImportJson(json) == 1, "import count");
    Check(other.ImportJson(json) == 0, "import is idempotent");
    Check(other.Records("jp.line", null).Single().Text == "內文", "import body");
    Check(other.Search("內文", null, null).Single().Uid == "uid-1", "imported text is searchable");
}

void WindowsChecks(string directory)
{
    var path = Path.Combine(directory, "wpndatabase.db");
    Directory.CreateDirectory(directory);
    var posted = new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero);
    using (var connection = new SqliteConnection($"Data Source={path}"))
    {
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE NotificationHandler (RecordId INTEGER PRIMARY KEY, PrimaryId TEXT);
            CREATE TABLE Notification (Id INTEGER PRIMARY KEY, HandlerId INTEGER, Payload BLOB, ArrivalTime INTEGER);
            INSERT INTO NotificationHandler VALUES (1, 'jp.naver.line.android');
            INSERT INTO Notification VALUES (9, 1, $payload, $time);
            """;
        command.Parameters.AddWithValue("$payload", Encoding.UTF8.GetBytes(
            "<toast><visual><binding template=\"ToastGeneric\"><text>標題</text><text>內文</text></binding></visual></toast>"));
        command.Parameters.AddWithValue("$time", posted.UtcDateTime.ToFileTimeUtc());
        command.ExecuteNonQuery();
    }

    var items = WindowsWpnFeed.ReadCopy(path);
    Check(items.Count == 1 && items[0].AppLabel == "LINE" && items[0].Title == "標題" && items[0].Text == "內文", "windows toast");
    Check(items[0].PostedAt == posted.ToUnixTimeMilliseconds(), "windows file time");
}

void MacChecks(string directory)
{
    Directory.CreateDirectory(directory);
    var path = Path.Combine(directory, "db");
    var plist = """
        <?xml version="1.0" encoding="UTF-8"?>
        <plist version="1.0"><dict>
          <key>title</key><string>你好</string>
          <key>body</key><string>內容 &amp; 更多</string>
          <key>appName</key><string>聊天</string>
        </dict></plist>
        """;
    using (var connection = new SqliteConnection($"Data Source={path}"))
    {
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE app (app_id INTEGER PRIMARY KEY, identifier TEXT);
            CREATE TABLE record (rec_id INTEGER PRIMARY KEY, app_id INTEGER, data BLOB, delivered_date REAL);
            INSERT INTO app VALUES (1, 'com.example.Chat');
            INSERT INTO record VALUES (7, 1, $data, 1);
            """;
        command.Parameters.AddWithValue("$data", Encoding.UTF8.GetBytes(plist));
        command.ExecuteNonQuery();
    }

    var snapshot = MacUserNotedFeed.ReadCopy(path);
    Check(snapshot.Recognized && snapshot.Items.Count == 1, "mac schema");
    Check(snapshot.Items[0].Title == "你好" && snapshot.Items[0].Text == "內容 & 更多" && snapshot.Items[0].AppLabel == "聊天", "mac plist");
    var expected = DateTimeOffset.Parse("2001-01-01T00:00:01Z").ToUnixTimeMilliseconds();
    Check(MacUserNotedFeed.CocoaToUnixMs(1) == expected, "cocoa epoch");

    var missing = MacUserNotedFeed.Read(Path.Combine(directory, "missing.db"));
    Check(!missing.Accessible && missing.ShortStatus.Contains("找不到", StringComparison.Ordinal), "missing mac database");
}

void LinuxChecks()
{
    var transcript = """
        method call time=1 sender=:1.2 -> dest=:1.3 serial=4 path=/org/freedesktop/Notifications; interface=org.freedesktop.Notifications; member=Notify
           string "Firefox"
           uint32 0
           string ""
           string "下載完成"
           string "report.pdf"
        """;
    var items = LinuxDBusFeed.Parse(transcript);
    Check(items.Count == 1 && items[0].AppLabel == "Firefox" && items[0].Title == "下載完成" && items[0].Text == "report.pdf", "dbus notify");
}

static void Insert(SqliteRecordStore store, string uid, string packageName, string label, string title, string text, long posted, bool blocked)
{
    store.InsertIgnore(new StoredNotification
    {
        Uid = uid,
        PackageName = packageName,
        AppLabel = label,
        Title = title,
        Text = text,
        PostedAt = posted,
        Blocked = blocked,
        Source = RecordSources.Seed
    });
}

static void Reset(SqliteRecordStore store) => store.Forget("test.messages");

static int AcceptCount(SqliteRecordStore store, params IncomingNotification[] notices)
{
    foreach (var notice in notices)
        NotificationIngestor.Accept(store, notice, history: false);
    return store.Records("test.messages", null).Count;
}

static IncomingNotification Event(int id, long posted, long messageTime = 1000, string text = "37", bool summary = false, long? embedded = null, bool ongoing = false, string packageName = "test.messages")
{
    var messages = new List<IncomingMessage>();
    if (embedded is long timestamp)
    {
        messages.Add(new IncomingMessage { Timestamp = timestamp, Text = text, Sender = "Sender" });
    }

    return new IncomingNotification
    {
        Key = $"0|{packageName}|{id}|null|10001",
        PackageName = packageName,
        AppLabel = "Messages",
        Title = summary ? "" : "Sender",
        Text = summary ? "" : text,
        PostedAt = posted,
        When = messageTime,
        IsSummary = summary,
        Ongoing = ongoing,
        Messages = messages,
        SystemCategory = "msg"
    };
}
