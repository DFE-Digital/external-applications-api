using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetApplicationsByIdsQueryObjectTests
{
    [Theory, CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnMatchingApplications(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var app1 = fixture.Create<Domain.Entities.Application>();
        var app2 = fixture.Create<Domain.Entities.Application>();
        var app3 = fixture.Create<Domain.Entities.Application>();

        var ids = new[] { app1.Id!, app3.Id! };
        var list = new List<Domain.Entities.Application> { app1, app2, app3 };

        var sut = new GetApplicationsByIdsQueryObject(ids);
        var result = sut.Apply(list.AsQueryable()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(app1, result);
        Assert.Contains(app3, result);
    }
}