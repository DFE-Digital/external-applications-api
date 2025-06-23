using DfE.CoreLibs.Security;
using DfE.CoreLibs.Security.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization
{
    [ExcludeFromCodeCoverage]
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddExternalIdentityValidation(configuration);

            // Config
            services
                .Configure<OpenIdConnectOptions>(
                    configuration.GetSection("DfESignIn"));

            var tokenSettings = new TokenSettings();
            configuration.GetSection("Authorization:TokenSettings").Bind(tokenSettings);

            services.AddUserTokenService(configuration);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "CompositeScheme";
                    options.DefaultChallengeScheme = "CompositeScheme";
                })
                .AddPolicyScheme("CompositeScheme", "CompositeAuth", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var header = context.Request.Headers["Authorization"].FirstOrDefault();
                        if (header?.StartsWith("Bearer ") == true)
                        {
                            var token = header.Substring(7);
                            var handler = new JwtSecurityTokenHandler();
                            var jwt = handler.ReadJwtToken(token);
                            return jwt.Issuer == tokenSettings.Issuer
                                ? AuthConstants.UserScheme
                                : AuthConstants.AzureAdScheme;
                        }
                        return AuthConstants.AzureAdScheme;
                    };
                })
                .AddJwtBearer(AuthConstants.UserScheme, opts =>
                {
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = tokenSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = tokenSettings.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(tokenSettings.SecretKey))
                    };
                })
                .AddMicrosoftIdentityWebApi(
                    configuration.GetSection(AuthConstants.AzureAdSection),
                    jwtBearerScheme: AuthConstants.AzureAdScheme,
                    subscribeToJwtBearerMiddlewareDiagnosticsEvents: false);

            // set-up and define User and Template Permission policies
            var policyCustomizations = new Dictionary<string, Action<AuthorizationPolicyBuilder>>
            {
                ["CanReadTemplate"] = pb =>
                {
                    pb.AddAuthenticationSchemes("CompositeScheme");
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.TemplatePermissionRequirement(AccessType.Read.ToString()));
                },
                ["CanReadUser"] = pb =>
                {
                    pb.AddAuthenticationSchemes("CompositeScheme");
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.UserPermissionRequirement(AccessType.Read.ToString()));
                }
            };

            services.AddApplicationAuthorization(
                configuration,
                policyCustomizations: policyCustomizations,
                apiAuthenticationScheme: "CompositeScheme",
                configureResourcePolicies: null);

            services.AddSingleton<IAuthorizationHandler, Handlers.TemplatePermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, Handlers.UserPermissionHandler>();
            services.AddTransient<ICustomClaimProvider, PermissionsClaimProvider>();
            services.AddTransient<ICustomClaimProvider, TemplatePermissionsClaimProvider>();

            return services;
        }
    }
}