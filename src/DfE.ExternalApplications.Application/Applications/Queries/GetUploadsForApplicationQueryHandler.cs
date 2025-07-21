using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetUploadsForApplicationQuery(ApplicationId ApplicationId) : IRequest<Result<IReadOnlyCollection<UploadDto>>>;

public class GetUploadsForApplicationQueryHandler(IEaRepository<Upload> uploadRepository)
    : IRequestHandler<GetUploadsForApplicationQuery, Result<IReadOnlyCollection<UploadDto>>>
{
    public async Task<Result<IReadOnlyCollection<UploadDto>>> Handle(GetUploadsForApplicationQuery request, CancellationToken cancellationToken)
    {
        var uploads = (await new GetUploadsByApplicationIdQueryObject(request.ApplicationId)
            .Apply(uploadRepository.Query())
            .Select(u => new UploadDto
            {
                Id = u.Id!.Value,
                ApplicationId = u.ApplicationId.Value,
                UploadedBy = u.UploadedBy.Value,
                Name = u.Name,
                Description = u.Description,
                OriginalFileName = u.OriginalFileName,
                FileName = u.FileName,
                UploadedOn = u.UploadedOn
            })
            .ToListAsync(cancellationToken)).AsReadOnly();

        return Result<IReadOnlyCollection<UploadDto>>.Success(uploads);
    }
} 