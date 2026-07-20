using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.FlexForms.Domain.Services;
using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers;

/// <summary>
/// Authorization handler that succeeds when the user has at least one application read permission claim.
/// </summary>
public sealed class ApplicationListPermissionHandler : AuthorizationHandler<ApplicationListPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApplicationListPermissionRequirement requirement)
    {
        if (requirement.Action.Equals(AccessType.Read.ToString(), StringComparison.OrdinalIgnoreCase)
            && PermissionClaimEvaluator.CanReadAllApplications(context.User))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!Enum.TryParse<AccessType>(requirement.Action, ignoreCase: true, out var accessType))
            return Task.CompletedTask;

        if (PermissionClaimEvaluator.HasAnyExplicitPermissionClaim(
                context.User,
                ResourceType.Application,
                accessType)
            || PermissionClaimEvaluator.HasAnyPermissionClaim(
                context.User,
                ResourceType.Template,
                accessType))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
