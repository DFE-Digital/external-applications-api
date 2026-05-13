using System.Security.Cryptography;
using System.Text;

namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Canonical, framework-agnostic hashing rule for tenant API keys. Both the write path
/// (admin upserting an <c>AuthProviders</c> entry that stores <see cref="TenantAuthProvider.ApiKeyHash"/>)
/// and the read path (the API-key authentication scheme looking the hash up in
/// <see cref="ITenantAuthProviderRegistry.GetByApiKeyHash(string)"/>) MUST use this helper so
/// the two sides cannot drift.
/// <para>
/// Lives in the Domain layer because the hashing scheme is a domain invariant of how
/// <see cref="TenantAuthProvider"/> identifies an API-key caller, not an implementation detail
/// of any specific transport or framework.
/// </para>
/// </summary>
public static class TenantApiKeyHasher
{
    /// <summary>
    /// Produces a lower-case hex digest of the SHA-256 of <paramref name="rawKey"/>. Returns an
    /// empty string when <paramref name="rawKey"/> is null or whitespace so callers can compare
    /// against stored empties without throwing.
    /// </summary>
    /// <param name="rawKey">The raw, plain-text API key value.</param>
    public static string Hash(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
