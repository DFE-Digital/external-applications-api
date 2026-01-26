using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace DfE.ExternalApplications.Application.Services
{
    public class InternalAuthRequestChecker(
        ITenantContextAccessor tenantContextAccessor,
        ILogger<InternalAuthRequestChecker> logger)
        : ICustomRequestChecker
    {
        private const string ServiceEmailHeaderKey = "x-service-email";
        private const string ServiceApiHeaderKey = "x-service-api-key";

        /// <summary>
        /// Validates if the current HTTP request is a valid internal request
        /// </summary>
        /// <param name="httpContext">The HTTP context to validate</param>
        /// <returns>True if this is a valid request with correct headers and secret</returns>
        public bool IsValidRequest(HttpContext httpContext)
        {
            // Get tenant-specific InternalServiceAuth configuration
            var tenant = tenantContextAccessor.CurrentTenant;
            if (tenant == null)
            {
                logger.LogWarning("No tenant context available for internal auth validation");
                return false;
            }
            
            var config = new InternalServiceAuthOptions();
            tenant.Settings.GetSection("InternalServiceAuth").Bind(config);
            
            if (config.Services == null || !config.Services.Any())
            {
                logger.LogWarning("No internal service auth configuration found for tenant: {TenantName}", tenant.Name);
                return false;
            }
            
            // Check for email header
            var serviceEmail = httpContext.Request.Headers[ServiceEmailHeaderKey].ToString();
            var serviceApiKey = httpContext.Request.Headers[ServiceApiHeaderKey].ToString();

            var serviceConfig = config.Services
                .FirstOrDefault(s => s.Email.Equals(serviceEmail, StringComparison.OrdinalIgnoreCase));

            if (serviceConfig == null)
            {
                logger.LogDebug("Service email not found in configuration for tenant {TenantName}: {Email}", tenant.Name, serviceEmail);
                return false;
            }

            var isValid = ConstantTimeEquals(serviceConfig.ApiKey, serviceApiKey);

            if (!isValid)
            {
                logger.LogWarning(
                    "Invalid API key provided for service: {Email} (Tenant: {TenantName})",
                    serviceEmail, tenant.Name);
            }
            else
            {
                logger.LogDebug("Service credentials validated successfully for: {Email} (Tenant: {TenantName})", serviceEmail, tenant.Name);
            }

            return isValid;
        }


        /// <summary>
        /// Constant-time string comparison to prevent timing attacks
        /// </summary>
        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);

            if (aBytes.Length != bBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }
    }

}
