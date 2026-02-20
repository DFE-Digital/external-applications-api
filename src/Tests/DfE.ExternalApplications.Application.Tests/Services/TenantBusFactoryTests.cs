using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class TenantBusFactoryTests
{
    private readonly ITenantConfigurationProvider _tenantProvider;
    private readonly ILogger<TenantBusFactory> _logger;

    public TenantBusFactoryTests()
    {
        _tenantProvider = Substitute.For<ITenantConfigurationProvider>();
        _logger = Substitute.For<ILogger<TenantBusFactory>>();
    }

    private TenantConfiguration CreateTenant(Guid id, string? serviceBusConnection = null)
    {
        var configData = new Dictionary<string, string?>();
        if (serviceBusConnection != null)
        {
            configData["ConnectionStrings:ServiceBus"] = serviceBusConnection;
        }
        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        return new TenantConfiguration(id, "TestTenant", settings, Array.Empty<string>());
    }

    [Fact]
    public async Task GetBusAsync_ShouldThrowInvalidOperationException_WhenTenantNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantProvider.GetTenant(tenantId).Returns((TenantConfiguration?)null);
        var factory = new TenantBusFactory(_tenantProvider, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.GetBusAsync(tenantId));
    }

    [Fact]
    public async Task GetBusAsync_ShouldReturnInMemoryBus_WhenNoConnectionString()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        _tenantProvider.GetTenant(tenantId).Returns(tenant);
        var factory = new TenantBusFactory(_tenantProvider, _logger);

        // Act
        var bus = await factory.GetBusAsync(tenantId);

        // Assert
        Assert.NotNull(bus);
    }

    [Fact]
    public async Task GetBusAsync_ShouldReturnInMemoryBus_WhenDummyConnectionString()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId, "Endpoint=sb://dummy.servicebus.windows.net/;SharedAccessKey=dummy");
        _tenantProvider.GetTenant(tenantId).Returns(tenant);
        var factory = new TenantBusFactory(_tenantProvider, _logger);

        // Act
        var bus = await factory.GetBusAsync(tenantId);

        // Assert
        Assert.NotNull(bus);
    }

    [Fact]
    public async Task GetBusAsync_ShouldReturnSameBus_WhenCalledTwice()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        _tenantProvider.GetTenant(tenantId).Returns(tenant);
        var factory = new TenantBusFactory(_tenantProvider, _logger);

        // Act
        var bus1 = await factory.GetBusAsync(tenantId);
        var bus2 = await factory.GetBusAsync(tenantId);

        // Assert
        Assert.Same(bus1, bus2);
    }

    [Fact]
    public void HasBus_ShouldReturnFalse_WhenBusNotCreated()
    {
        // Arrange
        var factory = new TenantBusFactory(_tenantProvider, _logger);

        // Act & Assert
        Assert.False(factory.HasBus(Guid.NewGuid()));
    }

    [Fact]
    public async Task HasBus_ShouldReturnTrue_WhenBusCreated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        _tenantProvider.GetTenant(tenantId).Returns(tenant);
        var factory = new TenantBusFactory(_tenantProvider, _logger);

        await factory.GetBusAsync(tenantId);

        // Act & Assert
        Assert.True(factory.HasBus(tenantId));
    }

    [Fact]
    public async Task GetBusAsync_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        var factory = new TenantBusFactory(_tenantProvider, _logger);
        await factory.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            factory.GetBusAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow_WhenCalledMultipleTimes()
    {
        // Arrange
        var factory = new TenantBusFactory(_tenantProvider, _logger);

        // Act & Assert
        await factory.DisposeAsync();
        await factory.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldStopAllBuses()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        _tenantProvider.GetTenant(tenantId).Returns(tenant);
        var factory = new TenantBusFactory(_tenantProvider, _logger);
        await factory.GetBusAsync(tenantId);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => factory.DisposeAsync().AsTask());
        Assert.Null(exception);
    }
}
