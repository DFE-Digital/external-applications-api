namespace GovUK.Dfe.FlexForms.Domain.Tenancy;

/// <summary>
/// Canonical claim type strings stamped on principals that authenticate via a
/// <see cref="TenantAuthProvider"/>. Keeping these in the Domain layer ensures Application handlers,
/// Infrastructure projectors, and the API host all share one vocabulary for tenant-scoped identity.
/// </summary>
public static class TenantAuthClaimTypes
{
    /// <summary>Logical tenant identifier (GUID string) for the current request principal.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>
    /// Machine-readable flag for service principals: <c>"true"</c> or <c>"false"</c> string values
    /// (JWT and some IdPs use string booleans).
    /// </summary>
    public const string IsService = "is_service";

    /// <summary>Tenant-local logical name of the <see cref="TenantAuthProvider"/> that authenticated the caller.</summary>
    public const string AuthProvider = "auth_provider";

    /// <summary>Common Entra / Azure AD directory (tenant) id claim; used only as a fallback when resolving <see cref="TenantId"/>.</summary>
    public const string AzureDirectoryTenantId = "tid";

    /// <summary>Alternate email-style claim used by some providers when <see cref="System.Security.Claims.ClaimTypes.Email"/> is absent.</summary>
    public const string Email = "email";

    /// <summary>Often holds UPN or email in Entra/OIDC tokens.</summary>
    public const string PreferredUsername = "preferred_username";
}
