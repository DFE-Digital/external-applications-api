using System.Security.Claims;
using Asp.Versioning;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Infrastructure.Security;
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
    [HttpGet("{templateName}/schema")]
    [SwaggerResponse(200, "The latest template schema.", typeof(TemplateSchemaDto))]
    [SwaggerResponse(400, "Request was invalid or access denied.")]
    [Authorize(AuthenticationSchemes = AuthConstants.UserScheme, Policy = "CanReadTemplate")]
    [Authorize(AuthenticationSchemes = AuthConstants.AzureAdScheme, Policy = "CanRead")]
    public async Task<IActionResult> GetLatestTemplateSchemaAsync(
        [FromRoute] string templateName, CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? throw new InvalidOperationException("No email claim in token");

        var query = new GetLatestTemplateSchemaQuery(templateName, email);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
