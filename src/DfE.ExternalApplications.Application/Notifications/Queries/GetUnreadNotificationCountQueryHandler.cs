using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Notifications.Interfaces;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Notifications.Queries;

public sealed record GetUnreadNotificationCountQuery(string? Context = null, string? Category = null) : IRequest<Result<int>>;

public sealed class GetUnreadNotificationCountQueryHandler(
    INotificationService notificationService,
    IPermissionCheckerService permissionCheckerService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetUnreadNotificationCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetUnreadNotificationCountQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<int>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue(ClaimTypes.Email);
            
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                return Result<int>.Forbid("No user identifier");

            var canAccess = permissionCheckerService.HasPermission(ResourceType.Notifications, principalId, AccessType.Read);
            if (!canAccess)
                return Result<int>.Forbid("User does not have permission to read notifications");

            var count = await notificationService.GetUnreadCountAsync(principalId, request.Context, request.Category, cancellationToken);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(ex.Message);
        }
    }
}
