using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class UserCacheInvalidatorTests
{
    [Fact]
    public async Task InvalidateForUserAsync_ShouldRemovePermissionAndListingCacheKeys()
    {
        var cacheService = Substitute.For<ICacheService<IRedisCacheType>>();
        var advancedRedisCacheService = Substitute.For<IAdvancedRedisCacheService>();
        var tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
        tenantContextAccessor.CurrentTenant.Returns(
            new TenantConfiguration(Guid.NewGuid(), "TestTenant", new ConfigurationBuilder().Build(), []));

        var email = "Contributor@Example.com";
        var userId = new UserId(Guid.NewGuid());
        var invalidator = new UserCacheInvalidator(cacheService, advancedRedisCacheService, tenantContextAccessor);

        await invalidator.InvalidateForUserAsync(email, "external-id", userId);

        cacheService.Received(3).Remove(Arg.Any<string>());
        await advancedRedisCacheService.Received(2).RemoveByPatternAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task InvalidateForUserAsync_ShouldRemoveOnlyEmailListingKey_WhenExternalProviderIdMissing()
    {
        var cacheService = Substitute.For<ICacheService<IRedisCacheType>>();
        var advancedRedisCacheService = Substitute.For<IAdvancedRedisCacheService>();
        var tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
        tenantContextAccessor.CurrentTenant.Returns(
            new TenantConfiguration(Guid.NewGuid(), "TestTenant", new ConfigurationBuilder().Build(), []));

        var invalidator = new UserCacheInvalidator(cacheService, advancedRedisCacheService, tenantContextAccessor);

        await invalidator.InvalidateForUserAsync("user@example.com", null, new UserId(Guid.NewGuid()));

        await advancedRedisCacheService.Received(1).RemoveByPatternAsync(Arg.Is<string>(p => p.Contains("Applications_ForUser_")));
    }
}
