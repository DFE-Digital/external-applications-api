using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed record RemoveContributorCommand(
    Guid ApplicationId,
    Guid UserId) : IRequest<Result<bool>>; 