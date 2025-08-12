using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Notifications.Interfaces;
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

public class RemoveNotificationCommandHandlerTests
{
    private readonly INotificationService _notificationService;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly INotificationSignalRService _notificationSignalRService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RemoveNotificationCommandHandler _handler;

    public RemoveNotificationCommandHandlerTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        _notificationSignalRService = new MockNotificationSignalRService();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        _handler = new RemoveNotificationCommandHandler(
            _notificationService,
            _permissionCheckerService,
            _notificationSignalRService,
            _httpContextAccessor);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldRemoveNotification_WhenValidRequestAndUserHasPermission(
        RemoveNotificationCommand command)
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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _notificationService.Received(1).RemoveNotificationAsync(command.NotificationId, Arg.Any<CancellationToken>());
        
        // Verify SignalR notification was sent
        var mockService = (MockNotificationSignalRService)_notificationSignalRService;
        Assert.Single(mockService.DeletedNotificationIds);
        Assert.Equal(command.NotificationId, mockService.DeletedNotificationIds.First());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserDoesNotHavePermission(
        RemoveNotificationCommand command)
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
        Assert.Equal("User does not have permission to modify notifications", result.Error);
        await _notificationService.DidNotReceive().RemoveNotificationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnForbidden_WhenUserNotAuthenticated(
        RemoveNotificationCommand command)
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
        await _notificationService.DidNotReceive().RemoveNotificationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenExceptionThrown(
        RemoveNotificationCommand command)
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
        _notificationService.RemoveNotificationAsync(command.NotificationId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(exceptionMessage, result.Error);
    }
}
