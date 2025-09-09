using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Notifications.Interfaces;
using GovUK.Dfe.CoreLibs.Notifications.Models;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Notifications.Queries;
using DfE.ExternalApplications.Domain.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Notifications;

public class GetUnreadNotificationsQueryHandlerTests
{
    private readonly INotificationService _notificationService;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly GetUnreadNotificationsQueryHandler _handler;

    public GetUnreadNotificationsQueryHandlerTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        _handler = new GetUnreadNotificationsQueryHandler(
            _notificationService,
            _permissionCheckerService,
            _httpContextAccessor);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnNotifications_WhenValidRequestAndUserHasPermission(
        GetUnreadNotificationsQuery query,
        List<Notification> notifications)
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

        // Setup notifications with proper data
        foreach (var notification in notifications)
        {
            notification.UserId = email;
            notification.IsRead = false;
            notification.CreatedAt = DateTime.UtcNow;
            notification.Type = NotificationType.Info;
            notification.Priority = NotificationPriority.Normal;
        }

        _notificationService.GetUnreadNotificationsAsync(email, Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(notifications.Count, result.Value.Count());

        var resultList = result.Value.ToList();
        for (int i = 0; i < notifications.Count; i++)
        {
            Assert.Equal(notifications[i].Id, resultList[i].Id);
            Assert.Equal(notifications[i].Message, resultList[i].Message);
            Assert.Equal(notifications[i].Type, resultList[i].Type);
            Assert.Equal(notifications[i].UserId, resultList[i].UserId);
            Assert.Equal(notifications[i].IsRead, resultList[i].IsRead);
        }

        await _notificationService.Received(1).GetUnreadNotificationsAsync(email, Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserDoesNotHavePermission(
        GetUnreadNotificationsQuery query)
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
        await _notificationService.DidNotReceive().GetUnreadNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserNotAuthenticated(
        GetUnreadNotificationsQuery query)
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
        await _notificationService.DidNotReceive().GetUnreadNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenNoUserIdentifier(
        GetUnreadNotificationsQuery query)
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
        await _notificationService.DidNotReceive().GetUnreadNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldUseAppIdAsUserId_WhenEmailNotAvailable(
        GetUnreadNotificationsQuery query,
        List<Notification> notifications)
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

        _notificationService.GetUnreadNotificationsAsync(appId, Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _permissionCheckerService.Received(1).HasPermission(ResourceType.Notifications, appId, AccessType.Read);
        await _notificationService.Received(1).GetUnreadNotificationsAsync(appId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenExceptionThrown(
        GetUnreadNotificationsQuery query)
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
        _notificationService.GetUnreadNotificationsAsync(email, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(exceptionMessage, result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnEmptyList_WhenNoUnreadNotifications(
        GetUnreadNotificationsQuery query)
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

        _notificationService.GetUnreadNotificationsAsync(email, Arg.Any<CancellationToken>())
            .Returns(new List<Notification>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }
}
