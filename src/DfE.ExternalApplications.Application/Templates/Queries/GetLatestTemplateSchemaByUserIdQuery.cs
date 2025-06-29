using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;

namespace DfE.ExternalApplications.Application.Templates.Queries;

public sealed record GetLatestTemplateSchemaByUserIdQuery(Guid TemplateId, UserId UserId)
    : IRequest<Result<TemplateSchemaDto>>;

