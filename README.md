# 書本管理系統 (Book)

以 Blazor Web App (.NET 10) 開發的書本管理系統，提供書籍維護、借還書、逾期追蹤與使用者管理。資料存放於 SQL Server，帳號、角色、密碼與鎖定機制使用 ASP.NET Core Identity。

## 功能

### 一般使用者
- 註冊 / 登入
- 書籍查詢（關鍵字、分類篩選，按下「查詢」才套用條件，分頁每頁 10 筆）
- 借書 / 還書
- 我的借閱：顯示剩餘天數、逾期天數、已歸還狀態
- 即將到期 / 逾期提醒橫幅

### 管理員
- 書籍與分類管理（新增、編輯、刪除；仍有未歸還紀錄的書籍不可刪除）
- 借閱紀錄管理
- 使用者管理（編輯、軟刪除；不可刪除自己或仍有未歸還書籍的使用者）

### 借閱規則
定義於 [BorrowRules.cs](Book/Common/BorrowRules.cs)：

| 項目 | 值 |
| --- | --- |
| 借閱期限 | 14 天 |
| 借閱上限 | 5 本 |
| 即將到期門檻 | 7 天 |
| 每頁筆數 | 10 筆 |

借閱狀態不落地儲存，由 `BorrowStatus.Of(record, today)` 於顯示時依到期日計算。

## 技術堆疊

- .NET 10、Blazor Web App（Interactive Server，登入頁為 static SSR）
- EF Core 10 + SQL Server
- ASP.NET Core Identity（密碼、角色、5 次失敗鎖定 5 分鐘）
- Cookie 驗證（`Book.Auth`，8 小時滑動過期，每次請求重新驗證使用者狀態）
- BootstrapBlazor 11
- 測試：xUnit v3 + Microsoft Testing Platform，對真實 SQL Server (LocalDB) 執行
- 部署：Docker + GitHub Actions（Workload Identity Federation）-> Artifact Registry -> GCP VM

## 專案結構

```
Book.slnx
Book/                  主專案
  Common/              常數、驗證擴充、DI 註冊 (AddBookServices)
  Components/          Layout 與頁面 (Auth / Books / User)
  Data/                DbContext、實體、Migrations
  Services/            BookService、UserService、IdentitySeeder、BorrowStatus
Book.Tests/            服務層整合測試
deploy/                docker-compose.yml (app + SQL Server)
doc/                   開發文件 (PDF)
.github/workflows/     CI/CD 部署流程
```

## 頁面路由

| 路由 | 說明 | 權限 |
| --- | --- | --- |
| `/login` `/register` `/register-done` | 登入 / 註冊 | 公開 |
| `/book-search` | 書籍查詢與借書 | 登入 |
| `/my-borrow` | 我的借閱 | 一般使用者 |
| `/book-manage` | 書籍管理 | 管理員 |
| `/borrow-manage` | 借閱紀錄管理 | 管理員 |
| `/user-manage` | 使用者管理 | 管理員 |

## 本機開發

需求：.NET 10 SDK、SQL Server（LocalDB 或其他實例）。

1. 在 `Book/` 底下建立 `appsettings.Development.json`（已被 `.gitignore` 排除，不會進版控），填入連線字串與預設管理員帳密：

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=localhost;Initial Catalog=Book;Integrated Security=True;TrustServerCertificate=True"
     },
     "DefaultAdmin": {
       "UserName": "admin",
       "Password": "請自行設定",
       "DisplayName": "管理員"
     }
   }
   ```

2. 啟動：

   ```powershell
   dotnet run
   ```

預設管理員由 `DefaultAdmin:*` 設定於啟動時建立（`appsettings.json` 內有預設值，本機請用 `appsettings.Development.json` 覆寫，正式環境以環境變數覆寫，例如 `DefaultAdmin__Password`）。一般使用者可自行於 `/register` 註冊。

> 重新建置前請先停止執行中的實例，否則 `Book.exe` 被鎖定會導致建置失敗。

## 測試

於 repo 根目錄執行：

```powershell
dotnet test                                          # 全部
dotnet test -- --filter-class "*BookServiceTests"    # 單一類別
dotnet test -- --filter-method "*Borrow*"            # 單一方法
```

測試預設使用 `(localdb)\MSSQLLocalDB`，每個測試類別建立獨立的 `Book_Test_{guid}` 資料庫並於結束後刪除。可用環境變數 `BOOK_TEST_SQL` 指定其他 SQL Server。

## 部署

推送到 `master` 會觸發 [deploy.yml](.github/workflows/deploy.yml)：建置映像檔、推送至 Artifact Registry（以 commit sha 為 tag），再於 VM 上以 [docker-compose.yml](deploy/docker-compose.yml) 啟動 app 與 SQL Server 2022 Express。SQL Server 不對外開放，只有 compose 網路內的 app 可連線；`MSSQL_SA_PASSWORD` 由 VM 上同目錄的 `.env` 提供。
