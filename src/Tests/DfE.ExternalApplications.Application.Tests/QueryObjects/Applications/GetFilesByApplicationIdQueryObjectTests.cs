using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetFilesByApplicationIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnFilesForApplication_WhenApplicationIdMatches(FileCustomization fileCustom)
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var targetFile1 = fixture.Create<File>();
        var targetFile2 = fixture.Create<File>();
        var otherFile = fixture.Create<File>();
        
        // Use reflection to set the ApplicationId property
        var applicationIdProperty = typeof(File).GetProperty("ApplicationId");
        applicationIdProperty?.SetValue(targetFile1, targetApplicationId);
        applicationIdProperty?.SetValue(targetFile2, targetApplicationId);
        applicationIdProperty?.SetValue(otherFile, otherApplicationId);
        
        var files = new[] { targetFile1, targetFile2, otherFile };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFilesByApplicationIdQueryObject(targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(targetFile1, result);
        Assert.Contains(targetFile2, result);
        Assert.DoesNotContain(otherFile, result);
    }
    
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoFilesForApplication(FileCustomization fileCustom)
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var file = fixture.Create<File>();
        
        // Use reflection to set the ApplicationId property
        var applicationIdProperty = typeof(File).GetProperty("ApplicationId");
        applicationIdProperty?.SetValue(file, otherApplicationId);
        
        var files = new[] { file };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFilesByApplicationIdQueryObject(targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnAllFiles_WhenMultipleFilesForSameApplication(FileCustomization fileCustom)
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var file1 = fixture.Create<File>();
        var file2 = fixture.Create<File>();
        var file3 = fixture.Create<File>();
        
        // Use reflection to set the ApplicationId property
        var applicationIdProperty = typeof(File).GetProperty("ApplicationId");
        applicationIdProperty?.SetValue(file1, applicationId);
        applicationIdProperty?.SetValue(file2, applicationId);
        applicationIdProperty?.SetValue(file3, applicationId);
        
        var files = new[] { file1, file2, file3 };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFilesByApplicationIdQueryObject(applicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(file1, result);
        Assert.Contains(file2, result);
        Assert.Contains(file3, result);
    }
} 