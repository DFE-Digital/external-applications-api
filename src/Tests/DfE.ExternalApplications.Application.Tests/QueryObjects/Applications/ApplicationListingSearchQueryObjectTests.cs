using AutoFixture;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetApplicationsByDateStartedRangeQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldFilterByFromDate_Inclusive(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var inRange = fixture.Create<Domain.Entities.Application>();
        var outOfRange = fixture.Create<Domain.Entities.Application>();

        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.CreatedOn))!
            .SetValue(inRange, new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc));
        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.CreatedOn))!
            .SetValue(outOfRange, new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc));

        var queryable = new[] { inRange, outOfRange }.AsQueryable().BuildMock();
        var result = new GetApplicationsByDateStartedRangeQueryObject(
            new DateTime(2024, 6, 10),
            null).Apply(queryable).ToList();

        Assert.Single(result);
        Assert.Equal(inRange, result.First());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldFilterByToDate_InclusiveOfWholeDay(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var inRange = fixture.Create<Domain.Entities.Application>();
        var outOfRange = fixture.Create<Domain.Entities.Application>();

        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.CreatedOn))!
            .SetValue(inRange, new DateTime(2024, 6, 15, 23, 59, 59, DateTimeKind.Utc));
        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.CreatedOn))!
            .SetValue(outOfRange, new DateTime(2024, 6, 16, 0, 0, 1, DateTimeKind.Utc));

        var queryable = new[] { inRange, outOfRange }.AsQueryable().BuildMock();
        var result = new GetApplicationsByDateStartedRangeQueryObject(
            null,
            new DateTime(2024, 6, 15)).Apply(queryable).ToList();

        Assert.Single(result);
        Assert.Equal(inRange, result.First());
    }
}

public class GetApplicationsByDateSubmittedRangeQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnOnlySubmittedApplicationsInRange(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var submittedInRange = fixture.Create<Domain.Entities.Application>();
        var submittedOutOfRange = fixture.Create<Domain.Entities.Application>();
        var inProgress = fixture.Create<Domain.Entities.Application>();

        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.Status))!
            .SetValue(submittedInRange, ApplicationStatus.Submitted);
        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.LastModifiedOn))!
            .SetValue(submittedInRange, new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.Status))!
            .SetValue(submittedOutOfRange, ApplicationStatus.Submitted);
        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.LastModifiedOn))!
            .SetValue(submittedOutOfRange, new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc));

        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.Status))!
            .SetValue(inProgress, ApplicationStatus.InProgress);
        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.LastModifiedOn))!
            .SetValue(inProgress, new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var queryable = new[] { submittedInRange, submittedOutOfRange, inProgress }.AsQueryable().BuildMock();
        var result = new GetApplicationsByDateSubmittedRangeQueryObject(
            new DateTime(2024, 6, 1),
            new DateTime(2024, 6, 30)).Apply(queryable).ToList();

        Assert.Single(result);
        Assert.Equal(submittedInRange, result.First());
    }
}

public class GetApplicationsByStatusQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnApplicationsWithMatchingStatus(ApplicationCustomization appCustom)
    {
        var fixture = new Fixture().Customize(appCustom);
        var submitted = fixture.Create<Domain.Entities.Application>();
        var inProgress = fixture.Create<Domain.Entities.Application>();

        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.Status))!
            .SetValue(submitted, ApplicationStatus.Submitted);
        typeof(Domain.Entities.Application).GetProperty(nameof(Domain.Entities.Application.Status))!
            .SetValue(inProgress, ApplicationStatus.InProgress);

        var queryable = new[] { submitted, inProgress }.AsQueryable().BuildMock();
        var result = new GetApplicationsByStatusQueryObject(ApplicationStatus.Submitted).Apply(queryable).ToList();

        Assert.Single(result);
        Assert.Equal(submitted, result.First());
    }
}
