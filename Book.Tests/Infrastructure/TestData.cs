using Book.Common;
using Book.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Book.Tests.Infrastructure;

/// <summary>
/// 測試資料建立工具。每次呼叫都產生唯一的帳號 / 書名，
/// 同一個資料庫裡的測試不會互相撞名，也不修改種子資料。
/// </summary>
public static class TestData
{
    /// <summary>測試帳號的預設密碼（符合 8 碼、含英文與數字）。</summary>
    public const string Password = "Passw0rd";

    /// <summary>產生唯一字串（16 碼），用來當帳號或書名的一部分。</summary>
    public static string Unique(string prefix = "t") => $"{prefix}{Guid.NewGuid():N}"[..16];

    /// <summary>以 UserManager 建立使用者並指派角色，回傳 ApplicationUser。</summary>
    public static async Task<ApplicationUser> CreateUserAsync(
        this SqlServerFixture fx, string role = Roles.User, string password = Password, string? displayName = null)
    {
        using var scope = fx.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = Unique("u"), DisplayName = displayName ?? Unique("顯示") };
        Check(await um.CreateAsync(user, password));
        Check(await um.AddToRoleAsync(user, role));
        return user;
    }

    /// <summary>直接寫入一本書（分類 1：程式設計），回傳 Id。</summary>
    public static async Task<int> AddBookAsync(
        this SqlServerFixture fx, int quantity = 3, string? title = null, string? author = null,
        string? publisher = null, int categoryId = 1)
    {
        await using var db = await fx.CreateDbAsync();
        var book = new BookItem
        {
            Title = title ?? Unique("書"),
            Author = author ?? "作者",
            Publisher = publisher ?? "出版社",
            CategoryId = categoryId,
            Quantity = quantity,
        };
        db.Books.Add(book);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return book.Id;
    }

    /// <summary>
    /// 直接寫入一筆借閱紀錄（不經過 BorrowAsync，也不扣庫存），用來造出逾期 / 即將到期 / 已歸還等狀態。
    /// </summary>
    /// <param name="dueInDays">應還日 = 今天 + N 天（負數表示已逾期）。</param>
    /// <param name="returnedDaysAgo">null 表示未歸還；否則歸還時間 = 今天 - N 天。</param>
    public static async Task<int> AddRecordAsync(
        this SqlServerFixture fx, string userId, int bookId, int dueInDays, int? returnedDaysAgo = null)
    {
        await using var db = await fx.CreateDbAsync();
        var today = DateTime.Today;
        var due = today.AddDays(dueInDays);
        var rec = new BorrowRecord
        {
            UserId = userId,
            BookId = bookId,
            BorrowedAt = due.AddDays(-BorrowRules.LoanDays),
            DueDate = due,
            ReturnedAt = returnedDaysAgo == null ? null : today.AddDays(-returnedDaysAgo.Value).AddHours(12),
        };
        db.BorrowRecords.Add(rec);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return rec.Id;
    }

    /// <summary>讀取書籍目前的庫存。</summary>
    public static async Task<int> GetQuantityAsync(this SqlServerFixture fx, int bookId)
    {
        await using var db = await fx.CreateDbAsync();
        return (await db.Books.FindAsync([bookId], TestContext.Current.CancellationToken))!.Quantity;
    }

    private static void Check(IdentityResult r)
    {
        if (!r.Succeeded)
            throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
    }
}
