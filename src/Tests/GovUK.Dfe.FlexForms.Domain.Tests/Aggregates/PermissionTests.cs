using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Tests.Aggregates;

public class PermissionTests
{
    [Theory]
    [CustomAutoData(typeof(PermissionCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        UserId userId,
        ApplicationId appId,
        string resourceKey,
        ResourceType resourceType,
        AccessType accessType,
        DateTime grantedOn,
        UserId grantedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Permission(
                null!,           // id is null
                userId,
                appId,
                resourceKey,
                resourceType,
                accessType,
                grantedOn,
                grantedBy));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(PermissionCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenResourceKeyIsNull(
        PermissionId id,
        UserId userId,
        ApplicationId appId,
        AccessType accessType,
        ResourceType resourceType,
        DateTime grantedOn,
        UserId grantedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Permission(
                id,
                userId,
                appId,
                null!,           // resourceKey is null
                resourceType,
                accessType,
                grantedOn,
                grantedBy));

        Assert.Equal("resourceKey", ex.ParamName);
    }
}