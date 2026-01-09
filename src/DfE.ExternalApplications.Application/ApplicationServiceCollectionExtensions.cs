using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Consumers;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using ITenantBusFactory = DfE.ExternalApplications.Application.Services.ITenantBusFactory;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using GovUK.Dfe.CoreLibs.FileStorage;
using GovUK.Dfe.CoreLibs.Notifications.Extensions;
using GovUK.Dfe.CoreLibs.Utilities.RateLimiting;
using Microsoft.AspNetCore.Http;
using GovUK.Dfe.CoreLibs.Email;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationDependencyGroup(
            this IServiceCollection services, 
            IConfiguration config,
            ITenantConfigurationProvider tenantConfigurationProvider)
        {
            // Performance logging is enabled if any tenant has it enabled
            var performanceLoggingEnabled = tenantConfigurationProvider
                .GetAllTenants()
                .Any(t => t.Settings.GetValue<bool>("Features:PerformanceLoggingEnabled"));

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

            services.AddRateLimiting<string>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RateLimitingBehaviour<,>));

                if (performanceLoggingEnabled)
                {
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
                }
            });
            services.AddScoped<IPermissionCheckerService, ClaimBasedPermissionCheckerService>();

            services.AddKeyedScoped<ICustomRequestChecker, InternalAuthRequestChecker>("internal");

            services.AddTransient<IApplicationFactory, ApplicationFactory>();
            services.AddTransient<IUserFactory, UserFactory>();
            services.AddTransient<ITemplateFactory, TemplateFactory>();
            services.AddTransient<IFileFactory, FileFactory>();

            // Configure email template resolution from root config (shared across tenants)
            services.Configure<ApplicationTemplatesConfiguration>(config.GetSection("ApplicationTemplates"));
            services.Configure<EmailTemplatesConfiguration>(config.GetSection("EmailTemplates"));
            services.AddTransient<IEmailTemplateResolver, EmailTemplateResolver>();

            services.AddBackgroundService();
            services.AddNotificationServicesWithRedis(config);

            services.AddFileStorage(config);

            services.AddEmailServicesWithGovUkNotify(config);

            // Register tenant bus factory for per-tenant Service Bus connections (publishing)
            services.AddSingleton<ITenantBusFactory, TenantBusFactory>();
            
            // Register tenant-aware event publisher that uses tenant-specific bus
            services.AddScoped<IEventPublisher, TenantAwareEventPublisher>();

            // Skip MassTransit during NSwag/CodeGeneration to prevent assembly loading issues
            var skipMassTransit = config.GetValue<bool>("SkipMassTransit", false);
            if (!skipMassTransit)
            {
                // Register consumer for DI resolution
                services.AddScoped<ScanResultConsumer>();
                
                // Register hosted service that starts per-tenant consumer buses
                // Each tenant gets its own subscription based on their MassTransit config
                services.AddHostedService<TenantConsumerHostedService>();
            }

            return services;
        }
    }
}
