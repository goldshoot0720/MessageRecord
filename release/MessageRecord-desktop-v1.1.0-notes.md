# MessageRecord 1.1.0

桌面版。把手機版的紀錄、去重、搜尋與 JSON 匯出接到 Avalonia，並依作業系統讀取本機通知。

- macOS 讀取通知中心資料庫，需要完整磁碟取用權。
- Windows 讀取 `wpndatabase.db`。
- Linux 監看桌面通知匯流排。
- 手機版匯出的 JSON 可在設定頁匯入。
- 桌面系統不會把其他程式的通知從通知中心移除；攔截會寫進紀錄與規則。

安裝檔附在這個 Release。SHA-256：

```
fa3a66f740144903791fe126aad1ab0bea11795d66ec442ff7708c8054f4cd5d  MessageRecord-1.1.0-osx-arm64.zip
461472dfd4d98d79dbe2cf2278a11fd15d46d73714167fb01459447dffcf2363  MessageRecord-1.1.0-osx-arm64.dmg
cb982f3b2cfa4cf3136f6aa4838fd93c36f44d6b231444a9228248e9c8640500  MessageRecord-1.1.0-win-x64.zip
```
