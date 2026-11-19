using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YaaraFramesGifts.Data;

public sealed class YaaraDbContextFactory : IDesignTimeDbContextFactory<YaaraDbContext>
{
    public YaaraDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The 'DefaultConnection' connection string is not configured.");
        }

        var options = new DbContextOptionsBuilder<YaaraDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.MigrationsAssembly("YaaraFramesGifts.Shared"))
            .Options;

        return new YaaraDbContext(options);
    }
}
