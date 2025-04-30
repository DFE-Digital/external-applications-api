using DfE.CoreLibs.AsyncProcessing.Interfaces;
using DfE.ExternalApplications.Application.Services.BackgroundServices.Events;

namespace DfE.ExternalApplications.Application.Services.BackgroundServices.EventHandlers
{
    public class SimpleTaskCompletedEventHandler : IBackgroundServiceEventHandler<CreateReportExampleTaskCompletedEvent>
    {
        public Task Handle(CreateReportExampleTaskCompletedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Event received for Task: {notification.TaskName}, Message: {notification.Message}");
            return Task.CompletedTask;
        }
    }
}
