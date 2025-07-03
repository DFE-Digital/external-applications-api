using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.Queries;

public sealed record GetLatestTemplateSchemaByUserIdQuery(Guid TemplateId, UserId UserId)
    : IRequest<Result<TemplateSchemaDto>>;

public sealed class GetLatestTemplateSchemaByUserIdQueryHandler(
    IEaRepository<TemplateVersion> versionRepo,
    ISender mediator)
    : IRequestHandler<GetLatestTemplateSchemaByUserIdQuery, Result<TemplateSchemaDto>>
{
    public async Task<Result<TemplateSchemaDto>> Handle(
        GetLatestTemplateSchemaByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var latest = await new GetLatestTemplateVersionForTemplateQueryObject(new TemplateId(request.TemplateId))
                .Apply(versionRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is null)
            {
                return Result<TemplateSchemaDto>.Failure("Template version not found");
            }

            var dto = new TemplateSchemaDto
            {
                VersionNumber = latest.VersionNumber,
                JsonSchema = latest.JsonSchema,
                TemplateId = latest.TemplateId.Value,
                TemplateVersionId = latest.Id!.Value,
            };

            return Result<TemplateSchemaDto>.Success(dto);
        }
        catch (Exception e)
        {
            return Result<TemplateSchemaDto>.Failure(e.ToString());
        }
    }
} 