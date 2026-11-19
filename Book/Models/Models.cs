namespace Book.Models;

/// <summary>借閱規則常數（依文件：不使用設定檔）。</summary>
public static class BorrowRules
{
    public const int LoanDays = 14;      // 借閱期限
    public const int MaxBorrowCount = 5; // 借閱上限
    public const int DueSoonDays = 7;    // 即將到期天數
    public const int PageSize = 20;      // 每頁筆數
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static string Label(string role) => role == Admin ? "管理員" : "一般使用者";
}

/// <summary>業務規則結果（以 Result 回傳，不以例外控制流程）。</summary>
public record Result(bool Success, string Message)
{
    public static Result Ok(string message = "") => new(true, message);
    public static Result Fail(string message) => new(false, message);
}

public record Result<T>(bool Success, string Message, T? Value)
{
    public static Result<T> Ok(T value) => new(true, "", value);
    public static Result<T> Fail(string message) => new(false, message, default);
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Sort { get; set; }
}

public class BookItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Publisher { get; set; } = "";
    public int CategoryId { get; set; }
    public int Quantity { get; set; }

    public BookItem Clone() => (BookItem)MemberwiseClone();
}

public class BorrowRecord
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string UserId { get; set; } = "";
    public DateTime BorrowedAt { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
}

public class AppUser
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = Roles.User;
    public string Password { get; set; } = "";
    public int FailedCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

/// <summary>借閱紀錄 + 顯示用的書名與借閱人。</summary>
public record BorrowRow(BorrowRecord Record, string BookTitle, string BorrowerName);

public enum BorrowState { Normal, DueSoon, Overdue, Returned }

public record BorrowStatus(BorrowState State, int Days, string Label, string CssClass)
{
    /// <summary>每次進入頁面時，依應還日即時計算，不存欄位。</summary>
    public static BorrowStatus Of(BorrowRecord r, DateTime today)
    {
        if (r.ReturnedAt != null)
            return new(BorrowState.Returned, 0, "已歸還", "text-bg-success");

        var remain = (r.DueDate.Date - today.Date).Days;
        if (remain < 0)
            return new(BorrowState.Overdue, -remain, $"逾期 {-remain} 天", "text-bg-danger");
        if (remain <= BorrowRules.DueSoonDays)
            return new(BorrowState.DueSoon, remain, $"剩 {remain} 天", "text-bg-warning");
        return new(BorrowState.Normal, remain, $"剩 {remain} 天", "text-bg-primary");
    }
}

public enum BorrowFilter { All, Active, DueSoon, Overdue, Returned }
