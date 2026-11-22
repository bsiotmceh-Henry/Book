using Book.Data;
using Book.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Book.Common;

/// <summary>
/// 資料存取、Identity 與業務服務的 DI 註冊。
/// Program.cs 與測試專案共用同一份設定（密碼規則、鎖定規則），避免兩邊各寫一份而逐漸不一致。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="configureDb">DbContext 的資料庫設定（正式環境與測試各自指定連線字串）。</param>
    public static IServiceCollection AddBookServices(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContextFactory<AppDbContext>(configureDb);

        // Identity：使用者 + 角色，存於 AppDbContext
        services.AddIdentityCore<ApplicationUser>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireNonAlphanumeric = false;
                // 密碼規則：至少 8 碼、含英文字母與數字（大小寫不限）
                opt.Password.RequireUppercase = false;
                opt.Password.RequireLowercase = false;
                opt.User.RequireUniqueEmail = false;
                // 連續失敗 5 次鎖定 5 分鐘
                opt.Lockout.MaxFailedAccessAttempts = 5;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        // 業務邏輯（資料庫）
        services.AddSingleton<BookService>();
        services.AddSingleton<UserService>();

        return services;
    }
}
