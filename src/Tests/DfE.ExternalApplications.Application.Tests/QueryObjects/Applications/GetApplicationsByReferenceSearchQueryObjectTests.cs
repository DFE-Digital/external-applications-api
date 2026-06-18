using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetApplicationsByReferenceSearchQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnMatchingApplications_WhenReferenceContainsSearchTerm(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var matchingApp = fixture.Create<Domain.Entities.Application>();
        var nonMatchingApp = fixture.Create<Domain.Entities.Application>();

        typeof(Domain.Entities.Application).GetProperty("ApplicationReference")!
            .SetValue(matchingApp, "APP-2024-001");
        typeof(Domain.Entities.Application).GetProperty("ApplicationReference")!
            .SetValue(nonMatchingApp, "XYZ-9999-999");

        var queryable = new[] { matchingApp, nonMatchingApp }.AsQueryable().BuildMock();
        var result = new GetApplicationsByReferenceSearchQueryObject("APP-2024").Apply(queryable).ToList();

        Assert.Single(result);
        Assert.Equal(matchingApp, result.First());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldBeCaseInsensitive(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var app = fixture.Create<Domain.Entities.Application>();

        typeof(Domain.Entities.Application).GetProperty("ApplicationReference")!
            .SetValue(app, "APP-2024-001");

        var queryable = new[] { app }.AsQueryable().BuildMock();
        var result = new GetApplicationsByReferenceSearchQueryObject("app-2024").Apply(queryable).ToList();

        Assert.Single(result);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldSupportPartialMatch(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var app1 = fixture.Create<Domain.Entities.Application>();
        var app2 = fixture.Create<Domain.Entities.Application>();
        var app3 = fixture.Create<Domain.Entities.Application>();

        typeof(Domain.Entities.Application).GetProperty("ApplicationReference")!
            .SetValue(app1, "APP-2024-001");
        typeof(Domain.Entities.Application).GetProperty("ApplicationReference")!
            .SetValue(app2, "APP-2024-002");
        typeof(Domain.Entities.Application).GetProperty("ApplicationReference")!
            .SetValue(app3, "XYZ-0001-000");

        var queryable = new[] { app1, app2, app3 }.AsQueryable().BuildMock();
        var result = new GetApplicationsByReferenceSearchQueryObject("APP-2024").Apply(queryable).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(app3, result);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoApplicationsMatch(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var app = fixture.Create<Domain.Entities.Application>();

        typeof(Domain.Entities.Application).GetProperty("ApplicationReference")!
            .SetValue(app, "APP-2024-001");

        var queryable = new[] { app }.AsQueryable().BuildMock();
        var result = new GetApplicationsByReferenceSearchQueryObject("NOMATCH").Apply(queryable).ToList();

        Assert.Empty(result);
    }
}
