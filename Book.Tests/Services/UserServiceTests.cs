using Book.Common;
using Book.Data;
using Book.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Book.Tests.Services;

/// <summary>
/// UserService：登入（含 Identity 鎖定）、註冊、Cookie 身分驗證、使用者管理與軟刪除。
/// 整個類別共用一個資料庫，每個測試自己建立帳號（帳號名稱唯一）。
/// </summary>
public class UserServiceTests(SqlServerFixture fx) : IClassFixture<SqlServerFixture>
{
    private const string WrongCredentials = "帳號或密碼錯誤";
    private const string LockedOut = "登入失敗次數過多，帳號已鎖定 5 分鐘";
    private const string NotFound = "找不到此使用者";

    /// <summary>直接從資料庫讀出使用者目前的狀態與角色。</summary>
    private async Task<(ApplicationUser User, IList<string> Roles)> LoadAsync(string id)
    {
        using var scope = fx.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var u = (await um.FindByIdAsync(id))!;
        return (u, await um.GetRolesAsync(u));
    }

    /// <summary>用錯誤密碼連續登入 n 次。</summary>
    private async Task FailLoginAsync(string userName, int times)
    {
        for (var i = 0; i < times; i++)
            await fx.Users.LoginAsync(userName, "WrongPass1");
    }

    // ───────────── 註冊 ─────────────

    [Fact]
    public async Task Register_成功_角色為一般使用者且可登入()
    {
        var name = TestData.Unique("r");

        var r = await fx.Users.RegisterAsync($"  {name}  ", "  新使用者  ", TestData.Password);

        Assert.True(r.Success);
        Assert.Equal("註冊完成", r.Message);
        var login = await fx.Users.LoginAsync(name, TestData.Password);
        Assert.True(login.Success);
        Assert.Equal(name, login.Value!.UserName);          // 帳號前後空白已去除
        Assert.Equal("新使用者", login.Value.DisplayName);   // 顯示名稱前後空白已去除
        Assert.Equal(Roles.User, login.Value.Role);
    }

    [Fact]
    public async Task Register_帳號重複_不分大小寫一律擋下()
    {
        var name = TestData.Unique("r");
        await fx.Users.RegisterAsync(name, "A", TestData.Password);

        var same = await fx.Users.RegisterAsync(name, "B", TestData.Password);
        var upper = await fx.Users.RegisterAsync(name.ToUpperInvariant(), "C", TestData.Password);

        Assert.False(same.Success);
        Assert.Equal("此帳號已被使用", same.Message);
        Assert.False(upper.Success);
        Assert.Equal("此帳號已被使用", upper.Message);
    }

    [Theory]
    [InlineData("Pass0rd")]     // 7 碼
    [InlineData("Password")]    // 沒有數字
    public async Task Register_密碼不符規則_失敗且不建立帳號(string password)
    {
        var name = TestData.Unique("r");

        var r = await fx.Users.RegisterAsync(name, "A", password);

        Assert.False(r.Success);
        Assert.False((await fx.Users.LoginAsync(name, password)).Success);
    }

    // ───────────── 登入 ─────────────

    [Fact]
    public async Task Login_成功_回傳使用者資料與角色()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin, displayName: "管理者");

        var r = await fx.Users.LoginAsync($" {admin.UserName} ", TestData.Password);

        Assert.True(r.Success);
        Assert.Equal(admin.Id, r.Value!.Id);
        Assert.Equal("管理者", r.Value.DisplayName);
        Assert.Equal(Roles.Admin, r.Value.Role);
    }

    [Fact]
    public async Task Login_密碼錯誤或帳號不存在_回傳相同訊息()
    {
        var user = await fx.CreateUserAsync();

        var wrongPassword = await fx.Users.LoginAsync(user.UserName!, "WrongPass1");
        var noSuchUser = await fx.Users.LoginAsync(TestData.Unique("none"), TestData.Password);

        // 不透露帳號是否存在
        Assert.False(wrongPassword.Success);
        Assert.Equal(WrongCredentials, wrongPassword.Message);
        Assert.False(noSuchUser.Success);
        Assert.Equal(WrongCredentials, noSuchUser.Message);
        Assert.Null(noSuchUser.Value);
    }

    [Fact]
    public async Task Login_連續失敗5次_第5次起鎖定_正確密碼也無法登入()
    {
        var user = await fx.CreateUserAsync();

        for (var i = 1; i <= 4; i++)
            Assert.Equal(WrongCredentials, (await fx.Users.LoginAsync(user.UserName!, "WrongPass1")).Message);
        var fifth = await fx.Users.LoginAsync(user.UserName!, "WrongPass1");
        var correct = await fx.Users.LoginAsync(user.UserName!, TestData.Password);

        Assert.Equal(LockedOut, fifth.Message);
        Assert.False(correct.Success);
        Assert.Equal(LockedOut, correct.Message);
        // 鎖定 5 分鐘
        var (u, _) = await LoadAsync(user.Id);
        Assert.InRange(u.LockoutEnd!.Value - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Login_成功後失敗次數歸零()
    {
        var user = await fx.CreateUserAsync();
        await FailLoginAsync(user.UserName!, 4);
        Assert.True((await fx.Users.LoginAsync(user.UserName!, TestData.Password)).Success);

        // 若沒有歸零，這次會是第 5 次失敗而被鎖定
        var r = await fx.Users.LoginAsync(user.UserName!, "WrongPass1");

        Assert.Equal(WrongCredentials, r.Message);
        Assert.True((await fx.Users.LoginAsync(user.UserName!, TestData.Password)).Success);
    }

    [Fact]
    public async Task Login_已刪除的帳號_視同不存在()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();
        await fx.Users.DeleteUserAsync(user.Id, admin.Id);

        var r = await fx.Users.LoginAsync(user.UserName!, TestData.Password);

        Assert.False(r.Success);
        Assert.Equal(WrongCredentials, r.Message);
    }

    // ───────────── Cookie 身分驗證 ─────────────

    [Fact]
    public async Task GetActiveUser_正常帳號_回傳最新資料()
    {
        var user = await fx.CreateUserAsync(displayName: "原名");
        var admin = await fx.CreateUserAsync(Roles.Admin);
        await fx.Users.UpdateUserAsync(user.Id, "新名", Roles.Admin, null, admin.Id);

        var u = await fx.Users.GetActiveUserAsync(user.Id);

        // 角色與顯示名稱異動要立即反映（OnValidatePrincipal 依此更新 Cookie）
        Assert.NotNull(u);
        Assert.Equal("新名", u.DisplayName);
        Assert.Equal(Roles.Admin, u.Role);
    }

    [Fact]
    public async Task GetActiveUser_不存在_已刪除_已鎖定_都回傳null()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var deleted = await fx.CreateUserAsync();
        await fx.Users.DeleteUserAsync(deleted.Id, admin.Id);
        var locked = await fx.CreateUserAsync();
        await FailLoginAsync(locked.UserName!, 5);

        Assert.Null(await fx.Users.GetActiveUserAsync(Guid.NewGuid().ToString()));
        Assert.Null(await fx.Users.GetActiveUserAsync(""));
        Assert.Null(await fx.Users.GetActiveUserAsync(deleted.Id));
        Assert.Null(await fx.Users.GetActiveUserAsync(locked.Id));
    }

    // ───────────── 使用者列表 ─────────────

    [Fact]
    public async Task GetUsers_排除已刪除_依角色再依帳號排序()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var u1 = await fx.CreateUserAsync();
        var u2 = await fx.CreateUserAsync();
        var deleted = await fx.CreateUserAsync();
        await fx.Users.DeleteUserAsync(deleted.Id, admin.Id);
        var mine = new HashSet<string> { admin.Id, u1.Id, u2.Id, deleted.Id };

        var list = (await fx.Users.GetUsersAsync()).Where(u => mine.Contains(u.Id)).ToList();

        // 只比對本測試建立的使用者：管理員在前，一般使用者依帳號排序，已刪除的不出現
        var users = new[] { u1, u2 }.OrderBy(u => u.UserName).Select(u => u.Id);
        Assert.Equal(new[] { admin.Id }.Concat(users), list.Select(u => u.Id));
        Assert.Equal(Roles.Admin, list[0].Role);
    }

    // ───────────── 編輯使用者 ─────────────

    [Fact]
    public async Task UpdateUser_不可將自己降為一般使用者()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);

        var r = await fx.Users.UpdateUserAsync(admin.Id, "X", Roles.User, null, admin.Id);

        Assert.False(r.Success);
        Assert.Equal("不可將自己降為一般使用者", r.Message);
        Assert.Equal(new[] { Roles.Admin }, (await LoadAsync(admin.Id)).Roles);
    }

    [Fact]
    public async Task UpdateUser_可以修改自己的顯示名稱()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);

        var r = await fx.Users.UpdateUserAsync(admin.Id, "  我自己  ", Roles.Admin, null, admin.Id);

        Assert.True(r.Success);
        Assert.Equal("我自己", (await LoadAsync(admin.Id)).User.DisplayName);
    }

    [Fact]
    public async Task UpdateUser_角色不正確_擋下()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();

        var r = await fx.Users.UpdateUserAsync(user.Id, "X", "Root", null, admin.Id);

        Assert.False(r.Success);
        Assert.Equal("角色不正確", r.Message);
    }

    [Fact]
    public async Task UpdateUser_切換角色後只保留新角色()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();

        Assert.True((await fx.Users.UpdateUserAsync(user.Id, "X", Roles.Admin, null, admin.Id)).Success);
        Assert.Equal(new[] { Roles.Admin }, (await LoadAsync(user.Id)).Roles);

        Assert.True((await fx.Users.UpdateUserAsync(user.Id, "X", Roles.User, null, admin.Id)).Success);
        Assert.Equal(new[] { Roles.User }, (await LoadAsync(user.Id)).Roles);
    }

    [Fact]
    public async Task UpdateUser_設定新密碼_舊密碼失效()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();

        var r = await fx.Users.UpdateUserAsync(user.Id, "X", Roles.User, "NewPass99", admin.Id);

        Assert.True(r.Success);
        Assert.False((await fx.Users.LoginAsync(user.UserName!, TestData.Password)).Success);
        Assert.True((await fx.Users.LoginAsync(user.UserName!, "NewPass99")).Success);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UpdateUser_密碼留空_不變更密碼(string? newPassword)
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();

        await fx.Users.UpdateUserAsync(user.Id, "X", Roles.User, newPassword, admin.Id);

        Assert.True((await fx.Users.LoginAsync(user.UserName!, TestData.Password)).Success);
    }

    [Fact]
    public async Task UpdateUser_新密碼不符規則_失敗且原密碼仍可登入()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();

        var r = await fx.Users.UpdateUserAsync(user.Id, "X", Roles.User, "short1", admin.Id);

        Assert.False(r.Success);
        // 更新失敗不應該讓使用者失去原本的密碼
        Assert.True((await fx.Users.LoginAsync(user.UserName!, TestData.Password)).Success);
    }

    [Fact]
    public async Task UpdateUser_對象不存在或已刪除_回傳找不到()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var deleted = await fx.CreateUserAsync();
        await fx.Users.DeleteUserAsync(deleted.Id, admin.Id);

        var missing = await fx.Users.UpdateUserAsync(Guid.NewGuid().ToString(), "X", Roles.User, null, admin.Id);
        var gone = await fx.Users.UpdateUserAsync(deleted.Id, "X", Roles.User, null, admin.Id);

        Assert.Equal(NotFound, missing.Message);
        Assert.Equal(NotFound, gone.Message);
    }

    // ───────────── 刪除使用者（軟刪除） ─────────────

    [Fact]
    public async Task DeleteUser_不可刪除自己()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);

        var r = await fx.Users.DeleteUserAsync(admin.Id, admin.Id);

        Assert.False(r.Success);
        Assert.Equal("不可刪除自己", r.Message);
        Assert.Equal(UserStatus.Active, (await LoadAsync(admin.Id)).User.Status);
    }

    [Fact]
    public async Task DeleteUser_仍有未歸還書籍_擋下()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();
        await fx.AddRecordAsync(user.Id, await fx.AddBookAsync(), dueInDays: 3);

        var r = await fx.Users.DeleteUserAsync(user.Id, admin.Id);

        Assert.False(r.Success);
        Assert.Equal("該使用者仍有未歸還的借閱紀錄，無法刪除", r.Message);
        Assert.Equal(UserStatus.Active, (await LoadAsync(user.Id)).User.Status);
    }

    [Fact]
    public async Task DeleteUser_成功_軟刪除並永久鎖定_保留借閱歷史()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();
        await fx.AddRecordAsync(user.Id, await fx.AddBookAsync(), dueInDays: -3, returnedDaysAgo: 4);

        var r = await fx.Users.DeleteUserAsync(user.Id, admin.Id);

        Assert.True(r.Success);
        Assert.Equal("使用者已刪除", r.Message);
        var (u, _) = await LoadAsync(user.Id);
        Assert.Equal(UserStatus.Deleted, u.Status);
        Assert.True(u.LockoutEnabled);
        Assert.Equal(DateTimeOffset.MaxValue, u.LockoutEnd);
        // 借閱歷史仍在（使用者那列沒有實際刪除）
        Assert.Single(await fx.Books.GetUserRecordsAsync(user.Id));
    }

    [Fact]
    public async Task DeleteUser_重複刪除_回傳找不到()
    {
        var admin = await fx.CreateUserAsync(Roles.Admin);
        var user = await fx.CreateUserAsync();
        await fx.Users.DeleteUserAsync(user.Id, admin.Id);

        var r = await fx.Users.DeleteUserAsync(user.Id, admin.Id);

        Assert.False(r.Success);
        Assert.Equal(NotFound, r.Message);
    }
}
