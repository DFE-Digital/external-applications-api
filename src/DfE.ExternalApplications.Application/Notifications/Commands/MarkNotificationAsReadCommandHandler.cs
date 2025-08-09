using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.CoreLibs.Notifications.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

[RateLimit(10, 60)]
public sealed record MarkNotificationAsReadCommand(string NotificationId) : IRequest<Result<Unit>>, IRateLimitedRequest;

public sealed class MarkNotificationAsReadCommandHandler(
    INotificationService notificationService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<MarkNotificationAsReadCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        MarkNotificationAsReadCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<Unit>.Forbid("Not authenticated");

            await notificationService.MarkAsReadAsync(request.NotificationId, cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure(ex.Message);
        }
    }
}
