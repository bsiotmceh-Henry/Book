using Book.Common;
using Book.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Book.Services;

/// <summary>
/// 使用者帳號的業務邏輯（ASP.NET Core Identity）。Singleton：
/// 每個方法各自建立 DI scope 取得 UserManager / DbContext，避免長壽命 circuit 內的 DbContext 問題。
/// 業務規則違反時以 Result 回傳，不丟例外。
/// </summary>
public class UserService(IServiceScopeFactory scopeFactory)
{
    // ───────────── 帳號（ASP.NET Core Identity） ─────────────
    public async Task<Result<AppUser>> LoginAsync(string userName, string password)
    {
        const string fail = "帳號或密碼錯誤";
        const string locked = "登入失敗次數過多，帳號已鎖定 5 分鐘";

        using var scope = scopeFactory.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var u = await um.FindByNameAsync(userName?.Trim() ?? "");
        // 不存在或已軟刪除，一律回傳同一則訊息
        if (u == null || u.Status != UserStatus.Active) return Result<AppUser>.Fail(fail);

        if (await um.IsLockedOutAsync(u)) return Result<AppUser>.Fail(locked);

        if (!await um.CheckPasswordAsync(u, password))
        {
            await um.AccessFailedAsync(u);
            return Result<AppUser>.Fail(await um.IsLockedOutAsync(u) ? locked : fail);
        }

        await um.ResetAccessFailedCountAsync(u);
        return Result<AppUser>.Ok(await ToAppUserAsync(um, u));
    }

    /// <summary>
    /// 驗證 Cookie 內的身分是否仍有效：帳號存在、未刪除、未鎖定，並回傳最新的資料（角色異動立即生效）。
    /// 無效回傳 null。
    /// </summary>
    public async Task<AppUser?> GetActiveUserAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var u = await um.FindByIdAsync(userId);
        if (u == null || u.Status != UserStatus.Active || await um.IsLockedOutAsync(u)) return null;
        return await ToAppUserAsync(um, u);
    }

    public async Task<Result> RegisterAsync(string userName, string displayName, string password)
    {
        using var scope = scopeFactory.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = userName.Trim(), DisplayName = displayName.Trim() };
        var created = await um.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            if (created.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
                return Result.Fail("此帳號已被使用");
            return Result.Fail(string.Join("；", created.Errors.Select(e => e.Description)));
        }

        var role = await um.AddToRoleAsync(user, Roles.User);
        if (!role.Succeeded)
        {
            await um.DeleteAsync(user);   // 角色指派失敗時回復，避免產生沒有角色的帳號
            return Result.Fail("註冊失敗，請稍後再試");
        }
        return Result.Ok("註冊完成");
    }

    public async Task<List<AppUser>> GetUsersAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var list = await (from u in db.Users.AsNoTracking()
                          where u.Status == UserStatus.Active
                          join ur in db.UserRoles on u.Id equals ur.UserId
                          join r in db.Roles on ur.RoleId equals r.Id
                          select new AppUser
                          {
                              Id = u.Id, UserName = u.UserName ?? "", DisplayName = u.DisplayName, Role = r.Name ?? Roles.User,
                          }).ToListAsync();
        return list.OrderBy(u => u.Role).ThenBy(u => u.UserName).ToList();
    }

    public async Task<Result> UpdateUserAsync(string id, string displayName, string role, string? newPassword, string currentUserId)
    {
        if (id == currentUserId && role != Roles.Admin)
            return Result.Fail("不可將自己降為一般使用者");
        if (role is not (Roles.Admin or Roles.User)) return Result.Fail("角色不正確");

        using var scope = scopeFactory.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var u = await um.FindByIdAsync(id);
        if (u == null || u.Status != UserStatus.Active) return Result.Fail("找不到此使用者");

        // 先驗證新密碼，任何資料都還沒改；否則後面移除舊密碼後才發現新密碼不合規則，帳號會變成沒有密碼
        if (!string.IsNullOrEmpty(newPassword))
        {
            foreach (var validator in um.PasswordValidators)
            {
                var vr = await validator.ValidateAsync(um, u, newPassword);
                if (!vr.Succeeded) return Fail(vr);
            }
        }

        u.DisplayName = displayName.Trim();
        var upd = await um.UpdateAsync(u);
        if (!upd.Succeeded) return Fail(upd);

        // 角色：單一角色，不同才切換
        if (!await um.IsInRoleAsync(u, role))
        {
            var current = await um.GetRolesAsync(u);
            if (current.Count > 0)
            {
                var rm = await um.RemoveFromRolesAsync(u, current);
                if (!rm.Succeeded) return Fail(rm);
            }
            var add = await um.AddToRoleAsync(u, role);
            if (!add.Succeeded) return Fail(add);
        }

        // 重設密碼：空白表示不變更
        if (!string.IsNullOrEmpty(newPassword))
        {
            if (await um.HasPasswordAsync(u))
            {
                var rp = await um.RemovePasswordAsync(u);
                if (!rp.Succeeded) return Fail(rp);
            }
            var ap = await um.AddPasswordAsync(u, newPassword);
            if (!ap.Succeeded) return Fail(ap);
        }
        return Result.Ok("使用者已更新");
    }

    /// <summary>軟刪除：Status 改為 Deleted 並永久鎖定，保留借閱歷史。</summary>
    public async Task<Result> DeleteUserAsync(string id, string currentUserId)
    {
        if (id == currentUserId) return Result.Fail("不可刪除自己");

        using var scope = scopeFactory.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var u = await um.FindByIdAsync(id);
        if (u == null || u.Status != UserStatus.Active) return Result.Fail("找不到此使用者");

        if (await db.BorrowRecords.AnyAsync(r => r.UserId == id && r.ReturnedAt == null))
            return Result.Fail("該使用者仍有未歸還的借閱紀錄，無法刪除");

        u.Status = UserStatus.Deleted;
        u.LockoutEnabled = true;
        u.LockoutEnd = DateTimeOffset.MaxValue;
        var r = await um.UpdateAsync(u);
        return r.Succeeded ? Result.Ok("使用者已刪除") : Fail(r);
    }

    private static Result Fail(IdentityResult r)
        => Result.Fail(string.Join("；", r.Errors.Select(e => e.Description)));

    private static async Task<AppUser> ToAppUserAsync(UserManager<ApplicationUser> um, ApplicationUser u)
    {
        var roles = await um.GetRolesAsync(u);
        return new AppUser
        {
            Id = u.Id,
            UserName = u.UserName ?? "",
            DisplayName = u.DisplayName,
            Role = roles.Contains(Roles.Admin) ? Roles.Admin : Roles.User,
        };
    }
}
