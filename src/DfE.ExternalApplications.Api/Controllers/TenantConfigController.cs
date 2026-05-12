using Asp.Versioning;
using DfE.ExternalApplications.Application.TenantConfig.Queries;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Http.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DfE.ExternalApplications.Api.Controllers;

/// <summary>
/// Consume endpoint that allows a tenant's downstream applications (e.g. the Web container)
/// to securely retrieve their merged configuration. The tenant is resolved from the
/// authenticated principal in the JWT - never from a client-supplied parameter.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenant-config")]
[Authorize]
public class TenantConfigController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Returns the merged 'Shared' + target-specific configuration for the calling principal's tenant.
    /// </summary>
    /// <param name="target">The consuming application's target. One of: Web, Api, Shared. Defaults to Web.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [SwaggerResponse(200, "Tenant configuration.", typeof(TenantConfigurationDto))]
    [SwaggerResponse(400, "Invalid target.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized.", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Principal is not registered to any tenant.", typeof(ExceptionResponse))]
    [SwaggerResponse(404, "Tenant inactive or no longer exists.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    public async Task<IActionResult> GetTenantConfiguration(
        [FromQuery] string target = "Web",
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTenantConfigurationQuery(target), cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                DomainErrorCode.Forbidden => StatusCodes.Status403Forbidden,
                DomainErrorCode.NotFound => StatusCodes.Status404NotFound,
                DomainErrorCode.Validation => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };

            return new ObjectResult(result) { StatusCode = statusCode };
        }

        return new ObjectResult(result) { StatusCode = StatusCodes.Status200OK };
    }
}
