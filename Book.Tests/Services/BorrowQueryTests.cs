using Book.Common;
using Book.Services;
using Book.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Book.Tests.Services;

/// <summary>
/// 跨使用者的借閱查詢（管理員的借閱紀錄頁、全站到期統計）。
/// 這些查詢看的是整張 BorrowRecords，所以每個測試開始前先清空借閱紀錄，
/// 避免同一資料庫裡其他測試留下的資料影響總數與分頁。
/// </summary>
public class BorrowQueryTests(SqlServerFixture fx) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>xUnit 每個測試都會建立新的類別實例，這裡等於「每個測試開始前」。</summary>
    public async ValueTask InitializeAsync()
    {
        await using var db = await fx.CreateDbAsync();
        await db.BorrowRecords.ExecuteDeleteAsync(Ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// 建立一組涵蓋所有狀態的紀錄：
    /// r1 一般（+10）、r2 即將到期（+3）、r3 今天到期（0）、r4 逾期（-2）、
    /// r5 已歸還（3 天前還）、r6 逾期後才歸還（1 天前還）。
    /// </summary>
    private async Task<Dictionary<string, int>> SeedAllStatesAsync()
    {
        var a = await fx.CreateUserAsync(displayName: "甲");
        var b = await fx.CreateUserAsync(displayName: "乙");
        var book = await fx.AddBookAsync(title: "測試書");

        return new()
        {
            ["r1"] = await fx.AddRecordAsync(a.Id, book, dueInDays: 10),
            ["r2"] = await fx.AddRecordAsync(b.Id, book, dueInDays: 3),
            ["r3"] = await fx.AddRecordAsync(a.Id, book, dueInDays: 0),
            ["r4"] = await fx.AddRecordAsync(b.Id, book, dueInDays: -2),
            ["r5"] = await fx.AddRecordAsync(a.Id, book, dueInDays: 5, returnedDaysAgo: 3),
            ["r6"] = await fx.AddRecordAsync(b.Id, book, dueInDays: -10, returnedDaysAgo: 1),
        };
    }

    [Theory]
    [InlineData(BorrowFilter.Active, "r1,r2,r3,r4")]
    [InlineData(BorrowFilter.DueSoon, "r2,r3")]          // 今天到期也算即將到期
    [InlineData(BorrowFilter.Overdue, "r4")]             // 逾期但已歸還的不算
    [InlineData(BorrowFilter.Returned, "r5,r6")]
    public async Task GetAllRecords_依篩選條件回傳對應紀錄(BorrowFilter filter, string expected)
    {
        var ids = await SeedAllStatesAsync();

        var (rows, total) = await fx.Books.GetAllRecordsAsync(filter, 1);

        var want = expected.Split(',').Select(k => ids[k]).Order().ToArray();
        Assert.Equal(want.Length, total);
        Assert.Equal(want, rows.Select(r => r.Record.Id).Order().ToArray());
    }

    [Fact]
    public async Task GetAllRecords_全部_未歸還依應還日在前_已歸還依歸還日新到舊在後()
    {
        var ids = await SeedAllStatesAsync();

        var (rows, total) = await fx.Books.GetAllRecordsAsync(BorrowFilter.All, 1);

        Assert.Equal(6, total);
        Assert.Equal(new[] { "r4", "r3", "r2", "r1", "r6", "r5" }.Select(k => ids[k]), rows.Select(r => r.Record.Id));
        // 帶出書名與借閱人顯示名稱
        Assert.All(rows, r => Assert.Equal("測試書", r.BookTitle));
        Assert.Equal("乙", rows[0].BorrowerName);
        Assert.Equal("甲", rows[1].BorrowerName);
    }

    [Fact]
    public async Task GetAllRecords_分頁每頁10筆且順序一致()
    {
        var user = await fx.CreateUserAsync();
        var book = await fx.AddBookAsync();
        for (var i = 0; i < BorrowRules.PageSize + 1; i++)
            await fx.AddRecordAsync(user.Id, book, dueInDays: i);

        var (page1, total) = await fx.Books.GetAllRecordsAsync(BorrowFilter.All, 1);
        var (page2, _) = await fx.Books.GetAllRecordsAsync(BorrowFilter.All, 2);

        Assert.Equal(11, total);
        Assert.Equal(BorrowRules.PageSize, page1.Count);
        // 應還日最晚（+10 天）的那筆排在第 2 頁
        Assert.Equal(DateTime.Today.AddDays(10), Assert.Single(page2).Record.DueDate);
        Assert.True(page1.Select(r => r.Record.DueDate).SequenceEqual(page1.Select(r => r.Record.DueDate).Order()));
    }

    [Fact]
    public async Task CountDue_不指定使用者_統計全部人()
    {
        var a = await fx.CreateUserAsync();
        var b = await fx.CreateUserAsync();
        var book = await fx.AddBookAsync();

        await fx.AddRecordAsync(a.Id, book, dueInDays: 3);                        // 即將到期
        await fx.AddRecordAsync(a.Id, book, dueInDays: -1);                       // 逾期
        await fx.AddRecordAsync(b.Id, book, dueInDays: 7);                        // 即將到期
        await fx.AddRecordAsync(b.Id, book, dueInDays: -5);                       // 逾期
        await fx.AddRecordAsync(b.Id, book, dueInDays: 8);                        // 一般
        await fx.AddRecordAsync(b.Id, book, dueInDays: -9, returnedDaysAgo: 1);   // 已歸還

        var (soon, overdue) = await fx.Books.CountDueAsync(null);

        Assert.Equal(2, soon);
        Assert.Equal(2, overdue);
    }
}
