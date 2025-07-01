using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Users;

public class GetUserWithAllPermissionsByUserIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnMatchingUser_WhenUserIdMatches(
        UserId userId)
    {
        // Arrange
        var matchingUser = new Fixture()
            .Customize(new UserCustomization { OverrideId = userId })
            .Create<User>();

        var otherUser1 = new Fixture()
            .Customize(new UserCustomization())
            .Create<User>();

        var otherUser2 = new Fixture()
            .Customize(new UserCustomization())
            .Create<User>();

        var users = new[] { matchingUser, otherUser1, otherUser2 }.AsQueryable();
        var queryObject = new GetUserWithAllPermissionsByUserIdQueryObject(userId);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(userId, result[0].Id);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoUserMatches(
        UserId userId)
    {
        // Arrange
        var user1 = new Fixture()
            .Customize(new UserCustomization())
            .Create<User>();

        var user2 = new Fixture()
            .Customize(new UserCustomization())
            .Create<User>();

        var users = new[] { user1, user2 }.AsQueryable();
        var queryObject = new GetUserWithAllPermissionsByUserIdQueryObject(userId);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Empty(result);
    }
} 