using MessageRecord.Core;

namespace MessageRecord.ViewModels;

/// <summary>設定頁的一列開關。</summary>
public sealed class SettingItemViewModel : ObservableObject
{
    private bool _isOn;

    public SettingItemViewModel(string title, string description, bool isOn)
    {
        Title = title;
        Description = description;
        _isOn = isOn;
    }

    public string Title { get; }

    public string Description { get; }

    public bool IsOn
    {
        get => _isOn;
        set => Set(ref _isOn, value);
    }
}

/// <summary>側欄「設定」頁。開關可以撥，但不會真的改變任何行為（介面示範）。</summary>
public sealed class SettingsViewModel
{
    public IReadOnlyList<SettingItemViewModel> General { get; } = new[]
    {
        new SettingItemViewModel("開機時自動啟動", "登入 Windows 後在背景常駐，持續記錄通知", true),
        new SettingItemViewModel("最小化到系統匣", "關閉視窗時縮到工作列右下角，不結束程式", true),
        new SettingItemViewModel("新程式預設攔截", "第一次出現的程式自動列入攔截名單", false)
    };

    public IReadOnlyList<SettingItemViewModel> Records { get; } = new[]
    {
        new SettingItemViewModel("保留完整內容", "連同通知內文一起保存，關閉後只留標題與時間", true),
        new SettingItemViewModel("自動清理", "超過 90 天的紀錄自動刪除", false),
        new SettingItemViewModel("每日摘要", "每天 21:00 統整當日攔截狀況", true)
    };
}
