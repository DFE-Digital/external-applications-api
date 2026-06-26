using Asp.Versioning;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Templates.Commands;
using DfE.ExternalApplications.Application.Templates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using GovUK.Dfe.CoreLibs.Http.Models;
using DfE.ExternalApplications.Application.Templates.Models;

namespace DfE.ExternalApplications.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public class TemplatesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Returns the latest template schema for the specified template name if the user has access.
    /// </summary>
    [HttpGet("{templateId}/schema")]
    [SwaggerResponse(200, "The latest template schema.", typeof(TemplateSchemaDto))]
    [SwaggerResponse(400, "Request was invalid or template not found.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Access denied.", typeof(ExceptionResponse))]
    [SwaggerResponse(404, "Template not found.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadTemplate")]
    public async Task<IActionResult> GetLatestTemplateSchemaAsync(
        [FromRoute] Guid templateId, CancellationToken cancellationToken)
    {
        var query = new GetLatestTemplateSchemaQuery(templateId);
        var result = await sender.Send(query, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Get custom application statuses for a template.
    /// </summary>
    [HttpGet("{templateId}/custom-statuses")]
    [SwaggerResponse(200, "Custom statuses returned.", typeof(IReadOnlyCollection<CustomApplicationStatusDto>))]
    [Authorize(Policy = "CanReadTemplate")]
    public async Task<IActionResult> GetCustomApplicationStatusesAsync([FromRoute] Guid templateId, CancellationToken cancellationToken)
    {
        var query = new GetCustomApplicationStatusesQuery(templateId);
        var result = await sender.Send(query, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Create or update a custom application status for a template.
    /// If a CustomApplicationStatus for the TemplateId and ApplicationStatus exists, update its label; otherwise create one.
    /// Returns the created or updated CustomApplicationStatus.
    /// </summary>
    [HttpPost("{templateId}/custom-statuses")]
    [SwaggerResponse(201, "Custom status created/updated.", typeof(CustomApplicationStatusDto))]
    [Authorize(Policy = "CanWriteTemplate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCustomApplicationStatusAsync([FromRoute] Guid templateId, [FromBody] CustomApplicationStatusDto request, CancellationToken cancellationToken)
    {
        var command = new UpdateCustomApplicationStatusCommand(templateId, request.ApplicationStatus, request.Label);
        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status201Created
        };
    }

    /// <summary>
    /// Creates a new schema version for the specified template.
    /// </summary>
    [HttpPost("{templateId}/versions")]
    [SwaggerResponse(201, "Template version created successfully.", typeof(TemplateSchemaDto))]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Access denied.", typeof(ExceptionResponse))]
    [SwaggerResponse(404, "Template not found.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanWriteTemplate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTemplateVersionAsync(
        [FromRoute] Guid templateId,
        [FromBody] CreateTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTemplateVersionCommand(templateId, request.VersionNumber, request.JsonSchema);
        var result = await sender.Send(command, cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status201Created
        };
    }
}