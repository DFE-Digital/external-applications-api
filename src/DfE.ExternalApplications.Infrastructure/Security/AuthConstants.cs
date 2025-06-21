namespace DfE.ExternalApplications.Infrastructure.Security
{
    public static class AuthConstants
    {
        // Scheme names
        public const string UserScheme = "UserScheme";
        public const string AzureAdScheme = "AzureEntra";

        // Header names & prefixes
        public const string AuthorizationHeader = "Authorization";
        public const string ServiceAuthHeader = "X-Service-Authorization";
        public const string BearerPrefix = "Bearer ";

        // Configuration sections
        public const string ExternalIdpSection = "DfESignIn";
        public const string AzureAdSection = "AzureAd";
    }
}
