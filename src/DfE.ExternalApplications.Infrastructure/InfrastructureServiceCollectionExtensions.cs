using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Infrastructure;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

            //Db
            var connectionString = config.GetConnectionString("DefaultConnection");

            services.AddDbContext<ExternalApplicationsContext>(options =>
                options.UseSqlServer(connectionString));

            return services;
        }
    }
}
