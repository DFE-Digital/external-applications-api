using GovUK.Dfe.FlexForms.Application.Applications.Queries;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.FlexForms.Api.Models.Applications;

/// <summary>
/// Shared query-string parameters for application listing endpoints (pagination, schema, and search filters).
/// </summary>
public class ApplicationListingQueryParameters
{
    /// <summary>
    /// When true, includes the template JSON schema in each application response.
    /// </summary>
    public bool? IncludeSchema { get; init; }

    /// <summary>
    /// One-based page number for paged results.
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int? PageSize { get; init; }

    /// <summary>
    /// Partial or full application reference to search for.
    /// </summary>
    public string? ApplicationReference { get; init; }

    /// <summary>
    /// Inclusive start of the date-started (created) range.
    /// </summary>
    public DateTime? DateStartedFrom { get; init; }

    /// <summary>
    /// Inclusive end of the date-started (created) range.
    /// </summary>
    public DateTime? DateStartedTo { get; init; }

    /// <summary>
    /// Inclusive start of the date-submitted range.
    /// </summary>
    public DateTime? DateSubmittedFrom { get; init; }

    /// <summary>
    /// Inclusive end of the date-submitted range.
    /// </summary>
    public DateTime? DateSubmittedTo { get; init; }

    /// <summary>
    /// Application status filter.
    /// </summary>
    public ApplicationStatus? Status { get; init; }

    /// <summary>
    /// Maps HTTP query parameters to application-layer search criteria.
    /// </summary>
    public ApplicationListingSearchCriteria? ToSearchCriteria() =>
        ApplicationListingSearchCriteria.Create(
            ApplicationReference,
            DateStartedFrom,
            DateStartedTo,
            DateSubmittedFrom,
            DateSubmittedTo,
            Status);

    /// <summary>
    /// Maps HTTP query parameters to a template-scoped listing query.
    /// </summary>
    public GetApplicationsByTemplateQuery ToQuery(Guid templateId) =>
        new(templateId, IncludeSchema ?? false, PageNumber, PageSize, ToSearchCriteria());
}

/// <summary>
/// Query-string parameters for the current user's application listing endpoint.
/// </summary>
public sealed class GetMyApplicationsQueryParameters : ApplicationListingQueryParameters
{
    /// <summary>
    /// Optional template identifier to restrict results to a single template.
    /// </summary>
    public Guid? TemplateId { get; init; }

    /// <summary>
    /// Maps HTTP query parameters to the application-layer query.
    /// </summary>
    public GetMyApplicationsQuery ToQuery() =>
        new(IncludeSchema ?? false, TemplateId, PageNumber, PageSize, ToSearchCriteria());
}
