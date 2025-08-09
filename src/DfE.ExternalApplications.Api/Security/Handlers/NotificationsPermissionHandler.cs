using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Authorization handler that checks notifications permission claims for a specific user resource.
    /// </summary>
    public sealed class NotificationsPermissionHandler(IHttpContextAccessor accessor)
        : AuthorizationHandler<UserPermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            UserPermissionRequirement requirement)
        {
            var httpContext = accessor.HttpContext;
            var resourceKey = httpContext?.Request.RouteValues["email"]?.ToString();

            if (string.IsNullOrWhiteSpace(resourceKey))
                resourceKey = context.User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(resourceKey))
                resourceKey = context.User.FindFirst("appid")?.Value
                              ?? context.User.FindFirst("azp")?.Value;

            if (string.IsNullOrWhiteSpace(resourceKey))
                return Task.CompletedTask;

            var expected = $"{ResourceType.Notifications}:{resourceKey}:{requirement.Action}";
            var hasClaim = context.User.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase));

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
