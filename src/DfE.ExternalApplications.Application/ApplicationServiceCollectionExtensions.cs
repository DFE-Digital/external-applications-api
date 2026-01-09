using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Consumers;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.Tenancy;
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
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Extensions;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Entities.Topics;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Exceptions;
using MassTransit;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationDependencyGroup(
            this IServiceCollection services, 
            IConfiguration config,
            ITenantConfigurationProvider tenantConfigurationProvider)
        {
            // Get the first tenant's configuration for services that need root-level config
            // (CoreLibs extensions like FileStorage, Email, Notifications read from root config)
            var firstTenant = tenantConfigurationProvider.GetAllTenants().FirstOrDefault()
                ?? throw new InvalidOperationException("At least one tenant must be configured.");
            var tenantConfig = firstTenant.Settings;
            
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

            // Configure email template resolution from first tenant's config
            services.Configure<ApplicationTemplatesConfiguration>(tenantConfig.GetSection("ApplicationTemplates"));
            services.Configure<EmailTemplatesConfiguration>(tenantConfig.GetSection("EmailTemplates"));
            services.AddTransient<IEmailTemplateResolver, EmailTemplateResolver>();

            services.AddBackgroundService();
            
            // Use first tenant's config for CoreLibs services
            // These services read from root-level config, so we pass the tenant's settings
            services.AddNotificationServicesWithRedis(tenantConfig);

            services.AddFileStorage(tenantConfig);

            services.AddEmailServicesWithGovUkNotify(tenantConfig);

            // Skip MassTransit during NSwag/CodeGeneration to prevent assembly loading issues
            // Note: This reads from root config for backward compatibility with CodeGeneration environment
            var skipMassTransit = config.GetValue<bool>("SkipMassTransit", false);
            if (!skipMassTransit)
            {
                // Use CoreLibs AddDfEMassTransit for publishing
                // This properly configures MassTransit with DI integration
                // Publishing uses the first tenant's Service Bus connection (all tenants share the same namespace)
                services.AddDfEMassTransit(
                    tenantConfig, // Use first tenant's config which has MassTransit settings
                    configureConsumers: x =>
                    {
                        // Register consumer for DI resolution
                        x.AddConsumer<ScanResultConsumer>();
                    },
                    configureBus: (context, cfg) =>
                    {
                        // Configure topic names for message types
                        cfg.Message<ScanRequestedEvent>(m => m.SetEntityName(TopicNames.ScanRequests));
                        cfg.Message<ScanResultEvent>(m => m.SetEntityName(TopicNames.ScanResult));
                    },
                    configureAzureServiceBus: (context, cfg) =>
                    {
                        cfg.UseJsonSerializer();

                        // Get subscription name from first tenant's config
                        var subscriptionName = tenantConfig["MassTransit:AzureServiceBus:SubscriptionName"] ?? "extapi";

                        // Configure subscription endpoint with tenant-specific name
                        cfg.SubscriptionEndpoint<ScanResultEvent>(subscriptionName, e =>
                        {
                            e.UseMessageRetry(r =>
                            {
                                // For MessageNotForThisInstanceException (instance filtering in Local env)
                                r.Handle<MessageNotForThisInstanceException>();
                                r.Immediate(10);

                                // For all OTHER exceptions (real errors)
                                r.Ignore<MessageNotForThisInstanceException>();
                                r.Interval(3, TimeSpan.FromSeconds(5));
                            });

                            e.ConfigureConsumeTopology = false;
                            e.ConfigureConsumer<ScanResultConsumer>(context);
                        });
                    }
                );
                
                // For multi-tenant consuming (if tenants have different subscription names),
                // register the hosted service that starts per-tenant consumer buses
                // Note: This is in addition to the primary bus consumer configured above
                // services.AddHostedService<TenantConsumerHostedService>();
            }

            return services;
        }
    }
}
