using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Authorization handler that checks user permission claims for a specific file resource (by fileId).
    /// </summary>
    public sealed class FilePermissionHandler(IHttpContextAccessor accessor)
        : AuthorizationHandler<FilePermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            FilePermissionRequirement requirement)
        {
            var fileId = accessor.HttpContext?.Request.RouteValues["fileId"]?.ToString();
            if (string.IsNullOrWhiteSpace(fileId))
                return Task.CompletedTask;

            var expected = $"{ResourceType.File}:{fileId}:{requirement.Action}";
            var hasClaim = context.User.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase));

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
} 