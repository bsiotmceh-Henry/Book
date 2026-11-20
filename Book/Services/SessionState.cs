using Book.Data;
using Book.Common;

namespace Book.Services;

/// <summary>
/// 假的登入狀態（每個 Blazor 連線一份）。實際版本改用 ASP.NET Core Identity + Cookie。
/// </summary>
public class SessionState
{
    public AppUser? User { get; private set; }
    public bool IsLoggedIn => User != null;
    public bool IsAdmin => User?.Role == Roles.Admin;

    /// <summary>導覽列顯示名稱：管理員顯示「Henry（管理員）」。</summary>
    public string DisplayLabel => User == null ? "" : IsAdmin ? $"{User.DisplayName}（管理員）" : User.DisplayName;

    /// <summary>登入後的預設首頁：一般使用者→書籍查詢；管理員→書籍管理。</summary>
    public string HomePath => IsAdmin ? "/book-manage" : "/book-search";

    public event Action? Changed;

    public void SignIn(AppUser user) { User = user; Changed?.Invoke(); }
    public void SignOut() { User = null; Changed?.Invoke(); }
}
