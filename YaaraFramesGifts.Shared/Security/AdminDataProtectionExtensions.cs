using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace YaaraFramesGifts.Security;

public static class AdminDataProtectionExtensions
{
    public static IServiceCollection AddSharedAdminDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection()
            .SetApplicationName("YaaraFramesGifts.AdminAuth");
        var keyRingPath = configuration["AdminAuth:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            Directory.CreateDirectory(keyRingPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }
        else if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "AdminAuth:KeyRingPath must point to a shared, persistent Data Protection key directory outside Development.");
        }

        return services;
    }
}
