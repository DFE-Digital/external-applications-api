using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class UserPermissionCacheInvalidatorTests
{
    [Fact]
    public void InvalidateForUser_ShouldRemoveAllPermissionCacheKeys()
    {
        var cacheService = Substitute.For<ICacheService<IRedisCacheType>>();
        var tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
        tenantContextAccessor.CurrentTenant.Returns(
            new TenantConfiguration(Guid.NewGuid(), "TestTenant", new ConfigurationBuilder().Build(), []));

        var email = "Contributor@Example.com";
        var userId = new UserId(Guid.NewGuid());
        var invalidator = new UserPermissionCacheInvalidator(cacheService, tenantContextAccessor);

        invalidator.InvalidateForUser(email, userId);

        cacheService.Received(3).Remove(Arg.Any<string>());
    }
}
