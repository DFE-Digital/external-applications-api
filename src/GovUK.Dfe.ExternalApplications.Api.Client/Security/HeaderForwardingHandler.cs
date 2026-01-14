using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GovUK.Dfe.ExternalApplications.Api.Client.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security
{
    /// <summary>
    /// HTTP message handler that forwards specific headers from incoming requests to outgoing API calls.
    /// This is used to forward authentication-related headers (like Cypress test headers) from the web app to the API.
    /// Also automatically appends the X-Tenant-ID header if configured.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class HeaderForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HeaderForwardingHandler> _logger;
        private readonly string[] _headersToForward;
        private readonly Guid? _tenantId;

        /// <summary>
        /// The header name used to identify the tenant for multi-tenant API requests.
        /// </summary>
        public const string TenantIdHeaderName = "X-Tenant-ID";

        /// <summary>
        /// Default headers that should be forwarded from incoming requests to API calls if not configured
        /// </summary>
        private static readonly string[] DefaultHeadersToForward = new[]
        {
            "x-service-email",
            "x-service-api-key"
        };

        /// <summary>
        /// Initializes a new instance of the HeaderForwardingHandler
        /// </summary>
        /// <param name="httpContextAccessor">Accessor to get the current HTTP context</param>
        /// <param name="apiSettings">API client settings containing configuration for headers to forward</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public HeaderForwardingHandler(
            IHttpContextAccessor httpContextAccessor,
            ApiClientSettings apiSettings,
            ILogger<HeaderForwardingHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _tenantId = apiSettings.TenantId;
            
            // Use configured headers if provided, otherwise use defaults
            _headersToForward = apiSettings.HeadersToForward?.Any() == true
                ? apiSettings.HeadersToForward
                : DefaultHeadersToForward;
        }

        /// <summary>
        /// Sends an HTTP request, forwarding configured headers from the incoming request
        /// and appending the X-Tenant-ID header if configured.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Always add the tenant ID header if configured
            if (_tenantId.HasValue)
            {
                if (request.Headers.Contains(TenantIdHeaderName))
                {
                    request.Headers.Remove(TenantIdHeaderName);
                }
                
                request.Headers.Add(TenantIdHeaderName, _tenantId.Value.ToString());
                
                _logger.LogDebug(
                    "Added {HeaderName} header with value {TenantId} to API request: {RequestUri}",
                    TenantIdHeaderName,
                    _tenantId.Value,
                    request.RequestUri);
            }

            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                var headersForwarded = 0;

                // Forward each configured header if present in the incoming request
                foreach (var headerName in _headersToForward)
                {
                    if (httpContext.Request.Headers.TryGetValue(headerName, out var headerValue))
                    {
                        var value = headerValue.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            // Remove existing header if present (avoid duplicates)
                            if (request.Headers.Contains(headerName))
                            {
                                request.Headers.Remove(headerName);
                            }

                            // Add the forwarded header
                            request.Headers.Add(headerName, value);
                            headersForwarded++;

                            _logger.LogDebug(
                                "Forwarded header {HeaderName} to API request: {RequestUri}",
                                headerName,
                                request.RequestUri);
                        }
                    }
                }

                if (headersForwarded > 0)
                {
                    _logger.LogInformation(
                        "Forwarded {Count} header(s) to API request: {RequestUri}",
                        headersForwarded,
                        request.RequestUri);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

