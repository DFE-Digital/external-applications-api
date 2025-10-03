using DfE.ExternalApplications.Application.Common.EventHandlers;
using DfE.ExternalApplications.Domain.Events;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Users.EventHandlers;

public sealed class UserCreatedEventHandler(
    ILogger<UserCreatedEventHandler> logger) : BaseEventHandler<UserCreatedEvent>(logger)
{
    protected override async Task HandleEvent(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("User created: {UserId} - {Email} at {CreatedOn}",
            notification.User.Id!.Value,
            notification.User.Email,
            notification.CreatedOn);

        // Future: Add welcome email or other side effects here
        
        await Task.CompletedTask;
    }
}

