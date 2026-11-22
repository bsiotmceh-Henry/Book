using System.Security.Claims;
using Book.Common;
using Book.Data;

namespace Book.Tests.Common;

/// <summary>AuthExtensions：AppUser 與 Cookie 身分（ClaimsPrincipal）的轉換。</summary>
public class AuthExtensionsTests
{
    private static AppUser User(string role) => new()
    {
        Id = "id-123",
        UserName = "henry",
        DisplayName = "Henry",
        Role = role,
    };

    [Fact]
    public void ToPrincipal_來回轉換保留所有欄位()
    {
        var p = User(Roles.User).ToPrincipal();

        // 必須是已驗證的身分，否則 AuthorizeView / [Authorize] 會當成未登入
        Assert.True(p.Identity?.IsAuthenticated);
        Assert.Equal("id-123", p.UserId());
        Assert.Equal("henry", p.Identity?.Name);
        Assert.Equal("Henry", p.DisplayName());
        Assert.True(p.IsInRole(Roles.User));
        Assert.False(p.IsAdmin());
    }

    [Fact]
    public void 管理員_IsAdmin為真且顯示名稱加註管理員()
    {
        var p = User(Roles.Admin).ToPrincipal();

        Assert.True(p.IsAdmin());
        Assert.Equal("Henry（管理員）", p.DisplayLabel());
        Assert.Equal("/book-manage", p.HomePath());
    }

    [Fact]
    public void 一般使用者_顯示原名稱並導向書籍查詢()
    {
        var p = User(Roles.User).ToPrincipal();

        Assert.Equal("Henry", p.DisplayLabel());
        Assert.Equal("/book-search", p.HomePath());
    }

    [Fact]
    public void 未登入的空身分_各欄位回傳空字串而非例外()
    {
        var p = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Equal("", p.UserId());
        Assert.Equal("", p.DisplayName());
        Assert.False(p.IsAdmin());
        Assert.Equal("/book-search", p.HomePath());
    }
}
