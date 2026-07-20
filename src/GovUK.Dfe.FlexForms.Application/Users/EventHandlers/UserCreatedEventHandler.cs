using GovUK.Dfe.FlexForms.Application.Common.EventHandlers;
using GovUK.Dfe.FlexForms.Domain.Events;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.FlexForms.Application.Users.EventHandlers;

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

