using Asp.Versioning;
using DfE.ExternalApplications.Application.Users.Commands;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using GovUK.Dfe.CoreLibs.Http.Models;
using DfE.ExternalApplications.Infrastructure.Security;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

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

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Create and registers a new user using the data in the provided External-IDP token.
    /// </summary>
    [HttpPost("register")]
    [SwaggerResponse(200, "User registered successfully.", typeof(UserDto))]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
    [Authorize(AuthenticationSchemes = AuthConstants.AzureAdScheme, Policy = "SvcCanReadWrite")]
    public async Task<ActionResult<UserDto>> RegisterUserAsync(
        [FromBody] RegisterUserRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new RegisterUserCommand(request.AccessToken, request.TemplateId), ct);
        
        if (!result.IsSuccess)
            return BadRequest(new ExceptionResponse { Message = result.Error });
        
        return Ok(result.Value);
    }
}
