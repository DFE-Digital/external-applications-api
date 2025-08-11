using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Domain.Services;
using DfE.CoreLibs.Notifications.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

[RateLimit(5, 30)]
public sealed record ClearNotificationsByContextCommand(string Context) : IRequest<Result<bool>>, IRateLimitedRequest;

public sealed class ClearNotificationsByContextCommandHandler(
    INotificationService notificationService,
    IPermissionCheckerService permissionCheckerService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<ClearNotificationsByContextCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ClearNotificationsByContextCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<bool>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue(ClaimTypes.Email);
            
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                return Result<bool>.Forbid("No user identifier");

            var canAccess = permissionCheckerService.HasPermission(ResourceType.Notifications, principalId, AccessType.Write);
            if (!canAccess)
                return Result<bool>.Forbid("User does not have permission to modify notifications");

            await notificationService.ClearNotificationsByContextAsync(principalId, request.Context, cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
    }
}