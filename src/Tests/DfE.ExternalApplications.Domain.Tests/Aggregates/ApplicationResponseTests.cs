using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates;

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