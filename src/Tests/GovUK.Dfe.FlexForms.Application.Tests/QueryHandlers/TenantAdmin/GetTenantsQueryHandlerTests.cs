using GovUK.Dfe.FlexForms.Application.TenantAdmin.Queries;
using GovUK.Dfe.FlexForms.Domain.Services;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryHandlers.TenantAdmin;

public class GetTenantsQueryHandlerTests
{
    private readonly ITenantConfigurationProvider _provider = Substitute.For<ITenantConfigurationProvider>();
    private readonly ITenantContextAccessor _tenantContext = Substitute.For<ITenantContextAccessor>();
    private readonly IPermissionCheckerService _permissionChecker = Substitute.For<IPermissionCheckerService>();
    private readonly GetTenantsQueryHandler _handler;

    public GetTenantsQueryHandlerTests()
    {
        _handler = new GetTenantsQueryHandler(_provider, _tenantContext, _permissionChecker);
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyCurrentTenant_EvenWhenManyLoaded()
    {
        var ownId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var otherId = Guid.Parse("22222222-2222-4222-8222-222222222222");

        _permissionChecker.IsInteractiveTenantAdmin().Returns(true);
        _tenantContext.CurrentTenant.Returns(CreateTenant(ownId, "Transfers"));
        _provider.Source.Returns("Database");
        _provider.GetAllTenants().Returns(
        [
            CreateTenant(ownId, "Transfers"),
            CreateTenant(otherId, "Lsrp")
        ]);

        var result = await _handler.Handle(new GetTenantsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TenantCount);
        var tenant = Assert.Single(result.Value.Tenants);
        Assert.Equal(ownId, tenant.Id);
        Assert.Equal("Transfers", tenant.Name);
    }

    [Fact]
    public async Task Handle_ShouldForbid_WhenNotAdmin()
    {
        _permissionChecker.IsInteractiveTenantAdmin().Returns(false);

        var result = await _handler.Handle(new GetTenantsQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCode.Forbidden, result.ErrorCode);
    }

    private static TenantConfiguration CreateTenant(Guid id, string name) =>
        new(id, name, new ConfigurationBuilder().Build(), Array.Empty<string>());
}
