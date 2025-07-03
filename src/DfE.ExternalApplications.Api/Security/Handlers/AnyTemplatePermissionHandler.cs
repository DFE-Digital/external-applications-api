using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers;

/// <summary>
/// Authorization handler that succeeds when the user has at least one template read permission claim.
/// </summary>
public sealed class AnyTemplatePermissionHandler : AuthorizationHandler<AnyTemplatePermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyTemplatePermissionRequirement requirement)
    {
        var hasClaim = context.User.Claims.Any(c =>
            c.Type == "permission" &&
            c.Value.StartsWith("Template:", StringComparison.OrdinalIgnoreCase) &&
            c.Value.EndsWith($":{requirement.Action}", StringComparison.OrdinalIgnoreCase));

        if (hasClaim)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}