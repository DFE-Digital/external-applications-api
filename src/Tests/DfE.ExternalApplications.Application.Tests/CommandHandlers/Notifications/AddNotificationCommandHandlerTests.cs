using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Notifications.Commands;
using DfE.ExternalApplications.Domain.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using NSubstitute.ExceptionExtensions;
using Xunit;
using DfE.ExternalApplications.Tests.Common.Mocks;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Notifications;

public class AddNotificationCommandHandlerTests
{
    private readonly INotificationService _notificationService;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly INotificationSignalRService _notificationSignalRService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AddNotificationCommandHandler _handler;

    public AddNotificationCommandHandlerTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        _notificationSignalRService = new MockNotificationSignalRService();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        _handler = new AddNotificationCommandHandler(
            _notificationService,
            _permissionCheckerService,
            _notificationSignalRService,
            _httpContextAccessor);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldCreateNotification_WhenValidRequestAndUserHasPermission(
        AddNotificationCommand command,
        Notification notification)
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

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Write).Returns(true);

        // Set up the notification to return from the service
        notification.Id = Guid.NewGuid().ToString();
        notification.Message = command.Message;
        notification.Type = command.Type;
        notification.UserId = email;
        notification.CreatedAt = DateTime.UtcNow;
        notification.IsRead = false;
        notification.AutoDismiss = command.AutoDismiss ?? true;
        notification.AutoDismissSeconds = command.AutoDismissSeconds ?? 5;
        notification.Category = command.Category;
        notification.Context = command.Context;
        notification.ActionUrl = command.ActionUrl;
        notification.Metadata = command.Metadata;
        notification.Priority = command.Priority ?? NotificationPriority.Normal;

        _notificationService.AddNotificationAsync(
                command.Message,
                command.Type,
                Arg.Any<NotificationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(notification);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.Message, result.Value.Message);
        Assert.Equal(command.Type, result.Value.Type);
        Assert.Equal(email, result.Value.UserId);

        await _notificationService.Received(1).AddNotificationAsync(
            command.Message,
            command.Type,
            Arg.Is<NotificationOptions>(opts => 
                opts.UserId == email &&
                opts.Category == command.Category &&
                opts.Context == command.Context &&
                opts.AutoDismiss == (command.AutoDismiss ?? true) &&
                opts.AutoDismissSeconds == (command.AutoDismissSeconds ?? 5) &&
                opts.ActionUrl == command.ActionUrl &&
                opts.Metadata == command.Metadata &&
                opts.Priority == (command.Priority ?? NotificationPriority.Normal) &&
                opts.ReplaceExistingContext == (command.ReplaceExistingContext ?? true)),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserDoesNotHavePermission(
        AddNotificationCommand command)
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

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Write).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to create notifications", result.Error);

        await _notificationService.DidNotReceive().AddNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<NotificationType>(),
            Arg.Any<NotificationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserNotAuthenticated(
        AddNotificationCommand command)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);

        await _notificationService.DidNotReceive().AddNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<NotificationType>(),
            Arg.Any<NotificationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenNoUserIdentifier(
        AddNotificationCommand command)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>(); // No email or other identifier claims
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);

        await _notificationService.DidNotReceive().AddNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<NotificationType>(),
            Arg.Any<NotificationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldUseAppIdAsUserId_WhenEmailNotAvailable(
        AddNotificationCommand command,
        Notification notification)
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

        _permissionCheckerService.HasPermission(ResourceType.Notifications, appId, AccessType.Write).Returns(true);

        notification.UserId = appId;
        _notificationService.AddNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<NotificationType>(),
                Arg.Any<NotificationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(notification);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _permissionCheckerService.Received(1).HasPermission(ResourceType.Notifications, appId, AccessType.Write);
        await _notificationService.Received(1).AddNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<NotificationType>(),
            Arg.Is<NotificationOptions>(opts => opts.UserId == appId),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenExceptionThrown(
        AddNotificationCommand command)
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

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Write).Returns(true);

        var exceptionMessage = "Test exception";
        _notificationService.AddNotificationAsync(
                Arg.Any<string>(),
                Arg.Any<NotificationType>(),
                Arg.Any<NotificationOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(exceptionMessage, result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldSendSignalRNotification_WhenNotificationCreatedSuccessfully(
        AddNotificationCommand command,
        Notification notification)
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

        _permissionCheckerService.HasPermission(ResourceType.Notifications, email, AccessType.Write).Returns(true);

        // Set up the notification to return from the service
        notification.Id = Guid.NewGuid().ToString();
        notification.Message = command.Message;
        notification.Type = command.Type;
        notification.UserId = email;
        notification.CreatedAt = DateTime.UtcNow;
        notification.IsRead = false;
        notification.AutoDismiss = command.AutoDismiss ?? true;
        notification.AutoDismissSeconds = command.AutoDismissSeconds ?? 5;
        notification.Category = command.Category;
        notification.Context = command.Context;
        notification.ActionUrl = command.ActionUrl;
        notification.Metadata = command.Metadata;
        notification.Priority = command.Priority ?? NotificationPriority.Normal;

        _notificationService.AddNotificationAsync(
                command.Message,
                command.Type,
                Arg.Any<NotificationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(notification);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var mockService = (MockNotificationSignalRService)_notificationSignalRService;
        Assert.Single(mockService.SentNotifications);
        var sentNotification = mockService.SentNotifications.First();
        Assert.NotNull(sentNotification);
    }
}
