using Asp.Versioning;
using DfE.ExternalApplications.Application.TenantAdmin.Commands;
using DfE.ExternalApplications.Application.TenantAdmin.Queries;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Http.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DfE.ExternalApplications.Api.Controllers;

/// <summary>
/// Administrative endpoints for tenant configuration management.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/tenants")]
[Authorize] // TODO: tighten to [Authorize(Policy = "IsAdmin")] once the IsAdmin policy is registered.
public class TenantAdminController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Triggers an immediate refresh of the in-memory tenant configuration cache.
    /// </summary>
    [HttpPost("refresh")]
    [SwaggerResponse(200, "Tenant configuration refreshed.", typeof(RefreshTenantConfigurationResponse))]
    [SwaggerResponse(401, "Unauthorized.", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    public async Task<IActionResult> RefreshTenantConfiguration(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshTenantConfigurationCommand(), cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Returns a summary of all loaded tenant configurations.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of tenants.", typeof(GetTenantsResponse))]
    [SwaggerResponse(401, "Unauthorized.", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantsQuery(), cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Seeds tenant configuration from appsettings into the tenant config database.
    /// </summary>
    [HttpPost("seed")]
    [SwaggerResponse(200, "Seeding complete.", typeof(SeedTenantsResponse))]
    [SwaggerResponse(401, "Unauthorized.", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    public async Task<IActionResult> SeedFromAppSettings(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SeedTenantsFromAppSettingsCommand(), cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Adds or updates a configuration section for a specific tenant.
    /// Secret sections are encrypted before storage.
    /// </summary>
    [HttpPut("{tenantId:guid}/settings")]
    [SwaggerResponse(200, "Setting updated.", typeof(UpsertTenantSettingResponse))]
    [SwaggerResponse(201, "Setting created.", typeof(UpsertTenantSettingResponse))]
    [SwaggerResponse(400, "Validation error.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized.", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden.", typeof(ExceptionResponse))]
    [SwaggerResponse(404, "Tenant not found.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    public async Task<IActionResult> UpsertTenantSetting(
        Guid tenantId,
        [FromBody] UpsertTenantSettingRequest body,
        CancellationToken cancellationToken)
    {
        var command = new UpsertTenantSettingCommand(
            tenantId,
            body.Category,
            body.Target,
            body.SettingsJson,
            body.IsSecret);

        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return new ObjectResult(result) { StatusCode = StatusCodes.Status404NotFound };

        var statusCode = result.Value!.WasCreated
            ? StatusCodes.Status201Created
            : StatusCodes.Status200OK;

        return new ObjectResult(result) { StatusCode = statusCode };
    }
}