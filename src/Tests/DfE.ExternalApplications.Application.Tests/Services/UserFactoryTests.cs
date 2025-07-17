using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Domain.Factories;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Events;
using Xunit;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class UserFactoryTests
{
    private readonly IUserFactory _userFactory;

    public UserFactoryTests()
    {
        _userFactory = new UserFactory();
    }

    [Fact]
    public void CreateContributor_WithValidParameters_ShouldCreateUserWithCorrectProperties()
    {
        // Arrange
        var id = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var name = "John Doe";
        var email = "test@example.com";
        var createdBy = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());
        var createdOn = DateTime.UtcNow;

        // Act
        var result = _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId, createdOn);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(roleId, result.RoleId);
        Assert.Equal(name, result.Name);
        Assert.Equal(email, result.Email);
        Assert.Equal(createdOn, result.CreatedOn);
        Assert.Equal(createdBy, result.CreatedBy);
        Assert.Single(result.DomainEvents);
        Assert.Contains(result.DomainEvents, e => e is ContributorAddedEvent);
    }

    [Fact]
    public void CreateContributor_WithNullId_ShouldThrowArgumentException()
    {
        // Arrange
        UserId id = null!;
        var roleId = new RoleId(Guid.NewGuid());
        var name = "John Doe";
        var email = "test@example.com";
        var createdBy = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId));
        Assert.Contains("id", exception.Message);
    }

    [Fact]
    public void CreateContributor_WithNullRoleId_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new UserId(Guid.NewGuid());
        RoleId roleId = null!;
        var name = "John Doe";
        var email = "test@example.com";
        var createdBy = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId));
        Assert.Contains("roleId", exception.Message);
    }

    [Fact]
    public void CreateContributor_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        string name = null!;
        var email = "test@example.com";
        var createdBy = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId));
        Assert.Contains("name", exception.Message);
    }

    [Fact]
    public void CreateContributor_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var name = "";
        var email = "test@example.com";
        var createdBy = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId));
        Assert.Contains("name", exception.Message);
    }

    [Fact]
    public void CreateContributor_WithNullEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var name = "John Doe";
        string email = null!;
        var createdBy = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId));
        Assert.Contains("email", exception.Message);
    }

    [Fact]
    public void CreateContributor_WithEmptyEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var name = "John Doe";
        var email = "";
        var createdBy = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId));
        Assert.Contains("email", exception.Message);
    }

    [Fact]
    public void CreateContributor_WithNullCreatedBy_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var name = "John Doe";
        var email = "test@example.com";
        UserId createdBy = null!;
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId));
        Assert.Contains("createdBy", exception.Message);
    }

    [Fact]
    public void CreateContributor_WithNullApplicationId_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var name = "John Doe";
        var email = "test@example.com";
        var createdBy = new UserId(Guid.NewGuid());
        ApplicationId applicationId = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.CreateContributor(id, roleId, name, email, createdBy, applicationId));
        Assert.Contains("applicationId", exception.Message);
    }

    [Fact]
    public void AddPermissionToUser_WithValidParameters_ShouldAddPermissions()
    {
        // Arrange
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var resourceKey = "test-resource";
        var resourceType = ResourceType.Application;
        var accessTypes = new[] { AccessType.Read, AccessType.Write };
        var grantedBy = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());

        // Act
        _userFactory.AddPermissionToUser(user, resourceKey, resourceType, accessTypes, grantedBy, applicationId);

        // Assert
        Assert.Equal(2, user.Permissions.Count);
        Assert.Contains(user.Permissions, p => p.ResourceKey == resourceKey && p.AccessType == AccessType.Read);
        Assert.Contains(user.Permissions, p => p.ResourceKey == resourceKey && p.AccessType == AccessType.Write);
    }

    [Fact]
    public void AddPermissionToUser_WithNullUser_ShouldThrowArgumentException()
    {
        // Arrange
        User user = null!;
        var resourceKey = "test-resource";
        var resourceType = ResourceType.Application;
        var accessTypes = new[] { AccessType.Read };
        var grantedBy = new UserId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.AddPermissionToUser(user, resourceKey, resourceType, accessTypes, grantedBy));
        Assert.Contains("user", exception.Message);
    }

    [Fact]
    public void AddPermissionToUser_WithEmptyAccessTypes_ShouldNotAddAnyPermissions()
    {
        // Arrange
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var resourceKey = "test-resource";
        var resourceType = ResourceType.Application;
        var accessTypes = new AccessType[0];
        var grantedBy = new UserId(Guid.NewGuid());

        // Act
        _userFactory.AddPermissionToUser(user, resourceKey, resourceType, accessTypes, grantedBy);

        // Assert
        Assert.Empty(user.Permissions);
    }

    [Fact]
    public void AddPermissionToUser_WithNullAccessTypes_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var resourceKey = "test-resource";
        var resourceType = ResourceType.Application;
        AccessType[] accessTypes = null!;
        var grantedBy = new UserId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.AddPermissionToUser(user, resourceKey, resourceType, accessTypes, grantedBy));
        Assert.Contains("accessTypes", exception.Message);
    }

    [Fact]
    public void AddPermissionToUser_WithNullResourceKey_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        string resourceKey = null!;
        var resourceType = ResourceType.Application;
        var accessTypes = new[] { AccessType.Read };
        var grantedBy = new UserId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.AddPermissionToUser(user, resourceKey, resourceType, accessTypes, grantedBy));
        Assert.Contains("resourceKey", exception.Message);
    }

    [Fact]
    public void AddPermissionToUser_WithEmptyResourceKey_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var resourceKey = "";
        var resourceType = ResourceType.Application;
        var accessTypes = new[] { AccessType.Read };
        var grantedBy = new UserId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.AddPermissionToUser(user, resourceKey, resourceType, accessTypes, grantedBy));
        Assert.Contains("resourceKey", exception.Message);
    }

    [Fact]
    public void AddPermissionToUser_WithNullGrantedBy_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var resourceKey = "test-resource";
        var resourceType = ResourceType.Application;
        var accessTypes = new[] { AccessType.Read };
        UserId grantedBy = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _userFactory.AddPermissionToUser(user, resourceKey, resourceType, accessTypes, grantedBy));
        Assert.Contains("grantedBy", exception.Message);
    }
} 