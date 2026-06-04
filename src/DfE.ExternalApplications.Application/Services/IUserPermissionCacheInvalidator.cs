using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Invalidates cached permission data for a user so subsequent authentication loads current grants.
/// </summary>
public interface IUserPermissionCacheInvalidator
{
    /// <summary>
    /// Removes cached permission claims and related permission queries for the specified user.
    /// </summary>
    /// <param name="email">The user's email address used for claim cache keys.</param>
    /// <param name="userId">The user's identifier used for permission query cache keys.</param>
    void InvalidateForUser(string email, UserId userId);
}
