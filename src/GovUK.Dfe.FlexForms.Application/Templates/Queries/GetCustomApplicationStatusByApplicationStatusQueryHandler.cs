using GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Templates.Queries
{
    public sealed record GetCustomApplicationStatusByApplicationStatusQuery(Guid TemplateId, ApplicationStatus ApplicationStatus)
        : IRequest<Result<CustomApplicationStatusDto>>;

    public sealed class GetCustomApplicationStatusByApplicationStatusQueryHandler(
        IEaRepository<CustomApplicationStatus> customApplicationStatusRepo)
        : IRequestHandler<GetCustomApplicationStatusByApplicationStatusQuery, Result<CustomApplicationStatusDto>>
    {
        public async Task<Result<CustomApplicationStatusDto>> Handle(GetCustomApplicationStatusByApplicationStatusQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await new GetCustomApplicationStatusByTemplateIdAndApplicationStatusQueryObject(
                        request.TemplateId,
                        request.ApplicationStatus)
                    .Apply(customApplicationStatusRepo.Query())
                    .FirstOrDefaultAsync(cancellationToken);

                if (entity is null)
                    return Result<CustomApplicationStatusDto>.NotFound("Custom application status not found");

                var dto = new CustomApplicationStatusDto
                {
                    CustomApplicationStatusId = entity.Id!.Value,
                    TemplateId = entity.TemplateId.Value,
                    ApplicationStatus = entity.ApplicationStatus,
                    Label = entity.Label,
                    CreatedOn = entity.CreatedOn,
                    CreatedBy = entity.CreatedBy.Value
                };

                return Result<CustomApplicationStatusDto>.Success(dto);
            }
            catch (Exception e)
            {
                return Result<CustomApplicationStatusDto>.Failure(e.ToString());
            }
        }
    }
}
