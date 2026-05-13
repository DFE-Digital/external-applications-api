using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Authorization handler for <see cref="ServicePrincipalRequirement"/>. Reads the
    /// <see cref="TenantAuthProvider"/> stashed by the active authentication scheme into
    /// <c>HttpContext.Items[AuthorizationExtensions.MatchedAuthProviderKey]</c> and succeeds
    /// when <see cref="TenantAuthProvider.IsServicePrincipal"/> is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Falls back to inspecting the <c>is_service</c> claim on the principal so providers added
    /// before the registry was wired (or test customizations that don't stash the provider) can
    /// still opt in by emitting the claim explicitly.
    /// </remarks>
    public sealed class ServicePrincipalHandler(IHttpContextAccessor httpContextAccessor)
        : AuthorizationHandler<ServicePrincipalRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ServicePrincipalRequirement requirement)
        {
            var http = httpContextAccessor.HttpContext;
            if (http is not null
                && http.Items.TryGetValue(AuthorizationExtensions.MatchedAuthProviderKey, out var providerObj)
                && providerObj is TenantAuthProvider provider
                && provider.IsServicePrincipal)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Back-compat: principal-claim fallback for schemes that haven't migrated yet.
            if (context.User.HasClaim(c => c.Type == "is_service" && string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
