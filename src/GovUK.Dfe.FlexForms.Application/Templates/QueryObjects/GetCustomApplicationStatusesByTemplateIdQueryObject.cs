using GovUK.Dfe.FlexForms.Application.Common.QueriesObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using System;
using System.Linq;

namespace GovUK.Dfe.FlexForms.Application.Templates.QueryObjects
{
    public sealed class GetCustomApplicationStatusesByTemplateIdQueryObject : IQueryObject<CustomApplicationStatus>
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
