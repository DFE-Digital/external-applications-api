using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Infrastructure.Services;

public class DefaultApplicationReferenceProvider(
    IEaRepository<Application> applicationRepo,
    ITenantContextAccessor tenantContextAccessor,
    ILogger<DefaultApplicationReferenceProvider> logger) : IApplicationReferenceProvider
{
    private const string DefaultPrefix = "APP";

    public async Task<string> GenerateReferenceAsync(CancellationToken cancellationToken = default)
    {
        // Get prefix from tenant configuration, fallback to default
        var prefix = GetTenantPrefix();
        
        var today = DateTime.UtcNow.Date;
        var latestApp = await applicationRepo.Query()
            .Where(a => a.CreatedOn.Date == today)
            .OrderByDescending(a => a.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        var currentNumber = 1;
        if (latestApp != null)
        {
            var parts = latestApp.ApplicationReference.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var number))
            {
                currentNumber = number + 1;
            }
        }

        // Generate new reference in format "{PREFIX}-YYYYMMDD-NNN"
        var reference = $"{prefix}-{today:yyyyMMdd}-{currentNumber:000}";
        
        logger.LogDebug(
            "Generated application reference {Reference} for tenant {TenantName}",
            reference, tenantContextAccessor.CurrentTenant?.Name ?? "Unknown");
        
        return reference;
    }

    private string GetTenantPrefix()
    {
        var tenant = tenantContextAccessor.CurrentTenant;
        if (tenant == null)
        {
            logger.LogWarning("No tenant context available, using default prefix '{DefaultPrefix}'", DefaultPrefix);
            return DefaultPrefix;
        }

        // Read prefix from tenant settings: ApplicationReference:Prefix
        var prefix = tenant.Settings["ApplicationReference:Prefix"];
        
        if (string.IsNullOrWhiteSpace(prefix))
        {
            logger.LogDebug(
                "Tenant {TenantName} has no ApplicationReference:Prefix configured, using default '{DefaultPrefix}'",
                tenant.Name, DefaultPrefix);
            return DefaultPrefix;
        }

        return prefix.ToUpperInvariant();
    }
} 