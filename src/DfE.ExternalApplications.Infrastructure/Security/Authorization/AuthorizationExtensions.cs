using DfE.CoreLibs.Security.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System.Diagnostics.CodeAnalysis;
using DfE.ExternalApplications.Infrastructure.Security.Configurations;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization
{
    [ExcludeFromCodeCoverage]
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ExternalIdpOptions>(
                configuration.GetSection(AuthConstants.ExternalIdpSection));

            var externalOpts = configuration
                .GetSection(AuthConstants.ExternalIdpSection)
                .Get<ExternalIdpOptions>()!;

            var auth = services.AddAuthentication();

            // User Scheme
            auth.AddJwtBearer(AuthConstants.UserScheme, opts =>
            {
                opts.Authority = externalOpts.Authority;
                opts.Audience = externalOpts.ClientId;

                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var hdr = ctx.Request.Headers[AuthConstants.AuthorizationHeader]
                            .ToString();
                        if (hdr.StartsWith(AuthConstants.BearerPrefix,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.Token = hdr[AuthConstants.BearerPrefix.Length..].Trim();
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Service-Scheme Azure AD
            auth.AddMicrosoftIdentityWebApi(
                configureMicrosoftIdentityOptions: opts =>
                {
                    configuration
                        .GetSection(AuthConstants.AzureAdSection)
                        .Bind(opts);
                },
                configureJwtBearerOptions: opts =>
                {
                    opts.Events ??= new JwtBearerEvents();
                    opts.Events.OnMessageReceived = ctx =>
                    {
                        ctx.Token = ctx.Request
                            .Headers[AuthConstants.ServiceAuthHeader]
                            .FirstOrDefault();
                        return Task.CompletedTask;
                    };
                },
                jwtBearerScheme: AuthConstants.AzureAdScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents: false
            );

            services.AddApplicationAuthorization(configuration);

            return services;
        }
    }
}