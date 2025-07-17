using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Domain.Factories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetContributorsForApplicationQueryObjectTests
{
    private readonly IUserFactory _userFactory;

    public GetContributorsForApplicationQueryObjectTests()
    {
        _userFactory = Substitute.For<IUserFactory>();
    }

    [Fact]
    public void Apply_WithValidApplicationId_ShouldReturnUsersWithPermissions()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var queryObject = new GetContributorsForApplicationQueryObject(applicationId);

        var users = new List<User>
        {
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 1", "user1@example.com", DateTime.UtcNow, null, null, null),
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 2", "user2@example.com", DateTime.UtcNow, null, null, null),
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 3", "user3@example.com", DateTime.UtcNow, null, null, null)
        };

        // Add permissions to users using factory
        _userFactory.AddPermissionToUser(users[0], applicationId.Value.ToString(), ResourceType.Application, new[] { AccessType.Read }, new UserId(Guid.NewGuid()), applicationId, DateTime.UtcNow);
        _userFactory.AddPermissionToUser(users[1], applicationId.Value.ToString(), ResourceType.Application, new[] { AccessType.Write }, new UserId(Guid.NewGuid()), applicationId, DateTime.UtcNow);
        // User 3 has no permissions for this application

        var query = users.AsQueryable();

        // Act
        var result = queryObject.Apply(query).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Name == "User 1");
        Assert.Contains(result, u => u.Name == "User 2");
        Assert.DoesNotContain(result, u => u.Name == "User 3");
    }

    [Fact]
    public void Apply_WithNoUsers_ShouldReturnEmptyList()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var queryObject = new GetContributorsForApplicationQueryObject(applicationId);
        var users = new List<User>().AsQueryable();

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Apply_WithNoUsersHavingPermissions_ShouldReturnEmptyList()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var queryObject = new GetContributorsForApplicationQueryObject(applicationId);

        var users = new List<User>
        {
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 1", "user1@example.com", DateTime.UtcNow, null, null, null),
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 2", "user2@example.com", DateTime.UtcNow, null, null, null)
        };

        // No users have permissions for this application
        var query = users.AsQueryable();

        // Act
        var result = queryObject.Apply(query).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Apply_WithUsersHavingDifferentResourceTypes_ShouldOnlyReturnApplicationPermissions()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var queryObject = new GetContributorsForApplicationQueryObject(applicationId);

        var users = new List<User>
        {
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 1", "user1@example.com", DateTime.UtcNow, null, null, null),
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 2", "user2@example.com", DateTime.UtcNow, null, null, null)
        };

        // User 1 has Application permission
        _userFactory.AddPermissionToUser(users[0], applicationId.Value.ToString(), ResourceType.Application, new[] { AccessType.Read }, new UserId(Guid.NewGuid()), applicationId, DateTime.UtcNow);
        
        // User 2 has Template permission (should not be included)
        _userFactory.AddPermissionToUser(users[1], "template-id", ResourceType.Template, new[] { AccessType.Read }, new UserId(Guid.NewGuid()), null, DateTime.UtcNow);

        var query = users.AsQueryable();

        // Act
        var result = queryObject.Apply(query).ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, u => u.Name == "User 1");
        Assert.DoesNotContain(result, u => u.Name == "User 2");
    }

    [Fact]
    public void Apply_WithUsersHavingDifferentApplicationIds_ShouldOnlyReturnMatchingApplicationPermissions()
    {
        // Arrange
        var applicationId1 = new ApplicationId(Guid.NewGuid());
        var applicationId2 = new ApplicationId(Guid.NewGuid());
        var queryObject = new GetContributorsForApplicationQueryObject(applicationId1);

        var users = new List<User>
        {
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 1", "user1@example.com", DateTime.UtcNow, null, null, null),
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 2", "user2@example.com", DateTime.UtcNow, null, null, null)
        };

        // User 1 has permission for applicationId1
        _userFactory.AddPermissionToUser(users[0], applicationId1.Value.ToString(), ResourceType.Application, new[] { AccessType.Read }, new UserId(Guid.NewGuid()), applicationId1, DateTime.UtcNow);
        
        // User 2 has permission for applicationId2 (should not be included)
        _userFactory.AddPermissionToUser(users[1], applicationId2.Value.ToString(), ResourceType.Application, new[] { AccessType.Read }, new UserId(Guid.NewGuid()), applicationId2, DateTime.UtcNow);

        var query = users.AsQueryable();

        // Act
        var result = queryObject.Apply(query).ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, u => u.Name == "User 1");
        Assert.DoesNotContain(result, u => u.Name == "User 2");
    }

    [Fact]
    public void Apply_WithUsersHavingMultiplePermissions_ShouldReturnUsersOnce()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var queryObject = new GetContributorsForApplicationQueryObject(applicationId);

        var users = new List<User>
        {
            new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 1", "user1@example.com", DateTime.UtcNow, null, null, null)
        };

        // User has both Read and Write permissions for the same application
        _userFactory.AddPermissionToUser(users[0], applicationId.Value.ToString(), ResourceType.Application, new[] { AccessType.Read }, new UserId(Guid.NewGuid()), applicationId, DateTime.UtcNow);
        _userFactory.AddPermissionToUser(users[0], applicationId.Value.ToString(), ResourceType.Application, new[] { AccessType.Write }, new UserId(Guid.NewGuid()), applicationId, DateTime.UtcNow);

        var query = users.AsQueryable();

        // Act
        var result = queryObject.Apply(query).ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, u => u.Name == "User 1");
    }
} 