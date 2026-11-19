using YaaraFramesGifts.Components;
using YaaraFramesGifts.Data;
using Microsoft.EntityFrameworkCore;
using YaaraFramesGifts.Api;
using YaaraFramesGifts.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using YaaraFramesGifts.Models;
using YaaraFramesGifts.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<YaaraDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServer => sqlServer.MigrationsAssembly("YaaraFramesGifts.Shared")));
builder.Services.AddSharedAdminDataProtection(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<AdminAccessTokenService>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddAuthentication(AdminAuthenticationDefaults.Scheme)
    .AddAdminBearer();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminAuthenticationDefaults.Policy, policy =>
    {
        policy.AddAuthenticationSchemes(AdminAuthenticationDefaults.Scheme);
        policy.RequireRole("Admin");
    });
builder.Services.AddHttpClient<IYaaraApiClient, YaaraApiClient>();
builder.Services.AddScoped<CartSession>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartApiService, CartApiService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<YaaraDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(dbContext);
}

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    api =>
    {
        api.UseExceptionHandler();
        api.UseStatusCodePages();
    });

// Configure the HTTP request pipeline.
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
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
