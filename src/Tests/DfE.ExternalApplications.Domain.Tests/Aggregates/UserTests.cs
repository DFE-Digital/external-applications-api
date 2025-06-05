using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates;

public class UserTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        RoleId roleId,
        string name,
        string email,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new User(
                null!,              // id is null
                roleId,
                name,
                email,
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy,
                null));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenRoleIdIsNull(
        UserId id,
        string name,
        string email,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new User(
                id,
                null!,             // roleId is null
                name,
                email,
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy,
                null));

        Assert.Equal("roleId", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenNameIsNull(
        UserId id,
        RoleId roleId,
        string email,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new User(
                id,
                roleId,
                null!,            // name is null
                email,
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy,
                null));

        Assert.Equal("name", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenEmailIsNull(
        UserId id,
        RoleId roleId,
        string name,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new User(
                id,
                roleId,
                name,
                null!,            // email is null
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy,
                null));

        Assert.Equal("email", ex.ParamName);
    }
}