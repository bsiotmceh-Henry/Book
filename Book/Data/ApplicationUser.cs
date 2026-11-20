using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Book.Data;

/// <summary>Identity 使用者。登入帳號用 UserName，顯示名稱另存 DisplayName。</summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>畫面上顯示的名稱。</summary>
    [MaxLength(50)]
    public string DisplayName { get; set; } = "";

    /// <summary>帳號狀態；刪除使用者時改為 Deleted（軟刪除，實作待後續）。</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;
}
