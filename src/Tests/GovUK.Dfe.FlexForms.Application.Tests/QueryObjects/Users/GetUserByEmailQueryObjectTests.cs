using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Users.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryObjects.Users;

    public class GetUserByEmailQueryObjectTests
    {
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnMatchingUser_WhenEmailMatches(
        string email)
    {
        // Arrange
        var matchingUser = new Fixture()
            .Customize(new UserCustomization { OverrideEmail = email })
            .Create<User>();

        var role = new Role(matchingUser.RoleId, "Test Role");
        matchingUser.GetType().GetProperty("Role")!.SetValue(matchingUser, role);

        var otherUser1 = new Fixture()
            .Customize(new UserCustomization { OverrideEmail = "other-email-1@test.com" })
            .Create<User>();

        var otherUser2 = new Fixture()
            .Customize(new UserCustomization { OverrideEmail = "other-email-2@test.com" })
            .Create<User>();

        var users = new[] { matchingUser, otherUser1, otherUser2 }.AsQueryable();
        var queryObject = new GetUserByEmailQueryObject(email);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
            Assert.Single(result);
        Assert.Equal(email.ToLowerInvariant(), result[0].Email.ToLowerInvariant());
        Assert.NotNull(result[0].Role);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoUserMatches(
        string email)
    {
        // Arrange
        var user1 = new Fixture()
            .Customize(new UserCustomization { OverrideEmail = "other-email-1@test.com" })
            .Create<User>();

        var user2 = new Fixture()
            .Customize(new UserCustomization { OverrideEmail = "other-email-2@test.com" })
            .Create<User>();

        var users = new[] { user1, user2 }.AsQueryable();
        var queryObject = new GetUserByEmailQueryObject(email);

        // Act
        var result = queryObject.Apply(users).ToList();

        // Assert
        Assert.Empty(result);
    }
}
