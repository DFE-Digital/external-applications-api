using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Infrastructure.Services;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MockQueryable;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class DefaultApplicationReferenceProviderTests
{
    /// <summary>
    /// Creates a tenant context with ApplicationReference:Prefix from appsettings (used by DefaultApplicationReferenceProvider).
    /// </summary>
    private static (ITenantContextAccessor Accessor, string Prefix) CreateTenantContextWithPrefix(string prefix)
    {
        var configData = new Dictionary<string, string?> { ["ApplicationReference:Prefix"] = prefix };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        var tenant = new TenantConfiguration(Guid.NewGuid(), "TestTenant", configuration, Array.Empty<string>());
        var accessor = Substitute.For<ITenantContextAccessor>();
        accessor.CurrentTenant.Returns(tenant);
        return (accessor, prefix.ToUpperInvariant());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldGenerateFirstReference_WhenNoApplicationsExist(
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange - prefix from tenant appsettings (ApplicationReference:Prefix)
        var (tenantAccessor, prefix) = CreateTenantContextWithPrefix("TRF");
        var emptyQueryable = Array.Empty<Domain.Entities.Application>().AsQueryable().BuildMock();
        applicationRepo.Query().Returns(emptyQueryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo, tenantAccessor, Substitute.For<ILogger<DefaultApplicationReferenceProvider>>());
        var today = DateTime.UtcNow.Date;

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(reference);
        Assert.StartsWith($"{prefix}-", reference);
        Assert.EndsWith("-001", reference);
        Assert.Equal($"{prefix}-{today:yyyyMMdd}-001", reference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldIncrementNumber_WhenApplicationsExistForToday(
        ApplicationCustomization appCustom,
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange - prefix from tenant appsettings
        var (tenantAccessor, prefix) = CreateTenantContextWithPrefix("TRF");
        var today = DateTime.UtcNow.Date;
        appCustom.OverrideCreatedOn = today;
        appCustom.OverrideReference = $"{prefix}-{today:yyyyMMdd}-002";

        var fixture = new Fixture().Customize(appCustom);
        var existingApp = fixture.Create<Domain.Entities.Application>();

        var queryable = new[] { existingApp }.AsQueryable().BuildMock();
        applicationRepo.Query().Returns(queryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo, tenantAccessor, Substitute.For<ILogger<DefaultApplicationReferenceProvider>>());

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(reference);
        Assert.StartsWith($"{prefix}-", reference);
        Assert.EndsWith("-003", reference);
        Assert.Equal($"{prefix}-{today:yyyyMMdd}-003", reference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldStartFromOne_WhenNoApplicationsExistForToday(
        ApplicationCustomization appCustom,
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange - prefix from tenant appsettings
        var (tenantAccessor, prefix) = CreateTenantContextWithPrefix("TRF");
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        appCustom.OverrideCreatedOn = yesterday;
        appCustom.OverrideReference = $"{prefix}-{yesterday:yyyyMMdd}-002";

        var fixture = new Fixture().Customize(appCustom);
        var existingApp = fixture.Create<Domain.Entities.Application>();

        var queryable = new[] { existingApp }.AsQueryable().BuildMock();
        applicationRepo.Query().Returns(queryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo, tenantAccessor, Substitute.For<ILogger<DefaultApplicationReferenceProvider>>());
        var today = DateTime.UtcNow.Date;

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(reference);
        Assert.StartsWith($"{prefix}-", reference);
        Assert.EndsWith("-001", reference);
        Assert.Equal($"{prefix}-{today:yyyyMMdd}-001", reference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldHandleInvalidReferences_AndStartFromOne(
        ApplicationCustomization appCustom,
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange - prefix from tenant appsettings; existing app has invalid ref format so next is 001
        var (tenantAccessor, prefix) = CreateTenantContextWithPrefix("TRF");
        var today = DateTime.UtcNow.Date;
        appCustom.OverrideCreatedOn = today;
        appCustom.OverrideReference = "INVALID-REF";

        var fixture = new Fixture().Customize(appCustom);
        var existingApp = fixture.Create<Domain.Entities.Application>();

        var queryable = new[] { existingApp }.AsQueryable().BuildMock();
        applicationRepo.Query().Returns(queryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo, tenantAccessor, Substitute.For<ILogger<DefaultApplicationReferenceProvider>>());

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(reference);
        Assert.StartsWith($"{prefix}-", reference);
        Assert.EndsWith("-001", reference);
        Assert.Equal($"{prefix}-{today:yyyyMMdd}-001", reference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldUseDefaultPrefix_WhenTenantHasNoPrefixConfigured(
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange - no ApplicationReference:Prefix in tenant config => default "APP"
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        var tenant = new TenantConfiguration(Guid.NewGuid(), "TestTenant", configuration, Array.Empty<string>());
        var tenantAccessor = Substitute.For<ITenantContextAccessor>();
        tenantAccessor.CurrentTenant.Returns(tenant);

        var emptyQueryable = Array.Empty<Domain.Entities.Application>().AsQueryable().BuildMock();
        applicationRepo.Query().Returns(emptyQueryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo, tenantAccessor, Substitute.For<ILogger<DefaultApplicationReferenceProvider>>());
        var today = DateTime.UtcNow.Date;

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert - default prefix from provider is "APP"
        Assert.NotNull(reference);
        Assert.StartsWith("APP-", reference);
        Assert.EndsWith("-001", reference);
        Assert.Equal($"APP-{today:yyyyMMdd}-001", reference);
    }
} 