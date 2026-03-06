using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DfE.ExternalApplications.Infrastructure.Services;

namespace DfE.ExternalApplications.Infrastructure.Database
{
    public class GenericDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext> where TContext : DbContext
    {
        public TContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../DfE.ExternalApplications.Api");

            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var tenantProvider = new OptionsTenantConfigurationProvider(configuration);
            var defaultTenant = tenantProvider.GetAllTenants().FirstOrDefault();
            var tenantConfiguration = defaultTenant?.Settings ?? configuration;
            var connectionString = tenantConfiguration.GetConnectionString("DefaultConnection");

            var services = new ServiceCollection();

            var optionsBuilder = new DbContextOptionsBuilder<TContext>();
            optionsBuilder.UseSqlServer(connectionString);

            var serviceProvider = services.BuildServiceProvider();

            return (TContext)Activator.CreateInstance(
            typeof(TContext),
                optionsBuilder.Options,
                tenantConfiguration, serviceProvider)!;
        }
    }
}
