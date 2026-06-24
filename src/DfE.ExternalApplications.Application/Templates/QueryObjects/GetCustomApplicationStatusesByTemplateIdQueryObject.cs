using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using System;

namespace DfE.ExternalApplications.Application.Templates.QueryObjects
{
    public sealed class GetCustomApplicationStatusesByTemplateIdQueryObject : DfE.ExternalApplications.Application.Common.QueryObjects.IQueryObject<CustomApplicationStatus>
    {
        private readonly TemplateId _templateId;

        public GetCustomApplicationStatusesByTemplateIdQueryObject(Guid templateId)
        {
            _templateId = new TemplateId(templateId);
        }

        public IQueryable<CustomApplicationStatus> Apply(IQueryable<CustomApplicationStatus> query)
        {
            return query.Where(x => x.TemplateId == _templateId).OrderBy(x => x.ApplicationStatus);
        }
    }
}
