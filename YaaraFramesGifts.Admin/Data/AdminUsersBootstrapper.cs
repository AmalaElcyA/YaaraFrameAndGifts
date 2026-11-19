using System.ComponentModel.DataAnnotations;
using YaaraFramesGifts.Services;

namespace YaaraFramesGifts.Admin.Data;

public static class AdminUsersBootstrapper
{
    public static async Task EnsureAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var users = configuration.GetSection("AdminBootstrap:Users").GetChildren().ToArray();
        if (users.Length == 0)
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var authService = scope.ServiceProvider.GetRequiredService<AdminAuthService>();

        foreach (var section in users)
        {
            var username = section["Username"];
            var password = section["Password"];
            var fullName = section["FullName"];
            var email = section["Email"];
            if (string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password)
                || string.IsNullOrWhiteSpace(fullName)
                || string.IsNullOrWhiteSpace(email)
                || username.Trim().Length is < 3 or > 100
                || password.Length > 128
                || fullName.Trim().Length > 160
                || email.Trim().Length > 254
                || !new EmailAddressAttribute().IsValid(email.Trim()))
            {
                throw new InvalidOperationException(
                    "Each AdminBootstrap:Users entry requires Username, Password, FullName, and Email.");
            }

            await authService.EnsureBootstrapUserAsync(
                username,
                password,
                fullName,
                email,
                CancellationToken.None);
        }
    }
}
