namespace GovUK.Dfe.FlexForms.Infrastructure.Security
{
    public static class AuthConstants
    {
        // Scheme names
        public const string CompositeScheme = "CompositeScheme";

        /// <summary>
        /// Single dynamic JWT bearer scheme used by all bearer-token callers (user JWTs and Entra
        /// service tokens). Issuer/audience/signing-key are resolved per request from
        /// <c>ITenantAuthProviderRegistry</c>.
        /// </summary>
        public const string TenantBearer = "TenantBearer";

        /// <summary>Shared-secret API key scheme.</summary>
        public const string ApiKey = "ApiKey";

        /// <summary>Mutual-TLS / client certificate scheme.</summary>
        public const string Mtls = "Mtls";

        /// <summary>
        /// JWT bearer scheme for platform callers (Entra app-only tokens with platform app roles).
        /// </summary>
        public const string PlatformBearer = "PlatformBearer";

        // Header names & prefixes
        public const string AuthorizationHeader = "Authorization";
        public const string BearerPrefix = "Bearer ";
        public const string ApiKeyHeader = "X-Api-Key";

        // Configuration sections
        public const string ExternalIdpSection = "DfESignIn";
        public const string AzureAdSection = "AzureAd";

        /// <summary>
        /// <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> key used to share the matched
        /// <c>TenantAuthProvider</c> between the JwtBearer / ApiKey / Mtls schemes and the
        /// <c>ServiceCallers</c> authorization policy + <c>TenantClaimsTransformation</c>.
        /// </summary>
        public const string MatchedAuthProviderKey = "GovUK.Dfe.FlexForms.MatchedAuthProvider";

        /// <summary>
        /// Authorization policy for tenant-admin APIs that must be called with an interactive
        /// user JWT (Admin role). Explicitly rejects machine identities (<c>is_service=true</c>),
        /// including Entra client-credentials / API key / mTLS service principals.
        /// </summary>
        public const string TenantAdminUserPolicy = "TenantAdminUser";
    }
}
