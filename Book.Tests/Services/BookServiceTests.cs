using Book.Common;
using Book.Services;
using Book.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Book.Tests.Services;

/// <summary>
/// BookService：分類、書籍 CRUD、借閱 / 歸還、使用者借閱紀錄。
/// 整個類別共用一個資料庫，每個測試自己建立使用者與書籍，斷言只針對自己建的資料。
/// </summary>
public class BookServiceTests(SqlServerFixture fx) : IClassFixture<SqlServerFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ───────────── 分類 ─────────────

    [Fact]
    public async Task GetCategories_回傳種子分類並依Sort排序()
    {
        var list = await fx.Books.GetCategoriesAsync();

        Assert.Equal(new[] { "程式設計", "資料庫", "軟體工程", "其他" }, list.Select(c => c.Name));
    }

    // ───────────── 查詢 ─────────────

    [Fact]
    public async Task Search_關鍵字比對書名作者出版社()
    {
        var key = TestData.Unique("k");
        var byTitle = await fx.AddBookAsync(title: $"{key} 書名");
        var byAuthor = await fx.AddBookAsync(author: $"{key} 作者");
        var byPublisher = await fx.AddBookAsync(publisher: $"{key} 出版社");
        await fx.AddBookAsync();   // 不相關的書

        var (items, total) = await fx.Books.SearchBooksAsync(key, null, 1);

        Assert.Equal(3, total);
        Assert.Equal(new[] { byTitle, byAuthor, byPublisher }, items.Select(b => b.Id).Order());
        // 列表需要帶出分類名稱
        Assert.All(items, b => Assert.NotNull(b.Category));
    }

    [Fact]
    public async Task Search_關鍵字前後空白會去除且不分大小寫()
    {
        var key = TestData.Unique("k");
        var id = await fx.AddBookAsync(title: $"Clean {key.ToUpperInvariant()}");

        var (items, total) = await fx.Books.SearchBooksAsync($"  {key.ToLowerInvariant()}  ", null, 1);

        Assert.Equal(1, total);
        Assert.Equal(id, Assert.Single(items).Id);
    }

    [Fact]
    public async Task Search_分類篩選()
    {
        var key = TestData.Unique("k");
        var inDb = await fx.AddBookAsync(title: key, categoryId: 2);
        await fx.AddBookAsync(title: key, categoryId: 1);

        var (items, total) = await fx.Books.SearchBooksAsync(key, 2, 1);

        Assert.Equal(1, total);
        Assert.Equal(inDb, Assert.Single(items).Id);
    }

    [Fact]
    public async Task Search_分頁每頁10筆()
    {
        var key = TestData.Unique("p");
        for (var i = 0; i < BorrowRules.PageSize + 1; i++)
            await fx.AddBookAsync(publisher: key);

        var (page1, total1) = await fx.Books.SearchBooksAsync(key, null, 1);
        var (page2, total2) = await fx.Books.SearchBooksAsync(key, null, 2);

        Assert.Equal(11, total1);
        Assert.Equal(11, total2);
        Assert.Equal(BorrowRules.PageSize, page1.Count);
        Assert.Single(page2);
        // 依 Id 排序，兩頁不重複
        Assert.Empty(page1.Select(b => b.Id).Intersect(page2.Select(b => b.Id)));
    }

    // ───────────── 新增 / 編輯 ─────────────

    [Fact]
    public async Task SaveBook_新增時自動填入建立與更新時間()
    {
        var title = TestData.Unique("新書");
        var before = DateTime.UtcNow.AddSeconds(-1);

        var r = await fx.Books.SaveBookAsync(new() { Title = title, Author = "A", Publisher = "P", CategoryId = 1, Quantity = 2 });

        Assert.True(r.Success);
        Assert.Equal("書籍已新增", r.Message);
        await using var db = await fx.CreateDbAsync();
        var b = await db.Books.SingleAsync(x => x.Title == title, Ct);
        Assert.Equal(2, b.Quantity);
        Assert.True(b.CreateAt >= before);
        Assert.Equal(b.CreateAt, b.UpdateAt);
    }

    [Fact]
    public async Task SaveBook_編輯時更新欄位_建立時間不變()
    {
        var id = await fx.AddBookAsync(quantity: 1);
        DateTime createdAt;
        await using (var db = await fx.CreateDbAsync())
            createdAt = (await db.Books.FindAsync([id], Ct))!.CreateAt;

        // 表單物件的 CreateAt 故意給錯的值，確認不會被寫回
        var r = await fx.Books.SaveBookAsync(new()
        {
            Id = id, Title = "改過的書名", Author = "新作者", Publisher = "新出版社",
            CategoryId = 3, Quantity = 9, CreateAt = new DateTime(2000, 1, 1),
        });

        Assert.True(r.Success);
        Assert.Equal("書籍已更新", r.Message);
        await using var check = await fx.CreateDbAsync();
        var b = await check.Books.SingleAsync(x => x.Id == id, Ct);
        Assert.Equal(("改過的書名", "新作者", "新出版社", 3, 9), (b.Title, b.Author, b.Publisher, b.CategoryId, b.Quantity));
        Assert.Equal(createdAt, b.CreateAt);
        Assert.True(b.UpdateAt >= createdAt);
    }

    [Fact]
    public async Task SaveBook_編輯不存在的書_回傳失敗()
    {
        var r = await fx.Books.SaveBookAsync(new() { Id = int.MaxValue, Title = "X", Author = "X", Publisher = "X", CategoryId = 1 });

        Assert.False(r.Success);
        Assert.Equal("找不到此書籍", r.Message);
    }

    // ───────────── 刪除 ─────────────

    [Fact]
    public async Task DeleteBook_不存在_回傳失敗()
    {
        var r = await fx.Books.DeleteBookAsync(int.MaxValue);

        Assert.False(r.Success);
        Assert.Equal("找不到此書籍", r.Message);
    }

    [Fact]
    public async Task DeleteBook_仍有未歸還紀錄_擋下且不刪除()
    {
        var user = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync();
        await fx.AddRecordAsync(user.Id, id, dueInDays: 5);

        var r = await fx.Books.DeleteBookAsync(id);

        Assert.False(r.Success);
        Assert.Equal("此書仍有未歸還的借閱紀錄，無法刪除", r.Message);
        await using var db = await fx.CreateDbAsync();
        Assert.True(await db.Books.AnyAsync(b => b.Id == id, Ct));
    }

    [Fact]
    public async Task DeleteBook_只有已歸還紀錄_連同歷史一起刪除()
    {
        var user = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync();
        await fx.AddRecordAsync(user.Id, id, dueInDays: -3, returnedDaysAgo: 5);

        var r = await fx.Books.DeleteBookAsync(id);

        Assert.True(r.Success);
        await using var db = await fx.CreateDbAsync();
        Assert.False(await db.Books.AnyAsync(b => b.Id == id, Ct));
        Assert.False(await db.BorrowRecords.AnyAsync(x => x.BookId == id, Ct));
    }

    // ───────────── 借閱 ─────────────

    [Fact]
    public async Task Borrow_成功_扣庫存並建立紀錄()
    {
        var user = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync(quantity: 2);

        var r = await fx.Books.BorrowAsync(user.Id, id);

        Assert.True(r.Success);
        Assert.Equal("借閱成功", r.Message);
        Assert.Equal(1, await fx.GetQuantityAsync(id));
        await using var db = await fx.CreateDbAsync();
        var rec = await db.BorrowRecords.SingleAsync(x => x.UserId == user.Id, Ct);
        Assert.Equal(id, rec.BookId);
        Assert.Null(rec.ReturnedAt);
        // 應還日 = 借閱當天 + 14 天（只到日期）
        Assert.Equal(DateTime.Today.AddDays(BorrowRules.LoanDays), rec.DueDate);
    }

    [Fact]
    public async Task Borrow_同一本書未歸還前不可重複借()
    {
        var user = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync(quantity: 5);
        await fx.Books.BorrowAsync(user.Id, id);

        var r = await fx.Books.BorrowAsync(user.Id, id);

        Assert.False(r.Success);
        Assert.Equal("已借閱此書", r.Message);
        Assert.Equal(4, await fx.GetQuantityAsync(id));
    }

    [Fact]
    public async Task Borrow_歸還後可以再借同一本()
    {
        var user = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync(quantity: 1);
        await fx.AddRecordAsync(user.Id, id, dueInDays: -1, returnedDaysAgo: 1);

        var r = await fx.Books.BorrowAsync(user.Id, id);

        Assert.True(r.Success);
    }

    [Fact]
    public async Task Borrow_已借5本_第6本擋下()
    {
        var user = await fx.CreateUserAsync();
        for (var i = 0; i < BorrowRules.MaxBorrowCount; i++)
            Assert.True((await fx.Books.BorrowAsync(user.Id, await fx.AddBookAsync())).Success);
        var sixth = await fx.AddBookAsync(quantity: 1);

        var r = await fx.Books.BorrowAsync(user.Id, sixth);

        Assert.False(r.Success);
        Assert.Equal("已達借閱上限", r.Message);
        Assert.Equal(1, await fx.GetQuantityAsync(sixth));
    }

    [Fact]
    public async Task Borrow_已歸還的紀錄不計入上限()
    {
        var user = await fx.CreateUserAsync();
        for (var i = 0; i < BorrowRules.MaxBorrowCount; i++)
            await fx.AddRecordAsync(user.Id, await fx.AddBookAsync(), dueInDays: -1, returnedDaysAgo: 1);

        var r = await fx.Books.BorrowAsync(user.Id, await fx.AddBookAsync());

        Assert.True(r.Success);
    }

    [Fact]
    public async Task Borrow_無庫存_擋下且不建立紀錄()
    {
        var user = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync(quantity: 0);

        var r = await fx.Books.BorrowAsync(user.Id, id);

        Assert.False(r.Success);
        Assert.Equal("該書本已無庫存", r.Message);
        Assert.Equal(0, await fx.GetQuantityAsync(id));
        await using var db = await fx.CreateDbAsync();
        Assert.False(await db.BorrowRecords.AnyAsync(x => x.UserId == user.Id, Ct));
    }

    // ───────────── 歸還 ─────────────

    [Fact]
    public async Task Return_成功_加回庫存並記錄歸還時間()
    {
        var user = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync(quantity: 1);
        await fx.Books.BorrowAsync(user.Id, id);
        var recId = await RecordIdAsync(user.Id, id);
        var before = DateTime.Now.AddSeconds(-1);

        var r = await fx.Books.ReturnAsync(recId, user.Id);

        Assert.True(r.Success);
        Assert.Equal("歸還成功", r.Message);
        Assert.Equal(1, await fx.GetQuantityAsync(id));
        await using var db = await fx.CreateDbAsync();
        var rec = await db.BorrowRecords.SingleAsync(x => x.Id == recId, Ct);
        Assert.NotNull(rec.ReturnedAt);
        Assert.True(rec.ReturnedAt >= before.AddSeconds(-1));   // datetime2(0) 只到秒
    }

    [Fact]
    public async Task Return_不能歸還別人的紀錄()
    {
        var owner = await fx.CreateUserAsync();
        var other = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync(quantity: 1);
        await fx.Books.BorrowAsync(owner.Id, id);
        var recId = await RecordIdAsync(owner.Id, id);

        var r = await fx.Books.ReturnAsync(recId, other.Id);

        Assert.False(r.Success);
        Assert.Equal("此筆紀錄已歸還或不存在", r.Message);
        Assert.Equal(0, await fx.GetQuantityAsync(id));
    }

    [Fact]
    public async Task Return_重複歸還_庫存只加一次()
    {
        var user = await fx.CreateUserAsync();
        var id = await fx.AddBookAsync(quantity: 1);
        await fx.Books.BorrowAsync(user.Id, id);
        var recId = await RecordIdAsync(user.Id, id);

        var first = await fx.Books.ReturnAsync(recId, user.Id);
        var second = await fx.Books.ReturnAsync(recId, user.Id);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal("此筆紀錄已歸還或不存在", second.Message);
        Assert.Equal(1, await fx.GetQuantityAsync(id));
    }

    // ───────────── 使用者借閱紀錄 / 到期統計 ─────────────

    [Fact]
    public async Task GetUserRecords_未歸還依應還日由近到遠_已歸還依歸還日由新到舊()
    {
        var user = await fx.CreateUserAsync(displayName: "小明");
        var other = await fx.CreateUserAsync();
        var title = TestData.Unique("書");
        var book = await fx.AddBookAsync(title: title);

        var returnedOld = await fx.AddRecordAsync(user.Id, book, dueInDays: -20, returnedDaysAgo: 10);
        var activeFar = await fx.AddRecordAsync(user.Id, book, dueInDays: 10);
        var returnedNew = await fx.AddRecordAsync(user.Id, book, dueInDays: -5, returnedDaysAgo: 1);
        var activeNear = await fx.AddRecordAsync(user.Id, book, dueInDays: 2);
        await fx.AddRecordAsync(other.Id, book, dueInDays: 1);   // 別人的紀錄不應出現

        var rows = await fx.Books.GetUserRecordsAsync(user.Id);

        Assert.Equal(new[] { activeNear, activeFar, returnedNew, returnedOld }, rows.Select(x => x.Record.Id));
        Assert.All(rows, x =>
        {
            Assert.Equal(title, x.BookTitle);
            Assert.Equal("小明", x.BorrowerName);
        });
    }

    [Fact]
    public async Task CountDue_指定使用者_統計即將到期與逾期()
    {
        var user = await fx.CreateUserAsync();
        var other = await fx.CreateUserAsync();
        var book = await fx.AddBookAsync();

        await fx.AddRecordAsync(user.Id, book, dueInDays: 3);                       // 即將到期
        await fx.AddRecordAsync(user.Id, book, dueInDays: 7);                       // 即將到期（邊界）
        await fx.AddRecordAsync(user.Id, book, dueInDays: 8);                       // 一般
        await fx.AddRecordAsync(user.Id, book, dueInDays: -1);                      // 逾期
        await fx.AddRecordAsync(user.Id, book, dueInDays: -9, returnedDaysAgo: 2);  // 已歸還，不計
        await fx.AddRecordAsync(other.Id, book, dueInDays: -1);                     // 別人的，不計

        var (soon, overdue) = await fx.Books.CountDueAsync(user.Id);

        Assert.Equal(2, soon);
        Assert.Equal(1, overdue);
    }

    /// <summary>取得某使用者對某本書唯一一筆借閱紀錄的 Id。</summary>
    private async Task<int> RecordIdAsync(string userId, int bookId)
    {
        await using var db = await fx.CreateDbAsync();
        return await db.BorrowRecords.Where(x => x.UserId == userId && x.BookId == bookId).Select(x => x.Id).SingleAsync(Ct);
    }
}
