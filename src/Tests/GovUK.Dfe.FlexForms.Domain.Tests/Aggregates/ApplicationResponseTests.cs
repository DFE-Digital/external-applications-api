using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Domain.Tests.Aggregates;

public class ApplicationResponseTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationResponseCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        ApplicationId applicationId,
        string responseBody,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ApplicationResponse(
                null!,        // id
                applicationId,
                responseBody,
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationResponseCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenResponseBodyIsNull(
        ResponseId id,
        ApplicationId applicationId,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ApplicationResponse(
                id,
                applicationId,
                null!,       // responseBody
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy));

        Assert.Equal("responseBody", ex.ParamName);
    }
}