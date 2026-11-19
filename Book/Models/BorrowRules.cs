namespace Book.Models;

/// <summary>借閱規則常數（依文件：不使用設定檔）。</summary>
public static class BorrowRules
{
    public const int LoanDays = 14;      // 借閱期限
    public const int MaxBorrowCount = 5; // 借閱上限
    public const int DueSoonDays = 7;    // 即將到期天數
    public const int PageSize = 20;      // 每頁筆數
}
