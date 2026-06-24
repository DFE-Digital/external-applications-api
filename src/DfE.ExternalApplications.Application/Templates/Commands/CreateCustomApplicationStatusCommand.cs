using MediatR;
using System;

namespace DfE.ExternalApplications.Application.Templates.Commands
{
    public sealed record CreateCustomApplicationStatusCommand(
        Guid TemplateId,
        int ApplicationStatus,
        string Label) : IRequest<Result<Guid>>;
}
