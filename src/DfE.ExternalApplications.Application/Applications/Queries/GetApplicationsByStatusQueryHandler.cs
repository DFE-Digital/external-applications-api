using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetApplicationsByStatusQuery(ApplicationStatus Status)
    : IRequest<Result<IReadOnlyCollection<ApplicationDto>>>;

public sealed class GetApplicationsByStatusQueryHandler(
    IEaRepository<Domain.Entities.Application> appRepo)
{
    public async Task<Result<IReadOnlyCollection<ApplicationDto>>> Handle(
        GetApplicationsByStatusQuery request,
        CancellationToken cancellationToken)
    {
        // TODO SP check user has permission ()

        GetApplicationsByStatusQueryObject queryObject = new(request.Status);

        IQueryable<Domain.Entities.Application> objQuery = queryObject.Apply(appRepo.Query().AsNoTracking());

        List<Domain.Entities.Application> apps = await objQuery.ToListAsync(cancellationToken);

        var dtoList = apps.Select(a => new ApplicationDto
        {
            ApplicationId = a.Id!.Value,
            ApplicationReference = a.ApplicationReference,
            TemplateVersionId = a.TemplateVersionId.Value,
            DateCreated = a.CreatedOn,
            DateSubmitted = a.Status == ApplicationStatus.Submitted ? a.LastModifiedOn : null,
            Status = a.Status
        }).ToList().AsReadOnly();

        return Result<IReadOnlyCollection<ApplicationDto>>.Success(dtoList);
    }
}
