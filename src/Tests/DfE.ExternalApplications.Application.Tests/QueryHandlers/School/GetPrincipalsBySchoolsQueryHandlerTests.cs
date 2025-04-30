using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.AutoFixture.Customizations;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.MappingProfiles;
using DfE.ExternalApplications.Application.Schools.Queries.GetPrincipalsBySchools;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using DfE.ExternalApplications.Tests.Common.Customizations.Models;
using MockQueryable;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.School
{
    public class GetPrincipalsBySchoolsQueryHandlerTests
    {
        [Theory]
        [CustomAutoData(
            typeof(PrincipalCustomization),
            typeof(SchoolCustomization),
            typeof(AutoMapperCustomization<SchoolProfile>))]
        public async Task Handle_ShouldReturnPrincipal_WhenSchoolExists(
            [Frozen] ISchoolRepository mockSchoolRepository,
            [Frozen] ICacheService<IMemoryCacheType> mockCacheService,
            GetPrincipalsBySchoolsQueryHandler handler,
            GetPrincipalsBySchoolsQuery query,
            List<Domain.Entities.Schools.School> schools,
            IFixture fixture)
        {
            // Arrange
            var expectedMps = schools.Select(school =>
                fixture.Customize(new PrincipalCustomization()
                {
                    FirstName = school.NameDetails.NameListAs!.Split(",")[1].Trim(),
                    LastName = school.NameDetails.NameListAs.Split(",")[0].Trim(),
                    SchoolName = school.SchoolName,
                }).Create<Principal>()).ToList();

            var cacheKey = $"Principal_{CacheKeyHelper.GenerateHashedCacheKey(query.SchoolNames)}";

            var mock = schools.BuildMock();

            mockSchoolRepository.GetPrincipalsBySchoolsQueryable(query.SchoolNames)
                .Returns(mock);

            mockCacheService.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<List<Principal>>>>>(), Arg.Any<string>())
                .Returns(callInfo =>
                {
                    var callback = callInfo.ArgAt<Func<Task<Result<List<Principal>>>>>(1);
                    return callback();
                });

            // Act
            var result = await handler.Handle(query, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedMps.Count, result.Value?.Count);
            for (int i = 0; i < result.Value?.Count; i++)
            {
                Assert.Equal(expectedMps[i].FirstName, result.Value?[i].FirstName);
                Assert.Equal(expectedMps[i].LastName, result.Value?[i].LastName);
                Assert.Equal(expectedMps[i].SchoolName, result.Value?[i].SchoolName);
            }

            mockSchoolRepository.Received(1).GetPrincipalsBySchoolsQueryable(query.SchoolNames);
        }
    }
}
