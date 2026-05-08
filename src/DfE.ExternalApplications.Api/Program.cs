using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using DfE.ExternalApplications.Api.ExceptionHandlers;
using DfE.ExternalApplications.Api.Filters;
using DfE.ExternalApplications.Api.Middleware;
using DfE.ExternalApplications.Api.Security;
using DfE.ExternalApplications.Api.Swagger;
using GovUK.Dfe.CoreLibs.Http.Extensions;
using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Middlewares.CorrelationId;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.FeatureManagement;
using NetEscapades.AspNetCore.SecurityHeaders;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using DfE.ExternalApplications.Api.Tenancy;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.Caching;
using DfE.ExternalApplications.Infrastructure.Caching;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TelemetryConfiguration = Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration;

namespace DfE.ExternalApplications.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Initial Serilog config with console only
            // App Insights sink is added AFTER builder.Build() when TelemetryConfiguration is available
            builder.Host.UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
            });

            builder.Services.AddControllers(opts =>
                {
                    opts.Filters.Add<ResultToExceptionFilter>();
                })
                .AddJsonOptions(c =>
                {
                    c.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    c.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    c.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    // Disable automatic model validation to let MediatR ValidationBehaviour handle it
                    options.SuppressModelStateInvalidFilter = true;
                });

            builder.Services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;

            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            builder.Services.AddSwaggerGen(c =>
            {
                string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
                c.EnableAnnotations();
            });

            
            // Decorate IDistributedCache with tenant-aware wrapper
            // This ensures all cache operations are automatically scoped to the current tenant
            builder.Services.AddScoped<ITenantAwareDistributedCache>(sp =>
            {
                var innerCache = sp.GetRequiredService<IDistributedCache>();
                var tenantAccessor = sp.GetRequiredService<ITenantContextAccessor>();
                var logger = sp.GetRequiredService<ILogger<TenantAwareDistributedCache>>();
                return new TenantAwareDistributedCache(innerCache, tenantAccessor, logger);
            });

            builder.Services.ConfigureOptions<SwaggerOptions>();
            builder.Services.AddFeatureManagement();
            builder.Services.AddHttpContextAccessor();

            // Always register TenantConfigDbContext and encryptor (needed for seeding even in AppSettings mode)
            builder.Services.AddDbContext<TenantConfigDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("TenantConfigDatabase")));

            var encryptor = BuildTenantSettingsEncryptor(builder.Services, builder.Configuration);
            builder.Services.AddSingleton<ITenantSettingsEncryptor>(encryptor);
            builder.Services.AddScoped<ITenantConfigSeeder, DfE.ExternalApplications.Infrastructure.Services.TenantConfigSeederService>();
            builder.Services.AddScoped<ITenantSettingsWriter, DfE.ExternalApplications.Infrastructure.Services.TenantSettingsWriterService>();

            // Tenant configuration provider: Database or AppSettings based on config toggle
            var tenantConfigSource = builder.Configuration["TenantConfigSource"] ?? "AppSettings";
            ITenantConfigurationProvider tenantConfigurationProvider;

            if (string.Equals(tenantConfigSource, "Database", StringComparison.OrdinalIgnoreCase))
            {
                var tempScopeFactory = BuildServiceScopeFactory(builder.Services, builder.Configuration);

                var dbProvider = new DatabaseTenantConfigurationProvider(
                    tempScopeFactory,
                    LoggerFactory.Create(lb => lb.AddConsole())
                        .CreateLogger<DatabaseTenantConfigurationProvider>(),
                    encryptor: encryptor,
                    targetApplication: "Api");

                dbProvider.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();

                tenantConfigurationProvider = dbProvider;
                builder.Services.AddSingleton<ITenantConfigurationProvider>(dbProvider);
                builder.Services.AddSingleton<IHostedService>(dbProvider);
            }
            else
            {
                var optionsProvider = new OptionsTenantConfigurationProvider(builder.Configuration);
                tenantConfigurationProvider = optionsProvider;
                builder.Services.AddSingleton<ITenantConfigurationProvider>(optionsProvider);
            }

            var allTenants = tenantConfigurationProvider.GetAllTenants();

            if (!allTenants.Any())
            {
                throw new InvalidOperationException(
                    "At least one tenant must be configured.");
            }
            builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
            builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

            builder.Services.AddCustomExceptionHandler<ValidationExceptionHandler>();
            builder.Services.AddCustomExceptionHandler<ApplicationExceptionHandler>();

            // Collect all frontend origins from all tenants for the default CORS policy
            // TenantCorsPolicyProvider will handle per-tenant CORS dynamically
            var allFrontendOrigins = allTenants
                .SelectMany(t => t.FrontendOrigins)
                .Distinct()
                .ToArray();
            
            builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
                p.WithOrigins(allFrontendOrigins.Length > 0 ? allFrontendOrigins : new[] { "https://localhost:7020" })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

            builder.Services.AddSingleton<ICorsPolicyProvider, TenantCorsPolicyProvider>();

            // Configure SignalR with all tenant endpoints
            ConfigureSignalR(builder.Services, allTenants, builder.Environment);

            builder.Services.AddApplicationDependencyGroup(builder.Configuration, tenantConfigurationProvider);
            builder.Services.AddInfrastructureDependencyGroup(builder.Configuration, tenantConfigurationProvider);
            builder.Services.AddCustomAuthorization(builder.Configuration, tenantConfigurationProvider);

            builder.Services.AddOptions<SwaggerUIOptions>()
                .Configure<IHttpContextAccessor>((swaggerUiOptions, httpContextAccessor) =>
                {
                    var originalIndexStreamFactory = swaggerUiOptions.IndexStream;
                    swaggerUiOptions.IndexStream = () =>
                    {
                        using var originalStream = originalIndexStreamFactory();
                        using var originalStreamReader = new StreamReader(originalStream);
                        var originalIndexHtmlContents = originalStreamReader.ReadToEnd();
                        var requestSpecificNonce = httpContextAccessor?.HttpContext?.GetNonce();
                        var nonceEnabledIndexHtmlContents = originalIndexHtmlContents
                            .Replace("<script", $"<script nonce=\"{requestSpecificNonce}\" ",
                                StringComparison.OrdinalIgnoreCase)
                            .Replace("<style", $"<style nonce=\"{requestSpecificNonce}\" ",
                                StringComparison.OrdinalIgnoreCase);
                        return new MemoryStream(Encoding.UTF8.GetBytes(nonceEnabledIndexHtmlContents));
                    };
                });

            // Application Insights uses global configuration (not per-tenant)
            var appInsightsCnnStr = builder.Configuration["GlobalConfiguration:ApplicationInsights:ConnectionString"];
            
            if (!string.IsNullOrWhiteSpace(appInsightsCnnStr))
            {
                builder.Services.AddApplicationInsightsTelemetry(opt =>
                {
                    opt.ConnectionString = appInsightsCnnStr;
                });
            }
            
            // Disable the App Insights ILogger provider completely
            // All logs/exceptions should go through Serilog with our custom converter (includes ErrorId)
            builder.Logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(
                (category, level) => false);

            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            builder.Services.AddOpenApiDocument(configure => { configure.Title = "Api"; });


            var app = builder.Build();

            // Reconfigure Serilog to add Application Insights sink now that TelemetryConfiguration is available
            var telemetryConfig = app.Services.GetService<TelemetryConfiguration>();
            if (telemetryConfig != null)
            {
                Log.Logger = new Serilog.LoggerConfiguration()
                    .ReadFrom.Configuration(builder.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.ApplicationInsights(
                        telemetryConfig,
                        new DfE.ExternalApplications.Api.Telemetry.ExceptionTrackingTelemetryConverter())
                    .CreateLogger();
            }

            var forwardOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All,
                RequireHeaderSymmetry = false
            };
            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardOptions);

            app.UseMiddleware<TenantResolutionMiddleware>();

            // CORS must be early in the pipeline to ensure headers are added to all responses (including errors)
            app.UseCors("Frontend");

            app.UseSecurityHeaders(options =>
            {
                options.AddFrameOptionsDeny()
                    .AddXssProtectionDisabled()
                    .AddContentTypeOptionsNoSniff()
                    .RemoveServerHeader()
                    .AddContentSecurityPolicy(builder =>
                    {
                        builder.AddDefaultSrc().Self();
                        builder.AddStyleSrc().Self().WithNonce();
                        builder.AddScriptSrc().Self().WithNonce();
                    })
                    .AddPermissionsPolicy(builder =>
                    {
                        builder.AddAccelerometer().None();
                        builder.AddAutoplay().None();
                        builder.AddCamera().None();
                        builder.AddEncryptedMedia().None();
                        builder.AddFullscreen().None();
                        builder.AddGeolocation().None();
                        builder.AddGyroscope().None();
                        builder.AddMagnetometer().None();
                        builder.AddMicrophone().None();
                        builder.AddMidi().None();
                        builder.AddPayment().None();
                        builder.AddPictureInPicture().None();
                        builder.AddSyncXHR().None();
                        builder.AddUsb().None();
                    });
            });

            app.UseHsts();
            app.UseHttpsRedirection();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var desc in provider.ApiVersionDescriptions)
                {
                    c.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName.ToUpperInvariant());
                }

                c.SupportedSubmitMethods(SubmitMethod.Get, SubmitMethod.Post, SubmitMethod.Put, SubmitMethod.Delete);
            });

            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseGlobalExceptionHandler(options =>
            {
                options.IncludeDetails = builder.Environment.IsDevelopment();
                options.LogExceptions = true;
                options.DefaultErrorMessage = "Something went wrong";
            });


            app.UseMiddleware<UrlDecoderMiddleware>();

            app.UseRouting();

            app.UseCors("Frontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHub<Hubs.NotificationHub>("/hubs/notifications")
                    .RequireAuthorization("Cookies.CanReadNotifications")
                    .RequireCors("Frontend");
            });

            ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Logger is working...");

            try
            {
                await app.RunAsync();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureSignalR(
            IServiceCollection services, 
            IReadOnlyCollection<TenantConfiguration> tenants, 
            IWebHostEnvironment environment)
        {
            // Collect all Azure SignalR connection strings from all tenants
            var signalREndpoints = tenants
                .Select(t => new { Tenant = t, ConnectionString = t.GetConnectionString("AzureSignalR") })
                .Where(x => !string.IsNullOrEmpty(x.ConnectionString))
                .ToList();

            if (signalREndpoints.Any())
            {
                // Use Azure SignalR Service with multiple endpoints (one per tenant)
                services.AddSignalR()
                    .AddAzureSignalR(options =>
                    {
                        options.Endpoints = signalREndpoints
                            .Select(x => new Microsoft.Azure.SignalR.ServiceEndpoint(
                                x.ConnectionString!, 
                                Microsoft.Azure.SignalR.EndpointType.Primary, 
                                x.Tenant.Id.ToString()))
                            .ToArray();
                    });
            }
            else
            {
                // Use local ASP.NET Core SignalR (development)
                services.AddSignalR();
            }

            // Register the hub context abstraction
            services.AddScoped<DfE.ExternalApplications.Domain.Services.INotificationHubContext, DfE.ExternalApplications.Api.Services.NotificationHubContext>();
        }

        /// <summary>
        /// Builds a temporary IServiceScopeFactory so the DatabaseTenantConfigurationProvider
        /// can load tenants before the full application DI container is built.
        /// </summary>
        private static IServiceScopeFactory BuildServiceScopeFactory(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var tempServices = new ServiceCollection();
            tempServices.AddDbContext<TenantConfigDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("TenantConfigDatabase")));
            tempServices.AddLogging(lb => lb.AddConsole());
            return tempServices.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        }

        /// <summary>
        /// Builds a DataProtection-backed ITenantSettingsEncryptor for encrypting/decrypting
        /// secret tenant settings. Uses the application's Data Protection key ring.
        /// </summary>
        private static ITenantSettingsEncryptor BuildTenantSettingsEncryptor(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var tempServices = new ServiceCollection();
            tempServices.AddDataProtection();
            tempServices.AddLogging(lb => lb.AddConsole());
            var tempProvider = tempServices.BuildServiceProvider();
            return new DataProtectionTenantSettingsEncryptor(
                tempProvider.GetRequiredService<IDataProtectionProvider>());
        }
    }
}
