using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Authorization handler that checks user permission claims for a specific application resource.
    /// </summary>
    public sealed class ApplicationPermissionHandler(
        IHttpContextAccessor accessor)
        : AuthorizationHandler<ApplicationPermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ApplicationPermissionRequirement requirement)
        {
            var applicationId = accessor.HttpContext?.Request.RouteValues["applicationId"]?.ToString();
            if (string.IsNullOrWhiteSpace(applicationId))
                return Task.CompletedTask;

            var expected = $"Application:{applicationId}:{requirement.Action}";
            var hasClaim = context.User.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase));

            var claimStrings = context.User.Claims
                .OrderBy(c => c.Type)
                .Select(c => $"{c.Type}:{c.Value}")
                .ToList();

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
