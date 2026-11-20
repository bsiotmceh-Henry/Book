using Book.Common;
using Microsoft.AspNetCore.Identity;

namespace Book.Data;

/// <summary>
/// 啟動時建立預設管理員（可重複執行）。角色由 migration 的 HasData 建立，這裡仍補檢查以防資料庫是手動建立的。
/// 帳號密碼來自設定 DefaultAdmin:UserName / Password / DisplayName。
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var config = sp.GetRequiredService<IConfiguration>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // 角色
        foreach (var role in new[] { Roles.Admin, Roles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
                Check(await roleManager.CreateAsync(new IdentityRole(role)), $"建立角色 {role}");
        }

        // 管理員帳號
        var userName = config["DefaultAdmin:UserName"] ?? "henry";
        var password = config["DefaultAdmin:Password"] ?? throw new InvalidOperationException("缺少 DefaultAdmin:Password 設定");

        var admin = await userManager.FindByNameAsync(userName);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = userName,
                DisplayName = config["DefaultAdmin:DisplayName"] ?? userName,
                // 預設帳號不需信箱驗證
                EmailConfirmed = true,
            };
            Check(await userManager.CreateAsync(admin, password), "建立管理員");
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
            Check(await userManager.AddToRoleAsync(admin, Roles.Admin), "指派管理員角色");
    }

    private static void Check(IdentityResult r, string action)
    {
        if (!r.Succeeded)
            throw new InvalidOperationException($"{action}失敗：{string.Join("; ", r.Errors.Select(e => e.Description))}");
    }
}
