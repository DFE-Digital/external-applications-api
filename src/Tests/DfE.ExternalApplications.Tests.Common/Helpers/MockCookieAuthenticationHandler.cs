using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock cookie authentication handler that supports SignInAsync for SignalR hub authentication in tests
/// </summary>
public class MockCookieAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationSignInHandler
{
    public MockCookieAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if there's a cookie in the request
        var cookieHeader = Request.Headers["Cookie"].FirstOrDefault();
        if (string.IsNullOrEmpty(cookieHeader))
        {
            // No cookie provided, return failure
            return Task.FromResult(AuthenticateResult.Fail("No authentication cookie provided"));
        }

        // Check if the test authentication cookie is present
        if (!cookieHeader.Contains("test-auth-cookie=authenticated"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid authentication cookie"));
        }

        // For tests, we'll authenticate successfully if the test cookie is present
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test.user@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }

    public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        // Mock implementation - set a simple authentication cookie for tests
        var cookieValue = "test-auth-cookie=authenticated; Path=/; HttpOnly";
        Response.Headers.Add("Set-Cookie", cookieValue);
        return Task.CompletedTask;
    }

    public Task SignOutAsync(AuthenticationProperties? properties)
    {
        // Mock implementation - in a real scenario this would clear the authentication cookie
        // For tests, we'll just return success
        return Task.CompletedTask;
    }
}
