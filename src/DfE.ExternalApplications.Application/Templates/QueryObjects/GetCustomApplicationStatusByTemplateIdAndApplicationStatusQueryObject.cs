using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects;

/// <summary>
/// Filters custom application statuses by template and application status.
/// </summary>
public sealed class GetCustomApplicationStatusByTemplateIdAndApplicationStatusQueryObject(
    Guid templateId,
    ApplicationStatus applicationStatus) : IQueryObject<CustomApplicationStatus>
{
    private readonly TemplateId _templateId = new(templateId);

    public IQueryable<CustomApplicationStatus> Apply(IQueryable<CustomApplicationStatus> query) =>
        query.Where(x => x.TemplateId == _templateId && x.ApplicationStatus == applicationStatus);
}
