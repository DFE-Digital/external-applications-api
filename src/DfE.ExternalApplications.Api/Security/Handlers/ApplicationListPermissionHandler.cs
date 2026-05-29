using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers;

/// <summary>
/// Authorization handler that succeeds when the user has at least one application read permission claim.
/// </summary>
public sealed class ApplicationListPermissionHandler : AuthorizationHandler<ApplicationListPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApplicationListPermissionRequirement requirement)
    {
        UserRole userRole = new(context.User);

        // Admin bypass - Admin users have full access
        if (userRole.IsAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Case worker bypass - Case workers have read access
        // TODO check if read requirement?
        if (userRole.IsCaseworker)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var hasClaim = context.User.Claims.Any(c =>
            c.Type == "permission" &&
            c.Value.StartsWith("Application:", StringComparison.OrdinalIgnoreCase) &&
            c.Value.EndsWith($":{requirement.Action}", StringComparison.OrdinalIgnoreCase));

        if (hasClaim)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}