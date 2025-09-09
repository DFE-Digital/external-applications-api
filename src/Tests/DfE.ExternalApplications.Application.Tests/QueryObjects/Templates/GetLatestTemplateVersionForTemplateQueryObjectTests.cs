using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Templates;

public class GetLatestTemplateVersionForTemplateQueryObjectTests
{
    [Theory, CustomAutoData(typeof(TemplateVersionCustomization))]
    public void Apply_ShouldReturnLatestVersion_ForTemplate(
        TemplateVersionCustomization tvCustom)
    {
        var templateId = new TemplateId(Guid.NewGuid());

        tvCustom.OverrideTemplateId = templateId;
        tvCustom.OverrideCreatedOn = DateTime.UtcNow.AddDays(-1);
        var fixture = new Fixture().Customize(tvCustom);
        var older = fixture.Create<TemplateVersion>();

        var newerCustomization = new TemplateVersionCustomization
        {
            OverrideTemplateId = templateId,
            OverrideCreatedOn = DateTime.UtcNow
        };
        var newer = new Fixture().Customize(newerCustomization).Create<TemplateVersion>();

        var list = new List<TemplateVersion> { older, newer };

        var sut = new GetLatestTemplateVersionForTemplateQueryObject(templateId);
        var result = sut.Apply(list.AsQueryable()).Take(1).ToList();

        Assert.Single(result);
        Assert.Equal(newer, result[0]);
    }
}
