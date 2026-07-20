using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;

namespace GovUK.Dfe.FlexForms.Application.Applications.Commands;

/// <summary>
/// Internal command to delete infected files without authentication.
/// Used by background consumers for automated file deletion.
/// </summary>
internal sealed record DeleteInfectedFileCommand(FileId FileId) : IRequest<Result<bool>>;

