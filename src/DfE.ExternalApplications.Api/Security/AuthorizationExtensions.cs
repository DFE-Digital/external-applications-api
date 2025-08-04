using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Security;
using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Api.Security.Handlers;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace DfE.ExternalApplications.Api.Security
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
                    options.DefaultAuthenticateScheme = AuthConstants.CompositeScheme;
                    options.DefaultChallengeScheme = AuthConstants.CompositeScheme;
                })
                .AddPolicyScheme(AuthConstants.CompositeScheme, "CompositeAuth", options =>
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
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.TemplatePermissionRequirement(AccessType.Read.ToString()));
                },
                ["CanWriteTemplate"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.TemplatePermissionRequirement(AccessType.Write.ToString()));
                },
                ["CanReadUser"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.UserPermissionRequirement(AccessType.Read.ToString()));
                },
                ["CanReadApplication"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.ApplicationPermissionRequirement(AccessType.Read.ToString()));
                },
                ["CanUpdateApplication"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.ApplicationPermissionRequirement(AccessType.Write.ToString()));
                },
                ["CanReadAnyApplication"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.ApplicationListPermissionRequirement(AccessType.Read.ToString()));
                },
                ["CanCreateAnyApplication"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.AnyTemplatePermissionRequirement(AccessType.Write.ToString()));
                },
                ["CanReadApplicationFiles"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.ApplicationFilesPermissionRequirement(AccessType.Read.ToString()));
                },
                ["CanWriteApplicationFiles"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.ApplicationFilesPermissionRequirement(AccessType.Write.ToString()));
                },
                ["CanDeleteApplicationFiles"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.ApplicationFilesPermissionRequirement(AccessType.Delete.ToString()));
                }
            };

            services.AddApplicationAuthorization(
                configuration,
                policyCustomizations: policyCustomizations,
                apiAuthenticationScheme: AuthConstants.CompositeScheme,
                configureResourcePolicies: null);

            services.AddSingleton<IAuthorizationHandler, TemplatePermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, UserPermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, ApplicationPermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, ApplicationListPermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, AnyTemplatePermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, ApplicationFilesPermissionHandler>();
            services.AddTransient<ICustomClaimProvider, PermissionsClaimProvider>();
            services.AddTransient<ICustomClaimProvider, TemplatePermissionsClaimProvider>();
            services.AddTransient<ICustomClaimProvider, UserPermissionClaimProvider>();

            return services;
        }
    }
}