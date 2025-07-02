using System.Security.Claims;
using Asp.Versioning;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
    [Authorize(AuthenticationSchemes = AuthConstants.UserScheme)]
    [SwaggerResponse(200, "A UserPermission object representing the User's Permissions.", typeof(IReadOnlyCollection<UserPermissionDto>))]
    [SwaggerResponse(401, "Unauthorized – no valid user token")]
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

    /// <summary>
    /// Returns all permissions for the user by {email}.
    /// </summary>
    [HttpGet("{email}/permissions")]
    [SwaggerResponse(200, "A UserPermission object representing the User's Permissions.", typeof(IReadOnlyCollection<UserPermissionDto>))]
    [SwaggerResponse(400, "Email cannot be null or empty.")]
    [Authorize(Policy = "CanReadUser")]
    public async Task<IActionResult> GetAllPermissionsForUserAsync(
        [FromRoute] string email,
        CancellationToken cancellationToken)
    {
        var query = new GetAllUserPermissionsQuery(email);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
