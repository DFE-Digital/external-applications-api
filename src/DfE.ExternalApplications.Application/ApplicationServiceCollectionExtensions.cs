using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Application.Common.Models;
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
            services.AddScoped<ICustomRequestChecker, CypressRequestChecker>();
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
                    },
                    configureBus: (context, cfg) =>
                    {
                        cfg.Message<FileUploadedEvent>(m => m.SetEntityName(TopicNames.FileScanner));

                    });
            }

            return services;
        }
    }
}
