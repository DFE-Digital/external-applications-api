using GovUK.Dfe.CoreLibs.Utilities.RateLimiting;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace DfE.ExternalApplications.Application.Tests.Common.Behaviours;

public class RateLimitingBehaviourTests
{
    private readonly IRateLimiterFactory<string> _rateLimiterFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRateLimiter<string> _rateLimiter;
    private readonly RateLimitingBehaviour<TestRateLimitedRequest, string> _behaviour;

    public RateLimitingBehaviourTests()
    {
        _rateLimiterFactory = Substitute.For<IRateLimiterFactory<string>>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _rateLimiter = Substitute.For<IRateLimiter<string>>();
        _behaviour = new RateLimitingBehaviour<TestRateLimitedRequest, string>(_rateLimiterFactory, _httpContextAccessor);
    }

    [RateLimit(5, 60)]
    public record TestRateLimitedRequest(string Value) : IRequest<string>, IRateLimitedRequest;

    [Fact]
    public async Task Handle_ShouldProceedToNext_WhenRateLimitAllows()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", "test-app-id")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _rateLimiterFactory.Create(5, TimeSpan.FromSeconds(60)).Returns(_rateLimiter);
        _rateLimiter.IsAllowed("test-app-id_TestRateLimitedRequest").Returns(true);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await _behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        await next.Received(1).Invoke();
        _rateLimiterFactory.Received(1).Create(5, TimeSpan.FromSeconds(60));
        _rateLimiter.Received(1).IsAllowed("test-app-id_TestRateLimitedRequest");
    }

    [Fact]
    public async Task Handle_ShouldThrowRateLimitExceededException_WhenRateLimitExceeded()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", "test-app-id")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _rateLimiterFactory.Create(5, TimeSpan.FromSeconds(60)).Returns(_rateLimiter);
        _rateLimiter.IsAllowed("test-app-id_TestRateLimitedRequest").Returns(false);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() =>
            _behaviour.Handle(request, next, CancellationToken.None));

        Assert.Equal("Too many requests. Please retry later.", exception.Message);
        await next.DidNotReceive().Invoke();
        _rateLimiterFactory.Received(1).Create(5, TimeSpan.FromSeconds(60));
        _rateLimiter.Received(1).IsAllowed("test-app-id_TestRateLimitedRequest");
    }

    [Fact]
    public async Task Handle_ShouldUseAzpClaim_WhenAppIdClaimNotPresent()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("azp", "test-azp-id")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _rateLimiterFactory.Create(5, TimeSpan.FromSeconds(60)).Returns(_rateLimiter);
        _rateLimiter.IsAllowed("test-azp-id_TestRateLimitedRequest").Returns(true);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await _behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        _rateLimiter.Received(1).IsAllowed("test-azp-id_TestRateLimitedRequest");
    }

    [Fact]
    public async Task Handle_ShouldUseEmailClaim_WhenAppIdAndAzpClaimsNotPresent()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _rateLimiterFactory.Create(5, TimeSpan.FromSeconds(60)).Returns(_rateLimiter);
        _rateLimiter.IsAllowed("test@example.com_TestRateLimitedRequest").Returns(true);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await _behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        _rateLimiter.Received(1).IsAllowed("test@example.com_TestRateLimitedRequest");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenNoPrincipalIdFound()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("some-other-claim", "some-value")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behaviour.Handle(request, next, CancellationToken.None));

        Assert.Equal("RateLimiter > Email/AppId claim missing", exception.Message);
        await next.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenHttpContextIsNull()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behaviour.Handle(request, next, CancellationToken.None));

        Assert.Equal("RateLimiter > Email/AppId claim missing", exception.Message);
        await next.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenUserIsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = null!;
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behaviour.Handle(request, next, CancellationToken.None));

        Assert.Equal("RateLimiter > Email/AppId claim missing", exception.Message);
        await next.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task Handle_ShouldPreferAppIdOverAzp_WhenBothPresent()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", "test-app-id"),
            new("azp", "test-azp-id"),
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _rateLimiterFactory.Create(5, TimeSpan.FromSeconds(60)).Returns(_rateLimiter);
        _rateLimiter.IsAllowed("test-app-id_TestRateLimitedRequest").Returns(true);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await _behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        _rateLimiter.Received(1).IsAllowed("test-app-id_TestRateLimitedRequest");
        _rateLimiter.DidNotReceive().IsAllowed("test-azp-id_TestRateLimitedRequest");
        _rateLimiter.DidNotReceive().IsAllowed("test@example.com_TestRateLimitedRequest");
    }

    [Fact]
    public async Task Handle_ShouldPreferAzpOverEmail_WhenAppIdNotPresent()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("azp", "test-azp-id"),
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _rateLimiterFactory.Create(5, TimeSpan.FromSeconds(60)).Returns(_rateLimiter);
        _rateLimiter.IsAllowed("test-azp-id_TestRateLimitedRequest").Returns(true);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await _behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        _rateLimiter.Received(1).IsAllowed("test-azp-id_TestRateLimitedRequest");
        _rateLimiter.DidNotReceive().IsAllowed("test@example.com_TestRateLimitedRequest");
    }

    [Fact]
    public async Task Handle_ShouldCreateRateLimiterWithCorrectParameters()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", "test-app-id")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _rateLimiterFactory.Create(5, TimeSpan.FromSeconds(60)).Returns(_rateLimiter);
        _rateLimiter.IsAllowed("test-app-id_TestRateLimitedRequest").Returns(true);

        var request = new TestRateLimitedRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        await _behaviour.Handle(request, next, CancellationToken.None);

        // Assert
        _rateLimiterFactory.Received(1).Create(
            Arg.Is<int>(max => max == 5),
            Arg.Is<TimeSpan>(timeSpan => timeSpan == TimeSpan.FromSeconds(60)));
    }
}