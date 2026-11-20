namespace Book.Data;

/// <summary>使用者狀態，用於軟刪除（資料不實際刪除，只改狀態）。</summary>
public enum UserStatus
{
    /// <summary>正常。</summary>
    Active = 0,

    /// <summary>已刪除（軟刪除）。</summary>
    Deleted = 1,
}
