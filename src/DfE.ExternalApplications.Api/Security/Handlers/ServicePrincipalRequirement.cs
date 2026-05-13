using Microsoft.AspNetCore.Authorization;

namespace DfE.ExternalApplications.Api.Security.Handlers
{
    /// <summary>
    /// Provider-agnostic requirement for the <c>ServiceCallers</c> policy. Replaces the legacy
    /// <c>SvcCanReadWrite</c> policy which assumed an Entra app identity. Any
    /// <see cref="DfE.ExternalApplications.Domain.Tenancy.TenantAuthProvider"/> whose
    /// <see cref="DfE.ExternalApplications.Domain.Tenancy.TenantAuthProvider.IsServicePrincipal"/>
    /// is <c>true</c> satisfies this requirement, so Entra apps, signed API keys and mTLS callers
    /// all pass through the same gate.
    /// </summary>
    public sealed class ServicePrincipalRequirement : IAuthorizationRequirement
    {
    }
}
