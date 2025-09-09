using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Notifications.Interfaces;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Notifications.Queries;

public sealed record GetNotificationsByCategoryQuery(string Category, bool UnreadOnly = false) : IRequest<Result<IEnumerable<NotificationDto>>>;

public sealed class GetNotificationsByCategoryQueryHandler(
    INotificationService notificationService,
    IPermissionCheckerService permissionCheckerService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetNotificationsByCategoryQuery, Result<IEnumerable<NotificationDto>>>
{
    public async Task<Result<IEnumerable<NotificationDto>>> Handle(
        GetNotificationsByCategoryQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<IEnumerable<NotificationDto>>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue(ClaimTypes.Email);
            
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                return Result<IEnumerable<NotificationDto>>.Forbid("No user identifier");

            var canAccess = permissionCheckerService.HasPermission(ResourceType.Notifications, principalId, AccessType.Read);
            if (!canAccess)
                return Result<IEnumerable<NotificationDto>>.Forbid("User does not have permission to read notifications");

            var notifications = await notificationService.GetNotificationsByCategoryAsync(
                request.Category, 
                request.UnreadOnly, 
                principalId, 
                cancellationToken);

            var dtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                Category = n.Category,
                Context = n.Context,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                AutoDismiss = n.AutoDismiss,
                AutoDismissSeconds = n.AutoDismissSeconds,
                UserId = n.UserId,
                ActionUrl = n.ActionUrl,
                Metadata = n.Metadata,
                Priority = n.Priority
            });

            return Result<IEnumerable<NotificationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<NotificationDto>>.Failure(ex.Message);
        }
    }
}
