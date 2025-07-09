using Asp.Versioning;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Request;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Templates.Commands;
using DfE.ExternalApplications.Application.Templates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerResponse(400, "Request was invalid or template not found.")]
    [SwaggerResponse(403, "Access denied.")]
    [Authorize(Policy = "CanReadTemplate")]
    public async Task<IActionResult> GetLatestTemplateSchemaAsync(
        [FromRoute] Guid templateId, CancellationToken cancellationToken)
    {
        var query = new GetLatestTemplateSchemaQuery(templateId);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Template version not found" => BadRequest(result.Error),
                "Access denied" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new schema version for the specified template.
    /// </summary>
    [HttpPost("{templateId}/schema")]
    [SwaggerResponse(201, "The template version was created successfully.", typeof(TemplateSchemaDto))]
    [SwaggerResponse(400, "Request was invalid or template not found.")]
    [SwaggerResponse(403, "Access denied.")]
    [Authorize(Policy = "CanWriteTemplate")]
    public async Task<IActionResult> CreateTemplateVersionAsync(
        [FromRoute] Guid templateId,
        [FromBody] CreateTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTemplateVersionCommand(
            templateId,
            request.VersionNumber,
            request.JsonSchema);

        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Template not found" => NotFound(result.Error),
                "Access denied" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return StatusCode(201, result.Value);
    }
}