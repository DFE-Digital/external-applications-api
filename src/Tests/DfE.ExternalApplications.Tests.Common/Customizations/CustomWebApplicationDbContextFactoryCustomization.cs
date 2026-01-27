using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.Mocks.Authentication;
using GovUK.Dfe.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Api;
using GovUK.Dfe.ExternalApplications.Api.Client.Extensions;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Seeders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Security.Claims;
using GovUK.Dfe.CoreLibs.Notifications.Interfaces;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using GovUK.Dfe.CoreLibs.Security;
using DfE.ExternalApplications.Tests.Common.Helpers;
using GovUK.Dfe.ExternalApplications.Api.Client;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using GovUK.Dfe.CoreLibs.Utilities.RateLimiting;

namespace DfE.ExternalApplications.Tests.Common.Customizations
{
    public class CustomWebApplicationDbContextFactoryCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<CustomWebApplicationDbContextFactory<Program>>(composer => composer.FromFactory(() =>
            {
                // Set environment to "Local" to bypass Azure-specific operations in tests
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Local");
                
                // Set environment variables for MassTransit configuration
                // These will be picked up by ConfigurationBuilder in Program.cs
                // For tests, provide a dummy connection string to satisfy validation
                Environment.SetEnvironmentVariable("SkipMassTransit", "false");
                Environment.SetEnvironmentVariable("MassTransit__Transport", "AzureServiceBus");
                Environment.SetEnvironmentVariable("MassTransit__AppPrefix", "");
                // Dummy connection string for tests - format: Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...
                Environment.SetEnvironmentVariable("MassTransit__AzureServiceBus__ConnectionString", "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test=");
                Environment.SetEnvironmentVariable("MassTransit__AzureServiceBus__AutoCreateEntities", "false");
                Environment.SetEnvironmentVariable("MassTransit__AzureServiceBus__ConfigureEndpoints", "false");
                Environment.SetEnvironmentVariable("MassTransit__AzureServiceBus__UseWebSockets", "true");
                // Configure service support email address for testing user feedback/support
                Environment.SetEnvironmentVariable("Email__ServiceSupportEmailAddress", "some.email@education.gov.uk");
                
                var tokenConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "Authorization:TokenSettings:SecretKey", "iw5/ivfUWaCpj+n3TihlGUzRVna+KKu8IfLP52GdgNXlDcqt3+N2MM45rwQ=" },
                        { "Authorization:TokenSettings:Issuer", "21f3ed37-8443-4755-9ed2-c68ca86b4398" },
                        { "Authorization:TokenSettings:Audience", "20dafd6d-79e5-4caf-8b72-d070dcc9716f" },
                        { "Authorization:TokenSettings:TokenLifetimeMinutes", "60" },
                        { "NotificationService:StorageProvider", "Redis" },
                        { "NotificationService:MaxNotificationsPerUser", "50" },
                        { "NotificationService:AutoCleanupIntervalMinutes", "60" },
                        { "NotificationService:MaxNotificationAgeHours", "24" },
                        { "NotificationService:RedisConnectionString", "localhost:6379" },
                        { "NotificationService:RedisKeyPrefix", "notifications:" },
                        { "NotificationService:SessionKey", "UserNotifications" }
                    })
                    .Build();

                var factory = new CustomWebApplicationDbContextFactory<Program>()
                {
                    SeedData = new Dictionary<Type, Action<DbContext>>
                    {
                        { typeof(ExternalApplicationsContext), context => EaContextSeeder.SeedTestData((ExternalApplicationsContext)context) },
                    },
                    ExternalServicesConfiguration = services =>
                    {

                        services.RemoveAll(typeof(IConfigureOptions<AuthenticationOptions>));
                        services.RemoveAll(typeof(IConfigureOptions<JwtBearerOptions>));
                        services.RemoveAll<IPostConfigureOptions<AuthenticationOptions>>();
                        services.RemoveAll<IPostConfigureOptions<JwtBearerOptions>>();

                        services.AddAuthentication(options =>
                            {
                                options.DefaultAuthenticateScheme = "CompositeScheme";
                                options.DefaultChallengeScheme = "CompositeScheme";
                            })
                            .AddPolicyScheme("CompositeScheme", "CompositeAuth", schemeOptions =>
                            {
                                schemeOptions.ForwardDefaultSelector = context =>
                                {
                                    var header = context.Request.Headers[AuthConstants.AuthorizationHeader]
                                        .FirstOrDefault();
                                    if (header?.StartsWith(AuthConstants.BearerPrefix) == true)
                                    {
                                        var token = header.Substring(AuthConstants.BearerPrefix.Length);
                                        return token.StartsWith("user")
                                            ? AuthConstants.UserScheme
                                            : AuthConstants.AzureAdScheme;
                                    }

                                    return AuthConstants.AzureAdScheme;
                                };
                            })
                            .AddScheme<AuthenticationSchemeOptions, MockJwtBearerHandler>(
                                AuthConstants.UserScheme,
                                _ => { /* picks up factory.TestClaims */ })

                            .AddScheme<AuthenticationSchemeOptions, MockJwtBearerHandler>(
                                AuthConstants.AzureAdScheme,
                                _ => { /* picks up the same factory.TestClaims */ })
                            
                            // Add HubCookie scheme for SignalR authentication with proper cookie handler
                            .AddScheme<AuthenticationSchemeOptions, MockCookieAuthenticationHandler>(
                                "HubCookie",
                                _ => { /* picks up factory.TestClaims */ });

                        services.RemoveAll<IExternalIdentityValidator>();
                        services.RemoveAll<IUserTokenService>();
                        services.RemoveAll<IRateLimiterFactory<string>>();

                        services.AddTransient<IExternalIdentityValidator, TestExternalIdentityValidator>();
                        services.AddUserTokenService(tokenConfig);
                        services.AddSingleton<IRateLimiterFactory<string>, MockRateLimiterFactory>();
                        
                        // Replace the notification service with our mock
                        services.RemoveAll<INotificationService>();
                        services.AddSingleton<INotificationService, MockNotificationService>();
                        
                        // Replace the email service with our mock to avoid sending actual emails in tests
                        services.RemoveAll<GovUK.Dfe.CoreLibs.Email.Interfaces.IEmailService>();
                        services.AddSingleton<GovUK.Dfe.CoreLibs.Email.Interfaces.IEmailService, MockEmailService>();
                        
                        // Replace the file storage service with our mock to avoid requiring actual Azure Storage connection strings in tests
                        services.RemoveAll<GovUK.Dfe.CoreLibs.FileStorage.Interfaces.IFileStorageService>();
                        services.AddSingleton<GovUK.Dfe.CoreLibs.FileStorage.Interfaces.IFileStorageService, MockFileStorageService>();
                        
                        // Also register our mock for the tenant-aware interface used by handlers
                        services.RemoveAll<DfE.ExternalApplications.Application.Services.ITenantAwareFileStorageService>();
                        services.AddSingleton<DfE.ExternalApplications.Application.Services.ITenantAwareFileStorageService, MockFileStorageService>();
                        
                        // Replace IAzureSpecificOperations with our mock to avoid requiring actual Azure Storage for SAS token generation
                        services.RemoveAll<GovUK.Dfe.CoreLibs.FileStorage.Interfaces.IAzureSpecificOperations>();
                        services.AddSingleton<GovUK.Dfe.CoreLibs.FileStorage.Interfaces.IAzureSpecificOperations, MockAzureSpecificOperations>();
                        
                        // Replace IEventPublisher with our mock to avoid hanging on MassTransit publish in tests
                        services.RemoveAll<GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces.IEventPublisher>();
                        services.AddSingleton<GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces.IEventPublisher, MockEventPublisher>();
                        
                        // Replace IStaticHtmlGeneratorService with our mock to avoid requiring Playwright in tests
                        services.RemoveAll<DfE.ExternalApplications.Domain.Services.IStaticHtmlGeneratorService>();
                        services.AddSingleton<DfE.ExternalApplications.Domain.Services.IStaticHtmlGeneratorService, MockStaticHtmlGeneratorService>();
                    },
                    ExternalHttpClientConfiguration = client =>
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "external-mock-token");
                    }
                };

                var client = factory.CreateClient();

                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "ExternalApplicationsApiClient:BaseUrl", client.BaseAddress!.ToString() },
                        { "ExternalApplicationsApiClient:RequestTokenExchange", "false" }
                    })
                    .Build();

                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(config);
                services.AddExternalApplicationsApiClient<IUsersClient, UsersClient>(config, client);
                services.AddExternalApplicationsApiClient<ITemplatesClient, TemplatesClient>(config, client);
                services.AddExternalApplicationsApiClient<ITokensClient, TokensClient>(config, client);
                services.AddExternalApplicationsApiClient<IApplicationsClient, ApplicationsClient>(config, client);
                services.AddExternalApplicationsApiClient<INotificationsClient, NotificationsClient>(config, client);
                services.AddExternalApplicationsApiClient<IUserFeedbackClient, UserFeedbackClient>(config, client);

                services.RemoveAll<IExternalIdentityValidator>();
                services.RemoveAll<IUserTokenService>();

                services.AddTransient<IExternalIdentityValidator, TestExternalIdentityValidator>();
                services.AddUserTokenService(config);

                var serviceProvider = services.BuildServiceProvider();

                fixture.Inject(factory);
                fixture.Inject(serviceProvider);
                fixture.Inject(client);
                fixture.Inject(serviceProvider.GetRequiredService<IUsersClient>());
                fixture.Inject(serviceProvider.GetRequiredService<ITemplatesClient>());
                fixture.Inject(serviceProvider.GetRequiredService<ITokensClient>());
                fixture.Inject(serviceProvider.GetRequiredService<IApplicationsClient>());
                fixture.Inject(serviceProvider.GetRequiredService<INotificationsClient>());
                fixture.Inject(serviceProvider.GetRequiredService<IUserFeedbackClient>());
                fixture.Inject(serviceProvider.GetRequiredService<IExternalIdentityValidator>());
                fixture.Inject(new List<Claim>());

                return factory;
            }));
        }
    }
}
