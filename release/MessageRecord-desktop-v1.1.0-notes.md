# MessageRecord 1.1.0

桌面版。把手機版的紀錄、去重、搜尋與 JSON 匯出接到 Avalonia，並依作業系統讀取本機通知。

- macOS 讀取通知中心資料庫，需要完整磁碟取用權。
- Windows 讀取 `wpndatabase.db`。
- Linux 監看桌面通知匯流排。
- 手機版匯出的 JSON 可在設定頁匯入。
- 桌面系統不會把其他程式的通知從通知中心移除；攔截會寫進紀錄與規則。
- 應用程式圖示是笑臉鈴鐺與盾牌，用於視窗、macOS App 與 Windows 執行檔。

安裝檔附在這個 Release。SHA-256：

```
6940c8882b798732fba6dbc38e127915f0ff2799bbb39e2b7805c39c345d2868  MessageRecord-1.1.0-osx-arm64.zip
4d4bef7a7778e17a673bad52f526dbd08ffc661e78a3f18a8d21e3d4d3ba8f54  MessageRecord-1.1.0-osx-arm64.dmg
ad1bb2f798d3f62b880846e2d99f3b6a84ac722926e4fe493dacf70d65ee65de  MessageRecord-1.1.0-win-x64.zip
```
