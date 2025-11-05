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
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Cache service
            services.AddServiceCaching(config);

            services.AddTransient<IApplicationReferenceProvider, DefaultApplicationReferenceProvider>();

            // SignalR Services
            services.AddScoped<INotificationSignalRService, NotificationSignalRService>();

            //Db
            var connectionString = config.GetConnectionString("DefaultConnection");

            services.AddDbContext<ExternalApplicationsContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                }));

            return services;
        }
    }
}
