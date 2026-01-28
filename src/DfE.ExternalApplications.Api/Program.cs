using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Azure.Core.Diagnostics;
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
using DfE.ExternalApplications.Infrastructure.Services;
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
            var tenantConfigurationProvider = new OptionsTenantConfigurationProvider(builder.Configuration);
            var allTenants = tenantConfigurationProvider.GetAllTenants();
            
            // Startup validation: at least one tenant must be configured
            if (!allTenants.Any())
            {
                throw new InvalidOperationException(
                    "At least one tenant must be configured in the 'Tenants' section of appsettings.");
            }

            builder.Services.AddSingleton<ITenantConfigurationProvider>(tenantConfigurationProvider);
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

            // Application Insights is configured from first tenant's config
            var firstTenantConfig = allTenants.FirstOrDefault()?.Settings;
            var appInsightsCnnStr = firstTenantConfig?.GetSection("ApplicationInsights")?["ConnectionString"];
            
            // Debug: Log App Insights configuration status
            Console.WriteLine($"=== APP INSIGHTS DEBUG ===");
            Console.WriteLine($"First tenant found: {allTenants.FirstOrDefault()?.Name ?? "NULL"}");
            Console.WriteLine($"Connection string found: {!string.IsNullOrWhiteSpace(appInsightsCnnStr)}");
            Console.WriteLine($"Connection string (first 50 chars): {appInsightsCnnStr?.Substring(0, Math.Min(50, appInsightsCnnStr?.Length ?? 0))}...");
            
            if (!string.IsNullOrWhiteSpace(appInsightsCnnStr))
            {
                Console.WriteLine("=== REGISTERING APP INSIGHTS TELEMETRY ===");
                // Configure App Insights for request/dependency tracking only
                builder.Services.AddApplicationInsightsTelemetry(opt =>
                {
                    opt.ConnectionString = appInsightsCnnStr;
                });
            }
            else
            {
                Console.WriteLine("=== APP INSIGHTS NOT REGISTERED - NO CONNECTION STRING ===");
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
            // This couldn't be done in UseSerilog because App Insights wasn't registered yet
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
                
                // Diagnostic: Log to verify Serilog App Insights sink is configured
                Log.Information("=== SERILOG APP INSIGHTS CONFIGURED ===");
                Log.Information("TelemetryConfiguration ConnectionString: {ConnectionString}", 
                    telemetryConfig.ConnectionString?.Substring(0, Math.Min(50, telemetryConfig.ConnectionString?.Length ?? 0)) + "...");
                Log.Information("InstrumentationKey present: {HasKey}", !string.IsNullOrEmpty(telemetryConfig.InstrumentationKey));
                
                // Test exception logging with ErrorId pattern via SERILOG
                try
                {
                    throw new InvalidOperationException("SERILOG_APPINSIGHTS_TEST: This is a startup test exception");
                }
                catch (Exception testEx)
                {
                    Log.Error(testEx, "=== SERILOG TEST === Exception with ErrorId: {ErrorId}, CorrelationId: {CorrelationId}", 
                        "STARTUP-TEST-123", "test-correlation-456");
                }
                
                // Also test DIRECT TelemetryClient to compare
                var testTelemetryClient = new Microsoft.ApplicationInsights.TelemetryClient(telemetryConfig);
                
                // Check if telemetry channel is active
                Console.WriteLine($"=== TELEMETRY CHANNEL STATUS ===");
                Console.WriteLine($"TelemetryChannel: {telemetryConfig.TelemetryChannel?.GetType().Name ?? "NULL"}");
                Console.WriteLine($"DeveloperMode: {telemetryConfig.TelemetryChannel?.DeveloperMode}");
                
                var directException = new InvalidOperationException("DIRECT_TELEMETRYCLIENT_TEST: Direct test");
                var exTelemetry = new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(directException);
                exTelemetry.Properties["ErrorId"] = "DIRECT-TEST-789";
                exTelemetry.Properties["Source"] = "DirectTelemetryClient";
                exTelemetry.Properties["TestTimestamp"] = DateTime.UtcNow.ToString("o");
                testTelemetryClient.TrackException(exTelemetry);
                
                // Force immediate send
                testTelemetryClient.Flush();
                Console.WriteLine("=== WAITING FOR TELEMETRY TO SEND ===");
                Thread.Sleep(5000); // Wait 5 seconds for transmission
                Console.WriteLine("=== DIRECT TELEMETRYCLIENT TEST SENT ===");
            }
            else
            {
                Log.Warning("=== SERILOG APP INSIGHTS NOT CONFIGURED === TelemetryConfiguration is null!");
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
                // Ensure all telemetry is flushed before shutdown
                Console.WriteLine("=== FLUSHING TELEMETRY ===");
                
                // Flush Serilog
                Log.CloseAndFlush();
                
                // Flush App Insights
                var telemetryClient = app.Services.GetService<Microsoft.ApplicationInsights.TelemetryClient>();
                if (telemetryClient != null)
                {
                    telemetryClient.Flush();
                    // Give it time to send
                    await Task.Delay(2000);
                }
                
                Console.WriteLine("=== TELEMETRY FLUSHED ===");
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
    }
}
