using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates;

public class UserTemplateAccessTests
{
    [Theory]
    [CustomAutoData(typeof(UserTemplateAccessCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        UserId userId,
        TemplateId templateId,
        DateTime grantedOn,
        UserId grantedBy)
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new UserTemplateAccess(
                null!,        // id
                userId,
                templateId,
                grantedOn,
                grantedBy));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(UserTemplateAccessCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenUserIdIsNull(
        UserTemplateAccessId id,
        TemplateId templateId,
        DateTime grantedOn,
        UserId grantedBy)
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new UserTemplateAccess(
                id,
                null!,        // userId
                templateId,
                grantedOn,
                grantedBy));

        Assert.Equal("userId", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(UserTemplateAccessCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenTemplateIdIsNull(
        UserTemplateAccessId id,
        UserId userId,
        DateTime grantedOn,
        UserId grantedBy)
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new UserTemplateAccess(
                id,
                userId,
                null!,       // templateId
                grantedOn,
                grantedBy));

        Assert.Equal("templateId", ex.ParamName);
    }
}