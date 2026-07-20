using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.FlexForms.Domain.Services;
using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers
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
            if (PermissionClaimEvaluator.HasFullAdminAccess(context.User))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var applicationId = accessor.HttpContext?.Request.RouteValues["applicationId"]?.ToString();
            if (string.IsNullOrWhiteSpace(applicationId))
                return Task.CompletedTask;

            var hasAccess = requirement.Action switch
            {
                var action when action.Equals(AccessType.Read.ToString(), StringComparison.OrdinalIgnoreCase)
                    => PermissionClaimEvaluator.CanReadApplicationFiles(context.User, applicationId),
                var action when action.Equals(AccessType.Write.ToString(), StringComparison.OrdinalIgnoreCase)
                    => PermissionClaimEvaluator.CanWriteApplicationFiles(context.User, applicationId),
                var action when action.Equals(AccessType.Delete.ToString(), StringComparison.OrdinalIgnoreCase)
                    => PermissionClaimEvaluator.CanDeleteApplicationFiles(context.User, applicationId),
                _ => false
            };

            if (hasAccess)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
