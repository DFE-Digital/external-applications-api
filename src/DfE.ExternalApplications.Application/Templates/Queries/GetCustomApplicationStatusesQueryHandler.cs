using DfE.ExternalApplications.Application.Templates.Models;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DfE.ExternalApplications.Application.Templates.Queries
{
    public sealed class GetCustomApplicationStatusesQueryHandler(
        IEaRepository<CustomApplicationStatus> repo)
        : IRequestHandler<GetCustomApplicationStatusesQuery, Result<IReadOnlyCollection<CustomApplicationStatusDto>>>
    {
        public async Task<Result<IReadOnlyCollection<CustomApplicationStatusDto>>> Handle(
            GetCustomApplicationStatusesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var items = await new GetCustomApplicationStatusesByTemplateIdQueryObject(request.TemplateId)
                    .Apply(repo.Query().AsNoTracking())
                    .Select(s => new CustomApplicationStatusDto
                    {
                        CustomApplicationStatusId = s.Id!.Value,
                        TemplateId = s.TemplateId.Value,
                        ApplicationStatus = s.ApplicationStatus,
                        Label = s.Label,
                        CreatedOn = s.CreatedOn,
                        CreatedBy = s.CreatedBy.Value
                    })
                    .ToListAsync(cancellationToken);

                return Result<IReadOnlyCollection<CustomApplicationStatusDto>>.Success(items.AsReadOnly());
            }
            catch (Exception e)
            {
                return Result<IReadOnlyCollection<CustomApplicationStatusDto>>.Failure(e.ToString());
            }
        }
    }
}
