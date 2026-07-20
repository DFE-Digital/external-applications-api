using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Users.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryObjects.Users;

public class GetUserByExternalProviderIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnMatchingUser_WhenExternalProviderIdMatches(
        string externalProviderId)
    {
        // Arrange
        var matchingUser = new Fixture()
            .Customize(new UserCustomization { OverrideExternalProviderId = externalProviderId })
            .Create<User>();

        var role = new Role(matchingUser.RoleId, "Test Role");
        matchingUser.GetType().GetProperty("Role")!.SetValue(matchingUser, role);

        var otherUser1 = new Fixture()
            .Customize(new UserCustomization { OverrideExternalProviderId = "other-id-1" })
            .Create<User>();

        var otherUser2 = new Fixture()
            .Customize(new UserCustomization { OverrideExternalProviderId = "other-id-2" })
            .Create<User>();

        var users = new[] { matchingUser, otherUser1, otherUser2 }.AsQueryable();
        var queryObject = new GetUserByExternalProviderIdQueryObject(externalProviderId);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(externalProviderId, result[0].ExternalProviderId);
        Assert.NotNull(result[0].Role);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoUserMatches(
        string externalProviderId)
    {
        // Arrange
        var user1 = new Fixture()
            .Customize(new UserCustomization { OverrideExternalProviderId = "other-id-1" })
            .Create<User>();

        var user2 = new Fixture()
            .Customize(new UserCustomization { OverrideExternalProviderId = "other-id-2" })
            .Create<User>();

        var users = new[] { user1, user2 }.AsQueryable();
        var queryObject = new GetUserByExternalProviderIdQueryObject(externalProviderId);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public void Apply_ShouldReturnEmpty_WhenExternalProviderIdIsNullOrEmpty(
        string externalProviderId)
    {
        // Arrange
        var user1 = new Fixture().Customize(new UserCustomization()).Create<User>();
        var user2 = new Fixture().Customize(new UserCustomization()).Create<User>();

        var users = new[] { user1, user2 }.AsQueryable();
        var queryObject = new GetUserByExternalProviderIdQueryObject(externalProviderId);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Empty(result);
    }
} 