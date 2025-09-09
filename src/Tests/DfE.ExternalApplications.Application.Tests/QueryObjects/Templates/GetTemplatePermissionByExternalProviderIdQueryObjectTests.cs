using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Templates;

public class GetTemplatePermissionByExternalProviderIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldReturnMatchingPermission_WhenExternalProviderIdAndTemplateIdMatch(
        string externalId,
        TemplatePermissionCustomization tpCustom)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization { OverrideExternalProviderId = externalId }).Create<User>();
        var otherUser = new Fixture().Customize(new UserCustomization { OverrideExternalProviderId = "other-id" }).Create<User>();

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
        var queryObject = new GetTemplatePermissionByExternalProviderIdQueryObject(externalId, template.Id.Value);

        // Act
        var result = queryObject.Apply(permissions).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(externalId, result[0].User!.ExternalProviderId);
        Assert.Equal(template.Id, result[0].Template!.Id);
    }

    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoPermissionsMatch(
        string externalId,
        TemplatePermissionCustomization tpCustom)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization { OverrideExternalProviderId = "other-id" }).Create<User>();

        tpCustom.OverrideTemplateId = template.Id;
        tpCustom.OverrideUserId = user.Id;
        var permission = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.User))!.SetValue(permission, user);
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.Template))!.SetValue(permission, template);

        var permissions = new[] { permission }.AsQueryable();
        var queryObject = new GetTemplatePermissionByExternalProviderIdQueryObject(externalId, template.Id.Value);

        // Act
        var result = queryObject.Apply(permissions).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldReturnEmpty_WhenTemplateIdDoesNotMatch(
        string externalId,
        TemplatePermissionCustomization tpCustom)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var otherTemplate = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization { OverrideExternalProviderId = externalId }).Create<User>();

        tpCustom.OverrideTemplateId = template.Id;
        tpCustom.OverrideUserId = user.Id;
        var permission = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.User))!.SetValue(permission, user);
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.Template))!.SetValue(permission, template);

        var permissions = new[] { permission }.AsQueryable();
        var queryObject = new GetTemplatePermissionByExternalProviderIdQueryObject(externalId, otherTemplate.Id.Value);

        // Act
        var result = queryObject.Apply(permissions).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public void Apply_ShouldIncludeTemplateAndUser_InResults(
        string externalId,
        TemplatePermissionCustomization tpCustom)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization { OverrideExternalProviderId = externalId }).Create<User>();

        tpCustom.OverrideTemplateId = template.Id;
        tpCustom.OverrideUserId = user.Id;
        var permission = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.User))!.SetValue(permission, user);
        typeof(TemplatePermission).GetProperty(nameof(TemplatePermission.Template))!.SetValue(permission, template);

        var permissions = new[] { permission }.AsQueryable();
        var queryObject = new GetTemplatePermissionByExternalProviderIdQueryObject(externalId, template.Id.Value);

        // Act
        var result = queryObject.Apply(permissions).ToList();

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Template);
        Assert.NotNull(result[0].User);
        Assert.Equal(template.Id, result[0].Template!.Id);
        Assert.Equal(user.Id, result[0].User!.Id);
    }
} 