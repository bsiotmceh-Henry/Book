using Book.Common;
using Book.Data;
using Book.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Book.Tests.Infrastructure;

/// <summary>
/// 每個測試類別一個獨立的 SQL Server 資料庫（以 IClassFixture 使用）。
/// 用真正的 migration 建立結構與種子資料（分類、書籍、角色），類別內測試結束後刪除資料庫。
/// DI 註冊與正式站共用 AddBookServices，所以 Identity 的密碼 / 鎖定規則與正式環境一致。
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>
    /// 預設連 LocalDB；可用環境變數 BOOK_TEST_SQL 覆蓋（例如 CI 或本機 SQL Server），
    /// 資料庫名稱一律由 fixture 產生，覆蓋的連線字串裡的 Database 會被取代。
    /// </summary>
    private const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    private ServiceProvider? provider;

    public string ConnectionString { get; }

    public SqlServerFixture()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BOOK_TEST_SQL") ?? DefaultServer)
        {
            // 每個 fixture 各自一個資料庫，不同測試類別平行執行時互不干擾
            InitialCatalog = $"Book_Test_{Guid.NewGuid():N}",
        };
        ConnectionString = builder.ConnectionString;
    }

    /// <summary>與正式站相同設定的 DI 容器。</summary>
    public IServiceProvider Services => provider ?? throw new InvalidOperationException("Fixture 尚未初始化");

    public BookService Books => Services.GetRequiredService<BookService>();

    public UserService Users => Services.GetRequiredService<UserService>();

    /// <summary>建立一個短生命週期的 DbContext，用來準備資料或直接檢查資料庫狀態。</summary>
    public Task<AppDbContext> CreateDbAsync()
        => Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();

    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        // UserManager 需要 ILogger
        services.AddLogging();
        services.AddBookServices(opt => opt.UseSqlServer(ConnectionString));
        provider = services.BuildServiceProvider();

        // 跑真正的 migration（含 HasData 種子）；model 與 migration 不同步時 EF 會直接報錯
        await using var db = await CreateDbAsync();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (provider == null) return;

        await using (var db = await CreateDbAsync())
        {
            // 清掉連線池，避免殘留連線讓 DROP DATABASE 卡住
            SqlConnection.ClearAllPools();
            await db.Database.EnsureDeletedAsync();
        }
        await provider.DisposeAsync();
    }
}
