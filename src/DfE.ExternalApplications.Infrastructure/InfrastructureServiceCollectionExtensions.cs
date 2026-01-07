using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Infrastructure;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Infrastructure.Repositories;
using DfE.ExternalApplications.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureDependencyGroup(
            this IServiceCollection services, IConfiguration config)
        {
            //Repos
            services.AddScoped(typeof(IEaRepository<>), typeof(EaRepository<>));
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Cache service
            services.AddServiceCaching(config);

            services.AddTransient<IApplicationReferenceProvider, DefaultApplicationReferenceProvider>();
            services.AddTransient<IApplicationResponseAppender, ApplicationResponseAppender>();
            
            // Static HTML Generator Service
            services.AddScoped<IStaticHtmlGeneratorService, PlaywrightHtmlGeneratorService>();

            // SignalR Services
            services.AddScoped<INotificationSignalRService, NotificationSignalRService>();

            //Db
            services.AddDbContext<ExternalApplicationsContext>((serviceProvider, options) =>
            {
                var tenantAccessor = serviceProvider.GetRequiredService<DfE.ExternalApplications.Domain.Tenancy.ITenantContextAccessor>();
                var tenant = tenantAccessor.CurrentTenant ?? throw new InvalidOperationException("Tenant context not resolved for database access.");
                var connectionString = tenant.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException($"Tenant '{tenant.Name}' is missing DefaultConnection connection string.");

                options.UseSqlServer(connectionString, sql =>
                {
                });
            });

            return services;
        }
    }
}
