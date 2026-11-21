using System.Security.Claims;
using Book.Data;

namespace Book.Common;

/// <summary>Cookie 身分（ClaimsPrincipal）與 AppUser 之間的轉換與常用屬性。</summary>
public static class AuthExtensions
{
    /// <summary>顯示名稱使用的自訂 Claim。</summary>
    public const string DisplayNameClaim = "DisplayName";

    /// <summary>由登入成功的使用者建立 Cookie 身分。</summary>
    public static ClaimsPrincipal ToPrincipal(this AppUser u)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, u.Id),
            new Claim(ClaimTypes.Name, u.UserName),
            new Claim(DisplayNameClaim, u.DisplayName),
            new Claim(ClaimTypes.Role, u.Role),
        ], "Cookies");
        return new ClaimsPrincipal(identity);
    }

    public static string UserId(this ClaimsPrincipal p) => p.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public static bool IsAdmin(this ClaimsPrincipal p) => p.IsInRole(Roles.Admin);

    public static string DisplayName(this ClaimsPrincipal p) => p.FindFirstValue(DisplayNameClaim) ?? "";

    /// <summary>導覽列顯示名稱：管理員顯示「Henry（管理員）」。</summary>
    public static string DisplayLabel(this ClaimsPrincipal p) => p.IsAdmin() ? $"{p.DisplayName()}（管理員）" : p.DisplayName();

    /// <summary>登入後的預設首頁：一般使用者→書籍查詢；管理員→書籍管理。</summary>
    public static string HomePath(this ClaimsPrincipal p) => p.IsAdmin() ? "/book-manage" : "/book-search";
}
