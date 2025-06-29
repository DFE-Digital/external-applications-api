using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed record CreateApplicationCommand(
    Guid TemplateId,
    string InitialResponseBody) : IRequest<Result<ApplicationDto>>; 