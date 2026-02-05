using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Services
{
    /// <summary>
    /// Tenant-aware file storage service that resolves file storage settings
    /// from the current tenant's configuration at runtime.
    /// 
    /// This wrapper uses the CoreLibs options override feature to pass tenant-specific
    /// LocalFileStorageOptions directly to the inner service, ensuring files are stored
    /// in the correct tenant's directory.
    /// 
    /// Both the inner IFileStorageService and ITenantContextAccessor are resolved lazily
    /// via HttpContext.RequestServices to avoid DI lifetime issues (the CoreLibs service
    /// uses concrete type resolution internally that can't be decorated).
    /// </summary>
    public class TenantAwareFileStorageService : ITenantAwareFileStorageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TenantAwareFileStorageService> _logger;

        public TenantAwareFileStorageService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<TenantAwareFileStorageService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            
            _logger.LogDebug("TenantAwareFileStorageService initialized with lazy resolution support");
        }

        #region Default Interface Methods (without options override)

        public Task UploadAsync(string path, Stream content, string? originalFileName = null, CancellationToken cancellationToken = default)
        {
            var innerService = GetInnerService();
            var tenantOptions = GetTenantOptions();
            
            if (tenantOptions != null)
            {
                _logger.LogDebug("Uploading file with tenant options: path={Path}, baseDirectory={BaseDirectory}", 
                    path, tenantOptions.BaseDirectory);
                return innerService.UploadAsync(path, content, originalFileName, tenantOptions, cancellationToken);
            }
            
            _logger.LogDebug("Uploading file with default options: path={Path}", path);
            return innerService.UploadAsync(path, content, originalFileName, cancellationToken);
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            var innerService = GetInnerService();
            var tenantOptions = GetTenantOptions();
            
            if (tenantOptions != null)
            {
                _logger.LogDebug("Deleting file with tenant options: path={Path}, baseDirectory={BaseDirectory}", 
                    path, tenantOptions.BaseDirectory);
                return innerService.DeleteAsync(path, tenantOptions, cancellationToken);
            }
            
            _logger.LogDebug("Deleting file with default options: path={Path}", path);
            return innerService.DeleteAsync(path, cancellationToken);
        }

        public Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
        {
            var innerService = GetInnerService();
            var tenantOptions = GetTenantOptions();
            
            if (tenantOptions != null)
            {
                _logger.LogDebug("Downloading file with tenant options: path={Path}, baseDirectory={BaseDirectory}", 
                    path, tenantOptions.BaseDirectory);
                return innerService.DownloadAsync(path, tenantOptions, cancellationToken);
            }
            
            _logger.LogDebug("Downloading file with default options: path={Path}", path);
            return innerService.DownloadAsync(path, cancellationToken);
        }

        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var innerService = GetInnerService();
            var tenantOptions = GetTenantOptions();
            
            if (tenantOptions != null)
            {
                return innerService.ExistsAsync(path, tenantOptions, cancellationToken);
            }
            
            return innerService.ExistsAsync(path, cancellationToken);
        }

        #endregion

        #region Interface Methods with Options Override

        public Task UploadAsync(string path, Stream content, string? originalFileName, LocalFileStorageOptions? optionsOverride, CancellationToken cancellationToken = default)
        {
            var innerService = GetInnerService();
            
            // When explicit options are provided, use them directly (allows caller to override tenant options)
            if (optionsOverride != null)
            {
                _logger.LogDebug("Uploading file with explicit options override: path={Path}, baseDirectory={BaseDirectory}", 
                    path, optionsOverride.BaseDirectory);
                return innerService.UploadAsync(path, content, originalFileName, optionsOverride, cancellationToken);
            }
            
            // If no override provided, use tenant options
            return UploadAsync(path, content, originalFileName, cancellationToken);
        }

        public Task DeleteAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken cancellationToken = default)
        {
            var innerService = GetInnerService();
            
            if (optionsOverride != null)
            {
                _logger.LogDebug("Deleting file with explicit options override: path={Path}, baseDirectory={BaseDirectory}", 
                    path, optionsOverride.BaseDirectory);
                return innerService.DeleteAsync(path, optionsOverride, cancellationToken);
            }
            
            return DeleteAsync(path, cancellationToken);
        }

        public Task<Stream> DownloadAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken cancellationToken = default)
        {
            var innerService = GetInnerService();
            
            if (optionsOverride != null)
            {
                _logger.LogDebug("Downloading file with explicit options override: path={Path}, baseDirectory={BaseDirectory}", 
                    path, optionsOverride.BaseDirectory);
                return innerService.DownloadAsync(path, optionsOverride, cancellationToken);
            }
            
            return DownloadAsync(path, cancellationToken);
        }

        public Task<bool> ExistsAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken cancellationToken = default)
        {
            var innerService = GetInnerService();
            
            if (optionsOverride != null)
            {
                return innerService.ExistsAsync(path, optionsOverride, cancellationToken);
            }
            
            return ExistsAsync(path, cancellationToken);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Gets the inner IFileStorageService from the current request's service provider.
        /// This lazy resolution avoids DI lifetime issues with CoreLibs' internal concrete type resolution.
        /// </summary>
        private IFileStorageService GetInnerService()
        {
            var requestServices = _httpContextAccessor.HttpContext?.RequestServices
                ?? throw new InvalidOperationException("No HttpContext available for file storage operation");
            
            return requestServices.GetRequiredService<IFileStorageService>();
        }

        /// <summary>
        /// Gets the LocalFileStorageOptions for the current tenant.
        /// Returns null if no tenant context is available or no tenant-specific options are configured.
        /// </summary>
        private LocalFileStorageOptions? GetTenantOptions()
        {
            // Get request-scoped service provider from HttpContext
            var requestServices = _httpContextAccessor.HttpContext?.RequestServices;
            if (requestServices == null)
            {
                _logger.LogWarning("No HttpContext available for file storage operation");
                return null;
            }
            
            // Resolve scoped service lazily from the request's service provider
            var tenantContextAccessor = requestServices.GetService<ITenantContextAccessor>();
            var tenant = tenantContextAccessor?.CurrentTenant;
            if (tenant == null)
            {
                _logger.LogWarning("No tenant context available for file storage operation");
                return null;
            }

            // Get tenant-specific file storage settings
            var baseDirectory = tenant.Settings.GetValue<string>("FileStorage:Local:BaseDirectory");
            if (string.IsNullOrEmpty(baseDirectory))
            {
                _logger.LogDebug("No tenant-specific BaseDirectory configured for tenant {TenantName}", tenant.Name);
                return null;
            }

            // Build tenant-specific options from tenant configuration
            var options = new LocalFileStorageOptions
            {
                BaseDirectory = baseDirectory,
                CreateDirectoryIfNotExists = tenant.Settings.GetValue<bool?>("FileStorage:Local:CreateDirectoryIfNotExists") ?? true,
                AllowOverwrite = tenant.Settings.GetValue<bool?>("FileStorage:Local:AllowOverwrite") ?? true,
                MaxFileSizeBytes = tenant.Settings.GetValue<long?>("FileStorage:Local:MaxFileSizeBytes") ?? 100 * 1024 * 1024,
                AllowedExtensions = tenant.Settings.GetSection("FileStorage:Local:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>(),
                AllowedFileNamePattern = tenant.Settings.GetValue<string>("FileStorage:Local:AllowedFileNamePattern"),
                AllowedFileNamePatternFriendlyList = tenant.Settings.GetValue<string>("FileStorage:Local:AllowedFileNamePatternFriendlyList") ?? "a-z A-Z 0-9 _ - no-space",
                AllowedExtensionsFriendlyList = tenant.Settings.GetValue<string>("FileStorage:Local:AllowedExtensionsFriendlyList") ?? "jpg, png, pdf, docx"
            };

            _logger.LogDebug("Resolved tenant options for {TenantName}: BaseDirectory={BaseDirectory}", 
                tenant.Name, options.BaseDirectory);

            return options;
        }

        #endregion
    }
}
