using Asp.Versioning;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using DfE.CoreLibs.Http.Models;

namespace DfE.ExternalApplications.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public class UsersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Returns all my permissions.
    /// </summary>
    [HttpGet("/v{version:apiVersion}/me/permissions")]
    [SwaggerResponse(200, "A UserAuthorizationDto object representing the User's Permissions and Roles.", typeof(UserAuthorizationDto))]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> GetMyPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var query = new GetMyPermissionsQuery();
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
