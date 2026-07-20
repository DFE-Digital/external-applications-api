using System.Security.Claims;
using GovUK.Dfe.FlexForms.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers;

/// <summary>
/// Succeeds when the authenticated principal has the <see cref="PlatformConstants.TenantConfigReadAppRole"/> app role.
/// </summary>
public sealed class PlatformTenantConfigRoleAuthorizationHandler
    : AuthorizationHandler<PlatformTenantConfigRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformTenantConfigRoleRequirement requirement)
    {
        if (UserHasAppRole(context.User, PlatformConstants.TenantConfigReadAppRole))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool UserHasAppRole(ClaimsPrincipal user, string role)
    {
        foreach (var claim in user.Claims)
        {
            if (!IsRoleClaimType(claim.Type))
            {
                continue;
            }

            if (string.Equals(claim.Value, role, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRoleClaimType(string claimType) =>
        claimType.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("roles", StringComparison.OrdinalIgnoreCase);
}
