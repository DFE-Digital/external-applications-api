using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

public sealed class GetApplicationsByReferenceSearchQueryObject(string searchTerm)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.ApplicationReference.ToLower().Contains(searchTerm.ToLower()));
}
