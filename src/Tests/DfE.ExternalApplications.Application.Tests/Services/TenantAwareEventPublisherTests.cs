using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class TenantAwareEventPublisherTests
{
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly ITenantBusFactory _busFactory;
    private readonly ILogger<TenantAwareEventPublisher> _logger;
    private readonly TenantAwareEventPublisher _publisher;

    public TenantAwareEventPublisherTests()
    {
        _tenantAccessor = Substitute.For<ITenantContextAccessor>();
        _busFactory = Substitute.For<ITenantBusFactory>();
        _logger = Substitute.For<ILogger<TenantAwareEventPublisher>>();
        _publisher = new TenantAwareEventPublisher(_tenantAccessor, _busFactory, _logger);
    }

    private TenantConfiguration CreateTenant(Guid? id = null, string? serviceBusConnection = null)
    {
        var tenantId = id ?? Guid.NewGuid();
        var configData = new Dictionary<string, string?>();
        if (serviceBusConnection != null)
        {
            configData["ConnectionStrings:ServiceBus"] = serviceBusConnection;
        }
        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        return new TenantConfiguration(tenantId, "TestTenant", settings, Array.Empty<string>());
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowInvalidOperationException_WhenNoTenantContext()
    {
        // Arrange
        _tenantAccessor.CurrentTenant.Returns((TenantConfiguration?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _publisher.PublishAsync(new TestMessage("hello")));
    }

    [Fact]
    public async Task PublishAsync_ShouldGetBusFromFactory_WithTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        _tenantAccessor.CurrentTenant.Returns(tenant);

        var bus = Substitute.For<IBus>();
        var sendEndpoint = Substitute.For<ISendEndpoint>();
        bus.GetSendEndpoint(Arg.Any<Uri>()).Returns(sendEndpoint);
        _busFactory.GetBusAsync(tenantId, Arg.Any<CancellationToken>()).Returns(bus);

        // Act
        await _publisher.PublishAsync(new TestMessage("test"));

        // Assert
        await _busFactory.Received(1).GetBusAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldSendMessage_ToSendEndpoint()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        _tenantAccessor.CurrentTenant.Returns(tenant);

        var bus = Substitute.For<IBus>();
        var sendEndpoint = Substitute.For<ISendEndpoint>();
        bus.GetSendEndpoint(Arg.Any<Uri>()).Returns(sendEndpoint);
        _busFactory.GetBusAsync(tenantId, Arg.Any<CancellationToken>()).Returns(bus);

        var message = new TestMessage("content");

        // Act
        await _publisher.PublishAsync(message);

        // Assert
        await sendEndpoint.Received(1).Send(
            Arg.Is<TestMessage>(m => m.Data == "content"),
            Arg.Any<IPipe<SendContext<TestMessage>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithoutProperties_ShouldDelegateToOverload()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        _tenantAccessor.CurrentTenant.Returns(tenant);

        var bus = Substitute.For<IBus>();
        var sendEndpoint = Substitute.For<ISendEndpoint>();
        bus.GetSendEndpoint(Arg.Any<Uri>()).Returns(sendEndpoint);
        _busFactory.GetBusAsync(tenantId, Arg.Any<CancellationToken>()).Returns(bus);

        // Act
        await _publisher.PublishAsync(new TestMessage("data"), CancellationToken.None);

        // Assert
        await sendEndpoint.Received(1).Send(
            Arg.Any<TestMessage>(),
            Arg.Any<IPipe<SendContext<TestMessage>>>(),
            Arg.Any<CancellationToken>());
    }

    public record TestMessage(string Data);
}
