using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Authorization handler that checks user permission claims for application files resource 
    /// </summary>
    public sealed class ApplicationFilesPermissionHandler(IHttpContextAccessor accessor)
        : AuthorizationHandler<ApplicationFilesPermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ApplicationFilesPermissionRequirement requirement)
        {
            var applicationId = accessor.HttpContext?.Request.RouteValues["applicationId"]?.ToString();
            if (string.IsNullOrWhiteSpace(applicationId))
                return Task.CompletedTask;

            var expected = $"{ResourceType.ApplicationFiles}:{applicationId}:{requirement.Action}";
            var hasClaim = context.User.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase));

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
} 