using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;

namespace DfE.ExternalApplications.Application.TemplatePermissions.Queries;

public sealed record GetTemplatePermissionsForUserByUserIdQuery(UserId UserId) 
    : IRequest<Result<IReadOnlyCollection<TemplatePermissionDto>>>; 