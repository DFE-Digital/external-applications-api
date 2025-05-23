using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Validators;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Entities.Schools
{
#pragma warning disable CS8618
    public sealed class School : BaseAggregateRoot, IEntity<SchoolId>
    {
        public SchoolId Id { get; }
        public PrincipalId PrincipalId { get; private set; }
        public string SchoolName { get; private set; }
        public NameDetails NameDetails { get; private set; }
        public DateTime LastRefresh { get; private set; }
        public DateOnly? EndDate { get; private set; }

        public PrincipalDetails PrincipalDetails { get; private set; }

        private School() { }

        public School(
            SchoolId schoolId,
            PrincipalId principalId,
            string schoolName,
            NameDetails nameDetails,
            DateTime lastRefresh,
            DateOnly? endDate,
            PrincipalDetails principalDetails)
        {
            Id = schoolId ?? throw new ArgumentNullException(nameof(schoolId));
            PrincipalId = principalId ?? throw new ArgumentNullException(nameof(principalId));
            SchoolName = schoolName;
            NameDetails = nameDetails ?? throw new ArgumentNullException(nameof(nameDetails));
            LastRefresh = lastRefresh;
            EndDate = endDate;
            PrincipalDetails = principalDetails;
        }

        private School(
            string schoolName,
            NameDetails nameDetails,
            DateTime lastRefresh,
            DateOnly? endDate,
            PrincipalDetails principalDetails)
        {
            SchoolName = schoolName ?? throw new ArgumentNullException(nameof(schoolName));
            NameDetails = nameDetails ?? throw new ArgumentNullException(nameof(nameDetails));
            LastRefresh = lastRefresh;
            EndDate = endDate;
            PrincipalDetails = principalDetails;
        }

        public static School Create(
            string schoolName,
            NameDetails nameDetails,
            DateTime lastRefresh,
            DateOnly? endDate,
            string principalEmail,
            string principalPhone,
            int principalTypeId)
        {
            var principalDetails = new PrincipalDetails(principalTypeId, principalEmail, principalPhone);

            var school = new School(schoolName, nameDetails, lastRefresh, endDate, principalDetails);

            var createValidator = new SchoolCreateValidator();

            createValidator.ValidateAndThrow(school);

            school.AddDomainEvent(new SchoolCreatedEvent(school));

            return school;
        }
    }
#pragma warning restore CS8618
}
