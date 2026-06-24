using DfE.ExternalApplications.Application.Templates.Models;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;

namespace DfE.ExternalApplications.Application.Templates.Queries
{
    public sealed record GetCustomApplicationStatusesQuery(Guid TemplateId)
        : IRequest<Result<IReadOnlyCollection<CustomApplicationStatusDto>>>;
}
