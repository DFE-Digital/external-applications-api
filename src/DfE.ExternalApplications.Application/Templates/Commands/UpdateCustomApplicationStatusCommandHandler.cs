using DfE.ExternalApplications.Application.Templates.Models;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static MassTransit.ValidationResultExtensions;

namespace DfE.ExternalApplications.Application.Templates.Commands
{
    /// <summary>
    /// Command to create or update a custom application status for a template.
    /// If the status exists (by TemplateId and ApplicationStatus), it will be updated.
    /// Otherwise, a new one will be created.
    /// </summary>
    public sealed record UpdateCustomApplicationStatusCommand(
        Guid TemplateId,
        ApplicationStatus ApplicationStatus,
        string? Label) : IRequest<Result<CustomApplicationStatusDto>>;

    public sealed class UpdateCustomApplicationStatusCommandHandler(
        IEaRepository<CustomApplicationStatus> customApplicationStatusRepo,
        IEaRepository<User> userRepo,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateCustomApplicationStatusCommand, Result<CustomApplicationStatusDto>>
    {
        public async Task<Result<CustomApplicationStatusDto>> Handle(UpdateCustomApplicationStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if custom status exists for this template and application status
                var existing = await customApplicationStatusRepo.Query()
                    .FirstOrDefaultAsync(x => x.TemplateId == new TemplateId(request.TemplateId) && x.ApplicationStatus == request.ApplicationStatus, cancellationToken);

                if (existing is not null)
                {
                    // Update existing
                    existing.UpdateLabel(request.Label);
                    await unitOfWork.CommitAsync(cancellationToken);

                    var dto = new CustomApplicationStatusDto
                    {
                        CustomApplicationStatusId = existing.Id!.Value,
                        TemplateId = existing.TemplateId.Value,
                        ApplicationStatus = existing.ApplicationStatus,
                        Label = existing.Label,
                        CreatedOn = existing.CreatedOn,
                        CreatedBy = existing.CreatedBy.Value
                    };

                    return Result<CustomApplicationStatusDto>.Success(dto);
                }

                // Not found - create new
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                    return Result<CustomApplicationStatusDto>.Forbid("Not authenticated");

                var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");
                if (string.IsNullOrEmpty(principalId))
                    principalId = user.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(principalId))
                    return Result<CustomApplicationStatusDto>.Forbid("No user identifier");

                User? dbUser;
                if (principalId.Contains('@'))
                {
                    dbUser = await new DfE.ExternalApplications.Application.Users.QueryObjects.GetUserByEmailQueryObject(principalId)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);
                }
                else
                {
                    dbUser = await new DfE.ExternalApplications.Application.Users.QueryObjects.GetUserByExternalProviderIdQueryObject(principalId)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (dbUser is null || dbUser.Id is null)
                    return Result<CustomApplicationStatusDto>.Forbid("Unable to resolve CreatedBy user");

                var createdByUserId = dbUser.Id;

                var entity = new CustomApplicationStatus(
                    new CustomApplicationStatusId(Guid.NewGuid()),
                    new TemplateId(request.TemplateId),
                    request.ApplicationStatus,
                    request.Label,
                    DateTime.UtcNow,
                    createdByUserId);

                await customApplicationStatusRepo.AddAsync(entity, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                var createdDto = new CustomApplicationStatusDto
                {
                    CustomApplicationStatusId = entity.Id!.Value,
                    TemplateId = entity.TemplateId.Value,
                    ApplicationStatus = entity.ApplicationStatus,
                    Label = entity.Label,
                    CreatedOn = entity.CreatedOn,
                    CreatedBy = entity.CreatedBy.Value
                };

                return Result<CustomApplicationStatusDto>.Success(createdDto);
            }
            catch (Exception e)
            {
                return Result<CustomApplicationStatusDto>.Failure(e.ToString());
            }
        }
    }
}
