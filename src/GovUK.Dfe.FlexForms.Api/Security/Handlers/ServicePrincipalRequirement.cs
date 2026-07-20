using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.FlexForms.Api.Security.Handlers
{
    /// <summary>
    /// Provider-agnostic requirement for the <c>ServiceCallers</c> policy. Replaces the legacy
    /// <c>SvcCanReadWrite</c> policy which assumed an Entra app identity. Any
    /// <see cref="GovUK.Dfe.FlexForms.Domain.Tenancy.TenantAuthProvider"/> whose
    /// <see cref="GovUK.Dfe.FlexForms.Domain.Tenancy.TenantAuthProvider.IsServicePrincipal"/>
    /// is <c>true</c> satisfies this requirement, so Entra apps, signed API keys and mTLS callers
    /// all pass through the same gate.
    /// </summary>
    public sealed class ServicePrincipalRequirement : IAuthorizationRequirement
    {
    }
}
