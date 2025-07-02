using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var services = new ServiceCollection();

            var optionsBuilder = new DbContextOptionsBuilder<TContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // Try to add MediatR for runtime, but don't fail if the Application assembly isn't available
            try
            {
                // First, try to build the Application project to ensure the assembly is available
                var applicationProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../DfE.ExternalApplications.Application");
                if (Directory.Exists(applicationProjectPath))
                {
                    var buildProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{applicationProjectPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    });
                    
                    if (buildProcess != null)
                    {
                        buildProcess.WaitForExit();
                    }
                }

                var appAssembly = Assembly.Load("DfE.ExternalApplications.Application");
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(appAssembly);
                });
            }
            catch (Exception)
            {
                // If we can't load the Application assembly, create a minimal MediatR setup
                // This is acceptable for design-time operations like migrations
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GenericDbContextFactory<>).Assembly));
            }

            var serviceProvider = services.BuildServiceProvider();

            return (TContext)Activator.CreateInstance(
            typeof(TContext),
                optionsBuilder.Options,
                configuration, serviceProvider)!;
        }
    }
}
