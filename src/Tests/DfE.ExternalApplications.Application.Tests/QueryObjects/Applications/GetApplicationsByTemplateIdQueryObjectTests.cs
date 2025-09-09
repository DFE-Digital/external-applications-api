using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetApplicationsByTemplateIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldFilterByTemplateId_WhenTemplateIdProvided(ApplicationCustomization appCustom)
    {
        // Arrange
        var targetTemplateId = new TemplateId(Guid.NewGuid());
        var otherTemplateId = new TemplateId(Guid.NewGuid());
        
        // Create applications with different template IDs
        var fixture = new Fixture().Customize(appCustom);
        
        // Create template versions for different templates
        var targetTemplateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            targetTemplateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid())
        );
        
        var otherTemplateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            otherTemplateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid())
        );
        
        // Create applications with different template versions
        var targetApp = fixture.Create<Domain.Entities.Application>();
        var otherApp = fixture.Create<Domain.Entities.Application>();
        
        // Use reflection to set the TemplateVersion property
        var templateVersionProperty = typeof(Domain.Entities.Application).GetProperty("TemplateVersion");
        templateVersionProperty?.SetValue(targetApp, targetTemplateVersion);
        templateVersionProperty?.SetValue(otherApp, otherTemplateVersion);
        
        var applications = new[] { targetApp, otherApp };
        var queryable = applications.AsQueryable().BuildMock();
        
        var queryObject = new GetApplicationsByTemplateIdQueryObject(targetTemplateId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(targetApp, result.First());
    }
    
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoApplicationsMatchTemplateId(ApplicationCustomization appCustom)
    {
        // Arrange
        var targetTemplateId = new TemplateId(Guid.NewGuid());
        var otherTemplateId = new TemplateId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(appCustom);
        
        // Create template version for different template
        var otherTemplateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            otherTemplateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid())
        );
        
        var app = fixture.Create<Domain.Entities.Application>();
        
        // Use reflection to set the TemplateVersion property
        var templateVersionProperty = typeof(Domain.Entities.Application).GetProperty("TemplateVersion");
        templateVersionProperty?.SetValue(app, otherTemplateVersion);
        
        var applications = new[] { app };
        var queryable = applications.AsQueryable().BuildMock();
        
        var queryObject = new GetApplicationsByTemplateIdQueryObject(targetTemplateId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
} 