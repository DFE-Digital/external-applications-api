using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Authorization handler that checks user permission claims for a specific user resource.
    /// </summary>
    public sealed class UserPermissionHandler(IHttpContextAccessor accessor)
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

            var expected = $"User:{resourceKey}:{requirement.Action}";
            var hasClaim = context.User.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase));

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
