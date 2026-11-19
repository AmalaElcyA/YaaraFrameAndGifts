using YaaraFramesGifts.Admin.Components;
using YaaraFramesGifts.Admin.Data;
using YaaraFramesGifts.Admin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using YaaraFramesGifts.Data;
using YaaraFramesGifts.Models;
using YaaraFramesGifts.Security;
using YaaraFramesGifts.Services;

var builder = WebApplication.CreateBuilder(args);
var mainSettingsDirectory = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "YaaraFramesGifts"));
builder.Configuration
    .AddJsonFile(Path.Combine(mainSettingsDirectory, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddJsonFile(
        Path.Combine(mainSettingsDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"),
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddDbContext<YaaraDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "The main application's DefaultConnection connection string is not configured.");
    }

    options.UseSqlServer(
        connectionString,
        sqlServer => sqlServer.MigrationsAssembly("YaaraFramesGifts.Shared"));
});
builder.Services.AddSharedAdminDataProtection(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<AdminAccessTokenService>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "YaaraFramesGifts.Admin.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(55);
        options.SlidingExpiration = false;
    })
    .AddAdminBearer();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminAuthenticationDefaults.Policy, policy =>
    {
        policy.AddAuthenticationSchemes(AdminAuthenticationDefaults.Scheme);
        policy.RequireRole("Admin");
    });
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("admin-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddHttpClient("AdminApi", (services, client) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var configuredBaseUrl = configuration["AdminApi:BaseUrl"];
    if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseAddress)
        || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps)
        || (baseAddress.Scheme != Uri.UriSchemeHttps && !builder.Environment.IsDevelopment()))
    {
        throw new InvalidOperationException(
            "AdminApi:BaseUrl must be an absolute HTTPS URL (HTTP is allowed only in Development).");
    }

    client.BaseAddress = new Uri(baseAddress.ToString().TrimEnd('/') + "/");
});
builder.Services.AddScoped<AdminApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    api =>
    {
        api.UseExceptionHandler();
        api.UseStatusCodePages();
    });
if (!app.Environment.IsDevelopment())
{
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/api"),
        pages => pages.UseExceptionHandler("/Error", createScopeForErrors: true));
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    pages => pages.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<YaaraDbContext>();
    await dbContext.Database.MigrateAsync();
}
await AdminUsersBootstrapper.EnsureAsync(app.Services, builder.Configuration);

app.Run();
