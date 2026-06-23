using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class PermissionClaimEvaluatorTests
{
    [Fact]
    public void CanReadAllApplications_ReturnsTrue_ForCaseworkerRole()
    {
        var user = CreateUserWithRole(RoleNames.Caseworker);
        Assert.True(PermissionClaimEvaluator.CanReadAllApplications(user));
    }

    [Fact]
    public void CanWriteApplication_ReturnsFalse_ForCaseworkerWithoutWriteClaim()
    {
        var user = CreateUserWithRole(RoleNames.Caseworker);
        Assert.False(PermissionClaimEvaluator.CanWriteApplication(user, Guid.NewGuid().ToString()));
    }

    [Fact]
    public void CanReadApplication_ReturnsTrue_ForCaseworkerWithoutExplicitApplicationClaim()
    {
        var user = CreateUserWithRole(RoleNames.Caseworker);
        Assert.True(PermissionClaimEvaluator.CanReadApplication(user, Guid.NewGuid().ToString()));
    }

    [Fact]
    public void CanReadApplication_ReturnsFalse_ForStandardUserWithOnlyAnyReadWildcard()
    {
        var user = CreateUserWithPermissionClaims("Application:Any:Read");
        Assert.False(PermissionClaimEvaluator.CanReadApplication(user, Guid.NewGuid().ToString()));
    }

    [Fact]
    public void CanReadApplication_ReturnsTrue_ForStandardUserWithExplicitApplicationReadClaim()
    {
        var applicationId = Guid.NewGuid().ToString();
        var user = CreateUserWithPermissionClaims($"Application:{applicationId}:Read");
        Assert.True(PermissionClaimEvaluator.CanReadApplication(user, applicationId));
    }

    [Fact]
    public void CanReadAllApplications_ReturnsFalse_ForStandardUserWithAnyReadWildcard()
    {
        var user = CreateUserWithPermissionClaims("Application:Any:Read");
        Assert.False(PermissionClaimEvaluator.CanReadAllApplications(user));
    }

    [Fact]
    public void HasAnyExplicitPermissionClaim_ReturnsFalse_WhenOnlyWildcardClaimExists()
    {
        var user = CreateUserWithPermissionClaims("Application:Any:Read");
        Assert.False(PermissionClaimEvaluator.HasAnyExplicitPermissionClaim(user, ResourceType.Application, AccessType.Read));
    }

    [Fact]
    public void HasAnyExplicitPermissionClaim_ReturnsTrue_WhenExplicitApplicationClaimExists()
    {
        var user = CreateUserWithPermissionClaims($"Application:{Guid.NewGuid()}:Read");
        Assert.True(PermissionClaimEvaluator.HasAnyExplicitPermissionClaim(user, ResourceType.Application, AccessType.Read));
    }

    [Fact]
    public void ApplicationAccessResolver_ReturnsAllApplications_ForAdminRole()
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(RoleConstants.AdminRoleId),
            "Admin User",
            "admin@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        user.GetType().GetProperty(nameof(User.Role))!.SetValue(user,
            new Role(new RoleId(RoleConstants.AdminRoleId), RoleNames.Admin));

        var scope = ApplicationAccessResolver.Resolve(user);
        Assert.Equal(ApplicationAccessResolver.AccessMode.AllApplicationsInTenant, scope.Mode);
    }

    [Fact]
    public void ApplicationAccessResolver_ReturnsTemplateScoped_ForCaseworkerWithTemplateRead()
    {
        var userId = new UserId(Guid.NewGuid());
        var templateId = new TemplateId(Guid.NewGuid());
        var user = new User(
            userId,
            new RoleId(RoleConstants.CaseworkerRoleId),
            "Case Worker",
            "case@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        user.GetType().GetProperty(nameof(User.Role))!.SetValue(user,
            new Role(new RoleId(RoleConstants.CaseworkerRoleId), RoleNames.Caseworker));

        var templatePermissions = user.GetType()
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        templatePermissions.SetValue(user, new List<TemplatePermission>
        {
            new(
                new TemplatePermissionId(Guid.NewGuid()),
                userId,
                templateId,
                AccessType.Read,
                DateTime.UtcNow,
                userId)
        });

        var scope = ApplicationAccessResolver.Resolve(user);
        Assert.Equal(ApplicationAccessResolver.AccessMode.TemplateScoped, scope.Mode);
        Assert.Single(scope.TemplateIds);
        Assert.Equal(templateId, scope.TemplateIds.First());
    }

    [Fact]
    public void ApplicationAccessResolver_ReturnsSpecificApplications_ForStandardUserWithTenantWideReadGrant()
    {
        var userId = new UserId(Guid.NewGuid());
        var ownedApplicationId = new ApplicationId(Guid.NewGuid());
        var templateId = new TemplateId(Guid.NewGuid());
        var user = new User(
            userId,
            new RoleId(RoleConstants.UserRoleId),
            "Applicant",
            "user@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        user.GetType().GetProperty(nameof(User.Role))!.SetValue(user,
            new Role(new RoleId(RoleConstants.UserRoleId), RoleNames.User));

        var permissions = user.GetType()
            .GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        permissions.SetValue(user, new List<Permission>
        {
            new(
                new PermissionId(Guid.NewGuid()),
                userId,
                ownedApplicationId,
                ownedApplicationId.Value.ToString(),
                ResourceType.Application,
                AccessType.Read,
                DateTime.UtcNow,
                userId),
            new(
                new PermissionId(Guid.NewGuid()),
                userId,
                null,
                PermissionConstants.AnyResourceKey,
                ResourceType.Application,
                AccessType.Read,
                DateTime.UtcNow,
                userId)
        });

        var templatePermissions = user.GetType()
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        templatePermissions.SetValue(user, new List<TemplatePermission>
        {
            new(
                new TemplatePermissionId(Guid.NewGuid()),
                userId,
                templateId,
                AccessType.Read,
                DateTime.UtcNow,
                userId)
        });

        var scope = ApplicationAccessResolver.Resolve(user);

        Assert.Equal(ApplicationAccessResolver.AccessMode.SpecificApplicationIds, scope.Mode);
        Assert.Single(scope.ApplicationIds);
        Assert.Equal(ownedApplicationId, scope.ApplicationIds.First());
        Assert.Empty(scope.TemplateIds);
    }

    [Fact]
    public void ApplicationAccessResolver_ReturnsEmpty_ForCaseworkerWithoutTemplateRead()
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(RoleConstants.CaseworkerRoleId),
            "Case Worker",
            "case@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        user.GetType().GetProperty(nameof(User.Role))!.SetValue(user,
            new Role(new RoleId(RoleConstants.CaseworkerRoleId), RoleNames.Caseworker));

        var scope = ApplicationAccessResolver.Resolve(user);

        Assert.Equal(ApplicationAccessResolver.AccessMode.SpecificApplicationIds, scope.Mode);
        Assert.Empty(scope.ApplicationIds);
        Assert.Empty(scope.TemplateIds);
    }

    [Fact]
    public void CanListAllApplicationsForTemplate_ReturnsTrue_ForAdmin()
    {
        var templateId = new TemplateId(Guid.NewGuid());
        var user = CreateAdminUser();
        Assert.True(ApplicationAccessResolver.CanListAllApplicationsForTemplate(user, templateId));
    }

    [Fact]
    public void CanListAllApplicationsForTemplate_ReturnsTrue_WhenTemplateIsInScopedAccess()
    {
        var templateId = new TemplateId(Guid.NewGuid());
        var user = CreateCaseworkerWithTemplateRead(templateId);
        Assert.True(ApplicationAccessResolver.CanListAllApplicationsForTemplate(user, templateId));
    }

    [Fact]
    public void CanListAllApplicationsForTemplate_ReturnsFalse_ForCaseworkerWithoutTemplateAccess()
    {
        var allowedTemplateId = new TemplateId(Guid.NewGuid());
        var requestedTemplateId = new TemplateId(Guid.NewGuid());
        var user = CreateCaseworkerWithTemplateRead(allowedTemplateId);
        Assert.False(ApplicationAccessResolver.CanListAllApplicationsForTemplate(user, requestedTemplateId));
    }

    [Fact]
    public void CanListAllApplicationsForTemplate_ReturnsFalse_ForStandardUserWithApplicationOnlyAccess()
    {
        var userId = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(Guid.NewGuid());
        var user = new User(
            userId,
            new RoleId(RoleConstants.UserRoleId),
            "Standard User",
            "user@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        user.GetType().GetProperty(nameof(User.Role))!.SetValue(user,
            new Role(new RoleId(RoleConstants.UserRoleId), RoleNames.User));

        var permissions = user.GetType()
            .GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        permissions.SetValue(user, new List<Permission>
        {
            new(
                new PermissionId(Guid.NewGuid()),
                userId,
                applicationId,
                "Application:Read",
                ResourceType.Application,
                AccessType.Read,
                DateTime.UtcNow,
                userId)
        });

        Assert.False(ApplicationAccessResolver.CanListAllApplicationsForTemplate(user, new TemplateId(Guid.NewGuid())));
    }

    private static User CreateAdminUser()
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(RoleConstants.AdminRoleId),
            "Admin User",
            "admin@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        user.GetType().GetProperty(nameof(User.Role))!.SetValue(user,
            new Role(new RoleId(RoleConstants.AdminRoleId), RoleNames.Admin));

        return user;
    }

    private static User CreateCaseworkerWithTemplateRead(TemplateId templateId)
    {
        var userId = new UserId(Guid.NewGuid());
        var user = new User(
            userId,
            new RoleId(RoleConstants.CaseworkerRoleId),
            "Case Worker",
            "case@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        user.GetType().GetProperty(nameof(User.Role))!.SetValue(user,
            new Role(new RoleId(RoleConstants.CaseworkerRoleId), RoleNames.Caseworker));

        var templatePermissions = user.GetType()
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        templatePermissions.SetValue(user, new List<TemplatePermission>
        {
            new(
                new TemplatePermissionId(Guid.NewGuid()),
                userId,
                templateId,
                AccessType.Read,
                DateTime.UtcNow,
                userId)
        });

        return user;
    }

    private static ClaimsPrincipal CreateUserWithRole(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));

    private static ClaimsPrincipal CreateUserWithPermissionClaims(params string[] permissionValues)
    {
        var claims = permissionValues.Select(v => new Claim(PermissionClaimEvaluator.PermissionClaimType, v));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
