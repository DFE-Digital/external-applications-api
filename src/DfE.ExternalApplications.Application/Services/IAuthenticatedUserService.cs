using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.Entities;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Resolves the authenticated user from the current HTTP request against the database.
/// </summary>
public interface IAuthenticatedUserService
{
    /// <summary>
    /// Returns the current authenticated user from the database, or a failure result when unauthenticated or not found.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<User>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
