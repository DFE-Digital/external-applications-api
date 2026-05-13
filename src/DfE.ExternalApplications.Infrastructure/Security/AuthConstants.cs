namespace DfE.ExternalApplications.Infrastructure.Security
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

        // Header names & prefixes
        public const string AuthorizationHeader = "Authorization";
        public const string BearerPrefix = "Bearer ";
        public const string ApiKeyHeader = "X-Api-Key";

        // Configuration sections
        public const string ExternalIdpSection = "DfESignIn";
        public const string AzureAdSection = "AzureAd";
    }
}
