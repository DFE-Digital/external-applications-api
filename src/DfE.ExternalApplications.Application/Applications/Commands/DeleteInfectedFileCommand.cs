using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace DfE.ExternalApplications.Application.Applications.Commands;

/// <summary>
/// Internal command to delete infected files without authentication.
/// Used by background consumers for automated file deletion.
/// </summary>
internal sealed record DeleteInfectedFileCommand(FileId FileId) : IRequest<Result<bool>>;

