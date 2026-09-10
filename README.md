# NotifBlock · 通知訊息攔截紀錄（介面設計）

依「程式名稱」分類通知，點進任一程式就能看到它的**所有紀錄**。
跨平台桌面程式：**Windows / macOS / Linux** 同一份原始碼。

> 本版本只負責介面設計。畫面、互動、資料模型都完成了，資料來自示範資料集，
> 沒有接真實的通知攔截引擎；接上去的位置已經留好（見文末）。

- 技術：Avalonia 11.3 + .NET 8，MVVM，無第三方 MVVM 套件
- 目標平台：`win-x64` `win-arm64` `osx-x64` `osx-arm64` `linux-x64` `linux-arm64`

## 執行

```bash
dotnet run --project src/MessageRecord/MessageRecord.csproj
```

### 各平台打包

```bash
dotnet publish src/MessageRecord/MessageRecord.csproj -c Release -r win-x64   --self-contained true -p:PublishSingleFile=true
dotnet publish src/MessageRecord/MessageRecord.csproj -c Release -r osx-arm64 --self-contained true
dotnet publish src/MessageRecord/MessageRecord.csproj -c Release -r linux-x64 --self-contained true
```

平台差異已經處理掉的部分：

| 項目 | 做法 |
| --- | --- |
| 視窗外框 | Windows / Linux 用自繪標題列（`NoChrome` + 自製按鈕）；macOS 改用系統紅綠燈（`PreferSystemChrome`），品牌區自動往右讓開 |
| 中文字型 | 字型堆疊涵蓋三平台：Microsoft JhengHei UI → PingFang TC → Noto Sans CJK TC → Segoe UI |
| app.manifest | 只在 Windows 建置時套用（DPI 宣告），其他平台自動略過 |
| 系統 API | 介面層完全不碰平台 API，攔截層被隔離在 `Capture/INotificationSource` 後面 |

## 畫面

三欄式：側欄導覽 → 中欄程式分類 → 右欄該程式的所有紀錄。

### 側欄

- **應用程式 / 全部記錄 / 已攔截 / 已允許**：各自帶總數；選「已攔截」「已允許」會直接把右欄切到對應分頁。
- **統計**：近 7 天攔截趨勢曲線、本週合計 / 單日最高 / 日平均，以及通知最多的程式排行。
- **設定**：開機自動啟動、最小化到系統匣、保留完整內容、自動清理、每日摘要（介面示範，撥了不會改變行為）。
- 底部**本日攔截**卡：大數字 + 與昨日的增減百分比 + 當日逐時曲線（只畫到目前的小時）。

### 中欄：程式分類

每列一個程式：圖示方塊、程式名稱、通知則數、**該程式的攔截開關**、進入箭頭。
標題列可切換排序（按通知數量 / 最新時間 / 程式名稱），頂端搜尋框比對名稱與識別碼。

### 右欄：該程式的所有紀錄

- 標頭：程式名稱、「共 N 則通知・已攔截 M 則」，右側是這個程式的攔截總開關（與中欄同一個值）。
- 分頁：**所有記錄 / 已攔截 / 已允許**，各自帶數量；右側可切換最新在上 / 最舊在上。
- 每列紀錄：來源頭像、標題、內文摘要、時間（今天 / 昨天 / 日期）、狀態標籤（已攔截為藍、已允許為綠）、更多選單，可查看完整通知內容與複製。

## 專案結構

```
src/MessageRecord/
├── Program.cs / App.axaml         進入點與 Avalonia 設定
├── Themes/Palette.axaml           色票與字型（改配色動這裡）
├── Themes/Icons.axaml             介面圖示，全部是描邊路徑，不依賴任何圖示字型
├── Themes/Controls.axaml          控制項樣式：導覽列、程式列、分頁籤、開關、紀錄列
├── Controls/Sparkline.cs          面積折線圖（自繪 Control）
├── Capture/INotificationSource.cs ← 攔截層的介面
├── Capture/NotificationSourceFactory.cs  ← 依平台挑來源
├── Models/                        AppChannel、NotificationRecord
├── Mock/SampleData.cs             示範資料集
├── ViewModels/                    Main / AppList / AppChannel / AppDetail / Record / Stats / Settings
└── Views/                         MainWindow、AppListView、AppDetailView、StatsView、SettingsView
```

## 之後要接真實攔截時

介面只認識 `Capture/INotificationSource`：實作它、回傳 `List<AppChannel>`，
再到 `NotificationSourceFactory.Candidates()` 依平台排進去，畫面一行都不用改。

各平台可行的路線（`NotificationSourceFactory` 的註解裡也有一份）：

- **Windows**：讀 `%LOCALAPPDATA%\Microsoft\Windows\Notifications\wpndatabase.db`（SQLite），
  `Notification` join `NotificationHandler`：`PrimaryId` 是程式的 AUMID、`Payload` 是 toast XML、
  `ArrivalTime` 是 FILETIME。不需特殊權限，但系統會清掉過期紀錄，要自己另存一份才會有「所有紀錄」。
  即時事件則是 `UserNotificationListener`，需要封裝成 MSIX 並宣告 `userNotificationListener` 能力。
- **macOS**：讀 `~/Library/Group Containers/group.com.apple.usernoted/db2/db`（SQLite），
  或用 `UNUserNotificationCenter`；要跨程式監看通知中心需要「輔助使用」權限。
- **Linux**：在 D-Bus session bus 上監看 `org.freedesktop.Notifications` 的 `Notify` 呼叫
  （become monitor / eavesdrop），可以直接拿到 `app_name`、`summary`、`body`。

## 設計備忘

- 底色 `#080F17`，面板 `#0C151E`，主色藍 `#2196FF`，次要文字提高對比。
- 圓角：面板 14、列 12、標籤 7、開關全圓。
- 主要程式使用可縮放的品牌向量圖示（`Controls/AppIcon.cs`），其他程式以名稱首字作為備用圖示。
- 預設視窗 1536 × 1024，三欄隨視窗比例縮放，最小尺寸 1280 × 760。高度低於 900 時收起迷你趨勢圖並縮減間距，保留統計數字與完整導覽。
- 示例包含 28 個程式、326 則通知；攔截數與每日統計依示範紀錄計算。

## 介面驗證

```powershell
dotnet run --project tools/UiCheck/UiCheck.csproj -p:UsedAvaloniaProducts=
```

執行搜尋、空結果、通知篩選、排序、同步開關、統計與設定導覽檢查，並將實際 Avalonia 渲染輸出到 `artifacts/notifblock-1536.png` 與 `artifacts/notifblock-1280.png`、`artifacts/notifblock-1280-min.png`。另檢查三種尺寸的導覽區域與統計卡不重疊。驗證工具使用螢幕外視窗，不會搶走焦點。`UsedAvaloniaProducts` 空值僅用於略過建置遙測，避免受限環境寫入使用者目錄。

### 參考圖版面更新

已調整應用程式字級、列分隔線、通知卡片間距、工具圖示與本日攔截卡片排列。示範歷史狀態固定為 298 則攔截 / 28 則允許，LINE 為 120 / 8；每日統計仍由紀錄日期計算。

驗證：UiCheck 通過搜尋、空結果、分頁、排序、開關連動、統計及設定導覽，並輸出 1536×1024、1280×800、1280×760 畫面至 artifacts。

