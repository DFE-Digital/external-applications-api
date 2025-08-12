using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace DfE.ExternalApplications.Api.Controllers
{
    public class HubAuthController : ControllerBase
    {
        private readonly IDistributedCache _cache;

        //Create a single use ticket for the hub, which is valid for 1 minute
        [HttpPost("auth/hub-ticket")]
        [Authorize]
        public async Task<IActionResult> CreateHubTicket(CancellationToken ct)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrWhiteSpace(email))
                return Forbid();

            var ticket = Guid.NewGuid().ToString("N");
            await _cache.SetStringAsync($"hub:ticket:{ticket}", email,
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
        [AllowAnonymous]
        public async Task<IActionResult> RedeemHubCookie([FromQuery] string ticket, CancellationToken ct)
        {
            var key = $"hub:ticket:{ticket}";
            var email = await _cache.GetStringAsync(key, ct);
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            await _cache.RemoveAsync(key, ct); // single use

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
