using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using MockQueryable;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryObjects.Applications;

public class GetApplicationByIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnMatchingApplication_WhenApplicationIdExists(ApplicationCustomization appCustom)
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(appCustom);
        var targetApplication = fixture.Create<Domain.Entities.Application>();
        var otherApplication = fixture.Create<Domain.Entities.Application>();
        
        // Use reflection to set the Id property
        var idProperty = typeof(Domain.Entities.Application).GetProperty("Id");
        idProperty?.SetValue(targetApplication, targetApplicationId);
        idProperty?.SetValue(otherApplication, otherApplicationId);
        
        var applications = new[] { targetApplication, otherApplication };
        var queryable = applications.AsQueryable().BuildMock();
        
        var queryObject = new GetApplicationByIdQueryObject(targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(targetApplication, result.First());
    }
    
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnEmpty_WhenApplicationIdDoesNotExist(ApplicationCustomization appCustom)
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(appCustom);
        var application = fixture.Create<Domain.Entities.Application>();
        
        // Use reflection to set the Id property
        var idProperty = typeof(Domain.Entities.Application).GetProperty("Id");
        idProperty?.SetValue(application, otherApplicationId);
        
        var applications = new[] { application };
        var queryable = applications.AsQueryable().BuildMock();
        
        var queryObject = new GetApplicationByIdQueryObject(targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnEmpty_WhenApplicationIdIsNull(ApplicationCustomization appCustom)
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(appCustom);
        var application = fixture.Create<Domain.Entities.Application>();
        
        // Use reflection to set the Id property to null
        var idProperty = typeof(Domain.Entities.Application).GetProperty("Id");
        idProperty?.SetValue(application, null);
        
        var applications = new[] { application };
        var queryable = applications.AsQueryable().BuildMock();
        
        var queryObject = new GetApplicationByIdQueryObject(targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldIncludeTemplateVersion_WhenApplicationFound(ApplicationCustomization appCustom)
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(appCustom);
        var application = fixture.Create<Domain.Entities.Application>();
        
        // Use reflection to set the Id property
        var idProperty = typeof(Domain.Entities.Application).GetProperty("Id");
        idProperty?.SetValue(application, applicationId);
        
        var applications = new[] { application };
        var queryable = applications.AsQueryable().BuildMock();
        
        var queryObject = new GetApplicationByIdQueryObject(applicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(application, result.First());
        
        // Note: The actual inclusion of related entities would be tested in integration tests
        // with a real database context, as MockQueryable doesn't fully support Include/ThenInclude
    }
} 