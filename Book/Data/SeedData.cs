using Microsoft.AspNetCore.Identity;

namespace Book.Data;

/// <summary>
/// 預設資料（EF Core HasData，隨 migration 寫入）。
/// 只放分類與書籍；使用者待導入 Identity 後再種，借閱紀錄依賴使用者且日期為相對值，暫不種。
/// HasData 不經過 SaveChanges，所以時間必須寫死，不能用 DateTime.Now（否則每次產生 migration 都會有差異）。
/// </summary>
public static class SeedData
{
    /// <summary>種子資料的固定建立時間（UTC）。</summary>
    private static readonly DateTime SeedTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>預設角色。NormalizedName 必須自行填入大寫，HasData 不會經過 RoleManager。</summary>
    public static IdentityRole[] Roles() =>
    [
        new() { Id = "role-admin", Name = Common.Roles.Admin, NormalizedName = Common.Roles.Admin.ToUpperInvariant(), ConcurrencyStamp = "role-admin-stamp" },
        new() { Id = "role-user", Name = Common.Roles.User, NormalizedName = Common.Roles.User.ToUpperInvariant(), ConcurrencyStamp = "role-user-stamp" },
    ];

    public static Category[] Categories() =>
    [
        new() { Id = 1, Name = "程式設計", Sort = 1 },
        new() { Id = 2, Name = "資料庫", Sort = 2 },
        new() { Id = 3, Name = "軟體工程", Sort = 3 },
        new() { Id = 4, Name = "其他", Sort = 4 },
    ];

    public static BookItem[] Books()
    {
        // (書名, 作者, 出版社, 分類Id, 庫存)；Id 依順序從 1 開始，與原假資料一致
        (string Title, string Author, string Publisher, int Cat, int Qty)[] rows =
        [
            ("Clean Code", "Robert Martin", "Prentice Hall", 1, 3),
            ("資料庫系統概念", "Silberschatz", "McGraw-Hill", 2, 1),
            ("設計模式", "GoF", "Addison-Wesley", 1, 0),
            ("The Pragmatic Programmer", "Andrew Hunt", "Addison-Wesley", 1, 2),
            ("重構", "Martin Fowler", "Addison-Wesley", 1, 2),
            ("Code Complete", "Steve McConnell", "Microsoft Press", 3, 1),
            ("人月神話", "Frederick Brooks", "Addison-Wesley", 3, 4),
            ("SQL 學習指南", "Alan Beaulieu", "O'Reilly", 2, 3),
            ("高效能 MySQL", "Baron Schwartz", "O'Reilly", 2, 0),
            ("深入淺出 C#", "Andrew Stellman", "O'Reilly", 1, 5),
            ("C# in Depth", "Jon Skeet", "Manning", 1, 2),
            ("ASP.NET Core in Action", "Andrew Lock", "Manning", 1, 3),
            ("Entity Framework Core in Action", "Jon P Smith", "Manning", 2, 2),
            ("領域驅動設計", "Eric Evans", "Addison-Wesley", 3, 1),
            ("演算法導論", "Thomas Cormen", "MIT Press", 1, 2),
            ("Head First 設計模式", "Eric Freeman", "O'Reilly", 1, 3),
            ("敏捷軟體開發", "Robert Martin", "Prentice Hall", 3, 2),
            ("單元測試的藝術", "Roy Osherove", "Manning", 3, 1),
            ("資料密集型應用系統設計", "Martin Kleppmann", "O'Reilly", 2, 2),
            ("SQL Server 效能調校", "Itzik Ben-Gan", "Microsoft Press", 2, 1),
            ("程式設計師的職業素養", "Robert Martin", "Prentice Hall", 3, 3),
            ("黑客與畫家", "Paul Graham", "O'Reilly", 4, 2),
            ("原子習慣", "James Clear", "Avery", 4, 4),
            ("clean Architecture", "Robert Martin", "Prentice Hall", 3, 2),
            ("Blazor in Action", "Chris Sainty", "Manning", 1, 2),
            ("JavaScript 權威指南", "David Flanagan", "O'Reilly", 1, 3),
            ("你不知道的 JS", "Kyle Simpson", "O'Reilly", 1, 1),
            ("Python 程式設計入門", "Eric Matthes", "No Starch", 1, 6),
            ("Linux 指令大全", "William Shotts", "No Starch", 4, 2),
            ("計算機概論", "Behrouz Forouzan", "McGraw-Hill", 4, 3),
            ("作業系統概念", "Silberschatz", "Wiley", 4, 2),
            ("電腦網路", "Andrew Tanenbaum", "Pearson", 4, 1),
            ("編譯器設計", "Alfred Aho", "Pearson", 1, 0),
            ("資料結構與演算法", "Mark Weiss", "Pearson", 1, 3),
            ("軟體架構學習指南", "Mark Richards", "O'Reilly", 3, 2),
            ("微服務架構設計", "Sam Newman", "O'Reilly", 3, 2),
            ("Docker 技術入門", "Sean Kane", "O'Reilly", 4, 4),
            ("Kubernetes 實戰", "Marko Luksa", "Manning", 4, 1),
            ("Git 版本控制", "Scott Chacon", "Apress", 3, 5),
            ("Effective C#", "Bill Wagner", "Addison-Wesley", 1, 2),
            ("NoSQL 精粹", "Martin Fowler", "Addison-Wesley", 2, 2),
            ("Redis 實戰", "Josiah Carlson", "Manning", 2, 1),
            ("資料庫設計入門", "Michael Hernandez", "Addison-Wesley", 2, 3),
            ("UX 設計心理學", "Don Norman", "Basic Books", 4, 2),
            ("精實創業", "Eric Ries", "Crown", 4, 3),
            ("Scrum 敏捷專案管理", "Jeff Sutherland", "Crown", 3, 2),
        ];

        return rows.Select((r, i) => new BookItem
        {
            Id = i + 1,
            Title = r.Title,
            Author = r.Author,
            Publisher = r.Publisher,
            CategoryId = r.Cat,
            Quantity = r.Qty,
            CreateAt = SeedTime,
            UpdateAt = SeedTime,
        }).ToArray();
    }
}
