using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Authorization handler that checks user permission claims for a specific application resource.
    /// </summary>
    public sealed class ApplicationPermissionHandler(
        IHttpContextAccessor accessor,
        ILogger<ApplicationPermissionHandler> _logger)
        : AuthorizationHandler<ApplicationPermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ApplicationPermissionRequirement requirement)
        {
            _logger.LogWarning("ApplicationPermissionHandler > Entry");

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

            _logger.LogWarning("ApplicationPermissionHandler > Expected: {expected}", expected);
            _logger.LogWarning("ApplicationPermissionHandler > Claims : {claimStrings}", claimStrings);


            if (hasClaim)
            {
                _logger.LogWarning("ApplicationPermissionHandler > Has claim");
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
