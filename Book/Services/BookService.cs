using Book.Common;
using Book.Data;
using Microsoft.EntityFrameworkCore;

namespace Book.Services;

/// <summary>
/// 書籍、分類與借閱的業務邏輯（EF Core）。Singleton：
/// 每個方法各自建立短生命週期的 DbContext，避免 Blazor Server 長壽命 circuit 內的 DbContext 過期與併發問題。
/// 業務規則違反時以 Result 回傳，不丟例外。
/// </summary>
public class BookService(IDbContextFactory<AppDbContext> dbFactory)
{
    // ───────────── 分類 ─────────────
    public async Task<List<Category>> GetCategoriesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Categories.AsNoTracking().OrderBy(c => c.Sort).ToListAsync();
    }

    // ───────────── 書籍 ─────────────
    public async Task<(List<BookItem> Items, int Total)> SearchBooksAsync(string? keyword, int? categoryId, int page)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Books.AsNoTracking().Include(b => b.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(b => b.Title.Contains(k) || b.Author.Contains(k) || b.Publisher.Contains(k));
        }
        if (categoryId is > 0) q = q.Where(b => b.CategoryId == categoryId);

        var total = await q.CountAsync();
        var items = await q.OrderBy(b => b.Id)
            .Skip((page - 1) * BorrowRules.PageSize).Take(BorrowRules.PageSize)
            .ToListAsync();
        return (items, total);
    }

    /// <summary>新增（Id = 0）或編輯書籍。CreateAt / UpdateAt 由 AppDbContext 自動填入。</summary>
    public async Task<Result> SaveBookAsync(BookItem form)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (form.Id == 0)
        {
            db.Books.Add(new BookItem
            {
                Title = form.Title, Author = form.Author, Publisher = form.Publisher,
                CategoryId = form.CategoryId, Quantity = form.Quantity,
            });
            await db.SaveChangesAsync();
            return Result.Ok("書籍已新增");
        }

        var b = await db.Books.FirstOrDefaultAsync(x => x.Id == form.Id);
        if (b == null) return Result.Fail("找不到此書籍");
        b.Title = form.Title; b.Author = form.Author; b.Publisher = form.Publisher;
        b.CategoryId = form.CategoryId; b.Quantity = form.Quantity;
        await db.SaveChangesAsync();
        return Result.Ok("書籍已更新");
    }

    public async Task<Result> DeleteBookAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        if (!await db.Books.AnyAsync(x => x.Id == id)) return Result.Fail("找不到此書籍");
        if (await db.BorrowRecords.AnyAsync(r => r.BookId == id && r.ReturnedAt == null))
            return Result.Fail("此書仍有未歸還的借閱紀錄，無法刪除");

        // 連同已歸還的歷史借閱紀錄一併刪除
        await db.BorrowRecords.Where(r => r.BookId == id).ExecuteDeleteAsync();
        await db.Books.Where(x => x.Id == id).ExecuteDeleteAsync();
        await tx.CommitAsync();
        return Result.Ok("書籍已刪除");
    }

    // ───────────── 借閱 / 歸還 ─────────────
    public async Task<Result> BorrowAsync(string userId, int bookId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        // ReadCommitted + 依使用者的 applock：只序列化「同一使用者」的借閱（擋重複借閱與 5 本上限的競態），
        // 不同使用者互不阻塞；避免 Serializable 的 key-range lock 在 SELECT 後 INSERT 造成 deadlock
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        var lockResult = new Microsoft.Data.SqlClient.SqlParameter("@result", System.Data.SqlDbType.Int)
        {
            Direction = System.Data.ParameterDirection.Output,
        };
        // Owner = Transaction：交易結束（commit / rollback / dispose）時自動釋放；回傳值 < 0 代表逾時或失敗
        await db.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @res, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000",
            lockResult, new Microsoft.Data.SqlClient.SqlParameter("@res", $"borrow:{userId}"));
        if ((int)lockResult.Value < 0) return Result.Fail("系統忙碌中，請稍後再試");

        if (await db.BorrowRecords.AnyAsync(r => r.UserId == userId && r.BookId == bookId && r.ReturnedAt == null))
            return Result.Fail("已借閱此書");
        if (await db.BorrowRecords.CountAsync(r => r.UserId == userId && r.ReturnedAt == null) >= BorrowRules.MaxBorrowCount)
            return Result.Fail("已達借閱上限");

        // 原子扣庫存：UPDATE ... WHERE Quantity > 0，影響行數 0 代表已無庫存
        var now = DateTime.Now;
        var utc = DateTime.UtcNow;
        var affected = await db.Books
            .Where(b => b.Id == bookId && b.Quantity > 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Quantity, b => b.Quantity - 1)
                .SetProperty(b => b.UpdateAt, utc));
        if (affected == 0) return Result.Fail("該書本已無庫存");

        db.BorrowRecords.Add(new BorrowRecord
        {
            BookId = bookId,
            UserId = userId,
            BorrowedAt = now,
            DueDate = now.Date.AddDays(BorrowRules.LoanDays),
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Result.Ok("借閱成功");
    }

    public async Task<Result> ReturnAsync(int recordId, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var rec = await db.BorrowRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == recordId && x.UserId == userId && x.ReturnedAt == null);
        if (rec == null) return Result.Fail("此筆紀錄已歸還或不存在");

        var now = DateTime.Now;
        var utc = DateTime.UtcNow;
        // 條件式更新，避免重複歸還造成庫存多加
        var affected = await db.BorrowRecords
            .Where(x => x.Id == recordId && x.ReturnedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReturnedAt, now));
        if (affected == 0) return Result.Fail("此筆紀錄已歸還或不存在");

        await db.Books.Where(b => b.Id == rec.BookId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Quantity, b => b.Quantity + 1)
                .SetProperty(b => b.UpdateAt, utc));
        await tx.CommitAsync();
        return Result.Ok("歸還成功");
    }

    /// <summary>某使用者的借閱紀錄：未歸還在前（應還日近→遠），已歸還在後（歸還日新→舊）。</summary>
    public async Task<List<BorrowRow>> GetUserRecordsAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.BorrowRecords.AsNoTracking().Where(r => r.UserId == userId);
        return await ToRowsAsync(db, q);
    }

    public async Task<(List<BorrowRow> Items, int Total)> GetAllRecordsAsync(BorrowFilter filter, int page)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var soonEnd = today.AddDays(BorrowRules.DueSoonDays);

        var q = db.BorrowRecords.AsNoTracking();
        q = filter switch
        {
            BorrowFilter.Active => q.Where(r => r.ReturnedAt == null),
            BorrowFilter.DueSoon => q.Where(r => r.ReturnedAt == null && r.DueDate >= today && r.DueDate <= soonEnd),
            BorrowFilter.Overdue => q.Where(r => r.ReturnedAt == null && r.DueDate < today),
            BorrowFilter.Returned => q.Where(r => r.ReturnedAt != null),
            _ => q,
        };

        var total = await q.CountAsync();
        var rows = await ToRowsAsync(db, q, (page - 1) * BorrowRules.PageSize, BorrowRules.PageSize);
        return (rows, total);
    }

    /// <summary>Banner 用：(即將到期本數, 逾期本數)；userId 為 null 代表全部。</summary>
    public async Task<(int DueSoon, int Overdue)> CountDueAsync(string? userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var active = await db.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnedAt == null && (userId == null || r.UserId == userId))
            .ToListAsync();

        int soon = 0, over = 0;
        foreach (var r in active)
        {
            var s = BorrowStatus.Of(r, today).State;
            if (s == BorrowState.DueSoon) soon++;
            else if (s == BorrowState.Overdue) over++;
        }
        return (soon, over);
    }

    /// <summary>排序、分頁並帶出書名與借閱人顯示名稱。</summary>
    private static async Task<List<BorrowRow>> ToRowsAsync(AppDbContext db, IQueryable<BorrowRecord> q, int skip = 0, int? take = null)
    {
        var ordered = q
            .OrderBy(r => r.ReturnedAt != null)
            .ThenBy(r => r.ReturnedAt == null ? r.DueDate : DateTime.MaxValue)
            .ThenByDescending(r => r.ReturnedAt)
            .ThenBy(r => r.Id)
            .Skip(skip);
        if (take != null) ordered = ordered.Take(take.Value);

        var list = await (from r in ordered
                          join b in db.Books on r.BookId equals b.Id
                          join u in db.Users on r.UserId equals u.Id
                          select new { r, b.Title, u.DisplayName }).ToListAsync();

        // Join 後順序不保證，於記憶體再依同一規則排序一次
        return list
            .OrderBy(x => x.r.ReturnedAt != null)
            .ThenBy(x => x.r.ReturnedAt == null ? x.r.DueDate : DateTime.MaxValue)
            .ThenByDescending(x => x.r.ReturnedAt)
            .ThenBy(x => x.r.Id)
            .Select(x => new BorrowRow(x.r, x.Title, x.DisplayName))
            .ToList();
    }
}
