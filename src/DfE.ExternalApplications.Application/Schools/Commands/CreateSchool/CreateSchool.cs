using DfE.ExternalApplications.Application.Schools.Models;
using DfE.ExternalApplications.Domain.Entities.Schools;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;

namespace DfE.ExternalApplications.Application.Schools.Commands.CreateSchool
{
    public record CreateSchoolCommand(
        string SchoolName,
        DateTime LastRefresh,
        DateOnly? EndDate,
        NameDetailsModel NameDetails,
        PrincipalDetailsModel PrincipalDetails
    ) : IRequest<SchoolId>;

    public class CreateSchoolCommandHandler(IEaRepository<School> schoolRepository)
        : IRequestHandler<CreateSchoolCommand, SchoolId>
    {
        public async Task<SchoolId> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
        {
            var school = School.Create(
                request.SchoolName,
                new NameDetails(request.NameDetails.FirstName, request.NameDetails.FirstName, request.NameDetails.FirstName),
                request.LastRefresh,
                request.EndDate,
                request.PrincipalDetails.Email,
                request.PrincipalDetails.Phone,
                request.PrincipalDetails.TypeId
            );

            await schoolRepository.AddAsync(school, cancellationToken);

            return school.Id!;
        }
    }
}
