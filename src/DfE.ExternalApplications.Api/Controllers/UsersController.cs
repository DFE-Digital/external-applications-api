using Asp.Versioning;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Infrastructure.Security.Configurations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DfE.ExternalApplications.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    public class UsersController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// GET /api/users/{email}/permissions
        /// Returns all permissions for the user identified by {email}.
        /// </summary>
        [HttpGet("{email}/permissions")]
        [SwaggerResponse(200, "A UserPermission object representing the User's Permissions.", typeof(Principal))]
        [SwaggerResponse(400, "School cannot be null or empty.")]
        public async Task<IActionResult> GetAllPermissionsForUser(
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
}