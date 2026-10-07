using Microsoft.Data.Sqlite;

namespace MessageRecord.Data;

/// <summary>
/// 本機 SQLite。查詢語意對齊手機版 NotiGuardDao：
/// 空白搜尋沒有結果，% 與 _ 是普通文字，uid 衝突時略過不覆寫。
/// </summary>
public sealed class SqliteRecordStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _prefs = new(StringComparer.Ordinal);
    private int _bulk;
    private bool _dirty;
    private bool _master = true;
    private bool _defaultBlocking = true;
    private bool _keepFullText = true;

    public event Action? Changed;

    private SqliteRecordStore(SqliteConnection connection, string dataDirectory)
    {
        _connection = connection;
        DataDirectory = dataDirectory;
        CreateSchema();
        LoadPrefs();
    }

    public string DataDirectory { get; }

    public string DatabasePath => Path.Combine(DataDirectory, "records.db");

    public static SqliteRecordStore Open(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "records.db");
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            pragma.ExecuteNonQuery();
        }

        return new SqliteRecordStore(connection, directory);
    }

    public static string DefaultDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MessageRecord");

    public bool MasterEnabled
    {
        get { lock (_gate) return _master; }
        set => SetPref("master_enabled", value, master => _master = master, notify: false);
    }

    public bool DefaultBlocking
    {
        get { lock (_gate) return _defaultBlocking; }
        set => SetPref("default_blocking", value, blocking => _defaultBlocking = blocking, notify: true);
    }

    public bool KeepFullText
    {
        get { lock (_gate) return _keepFullText; }
        set => SetPref("keep_full_text", value, keep => _keepFullText = keep, notify: false);
    }

    public bool GetFlag(string key, bool fallback = false)
    {
        lock (_gate) return ReadBool(key, fallback);
    }

    public void SetFlag(string key, bool value)
    {
        lock (_gate) WritePref(key, value ? "1" : "0");
    }

    public bool IsBlocking(string packageName)
    {
        lock (_gate)
        {
            var rule = FindRule(packageName);
            return rule?.Blocking ?? _defaultBlocking;
        }
    }

    public StoredRule? Rule(string packageName)
    {
        lock (_gate) return FindRule(packageName);
    }

    public void SetBlocking(string packageName, string appLabel, bool blocking)
    {
        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                INSERT INTO app_rules (packageName, appLabel, blocking, updatedAt)
                VALUES ($package, $label, $blocking, $updated)
                ON CONFLICT(packageName) DO UPDATE SET
                    appLabel = excluded.appLabel,
                    blocking = excluded.blocking,
                    updatedAt = excluded.updatedAt
                """;
            command.Parameters.AddWithValue("$package", packageName);
            command.Parameters.AddWithValue("$label", appLabel);
            command.Parameters.AddWithValue("$blocking", blocking ? 1 : 0);
            command.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            command.ExecuteNonQuery();
            MarkDirty();
        }

        FireIfIdle();
    }

    public void Forget(string packageName)
    {
        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                DELETE FROM app_rules WHERE packageName = $package;
                DELETE FROM records WHERE packageName = $package;
                """;
            command.Parameters.AddWithValue("$package", packageName);
            command.ExecuteNonQuery();
            MarkDirty();
        }

        FireIfIdle();
    }

    public bool InsertIgnore(StoredNotification record, bool notify = true)
    {
        var inserted = false;
        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                INSERT OR IGNORE INTO records
                    (uid, packageName, appLabel, title, text, channelId, category, postedAt, removedAt, blocked, ongoing, source)
                VALUES
                    ($uid, $package, $label, $title, $text, $channel, $category, $posted, $removed, $blocked, $ongoing, $source)
                """;
            Bind(command, record);
            inserted = command.ExecuteNonQuery() > 0;
            if (inserted) NoteWrite(notify);
        }

        if (inserted && notify) FireIfIdle();
        return inserted;
    }

    public bool MarkRemoved(string uid, long at, bool notify = true)
    {
        var changed = false;
        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                UPDATE records SET removedAt = $at
                WHERE uid = $uid AND removedAt IS NULL
                """;
            command.Parameters.AddWithValue("$uid", uid);
            command.Parameters.AddWithValue("$at", at);
            changed = command.ExecuteNonQuery() > 0;
            if (changed) NoteWrite(notify);
        }

        if (changed && notify) FireIfIdle();
        return changed;
    }

    public IReadOnlyList<StoredSummary> AppSummaries()
    {
        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                SELECT r.packageName AS packageName,
                       MAX(r.appLabel) AS appLabel,
                       COUNT(*) AS total,
                       COALESCE(SUM(CASE WHEN r.blocked <> 0 THEN 1 ELSE 0 END), 0) AS blockedCount,
                       MAX(r.postedAt) AS lastPostedAt,
                       ru.blocking AS blocking
                FROM records r
                LEFT JOIN app_rules ru ON ru.packageName = r.packageName
                GROUP BY r.packageName
                ORDER BY total DESC, lastPostedAt DESC
                """;
            using var reader = command.ExecuteReader();
            var list = new List<StoredSummary>();
            while (reader.Read())
            {
                list.Add(new StoredSummary
                {
                    PackageName = reader.GetString(0),
                    AppLabel = reader.GetString(1),
                    Total = reader.GetInt32(2),
                    BlockedCount = reader.GetInt32(3),
                    LastPostedAt = reader.GetInt64(4),
                    Blocking = reader.IsDBNull(5) ? null : reader.GetInt64(5) != 0
                });
            }

            return list;
        }
    }

    public IReadOnlyList<StoredNotification> Records(string? packageName, bool? blocked)
    {
        lock (_gate) return QueryRecords(packageName, blocked);
    }

    public IReadOnlyList<StoredNotification> Search(string query, string? packageName, bool? blocked)
    {
        var term = query.Trim();
        if (term.Length == 0) return Array.Empty<StoredNotification>();

        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                SELECT id, uid, packageName, appLabel, title, text, channelId, category,
                       postedAt, removedAt, blocked, ongoing, source
                FROM records
                WHERE ($package IS NULL OR packageName = $package)
                  AND ($blocked IS NULL OR blocked = $blocked)
                  AND (instr(lower(appLabel), lower($query)) > 0
                    OR instr(lower(packageName), lower($query)) > 0
                    OR instr(lower(title), lower($query)) > 0
                    OR instr(lower(text), lower($query)) > 0)
                ORDER BY postedAt DESC, id DESC
                """;
            command.Parameters.AddWithValue("$query", term);
            command.Parameters.AddWithValue("$package", (object?)packageName ?? DBNull.Value);
            command.Parameters.AddWithValue("$blocked", blocked is null ? DBNull.Value : blocked.Value ? 1 : 0);
            using var reader = command.ExecuteReader();
            return ReadAll(reader);
        }
    }

    public IReadOnlyList<StoredNotification> ExportRecords(string? packageName)
    {
        lock (_gate) return QueryRecords(packageName, null);
    }

    public int ImportJson(string json)
    {
        var records = RecordExporter.Parse(json);
        var inserted = 0;
        BeginBulk();
        try
        {
            foreach (var record in records)
            {
                if (InsertIgnore(record, notify: false)) inserted++;
            }
        }
        finally
        {
            EndBulk();
        }

        return inserted;
    }

    public void BeginBulk()
    {
        lock (_gate) _bulk++;
    }

    public void EndBulk()
    {
        var fire = false;
        lock (_gate)
        {
            if (_bulk > 0) _bulk--;
            fire = _bulk == 0 && _dirty;
            if (_bulk == 0) _dirty = false;
        }

        if (fire) Changed?.Invoke();
    }

    public void Dispose() => _connection.Dispose();

    private IReadOnlyList<StoredNotification> QueryRecords(string? packageName, bool? blocked)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT id, uid, packageName, appLabel, title, text, channelId, category,
                   postedAt, removedAt, blocked, ongoing, source
            FROM records
            WHERE ($package IS NULL OR packageName = $package)
              AND ($blocked IS NULL OR blocked = $blocked)
            ORDER BY postedAt DESC, id DESC
            """;
        command.Parameters.AddWithValue("$package", (object?)packageName ?? DBNull.Value);
        command.Parameters.AddWithValue("$blocked", blocked is null ? DBNull.Value : blocked.Value ? 1 : 0);
        using var reader = command.ExecuteReader();
        return ReadAll(reader);
    }

    private static List<StoredNotification> ReadAll(SqliteDataReader reader)
    {
        var list = new List<StoredNotification>();
        while (reader.Read())
        {
            list.Add(new StoredNotification
            {
                Id = reader.GetInt64(0),
                Uid = reader.GetString(1),
                PackageName = reader.GetString(2),
                AppLabel = reader.GetString(3),
                Title = reader.GetString(4),
                Text = reader.GetString(5),
                ChannelId = reader.IsDBNull(6) ? null : reader.GetString(6),
                Category = reader.IsDBNull(7) ? null : reader.GetString(7),
                PostedAt = reader.GetInt64(8),
                RemovedAt = reader.IsDBNull(9) ? null : reader.GetInt64(9),
                Blocked = reader.GetInt64(10) != 0,
                Ongoing = reader.GetInt64(11) != 0,
                Source = reader.GetString(12)
            });
        }

        return list;
    }

    private static void Bind(SqliteCommand command, StoredNotification record)
    {
        command.Parameters.AddWithValue("$uid", record.Uid);
        command.Parameters.AddWithValue("$package", record.PackageName);
        command.Parameters.AddWithValue("$label", record.AppLabel);
        command.Parameters.AddWithValue("$title", record.Title);
        command.Parameters.AddWithValue("$text", record.Text);
        command.Parameters.AddWithValue("$channel", (object?)record.ChannelId ?? DBNull.Value);
        command.Parameters.AddWithValue("$category", (object?)record.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("$posted", record.PostedAt);
        command.Parameters.AddWithValue("$removed", (object?)record.RemovedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("$blocked", record.Blocked ? 1 : 0);
        command.Parameters.AddWithValue("$ongoing", record.Ongoing ? 1 : 0);
        command.Parameters.AddWithValue("$source", record.Source);
    }

    private StoredRule? FindRule(string packageName)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT packageName, appLabel, blocking, updatedAt FROM app_rules WHERE packageName = $package";
        command.Parameters.AddWithValue("$package", packageName);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new StoredRule
        {
            PackageName = reader.GetString(0),
            AppLabel = reader.GetString(1),
            Blocking = reader.GetInt64(2) != 0,
            UpdatedAt = reader.GetInt64(3)
        };
    }

    private void CreateSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                uid TEXT NOT NULL UNIQUE,
                packageName TEXT NOT NULL,
                appLabel TEXT NOT NULL,
                title TEXT NOT NULL,
                text TEXT NOT NULL,
                channelId TEXT,
                category TEXT,
                postedAt INTEGER NOT NULL,
                removedAt INTEGER,
                blocked INTEGER NOT NULL,
                ongoing INTEGER NOT NULL DEFAULT 0,
                source TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_records_package ON records(packageName);
            CREATE INDEX IF NOT EXISTS idx_records_posted ON records(postedAt);
            CREATE TABLE IF NOT EXISTS app_rules (
                packageName TEXT PRIMARY KEY,
                appLabel TEXT NOT NULL,
                blocking INTEGER NOT NULL,
                updatedAt INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS prefs (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private void LoadPrefs()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT key, value FROM prefs";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            _prefs[reader.GetString(0)] = reader.GetString(1);

        _master = ReadBool("master_enabled", true);
        _defaultBlocking = ReadBool("default_blocking", true);
        _keepFullText = ReadBool("keep_full_text", true);
    }

    private bool ReadBool(string key, bool fallback) =>
        _prefs.TryGetValue(key, out var value) ? value == "1" : fallback;

    private void SetPref(string key, bool value, Action<bool> assign, bool notify)
    {
        var fire = false;
        lock (_gate)
        {
            var current = ReadBool(key, !value);
            assign(value);
            if (current == value && _prefs.ContainsKey(key)) return;
            WritePref(key, value ? "1" : "0");
            if (notify) MarkDirty();
            fire = notify;
        }

        if (fire) FireIfIdle();
    }

    private void WritePref(string key, string value)
    {
        _prefs[key] = value;
        using var command = _connection.CreateCommand();
        command.CommandText = """
            INSERT INTO prefs (key, value) VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private void MarkDirty() => _dirty = true;

    /// <summary>批次寫入只記髒旗標，等 EndBulk 再通知一次。單筆寫入則立刻可通知。</summary>
    private void NoteWrite(bool notify)
    {
        if (_bulk > 0 || notify) _dirty = true;
    }

    private void FireIfIdle()
    {
        var fire = false;
        lock (_gate)
        {
            fire = _bulk == 0 && _dirty;
            if (fire) _dirty = false;
        }

        if (fire) Changed?.Invoke();
    }
}
