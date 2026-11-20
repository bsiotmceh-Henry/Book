using Book.Common;
using Book.Data;

namespace Book.Services;

/// <summary>借閱紀錄 + 顯示用的書名與借閱人。</summary>
public record BorrowRow(BorrowRecord Record, string BookTitle, string BorrowerName);

public enum BorrowState { Normal, DueSoon, Overdue, Returned }

public enum BorrowFilter { All, Active, DueSoon, Overdue, Returned }

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
