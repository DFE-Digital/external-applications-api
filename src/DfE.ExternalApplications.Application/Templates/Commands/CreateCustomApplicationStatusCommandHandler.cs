using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Templates.Commands
{
    public sealed record CreateCustomApplicationStatusCommand(
        Guid TemplateId,
        int ApplicationStatus,
        string Label) : IRequest<Result<Guid>>;

    public sealed class CreateCustomApplicationStatusCommandHandler(
        IEaRepository<CustomApplicationStatus> customApplicationStatusRepo,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork)
        : IRequestHandler<CreateCustomApplicationStatusCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(CreateCustomApplicationStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                    return Result<Guid>.Forbid("Not authenticated");

                var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");
                if (string.IsNullOrEmpty(principalId))
                    principalId = user.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(principalId))
                    return Result<Guid>.Forbid("No user identifier");

                // assume CreatedBy is the user id claim email/external id mapping exists elsewhere; for now use Guid.NewGuid as placeholder is not acceptable
                // We will attempt to parse a guid claim 'uid' or 'sub' if present
                Guid createdByGuid;
                var uidClaim = user.FindFirstValue("uid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(uidClaim, out createdByGuid))
                {
                    return Result<Guid>.Forbid("Unable to determine user id for CreatedBy");
                }

                var entity = new CustomApplicationStatus(
                    new CustomApplicationStatusId(Guid.NewGuid()),
                    new TemplateId(request.TemplateId),
                    request.ApplicationStatus,
                    request.Label,
                    DateTime.UtcNow,
                    new UserId(createdByGuid));

                await customApplicationStatusRepo.AddAsync(entity, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result<Guid>.Success(entity.Id!.Value);
            }
            catch (Exception e)
            {
                return Result<Guid>.Failure(e.ToString());
            }
        }
    }
}
