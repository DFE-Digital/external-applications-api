using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Notifications.Interfaces;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Notifications.Queries;
using DfE.ExternalApplications.Domain.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Notifications;

public class GetUnreadNotificationCountQueryHandlerTests
{
    private readonly INotificationService _notificationService;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly GetUnreadNotificationCountQueryHandler _handler;

    public GetUnreadNotificationCountQueryHandlerTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        _handler = new GetUnreadNotificationCountQueryHandler(
            _notificationService,
            _permissionCheckerService,
            _httpContextAccessor);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnUnreadCount_WhenValidRequestAndUserHasPermission(
        GetUnreadNotificationCountQuery query,
        int expectedCount)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Read).Returns(true);

        _notificationService.GetUnreadCountAsync(email, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedCount);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCount, result.Value);
        await _notificationService.Received(1).GetUnreadCountAsync(email, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserDoesNotHavePermission(
        GetUnreadNotificationCountQuery query)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Read).Returns(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to read notifications", result.Error);
        await _notificationService.DidNotReceive().GetUnreadCountAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserNotAuthenticated(
        GetUnreadNotificationCountQuery query)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
        await _notificationService.DidNotReceive().GetUnreadCountAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenNoUserIdentifier(
        GetUnreadNotificationCountQuery query)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>(); // No email or other identifier claims
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);
        await _notificationService.DidNotReceive().GetUnreadCountAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldUseAppIdAsUserId_WhenEmailNotAvailable(
        GetUnreadNotificationCountQuery query,
        int expectedCount)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var appId = "test-app-id";
        var claims = new List<Claim>
        {
            new("appid", appId)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _permissionCheckerService.HasPermission(ResourceType.Notifications, appId, AccessType.Read).Returns(true);

        _notificationService.GetUnreadCountAsync(appId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedCount);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCount, result.Value);
        _permissionCheckerService.Received(1).HasPermission(ResourceType.Notifications, appId, AccessType.Read);
        await _notificationService.Received(1).GetUnreadCountAsync(appId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenExceptionThrown(
        GetUnreadNotificationCountQuery query)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Read).Returns(true);

        var exceptionMessage = "Test exception";
        _notificationService.GetUnreadCountAsync(email, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(exceptionMessage, result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnZero_WhenNoUnreadNotifications(
        GetUnreadNotificationCountQuery query)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Read).Returns(true);

        _notificationService.GetUnreadCountAsync(email, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldUseAzpAsUserId_WhenEmailAndAppIdNotAvailable(
        GetUnreadNotificationCountQuery query,
        int expectedCount)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var azp = "test-azp-id";
        var claims = new List<Claim>
        {
            new("azp", azp)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _permissionCheckerService.HasPermission(ResourceType.Notifications, azp, AccessType.Read).Returns(true);

        _notificationService.GetUnreadCountAsync(azp, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedCount);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCount, result.Value);
        _permissionCheckerService.Received(1).HasPermission(ResourceType.Notifications, azp, AccessType.Read);
        await _notificationService.Received(1).GetUnreadCountAsync(azp, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
