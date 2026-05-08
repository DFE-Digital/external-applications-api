using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DfE.ExternalApplications.Infrastructure.Database;

/// <summary>
/// Design-time factory for TenantConfigDbContext, used by dotnet ef migrations commands.
/// Reads the TenantConfigDatabase connection string from the API appsettings.
/// </summary>
public class TenantConfigDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TenantConfigDbContext>
{
    public TenantConfigDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../DfE.ExternalApplications.Api");
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("TenantConfigDatabase");

        var optionsBuilder = new DbContextOptionsBuilder<TenantConfigDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TenantConfigDbContext(optionsBuilder.Options);
    }
}
