using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Security;
using GovUK.Dfe.CoreLibs.Security.Authorization;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Api.Security.Handlers;
using DfE.ExternalApplications.Infrastructure.Security;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public static IServiceCollection AddCustomAuthorization(
            this IServiceCollection services,
            IConfiguration configuration,
            ITenantConfigurationProvider tenantConfigurationProvider)
        {
            services.AddExternalIdentityValidation(configuration);

            // Config - DfESignIn from root config as base
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
                .AddJwtBearer(AuthConstants.AzureAdScheme, opts =>
                {
                    // Configure dynamic token validation based on tenant
                    opts.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Resolve tenant from the request (set by TenantResolutionMiddleware)
                            var tenantAccessor = context.HttpContext.RequestServices.GetService<ITenantContextAccessor>();
                            var tenant = tenantAccessor?.CurrentTenant;
                            
                            if (tenant != null)
                            {
                                var tenantConfig = tenant.Settings;
                                var azureAdSection = tenantConfig.GetSection("AzureAd");
                                
                                var instance = azureAdSection["Instance"] ?? "https://login.microsoftonline.com/";
                                var tenantId = azureAdSection["TenantId"];
                                var audience = azureAdSection["Audience"];
                                var clientId = azureAdSection["ClientId"];
                                
                                if (!string.IsNullOrEmpty(tenantId))
                                {
                                    context.Options.Authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
                                    context.Options.TokenValidationParameters = new TokenValidationParameters
                                    {
                                        ValidateIssuer = true,
                                        ValidIssuer = $"{instance.TrimEnd('/')}/{tenantId}/v2.0",
                                        ValidateAudience = true,
                                        ValidAudiences = new[] { audience, clientId, $"api://{clientId}" }
                                            .Where(a => !string.IsNullOrEmpty(a))
                                            .Distinct()
                                            .ToArray(),
                                        ValidateLifetime = true,
                                        ValidateIssuerSigningKey = true
                                    };
                                }
                            }
                            
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            // Log authentication failures for debugging
                            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                            logger?.LogWarning(context.Exception, "JWT authentication failed");
                            return Task.CompletedTask;
                        }
                    };
                    
                    // Set a default authority (will be overridden per-request)
                    var firstTenant = tenantConfigurationProvider.GetAllTenants().FirstOrDefault();
                    if (firstTenant != null)
                    {
                        var azureAdSection = firstTenant.Settings.GetSection("AzureAd");
                        var instance = azureAdSection["Instance"] ?? "https://login.microsoftonline.com/";
                        var tenantId = azureAdSection["TenantId"];
                        if (!string.IsNullOrEmpty(tenantId))
                        {
                            opts.Authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
                        }
                    }
                });

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
                ["CanDeleteApplication"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.ApplicationPermissionRequirement(AccessType.Delete.ToString()));
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
                },
                ["CanReadNotifications"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.NotificationsPermissionRequirement(AccessType.Read.ToString()));
                },
                ["CanWriteNotifications"] = pb =>
                {
                    pb.AddAuthenticationSchemes(AuthConstants.CompositeScheme);
                    pb.RequireAuthenticatedUser();
                    pb.AddRequirements(new Handlers.NotificationsPermissionRequirement(AccessType.Write.ToString()));
                }
            };

            // Cookie authentication for SignalR
            services.AddAuthentication() // do NOT set defaults here
                .AddCookie("HubCookie", o =>
                {
                    o.Cookie.Name = "hubauth";
                    o.Cookie.HttpOnly = true;
                    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    o.Cookie.SameSite = SameSiteMode.None;
                    o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                    o.SlidingExpiration = false;             // SignalR/WebSockets won't slide; we'll renew explicitly
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Cookies.CanReadNotifications", p =>
                        p.AddAuthenticationSchemes("HubCookie")
                            .RequireAuthenticatedUser()
                );
            });

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
            services.AddSingleton<IAuthorizationHandler, NotificationsPermissionHandler>(); 
            services.AddTransient<ICustomClaimProvider, PermissionsClaimProvider>();
            services.AddTransient<ICustomClaimProvider, TemplatePermissionsClaimProvider>();
            services.AddTransient<ICustomClaimProvider, UserPermissionClaimProvider>();

            return services;
        }
    }
}
