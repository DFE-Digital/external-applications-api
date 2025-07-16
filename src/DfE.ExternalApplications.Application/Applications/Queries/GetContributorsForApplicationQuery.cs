using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.Commands;
using MediatR;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetContributorsForApplicationQuery(
    Guid ApplicationId) : IRequest<Result<IReadOnlyCollection<UserDto>>>; 