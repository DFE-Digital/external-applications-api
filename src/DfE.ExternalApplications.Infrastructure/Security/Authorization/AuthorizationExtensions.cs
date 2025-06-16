using DfE.CoreLibs.Security.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System.Diagnostics.CodeAnalysis;
using DfE.ExternalApplications.Infrastructure.Security.Configurations;
using Microsoft.AspNetCore.Authorization;
using DfE.CoreLibs.Security.Interfaces;

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

            // set-up and define Template Permission policies
            var policyCustomizations = new Dictionary<string, Action<AuthorizationPolicyBuilder>>
            {
                ["CanReadTemplate"] = pb =>
                {
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.TemplatePermissionRequirement("Read"));
                },
                ["CanReadUser"] = pb =>
                {
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.UserPermissionRequirement("Read"));
                }
            };

            services.AddApplicationAuthorization(
                configuration,
                policyCustomizations: policyCustomizations,
                apiAuthenticationScheme: null,
                // set-up resource permissions and policies
                configureResourcePolicies: opts =>
                {
                    opts.Actions.AddRange(["Read", "Write"]);
                    opts.ClaimType = "permission";
                });

            services.AddSingleton<IAuthorizationHandler, Handlers.TemplatePermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, Handlers.UserPermissionHandler>();
            services.AddTransient<ICustomClaimProvider, TemplatePermissionsClaimProvider>();
            services.AddTransient<ICustomClaimProvider, PermissionsClaimProvider>();

            return services;
        }
    }
}