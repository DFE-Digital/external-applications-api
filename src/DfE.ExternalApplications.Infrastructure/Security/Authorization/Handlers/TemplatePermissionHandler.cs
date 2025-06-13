using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization.Handlers
{
    public sealed class TemplatePermissionHandler(IHttpContextAccessor accessor)
        : AuthorizationHandler<TemplatePermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TemplatePermissionRequirement requirement)
        {
            var templateId = accessor.HttpContext?.Request.RouteValues["templateId"]?.ToString();
            if (string.IsNullOrWhiteSpace(templateId))
                return Task.CompletedTask;

            var expected = $"Template:{templateId}:{requirement.Action}";
            var hasClaim = context.User.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase));

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
