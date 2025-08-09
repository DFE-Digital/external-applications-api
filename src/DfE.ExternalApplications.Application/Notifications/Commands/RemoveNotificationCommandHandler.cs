using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.CoreLibs.Notifications.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

[RateLimit(10, 60)]
public sealed record RemoveNotificationCommand(string NotificationId) : IRequest<Result<Unit>>, IRateLimitedRequest;

public sealed class RemoveNotificationCommandHandler(
    INotificationService notificationService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<RemoveNotificationCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        RemoveNotificationCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<Unit>.Forbid("Not authenticated");

            await notificationService.RemoveNotificationAsync(request.NotificationId, cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure(ex.Message);
        }
    }
}
