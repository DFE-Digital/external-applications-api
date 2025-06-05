using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates;

public class PermissionTests
{
    [Theory]
    [CustomAutoData(typeof(PermissionCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        UserId userId,
        ApplicationId appId,
        string resourceKey,
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
                accessType,
                grantedOn,
                grantedBy));

        Assert.Equal("resourceKey", ex.ParamName);
    }
}