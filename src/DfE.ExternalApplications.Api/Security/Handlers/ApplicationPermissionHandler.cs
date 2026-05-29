using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Services;
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
            if (PermissionClaimEvaluator.HasFullAdminAccess(context.User))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var applicationId = accessor.HttpContext?.Request.RouteValues["applicationId"]?.ToString();
            if (string.IsNullOrWhiteSpace(applicationId))
                return Task.CompletedTask;

            var hasAccess = requirement.Action.Equals(AccessType.Read.ToString(), StringComparison.OrdinalIgnoreCase)
                ? PermissionClaimEvaluator.CanReadApplication(context.User, applicationId)
                : PermissionClaimEvaluator.CanWriteApplication(context.User, applicationId);

            if (hasAccess)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
