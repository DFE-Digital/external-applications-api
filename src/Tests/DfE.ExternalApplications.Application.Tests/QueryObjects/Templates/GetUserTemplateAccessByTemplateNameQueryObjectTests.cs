using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Templates;

public class GetUserTemplateAccessByTemplateNameQueryObjectTests
{
    [Theory, CustomAutoData(typeof(UserTemplateAccessCustomization), typeof(TemplateCustomization))]
    public void Apply_ShouldReturnMatchingAccess_WhenUserAndTemplateNameMatch(
        Guid userGuid,
        string templateName,
        UserTemplateAccessCustomization accessCustom,
        TemplateCustomization templateCustom,
        [Frozen] IList<UserTemplateAccess> accessList)
    {
        templateCustom.OverrideName = templateName;
        var fixture = new Fixture().Customize(templateCustom);
        var template = fixture.Create<Template>();

        accessCustom.OverrideUserId = new UserId(userGuid);
        accessCustom.OverrideTemplateId = template.Id;
        var fixtureAccess = new Fixture().Customize(accessCustom);
        var matching = fixtureAccess.Create<UserTemplateAccess>();

        // attach template via reflection
        typeof(UserTemplateAccess)
            .GetProperty(nameof(UserTemplateAccess.Template))!
            .SetValue(matching, template);

        var otherTemplate = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var otherAccess = new Fixture().Customize(new UserTemplateAccessCustomization
        {
            OverrideUserId = new UserId(Guid.NewGuid()),
            OverrideTemplateId = otherTemplate.Id
        }).Create<UserTemplateAccess>();
        typeof(UserTemplateAccess)
            .GetProperty(nameof(UserTemplateAccess.Template))!
            .SetValue(otherAccess, otherTemplate);

        accessList.Clear();
        accessList.Add(matching);
        accessList.Add(otherAccess);

        var sut = new GetUserTemplateAccessByTemplateNameQueryObject(userGuid, templateName);
        var result = sut.Apply(accessList.AsQueryable()).ToList();

        Assert.Single(result);
        Assert.Equal(matching, result[0]);
    }
}
