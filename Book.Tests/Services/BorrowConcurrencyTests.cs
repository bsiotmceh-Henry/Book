using Book.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Book.Tests.Services;

/// <summary>
/// 借閱的併發行為。獨立一個類別（獨立的全新資料庫），
/// 因為 Serializable 的 key-range lock 範圍取決於 BorrowRecords 既有資料的分布，
/// 跟其他測試共用資料庫時結果會隨執行順序改變；空表是最容易重現的狀態（也是系統剛上線時的狀態）。
/// </summary>
public class BorrowConcurrencyTests(SqlServerFixture fx) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>每個測試開始前清空借閱紀錄，確保都從空表開始，結果不受測試執行順序影響。</summary>
    public async ValueTask InitializeAsync()
    {
        await using var db = await fx.CreateDbAsync();
        await db.BorrowRecords.ExecuteDeleteAsync(Ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task 多人同時搶最後一本_只有一人成功且庫存不為負()
    {
        const int n = 5;
        var users = new List<string>();
        for (var i = 0; i < n; i++) users.Add((await fx.CreateUserAsync()).Id);
        var id = await fx.AddBookAsync(quantity: 1);

        // 同時送出 n 筆借閱
        var results = await Task.WhenAll(users.Select(u => Task.Run(() => fx.Books.BorrowAsync(u, id), Ct)));

        Assert.Equal(1, results.Count(r => r.Success));
        Assert.All(results.Where(r => !r.Success), r => Assert.Equal("該書本已無庫存", r.Message));
        Assert.Equal(0, await fx.GetQuantityAsync(id));
        await using var db = await fx.CreateDbAsync();
        Assert.Equal(1, await db.BorrowRecords.CountAsync(x => x.BookId == id, Ct));
    }

    [Fact]
    public async Task 多人同時借不同的書_全部成功()
    {
        const int n = 5;
        var pairs = new List<(string User, int Book)>();
        for (var i = 0; i < n; i++)
            pairs.Add(((await fx.CreateUserAsync()).Id, await fx.AddBookAsync(quantity: 1)));

        // 彼此沒有競爭同一份庫存，不應該有任何一筆失敗
        var results = await Task.WhenAll(pairs.Select(p => Task.Run(() => fx.Books.BorrowAsync(p.User, p.Book), Ct)));

        Assert.All(results, r => Assert.True(r.Success, r.Message));
        foreach (var p in pairs)
            Assert.Equal(0, await fx.GetQuantityAsync(p.Book));
    }
}
