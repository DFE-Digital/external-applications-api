using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
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

    private static ClaimsPrincipal CreateUserWithRole(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));
}
