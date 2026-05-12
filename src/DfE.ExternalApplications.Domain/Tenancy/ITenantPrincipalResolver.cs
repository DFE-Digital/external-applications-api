namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Resolves an authenticated principal (e.g. a Managed Identity object id from a JWT 'oid' claim)
/// to the tenant it is authorised to act on behalf of. Returns null if the principal is not registered.
/// </summary>
public interface ITenantPrincipalResolver
{
    /// <summary>
    /// Looks up the tenant a principal is registered under.
    /// Only active principals (IsActive = true) are returned.
    /// </summary>
    /// <param name="principalObjectId">The principal id, typically the AAD 'oid' claim.</param>
    Task<TenantPrincipalResolution?> ResolveAsync(
        string principalObjectId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The outcome of resolving a principal to a tenant.
/// </summary>
public sealed record TenantPrincipalResolution(
    Guid TenantId,
    string TenantName,
    string PrincipalType);
