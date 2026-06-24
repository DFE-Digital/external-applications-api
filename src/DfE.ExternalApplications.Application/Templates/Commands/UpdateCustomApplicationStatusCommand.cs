using MediatR;
using System;

namespace DfE.ExternalApplications.Application.Templates.Commands
{
    public sealed record UpdateCustomApplicationStatusCommand(
        Guid CustomApplicationStatusId,
        int ApplicationStatus,
        string Label) : IRequest<Result>;
}
