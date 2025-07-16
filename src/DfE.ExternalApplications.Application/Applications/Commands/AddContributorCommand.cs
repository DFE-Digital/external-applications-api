using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.Commands;
using MediatR;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed record AddContributorCommand(
    Guid ApplicationId,
    string Name,
    string Email) : IRequest<Result<UserDto>>; 