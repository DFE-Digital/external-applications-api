using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Application.Services;

/// <summary>
/// Creates new application aggregates with generated references.
/// </summary>
public interface IApplicationCreationService
{
    /// <summary>
    /// Builds a new application aggregate with an initial response version.
    /// </summary>
    /// <param name="templateVersionId">The template version to use.</param>
    /// <param name="initialResponseBody">The initial encoded response body.</param>
    /// <param name="createdBy">The creating user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<(Domain.Entities.Application Application, ApplicationResponse Response)> CreateAsync(
        TemplateVersionId templateVersionId,
        string initialResponseBody,
        UserId createdBy,
        CancellationToken cancellationToken = default);
}
