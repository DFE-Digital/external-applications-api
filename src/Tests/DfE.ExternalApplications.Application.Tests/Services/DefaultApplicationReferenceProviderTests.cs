using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Infrastructure.Services;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class DefaultApplicationReferenceProviderTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldGenerateFirstReference_WhenNoApplicationsExist(
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange
        var emptyQueryable = Array.Empty<Domain.Entities.Application>().AsQueryable().BuildMock();
        applicationRepo.Query().Returns(emptyQueryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo);
        var today = DateTime.UtcNow.Date;

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(reference);
        Assert.StartsWith("APP-", reference);
        Assert.EndsWith("-001", reference);
        Assert.Equal($"APP-{today:yyyyMMdd}-001", reference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldIncrementNumber_WhenApplicationsExistForToday(
        ApplicationCustomization appCustom,
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        appCustom.OverrideCreatedOn = today;
        appCustom.OverrideReference = $"APP-{today:yyyyMMdd}-002";

        var fixture = new Fixture().Customize(appCustom);
        var existingApp = fixture.Create<Domain.Entities.Application>();

        var queryable = new[] { existingApp }.AsQueryable().BuildMock();
        applicationRepo.Query().Returns(queryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo);

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(reference);
        Assert.StartsWith("APP-", reference);
        Assert.EndsWith("-003", reference);
        Assert.Equal($"APP-{today:yyyyMMdd}-003", reference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldStartFromOne_WhenNoApplicationsExistForToday(
        ApplicationCustomization appCustom,
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        appCustom.OverrideCreatedOn = yesterday;
        appCustom.OverrideReference = $"APP-{yesterday:yyyyMMdd}-002";

        var fixture = new Fixture().Customize(appCustom);
        var existingApp = fixture.Create<Domain.Entities.Application>();

        var queryable = new[] { existingApp }.AsQueryable().BuildMock();
        applicationRepo.Query().Returns(queryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo);
        var today = DateTime.UtcNow.Date;

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(reference);
        Assert.StartsWith("APP-", reference);
        Assert.EndsWith("-001", reference);
        Assert.Equal($"APP-{today:yyyyMMdd}-001", reference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task GenerateReferenceAsync_ShouldHandleInvalidReferences_AndStartFromOne(
        ApplicationCustomization appCustom,
        IEaRepository<Domain.Entities.Application> applicationRepo)
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        appCustom.OverrideCreatedOn = today;
        appCustom.OverrideReference = "INVALID-REF";

        var fixture = new Fixture().Customize(appCustom);
        var existingApp = fixture.Create<Domain.Entities.Application>();

        var queryable = new[] { existingApp }.AsQueryable().BuildMock();
        applicationRepo.Query().Returns(queryable);

        var provider = new DefaultApplicationReferenceProvider(applicationRepo);

        // Act
        var reference = await provider.GenerateReferenceAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(reference);
        Assert.StartsWith("APP-", reference);
        Assert.EndsWith("-001", reference);
        Assert.Equal($"APP-{today:yyyyMMdd}-001", reference);
    }
} 