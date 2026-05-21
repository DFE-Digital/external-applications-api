using AutoFixture;
using AutoFixture.Xunit2;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using Microsoft.AspNetCore.Http;
using MockQueryable;
using NSubstitute;
using System.Security.Claims;
using Xunit.Abstractions;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications
{
    public class GetApplicationsByStatusQueryHandlerTests(ITestOutputHelper testOutput)
    {
        [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
        public async Task Handle_ShouldReturnApplications_WhenUserIsAdmin(
            [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
            ApplicationCustomization appCustom,
            IPermissionCheckerService permissionCheckerService)
        {
            permissionCheckerService.IsAdmin().Returns(true);

            Result<IReadOnlyCollection<ApplicationDto>> result = await GetApplicationsByStatus(appRepo, appCustom, permissionCheckerService);

            Assert.True(result.IsSuccess);
            IReadOnlyCollection<ApplicationDto>? apps = result.Value;
            Assert.NotNull(apps);
            Assert.NotEmpty(apps);
        }

        [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
        public async Task Handle_ShouldReturnApplications_WhenUserIsGlobalApplicationReader(
            [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
            ApplicationCustomization appCustom,
            IPermissionCheckerService permissionCheckerService)
        {
            permissionCheckerService.IsGlobalApplicationReader().Returns(true);

            Result<IReadOnlyCollection<ApplicationDto>> result = await GetApplicationsByStatus(appRepo, appCustom, permissionCheckerService);

            Assert.True(result.IsSuccess);
            IReadOnlyCollection<ApplicationDto>? apps = result.Value;
            Assert.NotNull(apps);
            Assert.NotEmpty(apps);
        }

        [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
        public async Task Handle_ShouldReturnStatusApplications(
            [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
            ApplicationCustomization appCustom,
            IPermissionCheckerService permissionCheckerService)
        {
            permissionCheckerService.IsGlobalApplicationReader().Returns(true);

            Result<IReadOnlyCollection<ApplicationDto>> result = await GetApplicationsByStatus(appRepo, appCustom, permissionCheckerService, ApplicationStatus.Submitted);

            Assert.True(result.IsSuccess);
            IReadOnlyCollection<ApplicationDto>? apps = result.Value;
            Assert.NotNull(apps);
            Assert.NotEmpty(apps);
        }

        private async Task<Result<IReadOnlyCollection<ApplicationDto>>> GetApplicationsByStatus(
            IEaRepository<Domain.Entities.Application> appRepo, 
            ApplicationCustomization appCustom, 
            IPermissionCheckerService permissionCheckerService, 
            ApplicationStatus? status = null)
        {
            Domain.Entities.Application app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
            if (status.HasValue)
            {
                app.GetType().GetProperty("Status")?.SetValue(app, status.Value);
            }
            List<Domain.Entities.Application> appList = [app];
            appRepo.Query().Returns(appList.AsQueryable().BuildMock());

            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var email = "test@example.com";
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, email)
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            httpContextAccessor.HttpContext.Returns(httpContext);

            var handler = new GetApplicationsByStatusQueryHandler(appRepo, permissionCheckerService, httpContextAccessor);
            Result<IReadOnlyCollection<ApplicationDto>> result = await handler.Handle(new GetApplicationsByStatusQuery(status), CancellationToken.None);
            if (!result.IsSuccess) testOutput.WriteLine($"{result.Error} ({result.ErrorCode})");
            return result;
        }
    }
}
