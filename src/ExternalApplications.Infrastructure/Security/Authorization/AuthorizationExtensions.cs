using DfE.CoreLibs.Security.Authorization;
using ExternalApplications.Infrastructure.Security.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System.Diagnostics.CodeAnalysis;
using DfE.CoreLibs.Security.Authorization;

namespace ExternalApplications.Infrastructure.Security.Authorization
{
    [ExcludeFromCodeCoverage]
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

            services.AddApplicationAuthorization(configuration);

            return services;
        }
    }
}