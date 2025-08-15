using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.ExternalApplications.Domain.Services;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

[RateLimit(5, 30)]
public sealed record AddNotificationCommand(
    string Message,
    NotificationType Type,
    string? Category = null,
    string? Context = null,
    bool? AutoDismiss = null,
    int? AutoDismissSeconds = null,
    string? ActionUrl = null,
    Dictionary<string, object>? Metadata = null,
    NotificationPriority? Priority = null,
    bool? ReplaceExistingContext = null) : IRequest<Result<NotificationDto>>, IRateLimitedRequest;

public sealed class AddNotificationCommandHandler(
    INotificationService notificationService,
    IPermissionCheckerService permissionCheckerService,
    INotificationSignalRService notificationSignalRService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<AddNotificationCommand, Result<NotificationDto>>
{
    public async Task<Result<NotificationDto>> Handle(
        AddNotificationCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<NotificationDto>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue(ClaimTypes.Email);
            
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                return Result<NotificationDto>.Forbid("No user identifier");

            var canAccess = permissionCheckerService.HasPermission(ResourceType.Notifications, principalId, AccessType.Write);

            if (!canAccess)
                return Result<NotificationDto>.Forbid("User does not have permission to create notifications");

            var options = new NotificationOptions
            {
                Category = request.Category,
                Context = request.Context,
                AutoDismiss = request.AutoDismiss ?? true,
                AutoDismissSeconds = request.AutoDismissSeconds ?? 5,
                UserId = principalId,
                ActionUrl = request.ActionUrl,
                Metadata = request.Metadata,
                Priority = request.Priority ?? NotificationPriority.Normal,
                ReplaceExistingContext = request.ReplaceExistingContext ?? true
            };

            var notification = await notificationService.AddNotificationAsync(
                request.Message, 
                request.Type, 
                options, 
                cancellationToken);

            var dto = new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                Type = notification.Type,
                Category = notification.Category,
                Context = notification.Context,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                AutoDismiss = notification.AutoDismiss,
                AutoDismissSeconds = notification.AutoDismissSeconds,
                UserId = notification.UserId,
                ActionUrl = notification.ActionUrl,
                Metadata = notification.Metadata,
                Priority = notification.Priority
            };

            // Send real-time notification via SignalR
            await notificationSignalRService.SendNotificationToUserAsync(principalId, dto, cancellationToken);

            return Result<NotificationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<NotificationDto>.Failure(ex.Message);
        }
    }
}
