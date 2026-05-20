using AutoFixture;
using AutoFixture.Xunit2;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using MockQueryable;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications
{
    public class GetApplicationsByStatusQueryHandlerTests
    {
        [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
        public async Task Handle_ShouldReturnSubmittedApplications_WhenUserHasPermission(
            [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
            ApplicationCustomization appCustom
            )
        {
            // TODO SP setup dependencies

            Domain.Entities.Application app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
            app.GetType().GetProperty("Status")?.SetValue(app, ApplicationStatus.Submitted);
            List<Domain.Entities.Application> appList = [app];
            appRepo.Query().Returns(appList.AsQueryable().BuildMock());

            var handler = new GetApplicationsByStatusQueryHandler(appRepo);
            Result<IReadOnlyCollection<ApplicationDto>> result = await handler.Handle(new GetApplicationsByStatusQuery(ApplicationStatus.Submitted), CancellationToken.None);

            // TODO SP check result
            Assert.True(result.IsSuccess);
            IReadOnlyCollection<ApplicationDto>? apps = result.Value;
            Assert.NotNull(apps);
            Assert.True(apps.Any());
        }
    }
}
