using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;

/// <summary>
/// Lightweight query for write operations that only need the Application aggregate root.
/// Intentionally avoids eager-loading large navigation graphs (e.g. Responses).
/// </summary>
public sealed class GetApplicationByIdForResponseWriteQueryObject(ApplicationId applicationId)
    : IQueryObject<Domain.Entities.Application>
{
    public IQueryable<Domain.Entities.Application> Apply(IQueryable<Domain.Entities.Application> query) =>
        query.Where(a => a.Id == applicationId);
}


