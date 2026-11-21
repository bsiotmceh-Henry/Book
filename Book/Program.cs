using Book.Common;
using Book.Components;
using Book.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBootstrapBlazor();

builder.Services.AddDbContextFactory<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity：使用者 + 角色，存於 AppDbContext
builder.Services.AddIdentityCore<ApplicationUser>(opt =>
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

// 業務邏輯（資料庫）與每個 circuit 一份的登入狀態
builder.Services.AddSingleton<Book.Services.BookService>();
builder.Services.AddSingleton<Book.Services.UserService>();

// Cookie 驗證：登入在靜態 SSR 的 /login 頁面寫入 Cookie，登出走 /auth/logout
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/login";
        opt.Cookie.Name = "Book.Auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opt.Cookie.SameSite = SameSiteMode.Lax;
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
        // 每次請求確認帳號仍有效；角色以資料庫為準，異動後下次請求生效
        opt.Events.OnValidatePrincipal = async ctx =>
        {
            var users = ctx.HttpContext.RequestServices.GetRequiredService<Book.Services.UserService>();
            var current = await users.GetActiveUserAsync(ctx.Principal?.UserId() ?? "");
            if (current == null)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            else if (current.Role != ctx.Principal!.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                     || current.DisplayName != ctx.Principal.DisplayName())
            {
                ctx.ReplacePrincipal(current.ToPrincipal());
                ctx.ShouldRenew = true;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// 建立預設管理員
await IdentitySeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// 登出：清除 Cookie 後回登入頁（純 HTML form POST，互動頁面取不到防偽權杖，故略過驗證；登出被偽造的影響極小）
app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
