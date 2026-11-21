using Book.Components;
using Book.Data;
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
builder.Services.AddScoped<Book.Services.SessionState>();

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
