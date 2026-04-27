using DfE.ExternalApplications.Api.Security;
using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security;

public class AuthorizationExtensionsEntraSsoTests
{
    private const string TestTenantId = "fad277c9-c60a-4da1-b5f3-b3b8b34a82f9";
    private const string TestClientId = "30067021-f76a-405f-8aeb-96d07f5bc080";
    private const string TestInstance = "https://login.microsoftonline.com/";
    private const string TestDfeSignInDiscovery = "https://test-oidc.signin.education.gov.uk/.well-known/openid-configuration";
    private const string TestDfeSignInIssuer = "https://test-oidc.signin.education.gov.uk:443";
    private const string TestDfeSignInClientId = "RSDExternalApps";

    [Fact]
    public void AddCustomAuthorization_WhenEntraSsoEnabled_AddsEntraSsoProvider()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = multiOpts.Providers.FirstOrDefault(p =>
            p.ClientId == TestClientId &&
            p.DiscoveryEndpoint!.Contains(TestTenantId));

        Assert.NotNull(entraProvider);
    }

    [Fact]
    public void AddCustomAuthorization_WhenEntraSsoDisabled_DoesNotAddEntraSsoProvider()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: false,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = multiOpts.Providers.FirstOrDefault(p =>
            p.ClientId == TestClientId &&
            p.DiscoveryEndpoint!.Contains(TestTenantId));

        Assert.Null(entraProvider);
    }

    [Fact]
    public void AddCustomAuthorization_WhenEntraSsoEnabledButTenantIdEmpty_DoesNotAddEntraSsoProvider()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: "",
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProviders = multiOpts.Providers.Where(p =>
            p.ClientId == TestClientId).ToList();

        Assert.DoesNotContain(entraProviders, p =>
            p.DiscoveryEndpoint!.Contains("/.well-known/openid-configuration") &&
            p.DiscoveryEndpoint != TestDfeSignInDiscovery);
    }

    [Fact]
    public void AddCustomAuthorization_EntraSsoProvider_HasCorrectIssuer()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.Equal($"https://login.microsoftonline.com/{TestTenantId}/v2.0", entraProvider!.Issuer);
    }

    [Fact]
    public void AddCustomAuthorization_EntraSsoProvider_HasCorrectAuthority()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.Equal($"https://login.microsoftonline.com/{TestTenantId}/v2.0", entraProvider!.Authority);
    }

    [Fact]
    public void AddCustomAuthorization_EntraSsoProvider_HasCorrectDiscoveryEndpoint()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.Equal(
            $"https://login.microsoftonline.com/{TestTenantId}/v2.0/.well-known/openid-configuration",
            entraProvider!.DiscoveryEndpoint);
    }

    [Fact]
    public void AddCustomAuthorization_EntraSsoProvider_HasCorrectClientId()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.Equal(TestClientId, entraProvider!.ClientId);
    }

    [Fact]
    public void AddCustomAuthorization_EntraSsoProvider_HasThreeValidIssuers()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.NotNull(entraProvider!.ValidIssuers);
        Assert.Equal(3, entraProvider.ValidIssuers!.Count);
        Assert.Contains($"https://login.microsoftonline.com/{TestTenantId}/v2.0", entraProvider.ValidIssuers);
        Assert.Contains($"https://sts.windows.net/{TestTenantId}/", entraProvider.ValidIssuers);
        Assert.Contains($"https://login.microsoftonline.com/{TestTenantId}/v2.0", entraProvider.ValidIssuers);
    }

    [Fact]
    public void AddCustomAuthorization_EntraSsoProvider_HasCorrectValidAudiences()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.NotNull(entraProvider!.ValidAudiences);
        Assert.Equal(2, entraProvider.ValidAudiences!.Count);
        Assert.Contains(TestClientId, entraProvider.ValidAudiences);
        Assert.Contains($"api://{TestClientId}", entraProvider.ValidAudiences);
    }

    [Fact]
    public void AddCustomAuthorization_EntraSsoProvider_HasValidationFlagsSetToTrue()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.True(entraProvider!.ValidateIssuer);
        Assert.True(entraProvider.ValidateAudience);
        Assert.True(entraProvider.ValidateLifetime);
    }

    [Fact]
    public void AddCustomAuthorization_WhenBothDfeSignInAndEntraSsoEnabled_AddsBothProviders()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        Assert.True(multiOpts.Providers.Count >= 2,
            $"Expected at least 2 providers (DfE Sign-In + Entra SSO), but found {multiOpts.Providers.Count}");

        var dfeProvider = multiOpts.Providers.FirstOrDefault(p =>
            p.DiscoveryEndpoint == TestDfeSignInDiscovery);
        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.NotNull(dfeProvider);
        Assert.NotNull(entraProvider);
    }

    [Fact]
    public void AddCustomAuthorization_WhenOnlyDfeSignInConfigured_AddsOnlyDfeSignInProvider()
    {
        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: false,
            entraTenantId: "",
            entraClientId: "");

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var dfeProvider = multiOpts.Providers.FirstOrDefault(p =>
            p.DiscoveryEndpoint == TestDfeSignInDiscovery);

        Assert.NotNull(dfeProvider);
        Assert.Single(multiOpts.Providers);
    }

    [Fact]
    public void AddCustomAuthorization_MultipleTenants_AddsEntraSsoForEachEnabledTenant()
    {
        var secondTenantId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        var secondClientId = "11111111-2222-3333-4444-555555555555";

        var (services, config, tenantProvider) = BuildMultiTenantServiceContext(
            tenant1EntraSsoEnabled: true,
            tenant1EntraTenantId: TestTenantId,
            tenant1EntraClientId: TestClientId,
            tenant2EntraSsoEnabled: true,
            tenant2EntraTenantId: secondTenantId,
            tenant2EntraClientId: secondClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProviders = multiOpts.Providers.Where(p =>
            p.DiscoveryEndpoint != null &&
            p.DiscoveryEndpoint.Contains("v2.0/.well-known/openid-configuration") &&
            p.DiscoveryEndpoint != TestDfeSignInDiscovery).ToList();

        Assert.Equal(2, entraProviders.Count);
        Assert.Contains(entraProviders, p => p.ClientId == TestClientId);
        Assert.Contains(entraProviders, p => p.ClientId == secondClientId);
    }

    [Fact]
    public void AddCustomAuthorization_MultipleTenants_OnlyAddsEntraSsoForEnabledTenants()
    {
        var secondTenantId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        var secondClientId = "11111111-2222-3333-4444-555555555555";

        var (services, config, tenantProvider) = BuildMultiTenantServiceContext(
            tenant1EntraSsoEnabled: true,
            tenant1EntraTenantId: TestTenantId,
            tenant1EntraClientId: TestClientId,
            tenant2EntraSsoEnabled: false,
            tenant2EntraTenantId: secondTenantId,
            tenant2EntraClientId: secondClientId);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProviders = multiOpts.Providers.Where(p =>
            p.DiscoveryEndpoint != null &&
            p.DiscoveryEndpoint.Contains("v2.0/.well-known/openid-configuration") &&
            p.DiscoveryEndpoint != TestDfeSignInDiscovery).ToList();

        Assert.Single(entraProviders);
        Assert.Equal(TestClientId, entraProviders[0].ClientId);
    }

    [Fact]
    public void AddCustomAuthorization_EntraSsoProvider_TrimsTrailingSlashFromInstance()
    {
        var instanceWithSlash = "https://login.microsoftonline.com/";

        var (services, config, tenantProvider) = BuildServiceContext(
            entraSsoEnabled: true,
            entraTenantId: TestTenantId,
            entraClientId: TestClientId,
            entraInstance: instanceWithSlash);

        services.AddCustomAuthorization(config, tenantProvider);

        var sp = services.BuildServiceProvider();
        var multiOpts = sp.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;

        var entraProvider = GetEntraSsoProvider(multiOpts);

        Assert.DoesNotContain("//fad", entraProvider!.Issuer);
        Assert.StartsWith("https://login.microsoftonline.com/fad", entraProvider.Issuer);
    }

    private static OpenIdConnectOptions? GetEntraSsoProvider(MultiProviderOpenIdConnectOptions multiOpts)
    {
        return multiOpts.Providers.FirstOrDefault(p =>
            p.ClientId == TestClientId &&
            p.DiscoveryEndpoint != null &&
            p.DiscoveryEndpoint.Contains(TestTenantId));
    }

    private static (IServiceCollection services, IConfiguration config, ITenantConfigurationProvider tenantProvider)
        BuildServiceContext(
            bool entraSsoEnabled,
            string entraTenantId,
            string entraClientId,
            string entraInstance = "https://login.microsoftonline.com/")
    {
        var tenantSettings = new Dictionary<string, string?>
        {
            ["Authorization:Policies:0:Name"] = "SvcCanRead",
            ["Authorization:Policies:0:Operator"] = "OR",
            ["Authorization:Policies:0:Roles:0"] = "API.Read",
            ["Authorization:TokenSettings:SecretKey"] = "iw5/ivfUWaCpj+n3TihlGUzRVna+KKu8IfLP52GdgNXlDcqt3+N2MM45rwQ=",
            ["Authorization:TokenSettings:Issuer"] = "test-issuer",
            ["Authorization:TokenSettings:Audience"] = "test-audience",
            ["DfESignIn:Authority"] = "https://test-oidc.signin.education.gov.uk",
            ["DfESignIn:ClientId"] = TestDfeSignInClientId,
            ["DfESignIn:Issuer"] = TestDfeSignInIssuer,
            ["DfESignIn:DiscoveryEndpoint"] = TestDfeSignInDiscovery,
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = "common",
            ["AzureAd:ClientId"] = "default-client",
            ["AzureAd:Audience"] = "api://default-client",
            ["EntraSso:Enabled"] = entraSsoEnabled.ToString(),
            ["EntraSso:Instance"] = entraInstance,
            ["EntraSso:TenantId"] = entraTenantId,
            ["EntraSso:ClientId"] = entraClientId,
            ["EntraSso:Scopes:0"] = "openid",
            ["EntraSso:Scopes:1"] = "profile",
            ["EntraSso:Scopes:2"] = "email",
            ["EntraSso:AllowedGroupId"] = ""
        };

        var tenantConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(tenantSettings)
            .Build();

        var tenant = new TenantConfiguration(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "Transfers",
            tenantConfig,
            Array.Empty<string>());

        var tenantProvider = Substitute.For<ITenantConfigurationProvider>();
        tenantProvider.GetAllTenants().Returns(new List<TenantConfiguration> { tenant }.AsReadOnly());

        var rootConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        return (services, rootConfig, tenantProvider);
    }

    private static (IServiceCollection services, IConfiguration config, ITenantConfigurationProvider tenantProvider)
        BuildMultiTenantServiceContext(
            bool tenant1EntraSsoEnabled,
            string tenant1EntraTenantId,
            string tenant1EntraClientId,
            bool tenant2EntraSsoEnabled,
            string tenant2EntraTenantId,
            string tenant2EntraClientId)
    {
        var tenant1Settings = new Dictionary<string, string?>
        {
            ["Authorization:Policies:0:Name"] = "SvcCanRead",
            ["Authorization:Policies:0:Operator"] = "OR",
            ["Authorization:Policies:0:Roles:0"] = "API.Read",
            ["Authorization:TokenSettings:SecretKey"] = "iw5/ivfUWaCpj+n3TihlGUzRVna+KKu8IfLP52GdgNXlDcqt3+N2MM45rwQ=",
            ["Authorization:TokenSettings:Issuer"] = "test-issuer-1",
            ["Authorization:TokenSettings:Audience"] = "test-audience-1",
            ["DfESignIn:Authority"] = "https://test-oidc.signin.education.gov.uk",
            ["DfESignIn:ClientId"] = TestDfeSignInClientId,
            ["DfESignIn:Issuer"] = TestDfeSignInIssuer,
            ["DfESignIn:DiscoveryEndpoint"] = TestDfeSignInDiscovery,
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = "common",
            ["AzureAd:ClientId"] = "default-client",
            ["AzureAd:Audience"] = "api://default-client",
            ["EntraSso:Enabled"] = tenant1EntraSsoEnabled.ToString(),
            ["EntraSso:Instance"] = TestInstance,
            ["EntraSso:TenantId"] = tenant1EntraTenantId,
            ["EntraSso:ClientId"] = tenant1EntraClientId,
            ["EntraSso:AllowedGroupId"] = ""
        };

        var tenant2Settings = new Dictionary<string, string?>
        {
            ["Authorization:Policies:0:Name"] = "SvcCanRead",
            ["Authorization:Policies:0:Operator"] = "OR",
            ["Authorization:Policies:0:Roles:0"] = "API.Read",
            ["Authorization:TokenSettings:SecretKey"] = "iw5/ivfUWaCpj+n3TihlGUzRVna+KKu8IfLP52GdgNXlDcqt3+N2MM45rwQ=",
            ["Authorization:TokenSettings:Issuer"] = "test-issuer-2",
            ["Authorization:TokenSettings:Audience"] = "test-audience-2",
            ["DfESignIn:Authority"] = "https://test-oidc.signin.education.gov.uk",
            ["DfESignIn:ClientId"] = "LsrpClient",
            ["DfESignIn:Issuer"] = TestDfeSignInIssuer,
            ["DfESignIn:DiscoveryEndpoint"] = TestDfeSignInDiscovery,
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = "common",
            ["AzureAd:ClientId"] = "default-client-2",
            ["AzureAd:Audience"] = "api://default-client-2",
            ["EntraSso:Enabled"] = tenant2EntraSsoEnabled.ToString(),
            ["EntraSso:Instance"] = TestInstance,
            ["EntraSso:TenantId"] = tenant2EntraTenantId,
            ["EntraSso:ClientId"] = tenant2EntraClientId,
            ["EntraSso:AllowedGroupId"] = ""
        };

        var tenant1Config = new ConfigurationBuilder()
            .AddInMemoryCollection(tenant1Settings)
            .Build();

        var tenant2Config = new ConfigurationBuilder()
            .AddInMemoryCollection(tenant2Settings)
            .Build();

        var tenant1 = new TenantConfiguration(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "Transfers",
            tenant1Config,
            Array.Empty<string>());

        var tenant2 = new TenantConfiguration(
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            "LSRP",
            tenant2Config,
            Array.Empty<string>());

        var tenantProvider = Substitute.For<ITenantConfigurationProvider>();
        tenantProvider.GetAllTenants().Returns(
            new List<TenantConfiguration> { tenant1, tenant2 }.AsReadOnly());

        var rootConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        return (services, rootConfig, tenantProvider);
    }
}
