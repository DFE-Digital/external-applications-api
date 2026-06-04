using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Interfaces.Repositories;

/// <summary>
/// Aggregate-root repository for <see cref="Application"/>.
/// </summary>
public interface IApplicationRepository : IEaRepository<Application>
{
    /// <summary>
    /// Returns the latest response for the given application, or null when none exist.
    /// </summary>
    Task<ApplicationResponse?> GetLatestResponseAsync(
        ApplicationId applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Appends a new response version to an application and updates last-modified tracking,
    /// without loading the full aggregate graph (e.g. historic responses).
    /// Returns null if the application does not exist.
    /// </summary>
    Task<(string ApplicationReference, ApplicationResponse Response)?> AppendResponseVersionAsync(
        ApplicationId applicationId,
        ApplicationResponse response,
        DateTime lastModifiedOn,
        UserId lastModifiedBy,
        CancellationToken cancellationToken);
}


