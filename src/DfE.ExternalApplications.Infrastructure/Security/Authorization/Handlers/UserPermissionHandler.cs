using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization.Handlers
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
            var email = httpContext?.Request.RouteValues["email"]?.ToString();

            if (string.IsNullOrWhiteSpace(email))
            {
                email = context.User.FindFirstValue(ClaimTypes.Email);
            }

            if (string.IsNullOrWhiteSpace(email))
                return Task.CompletedTask;

            var expected = $"User:{email}:{requirement.Action}";
            var hasClaim = context.User.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase));

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
