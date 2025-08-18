using DfE.CoreLibs.Http.Models;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Controllers
{
    public class HubAuthController(IDistributedCache cache) : ControllerBase
    {
        //Create a single use ticket for the hub, which is valid for 1 minute
        [HttpPost("auth/hub-ticket")]
        [SwaggerResponse(200, "The created ticket.", typeof(Dictionary<string, string>))]
        [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
        [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
        [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
        [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
        [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
        [Authorize(AuthenticationSchemes = AuthConstants.UserScheme)]
        public async Task<IActionResult> CreateHubTicket(CancellationToken ct)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrWhiteSpace(email))
                return Forbid();

            var ticket = Guid.NewGuid().ToString("N");
            await cache.SetStringAsync($"hub:ticket:{ticket}", email,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                }, ct);

            var redeemUrl = Url.ActionLink(
                action: nameof(RedeemHubCookie),
                controller: "HubAuth",
                values: new { ticket },
                protocol: Request.Scheme);

            return Ok(new { url = redeemUrl });
        }

        // Redeem and validate the ticket to create a cookie for the hub
        [HttpGet("auth/hub-cookie")]
        [SwaggerResponse(204)]
        [SwaggerResponse(400, "Invalid request data.", typeof(ExceptionResponse))]
        [SwaggerResponse(401, "Unauthorized - no valid user token", typeof(ExceptionResponse))]
        [SwaggerResponse(403, "Forbidden - user does not have required permissions", typeof(ExceptionResponse))]
        [SwaggerResponse(500, "Internal server error.", typeof(ExceptionResponse))]
        [SwaggerResponse(429, "Too Many Requests.", typeof(ExceptionResponse))]
        [AllowAnonymous]
        public async Task<IActionResult> RedeemHubCookie([FromQuery] string ticket, CancellationToken ct)
        {
            var key = $"hub:ticket:{ticket}";
            var email = await cache.GetStringAsync(key, ct);
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            await cache.RemoveAsync(key, ct); // single use

            var claims = new[] {
                new Claim(ClaimTypes.Email, email),
            };
            var id = new ClaimsIdentity(claims, "HubCookie");
            await HttpContext.SignInAsync("HubCookie",
                new ClaimsPrincipal(id),
                new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10) });

            return NoContent();
        }
    }
}
