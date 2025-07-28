using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetFileByFileNameApplicationIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnMatchingFile_WhenFileNameAndApplicationIdMatch(FileCustomization fileCustom)
    {
        // Arrange
        var targetFileName = "test-file.pdf";
        var otherFileName = "other-file.pdf";
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var targetFile = fixture.Create<File>();
        var otherFile1 = fixture.Create<File>();
        var otherFile2 = fixture.Create<File>();
        
        // Use reflection to set the properties
        var fileNameProperty = typeof(File).GetProperty("FileName");
        var applicationIdProperty = typeof(File).GetProperty("ApplicationId");
        
        fileNameProperty?.SetValue(targetFile, targetFileName);
        applicationIdProperty?.SetValue(targetFile, targetApplicationId);
        
        fileNameProperty?.SetValue(otherFile1, otherFileName);
        applicationIdProperty?.SetValue(otherFile1, targetApplicationId);
        
        fileNameProperty?.SetValue(otherFile2, targetFileName);
        applicationIdProperty?.SetValue(otherFile2, otherApplicationId);
        
        var files = new[] { targetFile, otherFile1, otherFile2 };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByFileNameApplicationIdQueryObject(targetFileName, targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(targetFile, result.First());
    }
    
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnEmpty_WhenFileNameDoesNotMatch(FileCustomization fileCustom)
    {
        // Arrange
        var targetFileName = "test-file.pdf";
        var otherFileName = "other-file.pdf";
        var applicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var file = fixture.Create<File>();
        
        // Use reflection to set the properties
        var fileNameProperty = typeof(File).GetProperty("FileName");
        var applicationIdProperty = typeof(File).GetProperty("ApplicationId");
        
        fileNameProperty?.SetValue(file, otherFileName);
        applicationIdProperty?.SetValue(file, applicationId);
        
        var files = new[] { file };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByFileNameApplicationIdQueryObject(targetFileName, applicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnEmpty_WhenApplicationIdDoesNotMatch(FileCustomization fileCustom)
    {
        // Arrange
        var fileName = "test-file.pdf";
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var file = fixture.Create<File>();
        
        // Use reflection to set the properties
        var fileNameProperty = typeof(File).GetProperty("FileName");
        var applicationIdProperty = typeof(File).GetProperty("ApplicationId");
        
        fileNameProperty?.SetValue(file, fileName);
        applicationIdProperty?.SetValue(file, otherApplicationId);
        
        var files = new[] { file };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByFileNameApplicationIdQueryObject(fileName, targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNeitherFileNameNorApplicationIdMatch(FileCustomization fileCustom)
    {
        // Arrange
        var targetFileName = "test-file.pdf";
        var otherFileName = "other-file.pdf";
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var file = fixture.Create<File>();
        
        // Use reflection to set the properties
        var fileNameProperty = typeof(File).GetProperty("FileName");
        var applicationIdProperty = typeof(File).GetProperty("ApplicationId");
        
        fileNameProperty?.SetValue(file, otherFileName);
        applicationIdProperty?.SetValue(file, otherApplicationId);
        
        var files = new[] { file };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByFileNameApplicationIdQueryObject(targetFileName, targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnSingleFile_WhenMultipleFilesExistButOnlyOneMatchesBothCriteria(FileCustomization fileCustom)
    {
        // Arrange
        var targetFileName = "test-file.pdf";
        var otherFileName = "other-file.pdf";
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var targetFile = fixture.Create<File>();
        var otherFile1 = fixture.Create<File>();
        var otherFile2 = fixture.Create<File>();
        var otherFile3 = fixture.Create<File>();
        
        // Use reflection to set the properties
        var fileNameProperty = typeof(File).GetProperty("FileName");
        var applicationIdProperty = typeof(File).GetProperty("ApplicationId");
        
        // Target file - matches both criteria
        fileNameProperty?.SetValue(targetFile, targetFileName);
        applicationIdProperty?.SetValue(targetFile, targetApplicationId);
        
        // Other file 1 - matches applicationId but not fileName
        fileNameProperty?.SetValue(otherFile1, otherFileName);
        applicationIdProperty?.SetValue(otherFile1, targetApplicationId);
        
        // Other file 2 - matches fileName but not applicationId
        fileNameProperty?.SetValue(otherFile2, targetFileName);
        applicationIdProperty?.SetValue(otherFile2, otherApplicationId);
        
        // Other file 3 - matches neither
        fileNameProperty?.SetValue(otherFile3, otherFileName);
        applicationIdProperty?.SetValue(otherFile3, otherApplicationId);
        
        var files = new[] { targetFile, otherFile1, otherFile2, otherFile3 };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByFileNameApplicationIdQueryObject(targetFileName, targetApplicationId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(targetFile, result.First());
    }
} 