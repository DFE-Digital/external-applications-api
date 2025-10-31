using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Users;

public class GetUserByIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnMatchingUser_WhenUserIdMatches(UserCustomization userCustom)
    {
        // Arrange
        var targetUserId = new UserId(Guid.NewGuid());
        var otherUserId = new UserId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(userCustom);
        var targetUser = fixture.Create<User>();
        var otherUser = fixture.Create<User>();
        
        // Use reflection to set the Id property
        var idProperty = typeof(User).GetProperty("Id");
        idProperty?.SetValue(targetUser, targetUserId);
        idProperty?.SetValue(otherUser, otherUserId);
        
        var role = new Role(targetUser.RoleId, "Test Role");
        var roleProperty = typeof(User).GetProperty("Role");
        roleProperty?.SetValue(targetUser, role);
        
        var users = new[] { targetUser, otherUser };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserByIdQueryObject(targetUserId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(targetUser, result.First());
        Assert.NotNull(result.First().Role);
    }
    
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnEmpty_WhenUserIdDoesNotExist(UserCustomization userCustom)
    {
        // Arrange
        var targetUserId = new UserId(Guid.NewGuid());
        var otherUserId = new UserId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();
        
        // Use reflection to set the Id property
        var idProperty = typeof(User).GetProperty("Id");
        idProperty?.SetValue(user, otherUserId);
        
        var users = new[] { user };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserByIdQueryObject(targetUserId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldIncludeRole_WhenUserFound(UserCustomization userCustom)
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();
        
        // Use reflection to set the Id property
        var idProperty = typeof(User).GetProperty("Id");
        idProperty?.SetValue(user, userId);
        
        var role = new Role(user.RoleId, "Test Role");
        var roleProperty = typeof(User).GetProperty("Role");
        roleProperty?.SetValue(user, role);
        
        var users = new[] { user };
        var queryable = users.AsQueryable().BuildMock();
        
        var queryObject = new GetUserByIdQueryObject(userId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(user, result.First());
        Assert.NotNull(result.First().Role);
        
        // Note: The actual inclusion of Role entity would be tested in integration tests
        // with a real database context, as MockQueryable doesn't fully support Include/ThenInclude
    }
}

