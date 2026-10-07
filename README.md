# MessageRecord

桌面版通知紀錄。依程式分類查看通知，搜尋、篩選、匯出，並在系統允許時讀取本機通知中心。

手機版在 [MessageRecordMobile](https://github.com/goldshoot0720/MessageRecordMobile)。兩邊共用 `shared/notiguard-record.schema.json`：手機匯出的 JSON 可以在桌面版設定頁匯入。

- 技術：Avalonia 11.3 + .NET 8
- 目標：`osx-arm64`、`win-x64`，同一份原始碼也包含 `osx-x64`、`win-arm64`、`linux-x64`、`linux-arm64`

## 下載

[Releases](https://github.com/goldshoot0720/MessageRecord/releases) 提供：

- `MessageRecord-<version>-osx-arm64.dmg`：macOS Apple 晶片
- `MessageRecord-<version>-osx-arm64.zip`：同一個 .app
- `MessageRecord-<version>-win-x64.zip`：Windows 64 位元免安裝

macOS 若被系統擋住，請在 Finder 對 App 按右鍵選「打開」。要讀取通知中心，再到「系統設定 → 隱私權與安全性 → 完整磁碟取用權」加入 MessageRecord，然後重新開啟。

## 行為

紀錄存在這台電腦的應用程式支援目錄（macOS 為 `~/Library/Application Support/MessageRecord/records.db`）。

- 以通知識別碼、來源訊息時間與內容的 SHA-256 去重。同一則更新不會重複新增；時間或內容不同則各留一筆。
- 群組摘要不另存。空白通知略過。常駐通知一律記成已允許。
- 第一次讀到的既有通知當成歷史，記成已允許。之後新出現的通知才依總開關與該程式的規則標記已攔截或已允許。
- 沒有個別規則的程式，預設攔截。總開關關閉時仍寫入紀錄，但標成已允許。
- 搜尋比對程式名稱、識別碼、標題與內容。`%`、`_` 與引號是普通文字。
- 匯出 JSON 與手機版格式相同，並多了 `desktop_listener`、`desktop_history` 兩種來源。

桌面系統沒有公開 API 可以把其他程式的通知從通知中心移除。攔截開關會留下規則與紀錄，不會假裝已經清掉系統通知。

各平台來源：

| 平台 | 來源 |
| --- | --- |
| macOS | `~/Library/Group Containers/group.com.apple.usernoted/db2/db`，需要完整磁碟取用權 |
| Windows | `%LOCALAPPDATA%\Microsoft\Windows\Notifications\wpndatabase.db` |
| Linux | 工作階段匯流排上的 `org.freedesktop.Notifications.Notify`（需要 `dbus-monitor`） |

## 執行

```bash
dotnet run --project src/MessageRecord/MessageRecord.csproj
```

打包（在 macOS 上產生 .dmg、.zip，並嘗試 Windows zip）：

```bash
bash tools/package.sh
```

## 畫面

三欄：側欄導覽、程式清單、該程式的紀錄。

- 側欄可切全部、已攔截、已允許、統計、設定。底部是本日攔截。
- 中欄可依數量、時間、名稱排序，也能搜尋程式或通知內容。
- 右欄可看完整內容、複製、匯出這個程式，或移除它的全部紀錄。
- 設定頁有攔截總開關、新程式預設攔截、保留完整內容，以及匯入、匯出、開啟資料夾。

`tools/UiCheck` 仍用內建示範資料檢查版面，不讀取你的通知中心。`tools/DataCheck` 檢查去重、搜尋、匯入與各平台解析。

```bash
dotnet run --project tools/DataCheck/DataCheck.csproj -c Release
dotnet run --project tools/UiCheck/UiCheck.csproj -c Release -p:UsedAvaloniaProducts=
```

## 專案結構

```
src/MessageRecord/
├── Data/          SQLite、搜尋、匯入匯出、分類
├── Capture/       macOS / Windows / Linux 來源與去重規則
├── ViewModels/    三欄畫面
├── Views/
└── Mock/          只給版面檢查用的示範資料
shared/notiguard-record.schema.json
packaging/macos/Info.plist
tools/package.sh
```
