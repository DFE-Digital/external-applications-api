using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using MockQueryable.NSubstitute;
using NSubstitute;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Templates;

public class GetAccessibleTemplatesQueryHandlerTests
{
    private static readonly TemplateId TemplateA = new(Guid.NewGuid());
    private static readonly TemplateId TemplateB = new(Guid.NewGuid());

    [Fact]
    public async Task Handle_ShouldReturnFullCatalogueIncludingNonLive_WhenAdmin()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var live = new Template(TemplateA, "Alpha", DateTime.UtcNow, createdBy, isLive: true);
        var draft = new Template(TemplateB, "Beta", DateTime.UtcNow, createdBy, isLive: false);
        var templates = new List<Template> { live, draft }.AsQueryable().BuildMockDbSet();

        var templateRepo = Substitute.For<IEaRepository<Template>>();
        templateRepo.Query().Returns(templates);

        var catalogue = Substitute.For<ITenantTemplateCatalogue>();
        catalogue.GetTemplateIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { TemplateA, TemplateB }.AsReadOnly());

        var permissionChecker = Substitute.For<IPermissionCheckerService>();
        permissionChecker.IsAdmin().Returns(true);

        var handler = new GetAccessibleTemplatesQueryHandler(
            templateRepo,
            Substitute.For<IEaRepository<User>>(),
            Substitute.For<IHttpContextAccessor>(),
            catalogue,
            Substitute.For<IUserAccessibleTemplateService>(),
            permissionChecker);

        var result = await handler.Handle(new GetAccessibleTemplatesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, t => t.TemplateId == TemplateA.Value && t.IsLive);
        Assert.Contains(result.Value, t => t.TemplateId == TemplateB.Value && !t.IsLive);
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyLiveAccessibleTemplates_WhenNonAdmin()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var live = new Template(TemplateA, "Alpha", DateTime.UtcNow, createdBy, isLive: true);
        var draft = new Template(TemplateB, "Beta", DateTime.UtcNow, createdBy, isLive: false);
        var templates = new List<Template> { live, draft }.AsQueryable().BuildMockDbSet();

        var templateRepo = Substitute.For<IEaRepository<Template>>();
        templateRepo.Query().Returns(templates);

        var email = "user@education.gov.uk";
        var user = new User(createdBy, new RoleId(Guid.NewGuid()), "User", email, DateTime.UtcNow, null, null, null);
        var userRepo = Substitute.For<IEaRepository<User>>();
        var userDbSet = new List<User> { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(userDbSet);

        var httpContext = Substitute.For<HttpContext>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            authenticationType: "Bearer"));
        httpContext.User.Returns(principal);
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var accessibleService = Substitute.For<IUserAccessibleTemplateService>();
        accessibleService.GetAccessibleTemplateIdsAsync(
                Arg.Any<IEnumerable<TemplatePermission>>(),
                Arg.Any<CancellationToken>())
            .Returns(new[] { TemplateA, TemplateB }.AsReadOnly());

        var permissionChecker = Substitute.For<IPermissionCheckerService>();
        permissionChecker.IsAdmin().Returns(false);

        var handler = new GetAccessibleTemplatesQueryHandler(
            templateRepo,
            userRepo,
            httpContextAccessor,
            Substitute.For<ITenantTemplateCatalogue>(),
            accessibleService,
            permissionChecker);

        var result = await handler.Handle(new GetAccessibleTemplatesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(TemplateA.Value, result.Value!.First().TemplateId);
        Assert.True(result.Value!.First().IsLive);
    }
}
