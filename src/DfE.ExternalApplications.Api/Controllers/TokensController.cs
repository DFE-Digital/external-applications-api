using Asp.Versioning;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DfE.ExternalApplications.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    public class TokensController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// Exchanges an DSI token for our ExternalApplications InternalUser JWT.
        /// </summary>
        [HttpPost("exchange")]
        [Authorize(AuthenticationSchemes = AuthConstants.AzureAdScheme, Policy = "SvcCanReadWrite")]
        public async Task<ActionResult<ExchangeTokenDto>> Exchange(
            [FromBody] ExchangeTokenDto request,
            CancellationToken ct)
        {
            var result = await sender.Send(
                new ExchangeTokenQuery(request.AccessToken), ct);
            return Ok(result);
        }
    }
}