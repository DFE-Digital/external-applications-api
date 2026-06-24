using DfE.ExternalApplications.Application.Templates.Models;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static MassTransit.ValidationResultExtensions;

namespace DfE.ExternalApplications.Application.Templates.Commands
{
    public sealed record UpdateCustomApplicationStatusCommand(
        Guid CustomApplicationStatusId,
        int ApplicationStatus,
        string Label) : IRequest<Result<CustomApplicationStatusDto>>;

    public sealed class UpdateCustomApplicationStatusCommandHandler(
        IEaRepository<CustomApplicationStatus> repo,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateCustomApplicationStatusCommand, Result<CustomApplicationStatusDto>>
    {
        public async Task<Result<CustomApplicationStatusDto>> Handle(UpdateCustomApplicationStatusCommand request, CancellationToken cancellationToken)
        {
            return Result<CustomApplicationStatusDto>.Success(new CustomApplicationStatusDto());
        }
    }
}
