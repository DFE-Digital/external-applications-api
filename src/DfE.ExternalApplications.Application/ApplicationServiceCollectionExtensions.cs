using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Consumers;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using GovUK.Dfe.CoreLibs.FileStorage;
using GovUK.Dfe.CoreLibs.Notifications.Extensions;
using GovUK.Dfe.CoreLibs.Utilities.RateLimiting;
using Microsoft.AspNetCore.Http;
using GovUK.Dfe.CoreLibs.Email;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Entities.Topics;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Extensions;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using MassTransit;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Exceptions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationDependencyGroup(
            this IServiceCollection services, IConfiguration config)
        {
            var performanceLoggingEnabled = config.GetValue<bool>("Features:PerformanceLoggingEnabled");

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

            // Configure email template resolution
            services.Configure<ApplicationTemplatesConfiguration>(config.GetSection("ApplicationTemplates"));
            services.Configure<EmailTemplatesConfiguration>(config.GetSection("EmailTemplates"));
            services.AddTransient<IEmailTemplateResolver, EmailTemplateResolver>();

            services.AddBackgroundService();
            services.AddNotificationServicesWithRedis(config);

            services.AddFileStorage(config);

            services.AddEmailServicesWithGovUkNotify(config);

            // Skip MassTransit during NSwag/CodeGeneration to prevent assembly loading issues
            var skipMassTransit = config.GetValue<bool>("SkipMassTransit", false);
            if (!skipMassTransit)
            {
                services.AddDfEMassTransit(
                    config,
                    configureConsumers: x =>
                    {
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

                        // Azure Service Bus specific configuration
                        // Use existing "extapi" subscription (topic is determined by Message<ScanResultEvent> config above)
                        cfg.SubscriptionEndpoint<ScanResultEvent>("extapi", e =>
                        {
                            e.UseMessageRetry(r =>
                            {
                                // For MessageNotForThisInstanceException (instance filtering in Local env)
                                // Retry immediately and frequently so other consumers pick it up fast
                                r.Handle<MessageNotForThisInstanceException>();
                                r.Immediate(10); // Try 10 times (supports up to 10 concurrent local developers)

                                // For all OTHER exceptions (real errors)
                                // Retry with delay for transient issues
                                r.Ignore<MessageNotForThisInstanceException>(); // Don't apply interval retry to this
                                r.Interval(3, TimeSpan.FromSeconds(5)); // 3 retries, 5 seconds apart for real errors
                            });
                            // Don't try to create new topology - use existing subscription
                            e.ConfigureConsumeTopology = false;

                            // Configure the consumer to process messages from this endpoint
                            e.ConfigureConsumer<ScanResultConsumer>(context);
                        });
                    });
            }

            return services;
        }
    }
}