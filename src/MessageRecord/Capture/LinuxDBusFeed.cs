using System.Diagnostics;
using System.Text;

namespace MessageRecord.Capture;

/// <summary>
/// 在工作階段匯流排上聽 org.freedesktop.Notifications 的 Notify。
/// 看得到的範圍取決於桌面環境是否允許 dbus-monitor。
/// </summary>
public sealed class LinuxDBusFeed : IDisposable
{
    private readonly object _gate = new();
    private readonly List<IncomingNotification> _pending = new();
    private Process? _process;
    private string _status = "正在監看桌面通知";

    public string Name => "linux-dbus";

    public void Start()
    {
        if (!OperatingSystem.IsLinux()) return;
        try
        {
            _process = Process.Start(new ProcessStartInfo
            {
                FileName = "dbus-monitor",
                ArgumentList = { "--session", "interface=org.freedesktop.Notifications,member=Notify" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
        }
        catch (Exception ex)
        {
            _status = "找不到 dbus-monitor";
            _pending.Clear();
            _ = ex;
            return;
        }

        if (_process is null)
        {
            _status = "找不到 dbus-monitor";
            return;
        }

        _process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is null) return;
            lock (_gate) _buffer.AppendLine(eventArgs.Data);
        };
        _process.BeginOutputReadLine();
        _status = "正在監看桌面通知";
    }

    private readonly StringBuilder _buffer = new();

    public FeedSnapshot Poll()
    {
        if (!OperatingSystem.IsLinux())
        {
            return FeedSnapshot.Failed("不是 Linux", "這個來源只在 Linux 桌面使用。");
        }

        string chunk;
        lock (_gate)
        {
            chunk = _buffer.ToString();
            _buffer.Clear();
        }

        var parsed = Parse(chunk);
        lock (_gate) _pending.AddRange(parsed);
        List<IncomingNotification> items;
        lock (_gate)
        {
            items = _pending.ToList();
            _pending.Clear();
        }

        var running = _process is { HasExited: false };
        return new FeedSnapshot
        {
            Accessible = running || items.Count > 0,
            Recognized = running,
            ShortStatus = running ? "正在監看桌面通知" : _status,
            LongStatus = running
                ? "正在聽取桌面通知。新通知會依攔截開關留下紀錄；是否能關掉彈出視窗取決於桌面環境。"
                : "沒有監聽到 org.freedesktop.Notifications。請確認已安裝 dbus-monitor，且目前的桌面環境允許監聽。",
            Items = items
        };
    }

    public static IReadOnlyList<IncomingNotification> Parse(string transcript)
    {
        var list = new List<IncomingNotification>();
        var strings = new List<string>();
        uint replaces = 0;
        var inCall = false;

        foreach (var raw in transcript.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Contains("member=Notify", StringComparison.Ordinal))
            {
                Flush();
                inCall = true;
                strings.Clear();
                replaces = 0;
                continue;
            }

            if (!inCall) continue;
            if (line.StartsWith("string ", StringComparison.Ordinal))
            {
                strings.Add(Unquote(line["string ".Length..]));
                continue;
            }

            if (line.StartsWith("uint32 ", StringComparison.Ordinal) && strings.Count == 1
                && uint.TryParse(line["uint32 ".Length..].Trim(), out var id))
            {
                replaces = id;
            }
        }

        Flush();
        return list;

        void Flush()
        {
            if (!inCall || strings.Count < 3) return;
            var app = strings[0];
            var summary = strings.Count > 2 ? strings[2] : "";
            var body = strings.Count > 3 ? strings[3] : "";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var key = replaces > 0 ? $"{app}:{replaces}" : $"{app}:{summary}:{body}:{now / 60000}";
            list.Add(new IncomingNotification
            {
                Key = key,
                PackageName = app,
                AppLabel = app,
                Title = summary,
                Text = body,
                PostedAt = now,
                When = now
            });
            inCall = false;
            strings.Clear();
        }
    }

    private static string Unquote(string value)
    {
        var text = value.Trim();
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
            text = text[1..^1];
        return text.Replace("\\\"", "\"").Replace("\\n", "\n");
    }

    public void Dispose()
    {
        if (_process is null) return;
        try
        {
            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // 行程可能已經結束。
        }

        _process.Dispose();
        _process = null;
    }
}
