using MessageRecord.Core;
using MessageRecord.Data;

namespace MessageRecord.ViewModels;

/// <summary>設定頁的一列開關。</summary>
public sealed class SettingItemViewModel : ObservableObject
{
    private readonly Action<bool>? _onChanged;
    private bool _isOn;

    public SettingItemViewModel(string title, string description, bool isOn, Action<bool>? onChanged = null)
    {
        Title = title;
        Description = description;
        _isOn = isOn;
        _onChanged = onChanged;
    }

    public string Title { get; }

    public string Description { get; }

    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (!Set(ref _isOn, value)) return;
            _onChanged?.Invoke(value);
        }
    }
}

/// <summary>側欄「設定」頁。示範模式的開關不改變行為；即時模式會寫進本機資料庫。</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSession? _session;
    private string _statusText = "";
    private string _importMessage = "";

    public SettingsViewModel()
    {
        Note = "這一頁的開關只做介面示範，不會改變實際行為";
        General = new[]
        {
            new SettingItemViewModel("開機時自動啟動", "登入 Windows 後在背景常駐，持續記錄通知", true),
            new SettingItemViewModel("最小化到系統匣", "關閉視窗時縮到工作列右下角，不結束程式", true),
            new SettingItemViewModel("新程式預設攔截", "第一次出現的程式自動列入攔截名單", false)
        };
        Records = new[]
        {
            new SettingItemViewModel("保留完整內容", "連同通知內文一起保存，關閉後只留標題與時間", true),
            new SettingItemViewModel("自動清理", "超過 90 天的紀錄自動刪除", false),
            new SettingItemViewModel("每日摘要", "每天 21:00 統整當日攔截狀況", true)
        };
    }

    public SettingsViewModel(AppSession session)
    {
        _session = session;
        Note = "規則存在這台電腦。桌面系統沒有公開 API 可以把其他程式的通知從通知中心移除，但紀錄會留下。";
        ShowFileActions = true;
        _statusText = session.StatusText;
        General = new[]
        {
            new SettingItemViewModel(
                "攔截總開關",
                "關閉後新通知仍會記錄，但標成已允許",
                session.MasterEnabled,
                value => session.MasterEnabled = value),
            new SettingItemViewModel(
                "新程式預設攔截",
                "沒有個別規則的程式，新通知記成已攔截",
                session.DefaultBlocking,
                value => session.DefaultBlocking = value)
        };
        Records = new[]
        {
            new SettingItemViewModel(
                "保留完整內容",
                "關閉後新紀錄只留標題與時間",
                session.KeepFullText,
                value => session.KeepFullText = value)
        };
    }

    public string Note { get; }

    public bool ShowFileActions { get; }

    public bool HasStatus => _statusText.Length > 0;

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (!Set(ref _statusText, value)) return;
            OnPropertyChanged(nameof(HasStatus));
        }
    }

    public string ImportMessage
    {
        get => _importMessage;
        private set => Set(ref _importMessage, value);
    }

    public bool HasImportMessage => _importMessage.Length > 0;

    public IReadOnlyList<SettingItemViewModel> General { get; }

    public IReadOnlyList<SettingItemViewModel> Records { get; }

    public void RefreshStatus()
    {
        if (_session is null) return;
        StatusText = _session.StatusText;
    }

    public (string FileName, string Json) BuildExport()
    {
        if (_session is null) throw new InvalidOperationException("示範模式不能匯出。");
        return (_session.ExportFileName(null), _session.ExportJson(null));
    }

    public int Import(string json)
    {
        if (_session is null) throw new InvalidOperationException("示範模式不能匯入。");
        var count = _session.ImportJson(json);
        Report(count == 0 ? "沒有新的紀錄可匯入" : $"已匯入 {count} 則紀錄");
        return count;
    }

    public void Report(string message)
    {
        ImportMessage = message;
        OnPropertyChanged(nameof(HasImportMessage));
    }

    public void OpenDataFolder() => _session?.RevealDataFolder();
}
