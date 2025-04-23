using ExternalApplications.Application.Common.EventHandlers;
using ExternalApplications.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ExternalApplications.Application.Schools.EventHandlers
{
    public class SchoolCreatedEventHandler(ILogger<SchoolCreatedEventHandler> logger)
        : BaseEventHandler<SchoolCreatedEvent>(logger)
    {
        protected override Task HandleEvent(SchoolCreatedEvent notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Test logic for SchoolCreatedEvent executed.");
            return Task.CompletedTask;
        }
    }
}
