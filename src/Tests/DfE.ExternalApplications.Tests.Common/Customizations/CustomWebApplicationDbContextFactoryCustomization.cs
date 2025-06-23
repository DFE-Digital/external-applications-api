using AutoFixture;
using DfE.CoreLibs.Testing.Mocks.Authentication;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Api;
using DfE.ExternalApplications.Api.Client.Extensions;
using DfE.ExternalApplications.Client;
using DfE.ExternalApplications.Client.Contracts;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Seeders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Security.Claims;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using DfE.CoreLibs.Security.Interfaces;
using DfE.CoreLibs.Security;
using DfE.ExternalApplications.Tests.Common.Helpers;

namespace DfE.ExternalApplications.Tests.Common.Customizations
{
    public class CustomWebApplicationDbContextFactoryCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<CustomWebApplicationDbContextFactory<Program>>(composer => composer.FromFactory(() =>
            {
                var tokenConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "Authorization:TokenSettings:SecretKey", "iw5/ivfUWaCpj+n3TihlGUzRVna+KKu8IfLP52GdgNXlDcqt3+N2MM45rwQ=" },
                        { "Authorization:TokenSettings:Issuer", "21f3ed37-8443-4755-9ed2-c68ca86b4398" },
                        { "Authorization:TokenSettings:Audience", "20dafd6d-79e5-4caf-8b72-d070dcc9716f" },
                        { "Authorization:TokenSettings:TokenLifetimeMinutes", "60" }
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
                                _ => { /* picks up the same factory.TestClaims */ });

                        services.RemoveAll<IExternalIdentityValidator>();
                        services.RemoveAll<IUserTokenService>();

                        services.AddTransient<IExternalIdentityValidator, TestExternalIdentityValidator>();
                        services.AddUserTokenService(tokenConfig);
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
                        { "ApiClient:BaseUrl", client.BaseAddress!.ToString() },
                    })
                    .Build();

                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(config);
                services.AddApiClient<IUsersClient, UsersClient>(config, client);
                services.AddApiClient<ITemplatesClient, TemplatesClient>(config, client);
                services.AddApiClient<ITokensClient, TokensClient>(config, client);

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
                fixture.Inject(serviceProvider.GetRequiredService<IExternalIdentityValidator>());

                fixture.Inject(new List<Claim>());

                return factory;
            }));
        }
    }
}
