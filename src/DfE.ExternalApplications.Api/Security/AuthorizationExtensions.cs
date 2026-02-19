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
            // Collect all DfESignIn configurations from all tenants for multi-provider support
            var allTenants = tenantConfigurationProvider.GetAllTenants();
            
            // Get first tenant's config for services that need root-level configuration
            var firstTenantForConfig = allTenants.FirstOrDefault();
            var baseConfig = firstTenantForConfig?.Settings ?? configuration;
            
            // Register external identity validation with multi-provider support
            // Each tenant's DfESignIn config is added as an isolated provider
            // The validator will try each provider until one fully validates (issuer + audience must match same provider)
            services.AddExternalIdentityValidation(baseConfig, multiOpts =>
            {
                foreach (var tenant in allTenants)
                {
                    var dfeSignInSection = tenant.Settings.GetSection("DfESignIn");
                    var discoveryEndpoint = dfeSignInSection["DiscoveryEndpoint"];
                    
                    // Only add providers with valid discovery endpoints
                    if (!string.IsNullOrEmpty(discoveryEndpoint))
                    {
                        var providerOpts = new OpenIdConnectOptions
                        {
                            // Identity
                            Issuer = dfeSignInSection["Issuer"],
                            Authority = dfeSignInSection["Authority"],
                            ClientId = dfeSignInSection["ClientId"],
                            ClientSecret = dfeSignInSection["ClientSecret"],
                            DiscoveryEndpoint = discoveryEndpoint,
                            
                            // Validation settings
                            ValidateIssuer = bool.TryParse(dfeSignInSection["ValidateIssuer"], out var vi) ? vi : true,
                            ValidateAudience = bool.TryParse(dfeSignInSection["ValidateAudience"], out var va) ? va : true,
                            ValidateLifetime = bool.TryParse(dfeSignInSection["ValidateLifetime"], out var vl) ? vl : true,
                            
                            // Other settings that may be configured
                            RedirectUri = dfeSignInSection["RedirectUri"],
                            Prompt = dfeSignInSection["Prompt"],
                            ResponseType = dfeSignInSection["ResponseType"] ?? "code",
                            RequireHttpsMetadata = bool.TryParse(dfeSignInSection["RequireHttpsMetadata"], out var rhm) ? rhm : true,
                            GetClaimsFromUserInfoEndpoint = bool.TryParse(dfeSignInSection["GetClaimsFromUserInfoEndpoint"], out var gc) ? gc : true,
                            SaveTokens = bool.TryParse(dfeSignInSection["SaveTokens"], out var st) ? st : true,
                            UseTokenLifetime = bool.TryParse(dfeSignInSection["UseTokenLifetime"], out var utl) ? utl : true,
                            NameClaimType = dfeSignInSection["NameClaimType"] ?? "email"
                        };
                        
                        // Add scopes if configured
                        var scopesSection = dfeSignInSection.GetSection("Scopes");
                        if (scopesSection.Exists())
                        {
                            providerOpts.Scopes = scopesSection.Get<List<string>>() ?? new List<string> { "openid", "profile", "email" };
                        }
                        
                        multiOpts.Providers.Add(providerOpts);
                    }
                }
            });

            var tokenSettings = new TokenSettings();
            baseConfig.GetSection("Authorization:TokenSettings").Bind(tokenSettings);

            services.AddUserTokenService(baseConfig);

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
                    // Set default authority from first tenant for signing key resolution
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
                        
                        // Collect all valid audiences from ALL tenants at startup
                        var allAudiences = tenantConfigurationProvider.GetAllTenants()
                            .SelectMany(t =>
                            {
                                var section = t.Settings.GetSection("AzureAd");
                                var audience = section["Audience"];
                                var clientId = section["ClientId"];
                                return new[] { audience, clientId, $"api://{clientId}" };
                            })
                            .Where(a => !string.IsNullOrEmpty(a))
                            .Distinct()
                            .ToArray();
                        
                        // Collect all valid issuers from ALL tenants (both v1.0 and v2.0 formats)
                        var allIssuers = tenantConfigurationProvider.GetAllTenants()
                            .SelectMany(t =>
                            {
                                var section = t.Settings.GetSection("AzureAd");
                                var inst = section["Instance"] ?? "https://login.microsoftonline.com/";
                                var tid = section["TenantId"];
                                if (string.IsNullOrEmpty(tid)) return Array.Empty<string>();
                                
                                // Azure AD tokens can have v1.0 or v2.0 issuer format
                                return new[]
                                {
                                    $"{inst.TrimEnd('/')}/{tid}/v2.0",           // v2.0 format
                                    $"https://sts.windows.net/{tid}/",            // v1.0 format
                                    $"https://login.microsoftonline.com/{tid}/v2.0" // explicit v2.0
                                };
                            })
                            .Where(i => !string.IsNullOrEmpty(i))
                            .Distinct()
                            .ToArray();
                        
                        opts.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuers = allIssuers,
                            ValidateAudience = true,
                            ValidAudiences = allAudiences,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true
                        };
                    }
                    
                    opts.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            // After token is validated, verify it matches the resolved tenant
                            var tenantAccessor = context.HttpContext.RequestServices.GetService<ITenantContextAccessor>();
                            var tenant = tenantAccessor?.CurrentTenant;
                            
                            if (tenant != null)
                            {
                                var azureAdSection = tenant.Settings.GetSection("AzureAd");
                                var expectedAudience = azureAdSection["Audience"];
                                var expectedClientId = azureAdSection["ClientId"];
                                
                                var tokenAudience = context.Principal?.Claims
                                    .FirstOrDefault(c => c.Type == "aud")?.Value;
                                
                                var validAudiences = new[] { expectedAudience, expectedClientId, $"api://{expectedClientId}" }
                                    .Where(a => !string.IsNullOrEmpty(a));
                                
                                if (!string.IsNullOrEmpty(tokenAudience) && !validAudiences.Contains(tokenAudience))
                                {
                                    context.Fail($"Token audience '{tokenAudience}' does not match tenant '{tenant.Name}'");
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
                baseConfig,
                policyCustomizations: policyCustomizations,
                apiAuthenticationScheme: AuthConstants.CompositeScheme,
                configureResourcePolicies: null);

            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationFailureResponseHandler>();
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
