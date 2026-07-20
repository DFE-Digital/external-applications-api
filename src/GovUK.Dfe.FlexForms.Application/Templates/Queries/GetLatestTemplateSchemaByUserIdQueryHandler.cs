using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Templates.Queries;

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
                return Result<TemplateSchemaDto>.NotFound("Template version not found");
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
