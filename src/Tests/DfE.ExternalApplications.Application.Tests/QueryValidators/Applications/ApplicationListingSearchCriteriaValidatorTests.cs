using DfE.ExternalApplications.Application.Applications.Queries;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Applications;

public class ApplicationListingSearchCriteriaValidatorTests
{
    private readonly ApplicationListingSearchCriteriaValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenDateRangesAreValid()
    {
        var criteria = new ApplicationListingSearchCriteria(
            Reference: "APP-001",
            DateStartedFrom: new DateTime(2024, 1, 1),
            DateStartedTo: new DateTime(2024, 12, 31),
            DateSubmittedFrom: new DateTime(2024, 2, 1),
            DateSubmittedTo: new DateTime(2024, 11, 30),
            Status: ApplicationStatus.Submitted);

        var result = _validator.Validate(criteria);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDateStartedFromIsAfterDateStartedTo()
    {
        var criteria = new ApplicationListingSearchCriteria(
            DateStartedFrom: new DateTime(2024, 12, 31),
            DateStartedTo: new DateTime(2024, 1, 1));

        var result = _validator.Validate(criteria);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDateSubmittedFromIsAfterDateSubmittedTo()
    {
        var criteria = new ApplicationListingSearchCriteria(
            DateSubmittedFrom: new DateTime(2024, 12, 31),
            DateSubmittedTo: new DateTime(2024, 1, 1));

        var result = _validator.Validate(criteria);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_ShouldReturnNull_WhenNoFiltersSpecified()
    {
        var criteria = ApplicationListingSearchCriteria.Create();

        Assert.Null(criteria);
    }

    [Fact]
    public void Create_ShouldReturnCriteria_WhenAnyFilterSpecified()
    {
        var criteria = ApplicationListingSearchCriteria.Create(status: ApplicationStatus.InProgress);

        Assert.NotNull(criteria);
        Assert.Equal(ApplicationStatus.InProgress, criteria!.Status);
    }
}
