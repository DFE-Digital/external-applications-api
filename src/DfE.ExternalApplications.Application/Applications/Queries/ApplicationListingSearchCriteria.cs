using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Application.Applications.Queries;

/// <summary>
/// Optional filters for application listing endpoints.
/// </summary>
public sealed record ApplicationListingSearchCriteria(
    string? Reference = null,
    DateTime? DateStartedFrom = null,
    DateTime? DateStartedTo = null,
    DateTime? DateSubmittedFrom = null,
    DateTime? DateSubmittedTo = null,
    ApplicationStatus? Status = null)
{
    /// <summary>
    /// Indicates whether any search filter has been specified.
    /// </summary>
    public bool HasAnyFilter =>
        !string.IsNullOrWhiteSpace(Reference) ||
        DateStartedFrom.HasValue ||
        DateStartedTo.HasValue ||
        DateSubmittedFrom.HasValue ||
        DateSubmittedTo.HasValue ||
        Status.HasValue;

    /// <summary>
    /// Builds search criteria from the supplied filter values. Returns null when no filters are specified.
    /// </summary>
    public static ApplicationListingSearchCriteria? Create(
        string? reference = null,
        DateTime? dateStartedFrom = null,
        DateTime? dateStartedTo = null,
        DateTime? dateSubmittedFrom = null,
        DateTime? dateSubmittedTo = null,
        ApplicationStatus? status = null)
    {
        var criteria = new ApplicationListingSearchCriteria(
            reference,
            dateStartedFrom,
            dateStartedTo,
            dateSubmittedFrom,
            dateSubmittedTo,
            status);

        return criteria.HasAnyFilter ? criteria : null;
    }

    /// <summary>
    /// Produces a stable cache key suffix for this search criteria.
    /// </summary>
    public string ToCacheKeySuffix() =>
        $"ref{Reference ?? ""}_dsf{DateStartedFrom:O}_dst{DateStartedTo:O}_subf{DateSubmittedFrom:O}_subt{DateSubmittedTo:O}_st{Status}";
}
