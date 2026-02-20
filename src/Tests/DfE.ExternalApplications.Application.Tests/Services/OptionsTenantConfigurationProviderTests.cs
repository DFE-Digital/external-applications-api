using DfE.ExternalApplications.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class OptionsTenantConfigurationProviderTests
{
    private IConfiguration BuildConfiguration(Dictionary<string, string?> data)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    [Fact]
    public void GetTenant_ShouldReturnTenant_WhenIdExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = tenantId.ToString(),
            ["Tenants:0:Name"] = "TenantA"
        });
        var provider = new OptionsTenantConfigurationProvider(config);

        // Act
        var tenant = provider.GetTenant(tenantId);

        // Assert
        Assert.NotNull(tenant);
        Assert.Equal(tenantId, tenant.Id);
        Assert.Equal("TenantA", tenant.Name);
    }

    [Fact]
    public void GetTenant_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = Guid.NewGuid().ToString(),
            ["Tenants:0:Name"] = "TenantA"
        });
        var provider = new OptionsTenantConfigurationProvider(config);

        // Act
        var tenant = provider.GetTenant(tenantId);

        // Assert
        Assert.Null(tenant);
    }

    [Fact]
    public void GetAllTenants_ShouldReturnAllConfiguredTenants()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = tenant1Id.ToString(),
            ["Tenants:0:Name"] = "TenantA",
            ["Tenants:1:Id"] = tenant2Id.ToString(),
            ["Tenants:1:Name"] = "TenantB"
        });
        var provider = new OptionsTenantConfigurationProvider(config);

        // Act
        var tenants = provider.GetAllTenants();

        // Assert
        Assert.Equal(2, tenants.Count);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTenantIdIsMissing()
    {
        // Arrange
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Name"] = "TenantA"
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OptionsTenantConfigurationProvider(config));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTenantIdIsInvalidGuid()
    {
        // Arrange
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = "not-a-guid",
            ["Tenants:0:Name"] = "TenantA"
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OptionsTenantConfigurationProvider(config));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTenantNameIsMissing()
    {
        // Arrange
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = Guid.NewGuid().ToString()
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OptionsTenantConfigurationProvider(config));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDuplicateTenantIds()
    {
        // Arrange
        var sameId = Guid.NewGuid();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = sameId.ToString(),
            ["Tenants:0:Name"] = "TenantA",
            ["Tenants:1:Id"] = sameId.ToString(),
            ["Tenants:1:Name"] = "TenantB"
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OptionsTenantConfigurationProvider(config));
    }

    [Fact]
    public void GetTenant_ShouldResolveFrontendOrigins_FromConfiguration()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = tenantId.ToString(),
            ["Tenants:0:Name"] = "TenantA",
            ["Tenants:0:Frontend:Origins:0"] = "https://app1.example.com",
            ["Tenants:0:Frontend:Origins:1"] = "https://app2.example.com"
        });
        var provider = new OptionsTenantConfigurationProvider(config);

        // Act
        var tenant = provider.GetTenant(tenantId);

        // Assert
        Assert.NotNull(tenant);
        Assert.Equal(2, tenant.FrontendOrigins.Length);
        Assert.Contains("https://app1.example.com", tenant.FrontendOrigins);
        Assert.Contains("https://app2.example.com", tenant.FrontendOrigins);
    }

    [Fact]
    public void GetTenant_ShouldResolveSingleOrigin_FromConfiguration()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = tenantId.ToString(),
            ["Tenants:0:Name"] = "TenantA",
            ["Tenants:0:Frontend:Origin"] = "https://app.example.com"
        });
        var provider = new OptionsTenantConfigurationProvider(config);

        // Act
        var tenant = provider.GetTenant(tenantId);

        // Assert
        Assert.NotNull(tenant);
        Assert.Single(tenant.FrontendOrigins);
        Assert.Equal("https://app.example.com", tenant.FrontendOrigins[0]);
    }

    [Fact]
    public void GetTenant_ShouldFlattenSettings_ForTenantConsumers()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Tenants:0:Id"] = tenantId.ToString(),
            ["Tenants:0:Name"] = "TenantA",
            ["Tenants:0:SomeCustomKey"] = "CustomValue"
        });
        var provider = new OptionsTenantConfigurationProvider(config);

        // Act
        var tenant = provider.GetTenant(tenantId);

        // Assert
        Assert.NotNull(tenant);
        Assert.Equal("CustomValue", tenant.Settings.GetValue<string>("SomeCustomKey"));
    }

    [Fact]
    public void GetAllTenants_ShouldReturnEmpty_WhenNoTenantsConfigured()
    {
        // Arrange
        var config = BuildConfiguration(new Dictionary<string, string?>());
        var provider = new OptionsTenantConfigurationProvider(config);

        // Act
        var tenants = provider.GetAllTenants();

        // Assert
        Assert.Empty(tenants);
    }
}
