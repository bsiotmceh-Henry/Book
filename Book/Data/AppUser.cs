using Book.Common;

namespace Book.Data;

/// <summary>使用者（目前為假資料；導入 Identity 後改由 Identity 管理）。</summary>
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
