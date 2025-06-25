using Asp.Versioning;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Infrastructure.Security;
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
    /// Returns all applications the current user can access.
    /// </summary>
    [HttpGet("/v{version:apiVersion}/me/applications")]
    [Authorize(AuthenticationSchemes = AuthConstants.UserScheme)]
    [SwaggerResponse(200, "A list of applications accessible to the user.", typeof(IReadOnlyCollection<ApplicationDto>))]
    [SwaggerResponse(401, "Unauthorized  no valid user token")]
    [Authorize(Policy = "CanReadApplication")]
    public async Task<IActionResult> GetMyApplicationsAsync(
        CancellationToken cancellationToken)
    {
        var query = new GetMyApplicationsQuery();
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns all applications for the user by {email}.
    /// </summary>
    [HttpGet("/v{version:apiVersion}/Users/{email}/applications")]
    [SwaggerResponse(200, "Applications for the user.", typeof(IReadOnlyCollection<ApplicationDto>))]
    [SwaggerResponse(400, "Email cannot be null or empty.")]
    [Authorize(Policy = "CanReadApplication")]
    public async Task<IActionResult> GetApplicationsForUserAsync(
        [FromRoute] string email,
        CancellationToken cancellationToken)
    {
        var query = new GetApplicationsForUserQuery(email);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}