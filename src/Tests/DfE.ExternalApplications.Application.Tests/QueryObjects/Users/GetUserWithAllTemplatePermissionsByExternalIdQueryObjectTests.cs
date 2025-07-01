using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Users;

public class GetUserWithAllTemplatePermissionsByExternalIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldReturnMatchingUser_WhenExternalProviderIdMatches(
        string externalProviderId)
    {
        // Arrange
        var matchingUser = new Fixture()
            .Customize(new UserCustomization { OverrideExternalProviderId = externalProviderId })
            .Create<User>();

        var backingField = typeof(User)
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backingField.SetValue(matchingUser, new List<TemplatePermission>());

        var templatePermission = new Fixture()
            .Customize(new TemplatePermissionCustomization())
            .Create<TemplatePermission>();
        ((List<TemplatePermission>)backingField.GetValue(matchingUser)!).Add(templatePermission);

        var otherUser1 = new Fixture()
            .Customize(new UserCustomization { OverrideExternalProviderId = "other-id-1" })
            .Create<User>();

        var otherUser2 = new Fixture()
            .Customize(new UserCustomization { OverrideExternalProviderId = "other-id-2" })
            .Create<User>();

        var users = new[] { matchingUser, otherUser1, otherUser2 }.AsQueryable();
        var queryObject = new GetUserWithAllTemplatePermissionsByExternalIdQueryObject(externalProviderId);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(externalProviderId, result[0].ExternalProviderId);
        Assert.Single(result[0].TemplatePermissions);
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
        var queryObject = new GetUserWithAllTemplatePermissionsByExternalIdQueryObject(externalProviderId);

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
        var queryObject = new GetUserWithAllTemplatePermissionsByExternalIdQueryObject(externalProviderId);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Empty(result);
    }
} 