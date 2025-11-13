using Asp.Versioning;
using DfE.ExternalApplications.Application.Applications.Commands;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;
using GovUK.Dfe.CoreLibs.Http.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DfE.ExternalApplications.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public class UserFeedbackController(ISender sender) : ControllerBase
{
    [HttpPost]
    [SwaggerResponse(202, "User feedback was submitted successfully.")]
    [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
    [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
    [SwaggerResponse(429, "Too many requests.", typeof(ExceptionResponse))]
    [AllowAnonymous]
    public async Task<IActionResult> PostAsync([FromBody] UserFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SubmitUserFeedbackCommand(request), cancellationToken);

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status202Accepted
        };
    }
}