using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.AutoFixture.Customizations;
using DfE.ExternalApplications.Domain.Entities.Schools;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Models;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates
{
    public class SchoolTests
    {
        [Theory]
        [CustomAutoData(typeof(PrincipalCustomization), typeof(DateOnlyCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenSchoolIdIsNull(
            PrincipalId principalId,
            string schoolName,
            NameDetails nameDetails,
            DateTime lastRefresh,
            DateOnly? endDate,
            PrincipalDetails principalDetails)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new School(null!, principalId, schoolName, nameDetails, lastRefresh, endDate, principalDetails));

            Assert.Equal("schoolId", exception.ParamName);
        }

        [Theory]
        [CustomAutoData(typeof(PrincipalCustomization), typeof(DateOnlyCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenPrincipalIdIsNull(
            SchoolId schoolId,
            string schoolName,
            NameDetails nameDetails,
            DateTime lastRefresh,
            DateOnly? endDate,
            PrincipalDetails principalDetails)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new School(schoolId, null!, schoolName, nameDetails, lastRefresh, endDate, principalDetails));

            Assert.Equal("principalId", exception.ParamName);
        }

        [Theory]
        [CustomAutoData(typeof(PrincipalCustomization), typeof(DateOnlyCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenNameDetailsIsNull(
            SchoolId schoolId,
            PrincipalId principalId,
            string schoolName,
            DateTime lastRefresh,
            DateOnly? endDate,
            PrincipalDetails principalDetails)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new School(schoolId, principalId, schoolName, null!, lastRefresh, endDate, principalDetails));

            Assert.Equal("nameDetails", exception.ParamName);
        }
    }
}
