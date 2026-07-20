using GovUK.Dfe.FlexForms.Application.Common.Pipeline;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GovUK.Dfe.FlexForms.Application.Tests.Common.Pipeline;

public class TenantContextConsumeFilterTests
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider;
    private readonly ILogger<TenantContextConsumeFilter<TestMessage>> _logger;
    private readonly TenantContextConsumeFilter<TestMessage> _filter;

    public TenantContextConsumeFilterTests()
    {
        _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
        _tenantConfigurationProvider = Substitute.For<ITenantConfigurationProvider>();
        _logger = Substitute.For<ILogger<TenantContextConsumeFilter<TestMessage>>>();
        _filter = new TenantContextConsumeFilter<TestMessage>(
            _tenantContextAccessor, _tenantConfigurationProvider, _logger);
    }

    [Fact]
    public async Task Send_ShouldSetCurrentTenant_WhenHeaderResolvesToKnownTenant()
    {
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        _tenantConfigurationProvider.GetTenant(tenantId).Returns(tenant);

        var context = CreateConsumeContext(tenantId.ToString());
        var next = Substitute.For<IPipe<ConsumeContext<TestMessage>>>();

        await _filter.Send(context, next);

        _tenantContextAccessor.Received(1).CurrentTenant = tenant;
        await next.Received(1).Send(context);
    }

    [Fact]
    public async Task Send_ShouldNotSetTenant_WhenHeaderMissing()
    {
        var context = CreateConsumeContext(null);
        var next = Substitute.For<IPipe<ConsumeContext<TestMessage>>>();

        await _filter.Send(context, next);

        _tenantContextAccessor.DidNotReceive().CurrentTenant = Arg.Any<TenantConfiguration>();
        await next.Received(1).Send(context);
    }

    [Fact]
    public async Task Send_ShouldNotSetTenant_WhenHeaderIsNotAGuid()
    {
        var context = CreateConsumeContext("not-a-guid");
        var next = Substitute.For<IPipe<ConsumeContext<TestMessage>>>();

        await _filter.Send(context, next);

        _tenantContextAccessor.DidNotReceive().CurrentTenant = Arg.Any<TenantConfiguration>();
        await next.Received(1).Send(context);
    }

    [Fact]
    public async Task Send_ShouldNotSetTenant_WhenTenantUnknown()
    {
        var tenantId = Guid.NewGuid();
        _tenantConfigurationProvider.GetTenant(tenantId).Returns((TenantConfiguration?)null);

        var context = CreateConsumeContext(tenantId.ToString());
        var next = Substitute.For<IPipe<ConsumeContext<TestMessage>>>();

        await _filter.Send(context, next);

        _tenantContextAccessor.DidNotReceive().CurrentTenant = Arg.Any<TenantConfiguration>();
        await next.Received(1).Send(context);
    }

    private static ConsumeContext<TestMessage> CreateConsumeContext(string? tenantIdHeader)
    {
        var context = Substitute.For<ConsumeContext<TestMessage>>();
        var headers = Substitute.For<Headers>();
        headers.Get<string>("TenantId").Returns(tenantIdHeader);
        context.Headers.Returns(headers);
        context.Message.Returns(new TestMessage("payload"));
        return context;
    }

    private static TenantConfiguration CreateTenant(Guid id)
    {
        var settings = new ConfigurationBuilder().Build();
        return new TenantConfiguration(id, "TestTenant", settings, Array.Empty<string>());
    }

    public record TestMessage(string Data);
}
