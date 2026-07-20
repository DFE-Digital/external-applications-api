using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryObjects.Templates;

public class GetTemplatePermissionByUserIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldReturnMatchingPermission_WhenUserIdAndTemplateIdMatch(
        UserId userId,
        TemplatePermissionCustomization tpCustom)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization()).Create<User>();
        var otherUser = new Fixture().Customize(new UserCustomization()).Create<User>();

        tpCustom.OverrideTemplateId = template.Id;
        tpCustom.OverrideUserId = user.Id;
        var matchingPermission = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.User))!.SetValue(matchingPermission, user);
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.Template))!.SetValue(matchingPermission, template);

        tpCustom.OverrideUserId = otherUser.Id;
        var otherPermission = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.User))!.SetValue(otherPermission, otherUser);
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.Template))!.SetValue(otherPermission, template);

        var permissions = new[] { matchingPermission, otherPermission }.AsQueryable();
        var queryObject = new GetTemplatePermissionByUserIdQueryObject(user.Id, template.Id.Value);

        // Act
        var result = queryObject.Apply(permissions).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(user.Id, result[0].User!.Id);
        Assert.Equal(template.Id, result[0].Template!.Id);
    }

    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoPermissionsMatch(
        UserId userId,
        TemplatePermissionCustomization tpCustom)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization()).Create<User>();

        tpCustom.OverrideTemplateId = template.Id;
        tpCustom.OverrideUserId = user.Id;
        var permission = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.User))!.SetValue(permission, user);
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.Template))!.SetValue(permission, template);

        var permissions = new[] { permission }.AsQueryable();
        var queryObject = new GetTemplatePermissionByUserIdQueryObject(userId, Guid.NewGuid()); // Different template ID

        // Act
        var result = queryObject.Apply(permissions).ToList();

        // Assert
        Assert.Empty(result);
    }
} 