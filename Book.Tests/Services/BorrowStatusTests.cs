using Book.Data;
using Book.Services;

namespace Book.Tests.Services;

/// <summary>BorrowStatus.Of：依應還日即時計算狀態（純邏輯，不碰資料庫）。</summary>
public class BorrowStatusTests
{
    /// <summary>固定「今天」，讓邊界值測試不受執行日期影響。</summary>
    private static readonly DateTime Today = new(2026, 3, 10);

    private static BorrowRecord Record(DateTime due, DateTime? returnedAt = null)
        => new() { DueDate = due, BorrowedAt = due.AddDays(-14), ReturnedAt = returnedAt };

    [Fact]
    public void 已歸還_不論應還日一律顯示已歸還()
    {
        // 應還日早已過，但已歸還就不算逾期
        var s = BorrowStatus.Of(Record(Today.AddDays(-30), returnedAt: Today.AddDays(-1)), Today);

        Assert.Equal(BorrowState.Returned, s.State);
        Assert.Equal(0, s.Days);
        Assert.Equal("已歸還", s.Label);
        Assert.Equal("text-bg-success", s.CssClass);
    }

    [Theory]
    [InlineData(-1, BorrowState.Overdue, 1, "逾期 1 天", "text-bg-danger")]
    [InlineData(-15, BorrowState.Overdue, 15, "逾期 15 天", "text-bg-danger")]
    [InlineData(0, BorrowState.DueSoon, 0, "剩 0 天", "text-bg-warning")]    // 當天到期仍算即將到期
    [InlineData(7, BorrowState.DueSoon, 7, "剩 7 天", "text-bg-warning")]    // DueSoonDays 邊界（含）
    [InlineData(8, BorrowState.Normal, 8, "剩 8 天", "text-bg-primary")]     // 超過邊界一天
    [InlineData(14, BorrowState.Normal, 14, "剩 14 天", "text-bg-primary")]  // 剛借出
    public void 未歸還_依剩餘天數決定狀態(int dueInDays, BorrowState state, int days, string label, string css)
    {
        var s = BorrowStatus.Of(Record(Today.AddDays(dueInDays)), Today);

        Assert.Equal(state, s.State);
        Assert.Equal(days, s.Days);
        Assert.Equal(label, s.Label);
        Assert.Equal(css, s.CssClass);
    }

    [Fact]
    public void 只比較日期_忽略時分秒()
    {
        // 應還日 3/11 00:00、今天 3/10 23:59 → 仍是剩 1 天，而不是因為不足 24 小時變成 0 天
        var s = BorrowStatus.Of(Record(Today.AddDays(1)), Today.AddHours(23).AddMinutes(59));
        Assert.Equal(1, s.Days);

        // 應還日帶時間（3/9 23:00）、今天 3/10 00:01 → 逾期 1 天
        var o = BorrowStatus.Of(Record(Today.AddHours(-1)), Today.AddMinutes(1));
        Assert.Equal(BorrowState.Overdue, o.State);
        Assert.Equal(1, o.Days);
    }
}
