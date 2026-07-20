using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

/// <summary>
/// Filters submitted applications by submission date (LastModifiedOn when status is Submitted), using inclusive date boundaries.
/// </summary>
public sealed class GetApplicationsByDateSubmittedRangeQueryObject(DateTime? from, DateTime? to)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a =>
            a.Status == ApplicationStatus.Submitted
            && (!from.HasValue || a.LastModifiedOn >= from.Value.Date)
            && (!to.HasValue || a.LastModifiedOn < to.Value.Date.AddDays(1)));
}
