using Book.Data;
using Book.Common;

namespace Book.Services;

/// <summary>
/// 記憶體假資料（不連資料庫）。Singleton，應用程式重啟後資料重置。
/// 業務規則依開發文件實作，之後換成 EF Core 時可沿用同樣的方法簽章。
/// </summary>
public class FakeDataStore
{
    private readonly object _lock = new();
    private readonly List<Category> _categories;
    private readonly List<BookItem> _books = new();
    private readonly List<BorrowRecord> _records = new();
    private readonly List<AppUser> _users;
    private int _nextBookId = 1;
    private int _nextRecordId = 1;

    public FakeDataStore()
    {
        _categories = new()
        {
            new() { Id = 1, Name = "程式設計", Sort = 1 },
            new() { Id = 2, Name = "資料庫", Sort = 2 },
            new() { Id = 3, Name = "軟體工程", Sort = 3 },
            new() { Id = 4, Name = "其他", Sort = 4 },
        };

        _users = new()
        {
            new() { Id = "u-henry", UserName = "henry", DisplayName = "Henry", Role = Roles.Admin, Password = "Admin1234" },
            new() { Id = "u-amy", UserName = "amy", DisplayName = "Amy", Role = Roles.User, Password = "User1234" },
            new() { Id = "u-bob", UserName = "bob", DisplayName = "Bob", Role = Roles.User, Password = "User1234" },
        };

        SeedBooks();
        SeedRecords();
    }

    private void AddBook(string title, string author, string publisher, int cat, int qty)
        => _books.Add(new() { Id = _nextBookId++, Title = title, Author = author, Publisher = publisher, CategoryId = cat, Quantity = qty });

    private void SeedBooks()
    {
        AddBook("Clean Code", "Robert Martin", "Prentice Hall", 1, 3);        // 1
        AddBook("資料庫系統概念", "Silberschatz", "McGraw-Hill", 2, 1);        // 2
        AddBook("設計模式", "GoF", "Addison-Wesley", 1, 0);                   // 3
        AddBook("The Pragmatic Programmer", "Andrew Hunt", "Addison-Wesley", 1, 2);
        AddBook("重構", "Martin Fowler", "Addison-Wesley", 1, 2);
        AddBook("Code Complete", "Steve McConnell", "Microsoft Press", 3, 1);
        AddBook("人月神話", "Frederick Brooks", "Addison-Wesley", 3, 4);
        AddBook("SQL 學習指南", "Alan Beaulieu", "O'Reilly", 2, 3);
        AddBook("高效能 MySQL", "Baron Schwartz", "O'Reilly", 2, 0);
        AddBook("深入淺出 C#", "Andrew Stellman", "O'Reilly", 1, 5);
        AddBook("C# in Depth", "Jon Skeet", "Manning", 1, 2);
        AddBook("ASP.NET Core in Action", "Andrew Lock", "Manning", 1, 3);
        AddBook("Entity Framework Core in Action", "Jon P Smith", "Manning", 2, 2);
        AddBook("領域驅動設計", "Eric Evans", "Addison-Wesley", 3, 1);
        AddBook("演算法導論", "Thomas Cormen", "MIT Press", 1, 2);
        AddBook("Head First 設計模式", "Eric Freeman", "O'Reilly", 1, 3);
        AddBook("敏捷軟體開發", "Robert Martin", "Prentice Hall", 3, 2);
        AddBook("單元測試的藝術", "Roy Osherove", "Manning", 3, 1);
        AddBook("資料密集型應用系統設計", "Martin Kleppmann", "O'Reilly", 2, 2);
        AddBook("SQL Server 效能調校", "Itzik Ben-Gan", "Microsoft Press", 2, 1);
        AddBook("程式設計師的職業素養", "Robert Martin", "Prentice Hall", 3, 3);
        AddBook("黑客與畫家", "Paul Graham", "O'Reilly", 4, 2);
        AddBook("原子習慣", "James Clear", "Avery", 4, 4);
        AddBook("clean Architecture", "Robert Martin", "Prentice Hall", 3, 2);
        AddBook("Blazor in Action", "Chris Sainty", "Manning", 1, 2);
        AddBook("JavaScript 權威指南", "David Flanagan", "O'Reilly", 1, 3);
        AddBook("你不知道的 JS", "Kyle Simpson", "O'Reilly", 1, 1);
        AddBook("Python 程式設計入門", "Eric Matthes", "No Starch", 1, 6);
        AddBook("Linux 指令大全", "William Shotts", "No Starch", 4, 2);
        AddBook("計算機概論", "Behrouz Forouzan", "McGraw-Hill", 4, 3);
        AddBook("作業系統概念", "Silberschatz", "Wiley", 4, 2);
        AddBook("電腦網路", "Andrew Tanenbaum", "Pearson", 4, 1);
        AddBook("編譯器設計", "Alfred Aho", "Pearson", 1, 0);
        AddBook("資料結構與演算法", "Mark Weiss", "Pearson", 1, 3);
        AddBook("軟體架構學習指南", "Mark Richards", "O'Reilly", 3, 2);
        AddBook("微服務架構設計", "Sam Newman", "O'Reilly", 3, 2);
        AddBook("Docker 技術入門", "Sean Kane", "O'Reilly", 4, 4);
        AddBook("Kubernetes 實戰", "Marko Luksa", "Manning", 4, 1);
        AddBook("Git 版本控制", "Scott Chacon", "Apress", 3, 5);
        AddBook("Effective C#", "Bill Wagner", "Addison-Wesley", 1, 2);
        AddBook("NoSQL 精粹", "Martin Fowler", "Addison-Wesley", 2, 2);
        AddBook("Redis 實戰", "Josiah Carlson", "Manning", 2, 1);
        AddBook("資料庫設計入門", "Michael Hernandez", "Addison-Wesley", 2, 3);
        AddBook("UX 設計心理學", "Don Norman", "Basic Books", 4, 2);
        AddBook("精實創業", "Eric Ries", "Crown", 4, 3);
        AddBook("Scrum 敏捷專案管理", "Jeff Sutherland", "Crown", 3, 2);
    }

    private void SeedRecords()
    {
        var today = DateTime.Today;
        // Amy：逾期 3 天、剩 4 天、已歸還
        AddRecord(1, "u-amy", today.AddDays(-17));
        AddRecord(2, "u-amy", today.AddDays(-10));
        AddRecord(3, "u-amy", today.AddDays(-30), returnedAt: today.AddDays(-17));
        AddRecord(4, "u-amy", today.AddDays(-3));      // 剩 11 天（藍）
        // Bob
        AddRecord(2, "u-bob", today.AddDays(-12));     // 剩 2 天（黃）
        AddRecord(5, "u-bob", today.AddDays(-2));
        AddRecord(6, "u-bob", today.AddDays(-40), returnedAt: today.AddDays(-30));
        // Henry（管理員也有歷史紀錄，方便展示刪除限制）
        AddRecord(7, "u-henry", today.AddDays(-50), returnedAt: today.AddDays(-45));
    }

    private void AddRecord(int bookId, string userId, DateTime borrowedAt, DateTime? returnedAt = null)
    {
        _records.Add(new()
        {
            Id = _nextRecordId++,
            BookId = bookId,
            UserId = userId,
            BorrowedAt = borrowedAt,
            DueDate = borrowedAt.Date.AddDays(BorrowRules.LoanDays),
            ReturnedAt = returnedAt,
        });
        // 示範資料：未歸還者視為已扣庫存（種子庫存已是扣完後的數字，不再額外扣）
    }

    // ───────────── 分類 ─────────────
    public List<Category> GetCategories()
    {
        lock (_lock) return _categories.OrderBy(c => c.Sort).ToList();
    }

    public string CategoryName(int id)
    {
        lock (_lock) return _categories.FirstOrDefault(c => c.Id == id)?.Name ?? "";
    }

    // ───────────── 書籍 ─────────────
    public (List<BookItem> Items, int Total) SearchBooks(string? keyword, int? categoryId, int page)
    {
        lock (_lock)
        {
            var q = _books.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                q = q.Where(b => Contains(b.Title, k) || Contains(b.Author, k) || Contains(b.Publisher, k));
            }
            if (categoryId is > 0) q = q.Where(b => b.CategoryId == categoryId);

            var list = q.OrderBy(b => b.Id).ToList();
            var items = list.Skip((page - 1) * BorrowRules.PageSize).Take(BorrowRules.PageSize)
                            .Select(b => b.Clone()).ToList();
            return (items, list.Count);
        }
    }

    private static bool Contains(string s, string k) => s.Contains(k, StringComparison.OrdinalIgnoreCase);

    /// <summary>新增（Id = 0）或編輯書籍。</summary>
    public Result SaveBook(BookItem form)
    {
        lock (_lock)
        {
            if (form.Id == 0)
            {
                form.Id = _nextBookId++;
                _books.Add(form.Clone());
                return Result.Ok("書籍已新增");
            }
            var b = _books.FirstOrDefault(x => x.Id == form.Id);
            if (b == null) return Result.Fail("找不到此書籍");
            b.Title = form.Title; b.Author = form.Author; b.Publisher = form.Publisher;
            b.CategoryId = form.CategoryId; b.Quantity = form.Quantity;
            return Result.Ok("書籍已更新");
        }
    }

    public Result DeleteBook(int id)
    {
        lock (_lock)
        {
            var b = _books.FirstOrDefault(x => x.Id == id);
            if (b == null) return Result.Fail("找不到此書籍");
            if (_records.Any(r => r.BookId == id && r.ReturnedAt == null))
                return Result.Fail("此書仍有未歸還的借閱紀錄，無法刪除");
            _records.RemoveAll(r => r.BookId == id); // 連同已歸還的歷史借閱紀錄一併刪除（CASCADE）
            _books.Remove(b);
            return Result.Ok("書籍已刪除");
        }
    }

    // ───────────── 借閱 / 歸還 ─────────────
    public Result Borrow(string userId, int bookId)
    {
        lock (_lock)
        {
            if (_records.Any(r => r.UserId == userId && r.BookId == bookId && r.ReturnedAt == null))
                return Result.Fail("已借閱此書");
            if (_records.Count(r => r.UserId == userId && r.ReturnedAt == null) >= BorrowRules.MaxBorrowCount)
                return Result.Fail("已達借閱上限");

            var book = _books.FirstOrDefault(b => b.Id == bookId);
            if (book == null || book.Quantity <= 0)   // 對應 UPDATE ... WHERE Quantity > 0 影響行數 = 0
                return Result.Fail("該書本已無庫存");

            book.Quantity--;
            var now = DateTime.Now;
            _records.Add(new()
            {
                Id = _nextRecordId++,
                BookId = bookId,
                UserId = userId,
                BorrowedAt = now,
                DueDate = now.Date.AddDays(BorrowRules.LoanDays),
            });
            return Result.Ok("借閱成功");
        }
    }

    public Result Return(int recordId, string userId)
    {
        lock (_lock)
        {
            var r = _records.FirstOrDefault(x => x.Id == recordId && x.UserId == userId);
            if (r == null || r.ReturnedAt != null) return Result.Fail("此筆紀錄已歸還或不存在");
            r.ReturnedAt = DateTime.Now;
            var book = _books.FirstOrDefault(b => b.Id == r.BookId);
            if (book != null) book.Quantity++;
            return Result.Ok("歸還成功");
        }
    }

    /// <summary>某使用者的借閱紀錄：未歸還在前（應還日近→遠），已歸還在後（歸還日新→舊）。</summary>
    public List<BorrowRow> GetUserRecords(string userId)
    {
        lock (_lock)
        {
            var rows = _records.Where(r => r.UserId == userId).Select(ToRow).ToList();
            return Sort(rows);
        }
    }

    public (List<BorrowRow> Items, int Total) GetAllRecords(BorrowFilter filter, int page)
    {
        lock (_lock)
        {
            var today = DateTime.Today;
            var rows = _records.Select(ToRow)
                .Where(x => Matches(x.Record, filter, today)).ToList();
            rows = Sort(rows);
            return (rows.Skip((page - 1) * BorrowRules.PageSize).Take(BorrowRules.PageSize).ToList(), rows.Count);
        }
    }

    /// <summary>Banner 用：(即將到期本數, 逾期本數)；userId 為 null 代表全部。</summary>
    public (int DueSoon, int Overdue) CountDue(string? userId)
    {
        lock (_lock)
        {
            var today = DateTime.Today;
            int soon = 0, over = 0;
            foreach (var r in _records.Where(r => userId == null || r.UserId == userId))
            {
                var s = BorrowStatus.Of(r, today).State;
                if (s == BorrowState.DueSoon) soon++;
                else if (s == BorrowState.Overdue) over++;
            }
            return (soon, over);
        }
    }

    private static bool Matches(BorrowRecord r, BorrowFilter f, DateTime today)
    {
        var s = BorrowStatus.Of(r, today).State;
        return f switch
        {
            BorrowFilter.Active => r.ReturnedAt == null,
            BorrowFilter.DueSoon => s == BorrowState.DueSoon,
            BorrowFilter.Overdue => s == BorrowState.Overdue,
            BorrowFilter.Returned => r.ReturnedAt != null,
            _ => true,
        };
    }

    private BorrowRow ToRow(BorrowRecord r) => new(
        r,
        _books.FirstOrDefault(b => b.Id == r.BookId)?.Title ?? "(已刪除)",
        _users.FirstOrDefault(u => u.Id == r.UserId)?.DisplayName ?? "(已刪除)");

    private static List<BorrowRow> Sort(List<BorrowRow> rows) => rows
        .OrderBy(x => x.Record.ReturnedAt != null)
        .ThenBy(x => x.Record.ReturnedAt == null ? x.Record.DueDate : DateTime.MaxValue)
        .ThenByDescending(x => x.Record.ReturnedAt)
        .ToList();

    // ───────────── 帳號 ─────────────
    public Result<AppUser> Login(string userName, string password)
    {
        lock (_lock)
        {
            const string fail = "帳號或密碼錯誤";
            var u = _users.FirstOrDefault(x => x.UserName.Equals(userName?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (u == null) return Result<AppUser>.Fail(fail);

            if (u.LockoutEnd > DateTime.Now)
                return Result<AppUser>.Fail("登入失敗次數過多，帳號已鎖定 5 分鐘");

            if (u.Password != password)
            {
                u.FailedCount++;
                if (u.FailedCount >= 5)
                {
                    u.FailedCount = 0;
                    u.LockoutEnd = DateTime.Now.AddMinutes(5);
                    return Result<AppUser>.Fail("登入失敗次數過多，帳號已鎖定 5 分鐘");
                }
                return Result<AppUser>.Fail(fail);
            }
            u.FailedCount = 0;
            u.LockoutEnd = null;
            return Result<AppUser>.Ok(u);
        }
    }

    public Result Register(string userName, string displayName, string password)
    {
        lock (_lock)
        {
            if (_users.Any(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)))
                return Result.Fail("此帳號已被使用");
            _users.Add(new()
            {
                Id = "u-" + Guid.NewGuid().ToString("N")[..8],
                UserName = userName,
                DisplayName = displayName,
                Password = password,
                Role = Roles.User,
            });
            return Result.Ok("註冊完成");
        }
    }

    public List<AppUser> GetUsers()
    {
        lock (_lock) return _users.OrderBy(u => u.Role).ThenBy(u => u.UserName).Select(Copy).ToList();
    }

    private static AppUser Copy(AppUser u) => new()
    {
        Id = u.Id, UserName = u.UserName, DisplayName = u.DisplayName, Role = u.Role,
    };

    public Result UpdateUser(string id, string displayName, string role, string? newPassword, string currentUserId)
    {
        lock (_lock)
        {
            var u = _users.FirstOrDefault(x => x.Id == id);
            if (u == null) return Result.Fail("找不到此使用者");
            if (id == currentUserId && role != Roles.Admin)
                return Result.Fail("不可將自己降為一般使用者");
            u.DisplayName = displayName;
            u.Role = role;
            if (!string.IsNullOrEmpty(newPassword)) u.Password = newPassword;
            return Result.Ok("使用者已更新");
        }
    }

    public Result DeleteUser(string id, string currentUserId)
    {
        lock (_lock)
        {
            var u = _users.FirstOrDefault(x => x.Id == id);
            if (u == null) return Result.Fail("找不到此使用者");
            if (id == currentUserId) return Result.Fail("不可刪除自己");
            if (_records.Any(r => r.UserId == id && r.ReturnedAt == null))
                return Result.Fail("該使用者仍有未歸還的借閱紀錄，無法刪除");
            _records.RemoveAll(r => r.UserId == id);
            _users.Remove(u);
            return Result.Ok("使用者已刪除");
        }
    }
}
