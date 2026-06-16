using DfE.ExternalApplications.Application.Common.QueriesObjects;

namespace DfE.ExternalApplications.Application.Applications.QueryObjects;

/// <summary>
/// Filters applications by date started (CreatedOn), using inclusive date boundaries.
/// </summary>
public sealed class GetApplicationsByDateStartedRangeQueryObject(DateTime? from, DateTime? to)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query)
    {
        if (from.HasValue)
            query = query.Where(a => a.CreatedOn >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(a => a.CreatedOn < to.Value.Date.AddDays(1));

        return query;
    }
}
