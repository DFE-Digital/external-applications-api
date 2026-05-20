using DfE.ExternalApplications.Application.TenantConfig.Queries;
using DfE.ExternalApplications.Domain.Tenancy;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.TenantConfig;

public class GetPlatformTenantConfigurationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTenantConfiguration_WhenTenantExists()
    {
        var tenantId = Guid.NewGuid();
        var reader = Substitute.For<ITenantSettingsReader>();
        reader.GetConfigurationAsync(tenantId, "Web", Arg.Any<CancellationToken>())
            .Returns(new TenantConfigurationSnapshot(
                tenantId,
                "Transfers",
                DateTime.UtcNow,
                new Dictionary<string, string?> { ["DfESignIn:ClientId"] = "test" }));

        var handler = new GetPlatformTenantConfigurationQueryHandler(reader);

        var result = await handler.Handle(
            new GetPlatformTenantConfigurationQuery(tenantId, "Web"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenantId, result.Value!.TenantId);
        Assert.Equal("test", result.Value.Configuration["DfESignIn:ClientId"]);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTenantMissing()
    {
        var tenantId = Guid.NewGuid();
        var reader = Substitute.For<ITenantSettingsReader>();
        reader.GetConfigurationAsync(tenantId, "Web", Arg.Any<CancellationToken>())
            .Returns((TenantConfigurationSnapshot?)null);

        var handler = new GetPlatformTenantConfigurationQueryHandler(reader);

        var result = await handler.Handle(
            new GetPlatformTenantConfigurationQuery(tenantId, "Web"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
