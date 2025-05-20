using DfE.CoreLibs.AsyncProcessing.Interfaces;
using DfE.ExternalApplications.Application.Services.BackgroundServices.Events;
using DfE.ExternalApplications.Application.Services.BackgroundServices.Tasks;
using MediatR;

namespace DfE.ExternalApplications.Application.Schools.Commands.CreateReport
{
    /// <summary>
    /// An example of enqueuing a background task
    /// </summary>
    public record CreateReportCommand() : IRequest<bool>;

    public class CreateReportCommandHandler( IBackgroundServiceFactory backgroundServiceFactory)
        : IRequestHandler<CreateReportCommand, bool>
    {
        public Task<bool> Handle(CreateReportCommand request, CancellationToken cancellationToken)
        {

            return Task.FromResult(true);
        }
    }
}
