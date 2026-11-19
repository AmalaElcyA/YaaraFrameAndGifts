using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YaaraFramesGifts.Data;

namespace YaaraFramesGifts.Security;

public sealed class AdminBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    YaaraDbContext dbContext,
    AdminAccessTokenService tokenService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorization, out var header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        if (!tokenService.TryReadToken(header.Parameter, out var token) || token is null)
        {
            return AuthenticateResult.Fail("The administrator access token is invalid or expired.");
        }

        var admin = await dbContext.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == token.UserId, Context.RequestAborted);
        if (admin is null
            || !admin.IsActive
            || !string.Equals(admin.SecurityStamp, token.SecurityStamp, StringComparison.Ordinal))
        {
            return AuthenticateResult.Fail("The administrator account is inactive or its session was revoked.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.GivenName, admin.FullName),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}

public static class AdminBearerAuthenticationExtensions
{
    public static AuthenticationBuilder AddAdminBearer(this AuthenticationBuilder builder) =>
        builder.AddScheme<AuthenticationSchemeOptions, AdminBearerAuthenticationHandler>(
            AdminAuthenticationDefaults.Scheme,
            _ => { });
}
