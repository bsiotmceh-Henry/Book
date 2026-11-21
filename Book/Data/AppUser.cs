using Book.Common;

namespace Book.Data;

/// <summary>畫面與登入狀態使用的使用者資料（由 Identity 的 ApplicationUser 轉出，不含密碼）。</summary>
public class AppUser
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = Roles.User;
}
