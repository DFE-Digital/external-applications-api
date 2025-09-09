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

public class GetNotificationsByCategoryQueryHandlerTests
{
    private readonly INotificationService _notificationService;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly GetNotificationsByCategoryQueryHandler _handler;

    public GetNotificationsByCategoryQueryHandlerTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        _handler = new GetNotificationsByCategoryQueryHandler(
            _notificationService,
            _permissionCheckerService,
            _httpContextAccessor);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnNotificationsByCategory_WhenValidRequestAndUserHasPermission(
        GetNotificationsByCategoryQuery query,
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
            notification.Category = query.Category;
            notification.CreatedAt = DateTime.UtcNow;
            notification.Type = NotificationType.Info;
            notification.Priority = NotificationPriority.Normal;
        }

        _notificationService.GetNotificationsByCategoryAsync(
                query.Category,
                query.UnreadOnly,
                email,
                Arg.Any<CancellationToken>())
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
            Assert.Equal(notifications[i].Category, resultList[i].Category);
            Assert.Equal(notifications[i].UserId, resultList[i].UserId);
            Assert.Equal(notifications[i].IsRead, resultList[i].IsRead);
        }

        await _notificationService.Received(1).GetNotificationsByCategoryAsync(
            query.Category,
            query.UnreadOnly,
            email,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserDoesNotHavePermission(
        GetNotificationsByCategoryQuery query)
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
        await _notificationService.DidNotReceive().GetNotificationsByCategoryAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserNotAuthenticated(
        GetNotificationsByCategoryQuery query)
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
        await _notificationService.DidNotReceive().GetNotificationsByCategoryAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenNoUserIdentifier(
        GetNotificationsByCategoryQuery query)
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
        await _notificationService.DidNotReceive().GetNotificationsByCategoryAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldUseAppIdAsUserId_WhenEmailNotAvailable(
        GetNotificationsByCategoryQuery query,
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

        _notificationService.GetNotificationsByCategoryAsync(
                query.Category,
                query.UnreadOnly,
                appId,
                Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _permissionCheckerService.Received(1).HasPermission(ResourceType.Notifications, appId, AccessType.Read);
        await _notificationService.Received(1).GetNotificationsByCategoryAsync(
            query.Category,
            query.UnreadOnly,
            appId,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenExceptionThrown(
        GetNotificationsByCategoryQuery query)
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
        _notificationService.GetNotificationsByCategoryAsync(
                query.Category,
                query.UnreadOnly,
                email,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(exceptionMessage, result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldFilterForUnreadOnly_WhenUnreadOnlyIsTrue(
        List<Notification> notifications)
    {
        // Arrange
        var query = new GetNotificationsByCategoryQuery("TestCategory", true);
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Read).Returns(true);

        _notificationService.GetNotificationsByCategoryAsync(
                "TestCategory",
                true,
                email,
                Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _notificationService.Received(1).GetNotificationsByCategoryAsync(
            "TestCategory",
            true,
            email,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnEmptyList_WhenNoCategoryNotifications(
        GetNotificationsByCategoryQuery query)
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

        _notificationService.GetNotificationsByCategoryAsync(
                query.Category,
                query.UnreadOnly,
                email,
                Arg.Any<CancellationToken>())
            .Returns(new List<Notification>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }
}
