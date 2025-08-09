using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using DfE.CoreLibs.Notifications.Interfaces;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

[RateLimit(1, 30)]
public sealed record ClearAllNotificationsCommand() : IRequest<Result<Unit>>, IRateLimitedRequest;

public sealed class ClearAllNotificationsCommandHandler(
    INotificationService notificationService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<ClearAllNotificationsCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        ClearAllNotificationsCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<Unit>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue(ClaimTypes.Email);
            
            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                return Result<Unit>.Forbid("No user identifier");

            await notificationService.ClearAllNotificationsAsync(principalId, cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure(ex.Message);
        }
    }
}
