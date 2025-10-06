using System;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Settings
{
    public class ApiClientSettings
    {
        public string? BaseUrl { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Authority { get; set; }
        public string? Scope { get; set; }
        public bool RequestTokenExchange { get; set; } = true;
        
        /// <summary>
        /// Enables automatic user registration when a user authenticates with external IDP for the first time.
        /// When true, users who don't exist in the system will be automatically registered during token exchange.
        /// </summary>
        public bool AutoRegisterUsers { get; set; } = true;
        
        /// <summary>
        /// Default template ID to use when auto-registering new users.
        /// This template determines what permissions and access the user will have.
        /// Required when AutoRegisterUsers is true.
        /// </summary>
        public Guid? DefaultTemplateId { get; set; }
    }
}
