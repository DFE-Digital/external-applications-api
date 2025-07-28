using AutoFixture;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetFileByIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnMatchingFile_WhenFileIdExists(FileCustomization fileCustom)
    {
        // Arrange
        var targetFileId = new FileId(Guid.NewGuid());
        var otherFileId = new FileId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var targetFile = fixture.Create<File>();
        var otherFile = fixture.Create<File>();
        
        // Use reflection to set the Id property
        var idProperty = typeof(File).GetProperty("Id");
        idProperty?.SetValue(targetFile, targetFileId);
        idProperty?.SetValue(otherFile, otherFileId);
        
        var files = new[] { targetFile, otherFile };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByIdQueryObject(targetFileId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(targetFile, result.First());
    }
    
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnEmpty_WhenFileIdDoesNotExist(FileCustomization fileCustom)
    {
        // Arrange
        var targetFileId = new FileId(Guid.NewGuid());
        var otherFileId = new FileId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var file = fixture.Create<File>();
        
        // Use reflection to set the Id property
        var idProperty = typeof(File).GetProperty("Id");
        idProperty?.SetValue(file, otherFileId);
        
        var files = new[] { file };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByIdQueryObject(targetFileId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void Apply_ShouldReturnEmpty_WhenFileIdIsNull(FileCustomization fileCustom)
    {
        // Arrange
        var targetFileId = new FileId(Guid.NewGuid());
        
        var fixture = new Fixture().Customize(fileCustom);
        var file = fixture.Create<File>();
        
        // Use reflection to set the Id property to null
        var idProperty = typeof(File).GetProperty("Id");
        idProperty?.SetValue(file, null);
        
        var files = new[] { file };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByIdQueryObject(targetFileId);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
} 