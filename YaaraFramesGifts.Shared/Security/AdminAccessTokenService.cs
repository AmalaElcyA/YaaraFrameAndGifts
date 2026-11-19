using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using YaaraFramesGifts.Models;

namespace YaaraFramesGifts.Security;

public sealed class AdminAccessTokenService
{
    private readonly ITimeLimitedDataProtector protector;

    public AdminAccessTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        protector = dataProtectionProvider
            .CreateProtector("YaaraFramesGifts.Admin.ApiToken.v1")
            .ToTimeLimitedDataProtector();
    }

    public string CreateToken(AdminUser user)
    {
        var payload = JsonSerializer.Serialize(new AdminAccessTokenPayload(user.Id, user.SecurityStamp));
        return protector.Protect(
            payload,
            TimeSpan.FromSeconds(AdminAuthenticationDefaults.AccessTokenLifetimeSeconds));
    }

    public bool TryReadToken(string protectedToken, out AdminAccessTokenPayload? payload)
    {
        try
        {
            var json = protector.Unprotect(protectedToken);
            payload = JsonSerializer.Deserialize<AdminAccessTokenPayload>(json);
            return payload is { UserId: > 0 } && !string.IsNullOrWhiteSpace(payload.SecurityStamp);
        }
        catch (CryptographicException)
        {
            payload = null;
            return false;
        }
        catch (JsonException)
        {
            payload = null;
            return false;
        }
    }
}

public sealed record AdminAccessTokenPayload(int UserId, string SecurityStamp);
