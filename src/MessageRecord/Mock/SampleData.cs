using MessageRecord.Models;

namespace MessageRecord.Mock;

/// <summary>
/// 介面設計用的示範資料來源。
/// 只負責產生看起來合理的通知紀錄，不做任何真實攔截。
/// 將來要接真實攔截引擎時，只需替換這個類別的輸出。
/// </summary>
public static class SampleData
{
    private sealed record Seed(
        string Key,
        string Name,
        string Badge,
        string Accent,
        bool Block,
        int Count,
        (string Title, string Body)[] Samples);

    private static readonly Seed[] Seeds =
    {
        new("com.squirrel.LINE.LINE", "LINE", "L", "#FF06C755", true, 128, new[]
        {
            ("工作群組", "張三：明天 10 點開會，記得準備簡報"),
            ("好友 小美", "傳送了貼圖"),
            ("LINE 官方帳號", "限時優惠！全館 8 折，立即搶購！"),
            ("家庭群組", "媽媽：晚餐要吃什麼？"),
            ("LINE 提醒", "您有 1 個新提醒"),
            ("好友 阿鋒", "哈囉～在嗎？"),
            ("LINE GAME", "最新活動開跑！登入送好禮"),
            ("LINE Pay", "交易完成 NT$ 150"),
            ("大學同學會", "地點改到市民大道那間，訂 7 點"),
            ("社區公告", "本週六上午進行水塔清洗，將停水 4 小時")
        }),

        new("com.google.YouTube", "YouTube", "Y", "#FFFF0033", true, 42, new[]
        {
            ("頻道更新", "你訂閱的頻道發布了新影片"),
            ("直播開始", "關注的創作者正在直播中"),
            ("留言回覆", "有人回覆了你的留言"),
            ("每週精選", "為你整理了 12 部推薦影片"),
            ("YouTube Music", "你的每週新發現已更新")
        }),

        new("Chrome", "Chrome", "C", "#FF4285F4", false, 36, new[]
        {
            ("下載完成", "release-notes-2026Q3.pdf 已下載完成"),
            ("網站通知", "GitHub：你被指派了一個新的 issue"),
            ("密碼檢查", "偵測到 1 組出現在外洩清單中的密碼"),
            ("mail.google.com", "新郵件：出貨通知（訂單 #A82931）"),
            ("vercel.app", "你有 2 筆新的部署紀錄")
        }),

        new("Facebook", "Facebook", "F", "#FF1877F2", true, 28, new[]
        {
            ("好友動態", "王小明分享了一則貼文"),
            ("社團通知", "「攝影同好會」有 5 則新貼文"),
            ("生日提醒", "今天是陳美玲的生日"),
            ("Marketplace", "有人對你的商品有興趣"),
            ("回顧", "看看你 3 年前的今天")
        }),

        new("Instagram", "Instagram", "I", "#FFE4405F", true, 18, new[]
        {
            ("新追蹤者", "有 3 位使用者開始追蹤你"),
            ("限時動態", "你追蹤的帳號發布了新限動"),
            ("私訊", "收到一則新訊息"),
            ("貼文互動", "你的貼文獲得 128 個讚"),
            ("推薦", "你可能認識的人")
        }),

        new("Gmail", "Gmail", "M", "#FFEA4335", false, 15, new[]
        {
            ("財務部", "九月份請款單據已進入審核流程"),
            ("系統通知", "你的信箱容量已使用 92%"),
            ("王品豪", "附件是修訂後的規格書，主要更動在第 4 章"),
            ("行事曆提醒", "「季度檢討會議」將於 30 分鐘後開始"),
            ("退信通知", "寄往 vendor@example.com 的郵件無法送達")
        }),

        new("Discord", "Discord", "D", "#FF5865F2", true, 12, new[]
        {
            ("#general", "有人要一起測新版本嗎？"),
            ("直接訊息 · Ray", "剛剛那個 bug 我復現了，等下開語音"),
            ("#release", "v2.4.0 已經推上去了，更新日誌在置頂"),
            ("語音頻道", "有 3 位成員正在「開發討論」頻道"),
            ("#random", "分享一個很好用的終端機主題")
        }),

        new("Slack", "Slack", "S", "#FF611F69", false, 9, new[]
        {
            ("#incident", "監控告警：API 回應時間超過門檻值"),
            ("Ivy Chen", "設計稿更新了，麻煩看一下互動說明"),
            ("#deploy", "production 部署成功（build 2841）"),
            ("提醒", "你昨天標記的訊息尚未處理"),
            ("#design-review", "明天的設計審查改成非同步進行")
        }),

        new("Spotify.exe", "Spotify", "S", "#FF1DB954", true, 8, new[]
        {
            ("正在播放", "Midnight Grid — Neon Harbor"),
            ("每週新發現", "為你更新了 30 首推薦曲目"),
            ("新專輯", "你追蹤的藝人發行了新專輯"),
            ("下載完成", "已離線儲存播放清單「深夜工作」"),
            ("播放清單", "有人把你的歌單加入收藏")
        }),

        new("MSTeams_8wekyb3d8bbwe!MSTeams", "Microsoft Teams", "T", "#FF6264A7", false, 6, new[]
        {
            ("林哲宇", "報表我改好了，你再幫我看一下第三頁的數字"),
            ("產品週會", "會議將在 10 分鐘後開始，點此加入"),
            ("工程部 · 一般", "@所有人 版本凍結時間提前到今天 18:00"),
            ("陳映萱", "剛剛那份合約掃描檔我放到共用資料夾了"),
            ("未接來電", "來自 張秉勳 的通話，未接聽")
        }),

        new("Microsoft.Outlook.Desktop.15", "Outlook", "O", "#FF0078D4", true, 24, new[]
        {
            ("採購部", "報價單已簽核，請安排出貨"),
            ("行事曆", "「客戶簡報」明天 14:00"),
            ("系統通知", "偵測到新裝置登入你的帳戶"),
            ("人資公告", "年度健檢預約已開放"),
            ("週報提醒", "本週工作週報尚未送出")
        }),

        new("Telegram.Desktop", "Telegram", "T", "#FF29A9EB", true, 21, new[]
        {
            ("開發頻道", "新版本 build 已上傳"),
            ("好友 Ken", "檔案收到了，謝啦"),
            ("訂閱頻道", "今日科技新聞摘要"),
            ("群組 · 揪團", "週末有人要爬山嗎"),
            ("機器人", "你的排程任務已完成")
        }),

        new("Valve.Steam.Client", "Steam", "S", "#FF66C0F4", true, 17, new[]
        {
            ("好友上線", "Nova 剛剛上線了"),
            ("更新完成", "你的一款遊戲已完成更新（4.2 GB）"),
            ("特價提醒", "願望清單中的項目正在特價（-65%）"),
            ("成就解鎖", "你解鎖了「深潛者」成就"),
            ("遊戲邀請", "Kite 邀請你加入遊戲")
        }),

        new("Microsoft.YourPhone_8wekyb3d8bbwe!App", "手機連結", "機", "#FF2B7FFF", false, 14, new[]
        {
            ("簡訊 · 0912-xxx-458", "您的驗證碼為 483920，請勿轉傳"),
            ("手機電量偏低", "已連線的手機剩餘電量 15%"),
            ("新相片", "手機新增 8 張相片，可直接在電腦開啟"),
            ("未接來電", "來自 +886 2 2xxx xxxx"),
            ("App 通知 · 外送平台", "您的餐點已在路上，預計 12 分鐘後送達")
        }),

        new("Notion.exe", "Notion", "N", "#FF5B6472", true, 11, new[]
        {
            ("工作區", "有人在文件中提到你"),
            ("任務提醒", "「介面設計稿」今天到期"),
            ("留言", "Amy 在段落中留下了評論"),
            ("資料庫", "新增了 4 筆項目"),
            ("週報模板", "自動建立本週頁面")
        }),

        new("GitHubDesktop", "GitHub", "G", "#FF4C5E76", false, 10, new[]
        {
            ("Pull Request", "#482 已通過所有檢查"),
            ("Issue", "有人回覆了你開的 issue"),
            ("Actions", "工作流程 build 失敗"),
            ("Release", "v2.4.0 已發布"),
            ("Review", "你被指派為審查者")
        }),

        new("Shopee", "蝦皮購物", "蝦", "#FFEE4D2D", true, 26, new[]
        {
            ("出貨通知", "您的訂單已交由物流配送"),
            ("限時特賣", "9.9 超級購物節倒數 2 小時"),
            ("蝦幣回饋", "獲得 120 蝦幣"),
            ("賣家訊息", "商品已為您保留"),
            ("到貨提醒", "包裹已送達門市")
        }),

        new("PXHomeApp", "PChome", "P", "#FFE60012", true, 13, new[]
        {
            ("24h 到貨", "您的商品明天送達"),
            ("優惠通知", "滿千折百券已入帳"),
            ("訂單狀態", "訂單 #772013 已完成"),
            ("補貨通知", "您關注的商品已補貨"),
            ("會員日", "本週三會員日雙倍回饋")
        }),

        new("Zoom.exe", "Zoom", "Z", "#FF2D8CFF", false, 7, new[]
        {
            ("會議提醒", "「跨部門同步」5 分鐘後開始"),
            ("錄影完成", "雲端錄影已可下載"),
            ("邀請", "李專員邀請你加入會議"),
            ("聊天", "會議聊天室有新訊息"),
            ("更新", "有新版本可安裝")
        }),

        new("Netflix", "Netflix", "N", "#FFE50914", true, 9, new[]
        {
            ("新集數", "你追的影集更新了"),
            ("推薦", "根據你的觀看紀錄推薦"),
            ("即將下架", "清單中有 2 部作品即將下架"),
            ("繼續觀看", "上次看到第 3 集 12 分處"),
            ("下載完成", "離線內容已就緒")
        }),

        new("Twitch", "Twitch", "T", "#FF9146FF", true, 15, new[]
        {
            ("開台通知", "你追蹤的實況主開台了"),
            ("小奇點", "本月訂閱獎勵已發放"),
            ("聊天室", "有人 @ 你"),
            ("精華", "新的精華片段已發布"),
            ("推薦頻道", "你可能會喜歡這個頻道")
        }),

        new("WhatsApp", "WhatsApp", "W", "#FF25D366", true, 19, new[]
        {
            ("家族群", "阿姨傳送了一張照片"),
            ("好友 Leo", "晚點打給你"),
            ("工作群", "檔案已上傳"),
            ("語音訊息", "收到 1 則語音訊息"),
            ("備份", "聊天備份已完成")
        }),

        new("X.Corp", "X", "X", "#FF3A4356", true, 22, new[]
        {
            ("提及", "有人在貼文中提到你"),
            ("新追隨者", "你有 4 位新追隨者"),
            ("熱門話題", "台灣的熱門趨勢"),
            ("私訊", "收到一則新私訊"),
            ("互動", "你的貼文被轉發 36 次")
        }),

        new("Reddit", "Reddit", "R", "#FFFF4500", true, 8, new[]
        {
            ("r/programming", "熱門貼文推薦"),
            ("回覆", "有人回覆了你的留言"),
            ("私訊", "版主傳送了訊息"),
            ("獎勵", "你的貼文獲得金獎"),
            ("追蹤社群", "今日精選整理")
        }),

        new("Windows.SystemToast.SecurityAndMaintenance", "Windows 安全性", "防", "#FF34D399", false, 12, new[]
        {
            ("病毒與威脅防護", "快速掃描已完成，未發現威脅"),
            ("防火牆", "已封鎖一個應用程式的連入連線要求"),
            ("裝置安全性", "核心隔離的記憶體完整性目前為關閉狀態"),
            ("帳戶保護", "建議為本機帳戶設定 Windows Hello"),
            ("受控資料夾存取", "已封鎖一次未授權的資料夾寫入")
        }),

        new("Windows.SystemToast.WindowsUpdate.Notification", "Windows Update", "更", "#FF6B7F98", false, 6, new[]
        {
            ("需要重新啟動", "更新已準備就緒，將於離峰時間自動重新啟動"),
            ("更新已安裝", "2026-09 累積更新已成功安裝"),
            ("下載中", "功能更新正在背景下載（62%）"),
            ("驅動程式更新", "已為顯示卡安裝新版驅動程式"),
            ("排程通知", "你的裝置將於今晚 03:00 重新啟動")
        }),

        new("Line.Bank", "銀行通知", "銀", "#FFF59E0B", false, 16, new[]
        {
            ("消費通知", "信用卡消費 NT$ 1,280"),
            ("轉帳成功", "已轉出 NT$ 5,000"),
            ("帳單提醒", "本期帳單將於 5 日後到期"),
            ("安全提醒", "偵測到新裝置登入"),
            ("回饋入帳", "本月現金回饋 NT$ 236")
        }),

        new("Uber.Eats", "Uber Eats", "U", "#FF06C167", true, 20, new[]
        {
            ("訂單已接受", "餐廳正在準備你的餐點"),
            ("外送中", "外送夥伴已取餐，預計 12 分鐘後送達"),
            ("優惠", "今日限定免外送費"),
            ("評價", "為上一筆訂單評分"),
            ("推薦", "你附近新開了一家店")
        })
    };

    public static List<AppChannel> Build()
    {
        var rng = new Random(20260910);
        var now = DateTime.Now;
        long id = 100000;
        var apps = new List<AppChannel>();

        foreach (var seed in Seeds)
        {
            var app = new AppChannel
            {
                Key = seed.Key,
                DisplayName = seed.Name,
                Badge = seed.Badge,
                Accent = seed.Accent,
                BlockEnabled = seed.Block
            };

            // 由近到遠鋪時間軸：越近的紀錄越密集，看起來才像持續在攔截。
            var cursor = now.AddMinutes(-rng.Next(3, 70));
            var seedIndex = Array.IndexOf(Seeds, seed);
            var count = seedIndex < 10 ? seed.Count : seedIndex < 16 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                var sample = seed.Samples[i < seed.Samples.Length ? i : rng.Next(seed.Samples.Length)];

                // 固定歷史狀態，讓示範的總數與參考稿一致（298 攔截 / 28 允許）。
                // 目前的程式開關只代表未來的規則，不會改寫歷史紀錄。
                int[] allowedCounts = { 8, 2, 4, 2, 1, 3, 1, 2, 1, 2 };
                var allowedCount = seedIndex < 10 ? allowedCounts[seedIndex] : seedIndex == 10 ? 2 : 0;
                var allowed = seedIndex == 0
                    ? i == 3 || i >= count - (allowedCount - 1)
                    : i >= count - allowedCount;
                var blocked = !allowed;

                app.Records.Add(new NotificationRecord
                {
                    Id = ++id,
                    AppKey = seed.Key,
                    Title = sample.Title,
                    Body = sample.Body,
                    ArrivalTime = cursor,
                    Blocked = blocked
                });

                cursor = cursor.AddMinutes(-(14 + i * 7 + rng.Next(0, 90)));
            }

            apps.Add(app);
        }

        return apps;
    }
}
