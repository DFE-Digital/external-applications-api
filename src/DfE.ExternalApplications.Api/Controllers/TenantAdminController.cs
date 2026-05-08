using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DfE.ExternalApplications.Api.Controllers;

/// <summary>
/// Administrative endpoints for tenant configuration management.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/tenants")]
//[Authorize(Policy = "IsAdmin")]
[ExcludeFromCodeCoverage]
public class TenantAdminController(
    DfE.ExternalApplications.Domain.Tenancy.ITenantConfigurationProvider tenantConfigProvider,
    ILogger<TenantAdminController> logger) : ControllerBase
{
    /// <summary>
    /// Triggers an immediate refresh of the in-memory tenant configuration cache.
    /// Only applicable when using database-backed tenant configuration.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshTenantConfiguration(CancellationToken cancellationToken)
    {
        if (tenantConfigProvider is DatabaseTenantConfigurationProvider dbProvider)
        {
            logger.LogInformation("Admin-triggered tenant configuration refresh requested");
            await dbProvider.RefreshAsync(cancellationToken);

            var tenants = dbProvider.GetAllTenants();
            return Ok(new
            {
                message = "Tenant configuration refreshed successfully",
                tenantCount = tenants.Count,
                tenants = tenants.Select(t => new { t.Id, t.Name })
            });
        }

        return Ok(new
        {
            message = "Tenant configuration is loaded from appsettings (not database). No refresh needed.",
            tenantCount = tenantConfigProvider.GetAllTenants().Count
        });
    }

    /// <summary>
    /// Returns a summary of all loaded tenant configurations.
    /// </summary>
    [HttpGet]
    public IActionResult GetTenants()
    {
        var tenants = tenantConfigProvider.GetAllTenants();
        return Ok(new
        {
            source = tenantConfigProvider is DatabaseTenantConfigurationProvider ? "Database" : "AppSettings",
            tenantCount = tenants.Count,
            tenants = tenants.Select(t => new
            {
                t.Id,
                t.Name,
                frontendOrigins = t.FrontendOrigins
            })
        });
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedFromAppSettings(
        [FromServices] TenantConfigDbContext dbContext,
        [FromServices] IConfiguration configuration,
        [FromServices] ITenantSettingsEncryptor encryptor)
    {
        await TenantConfigSeeder.SeedFromAppSettingsAsync(
            dbContext, configuration, encryptor, logger);

        return Ok(new { message = "Seeding complete" });
    }
}
