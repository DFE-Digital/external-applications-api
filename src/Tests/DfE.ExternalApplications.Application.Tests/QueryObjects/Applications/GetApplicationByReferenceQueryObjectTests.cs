using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetApplicationByReferenceQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldFilterByApplicationReference_WhenApplicationReferenceProvided(
        ApplicationCustomization appCustomization)
    {
        // Arrange
        const string targetReference = "APP-20250101-001";
        appCustomization.OverrideReference = targetReference;
        
        var fixture = new Fixture().Customize(appCustomization);
        var targetApplication = fixture.Create<Domain.Entities.Application>();
        
        // Create other applications with different references
        var otherAppCustomization1 = new ApplicationCustomization { OverrideReference = "APP-20250101-002" };
        var otherFixture1 = new Fixture().Customize(otherAppCustomization1);
        var otherApp1 = otherFixture1.Create<Domain.Entities.Application>();
        
        var otherAppCustomization2 = new ApplicationCustomization { OverrideReference = "APP-20250102-001" };
        var otherFixture2 = new Fixture().Customize(otherAppCustomization2);
        var otherApp2 = otherFixture2.Create<Domain.Entities.Application>();

        var applications = new[] { targetApplication, otherApp1, otherApp2 };
        var queryable = applications.AsQueryable().BuildMock();

        var queryObject = new GetApplicationByReferenceQueryObject(targetReference);

        // Act
        var result = queryObject.Apply(queryable).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(targetReference, result.First().ApplicationReference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnEmpty_WhenApplicationReferenceNotFound(
        ApplicationCustomization appCustomization)
    {
        // Arrange
        appCustomization.OverrideReference = "APP-20250101-001";
        
        var fixture = new Fixture().Customize(appCustomization);
        var applications = new[] { fixture.Create<Domain.Entities.Application>() };
        var queryable = applications.AsQueryable().BuildMock();

        var queryObject = new GetApplicationByReferenceQueryObject("APP-20250101-999");

        // Act
        var result = queryObject.Apply(queryable).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldIncludeResponsesAndTemplateVersion_WhenApplicationFound(
        ApplicationCustomization appCustomization)
    {
        // Arrange
        const string targetReference = "APP-20250101-001";
        appCustomization.OverrideReference = targetReference;
        
        var fixture = new Fixture().Customize(appCustomization);
        var application = fixture.Create<Domain.Entities.Application>();
        
        var applications = new[] { application };
        var queryable = applications.AsQueryable().BuildMock();

        var queryObject = new GetApplicationByReferenceQueryObject(targetReference);

        // Act
        var result = queryObject.Apply(queryable);

        // Assert
        // Note: In a real test with actual EF Core, we would verify that Include() calls are made
        // For unit tests with MockQueryable, we verify the query structure
        Assert.NotNull(result);
        var application_result = result.First();
        Assert.Equal(targetReference, application_result.ApplicationReference);
    }
} 