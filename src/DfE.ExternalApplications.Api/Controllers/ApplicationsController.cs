using Asp.Versioning;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Request;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DfE.ExternalApplications.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public class ApplicationsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a new application with initial response.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "The created application.", typeof(ApplicationDto))]
    [SwaggerResponse(400, "Invalid request data.")]
    [SwaggerResponse(401, "Unauthorized - no valid user token")]
    [Authorize(Policy = "CanCreateAnyApplication")]
    public async Task<IActionResult> CreateApplicationAsync(
        [FromBody] CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateApplicationCommand(request.TemplateId, request.InitialResponseBody), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Adds a new response version to an existing application.
    /// </summary>
    [HttpPost]
    [Route(("{applicationId}/responses"))]
    [SwaggerResponse(201, "Response version created.", typeof(ApplicationResponseDto))]
    [SwaggerResponse(400, "Invalid request data.")]
    [SwaggerResponse(401, "Unauthorized - no valid user token.")]
    [SwaggerResponse(403, "User does not have permission to update this application.")]
    [SwaggerResponse(404, "Application not found.")]
    [Authorize(Policy = "CanUpdateApplication")]
    public async Task<IActionResult> AddApplicationResponseAsync(
        [FromRoute] Guid applicationId,
        [FromBody] AddApplicationResponseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddApplicationResponseCommand(applicationId, request.ResponseBody);
        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return StatusCode(201, result.Value);
    }

    /// <summary>
    /// Returns all applications the current user can access.
    /// </summary>
    [HttpGet]
    [Route("/v{version:apiVersion}/me/applications")]
    [SwaggerResponse(200, "A list of applications accessible to the user.", typeof(IReadOnlyCollection<ApplicationDto>))]
    [SwaggerResponse(401, "Unauthorized  no valid user token")]
    [Authorize(Policy = "CanReadAnyApplication")]
    public async Task<IActionResult> GetMyApplicationsAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool? includeSchema = null)
    {
        var query = new GetMyApplicationsQuery(includeSchema ?? false);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns all applications for the user by {email}.
    /// </summary>
    [HttpGet]
    [Route("/v{version:apiVersion}/Users/{email}/applications")]
    [SwaggerResponse(200, "Applications for the user.", typeof(IReadOnlyCollection<ApplicationDto>))]
    [SwaggerResponse(400, "Email cannot be null or empty.")]
    [Authorize(Policy = "CanReadAnyApplication")]
    public async Task<IActionResult> GetApplicationsForUserAsync(
        [FromRoute] string email,
        CancellationToken cancellationToken,
        [FromQuery] bool? includeSchema = null)
    {
        var query = new GetApplicationsForUserQuery(email, includeSchema ?? false);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns application details with its latest response by application reference.
    /// </summary>
    [HttpGet("reference/{applicationReference}")]
    [SwaggerResponse(200, "Application details with latest response.", typeof(ApplicationDto))]
    [SwaggerResponse(400, "Invalid application reference or application not found.")]
    [SwaggerResponse(401, "Unauthorized - no valid user token")]
    [SwaggerResponse(403, "User does not have permission to read this application")]
    [SwaggerResponse(404, "Application not found")]
    [Authorize(Policy = "CanReadAnyApplication")]
    public async Task<IActionResult> GetApplicationByReferenceAsync(
        [FromRoute] string applicationReference,
        CancellationToken cancellationToken)
    {
        var query = new GetApplicationByReferenceQuery(applicationReference);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Application not found" => NotFound(result.Error),
                "User does not have permission to read this application" => Forbid(),
                "Not authenticated" => Unauthorized(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Submits an application, changing its status to Submitted.
    /// </summary>
    [HttpPost("{applicationId}/submit")]
    [SwaggerResponse(200, "Application submitted successfully.", typeof(ApplicationDto))]
    [SwaggerResponse(400, "Invalid request data or application already submitted.")]
    [SwaggerResponse(401, "Unauthorized - no valid user token")]
    [SwaggerResponse(403, "User does not have permission to submit this application")]
    [SwaggerResponse(404, "Application not found")]
    [Authorize(Policy = "CanUpdateApplication")]
    public async Task<IActionResult> SubmitApplicationAsync(
        [FromRoute] Guid applicationId,
        CancellationToken cancellationToken)
    {
        var command = new SubmitApplicationCommand(applicationId);
        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Application not found" => NotFound(result.Error),
                "User does not have permission to submit this application" => Forbid(),
                "Not authenticated" => Unauthorized(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all contributors for a specific application.
    /// </summary>
    [HttpGet("{applicationId}/contributors")]
    [SwaggerResponse(200, "List of contributors for the application.", typeof(IReadOnlyCollection<UserDto>))]
    [SwaggerResponse(400, "Invalid request data.")]
    [SwaggerResponse(401, "Unauthorized - no valid user token")]
    [SwaggerResponse(403, "User does not have permission to read this application")]
    [SwaggerResponse(404, "Application not found")]
    [Authorize(Policy = "CanReadAnyApplication")]
    public async Task<IActionResult> GetContributorsAsync(
        [FromRoute] Guid applicationId,
        CancellationToken cancellationToken,
        [FromQuery] bool? includePermissionDetails = null)
    {
        var query = new GetContributorsForApplicationQuery(applicationId, includePermissionDetails ?? false);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Application not found" => NotFound(result.Error),
                "Only the application owner or admin can view contributors" => Forbid(),
                "Not authenticated" => Unauthorized(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Adds a contributor to an application.
    /// </summary>
    [HttpPost("{applicationId}/contributors")]
    [SwaggerResponse(200, "Contributor added successfully.", typeof(UserDto))]
    [SwaggerResponse(400, "Invalid request data or contributor already exists.")]
    [SwaggerResponse(401, "Unauthorized - no valid user token")]
    [SwaggerResponse(403, "User does not have permission to manage contributors for this application")]
    [SwaggerResponse(404, "Application not found")]
    [Authorize(Policy = "CanUpdateApplication")]
    public async Task<IActionResult> AddContributorAsync(
        [FromRoute] Guid applicationId,
        [FromBody] AddContributorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddContributorCommand(applicationId, request.Name, request.Email);
        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Application not found" => NotFound(result.Error),
                "User does not have permission to manage contributors for this application" => Forbid(),
                "Not authenticated" => Unauthorized(),
                "Contributor already exists for this application" => BadRequest(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Removes a contributor from an application.
    /// </summary>
    [HttpDelete("{applicationId}/contributors/{userId}")]
    [SwaggerResponse(200, "Contributor removed successfully.")]
    [SwaggerResponse(400, "Invalid request data.")]
    [SwaggerResponse(401, "Unauthorized - no valid user token")]
    [SwaggerResponse(403, "User does not have permission to manage contributors for this application")]
    [SwaggerResponse(404, "Application or contributor not found")]
    [Authorize(Policy = "CanUpdateApplication")]
    public async Task<IActionResult> RemoveContributorAsync(
        [FromRoute] Guid applicationId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveContributorCommand(applicationId, userId);
        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Application not found" => NotFound(result.Error),
                "Contributor not found" => NotFound(result.Error),
                "User does not have permission to manage contributors for this application" => Forbid(),
                "Not authenticated" => Unauthorized(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok();
    }
}