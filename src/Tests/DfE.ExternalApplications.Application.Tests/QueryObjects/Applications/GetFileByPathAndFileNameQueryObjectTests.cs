using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetFileByPathAndFileNameQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnMatchingFile_WhenPathAndFileNameMatch(
        string path,
        string fileName)
    {
        // Arrange
        var matchingFile = new File(
            new FileId(Guid.NewGuid()),
            new ApplicationId(Guid.NewGuid()),
            "Test File",
            "Description",
            "original.txt",
            fileName,
            path,
            DateTime.UtcNow,
            new UserId(Guid.NewGuid()),
            1024L);
        
        var otherFile1 = new File(
            new FileId(Guid.NewGuid()),
            new ApplicationId(Guid.NewGuid()),
            "Other File 1",
            "Description",
            "other1.txt",
            "other-file-1.txt",
            "other/path",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid()),
            2048L);
        
        var otherFile2 = new File(
            new FileId(Guid.NewGuid()),
            new ApplicationId(Guid.NewGuid()),
            "Other File 2",
            "Description",
            "other2.txt",
            fileName, // Same fileName but different path
            "different/path",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid()),
            3072L);
        
        var files = new[] { matchingFile, otherFile1, otherFile2 };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByPathAndFileNameQueryObject(path, fileName);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(path, result[0].Path);
        Assert.Equal(fileName, result[0].FileName);
    }
    
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnEmpty_WhenNoFileMatches(
        string path,
        string fileName)
    {
        // Arrange
        var file1 = new File(
            new FileId(Guid.NewGuid()),
            new ApplicationId(Guid.NewGuid()),
            "File 1",
            "Description",
            "file1.txt",
            "different-file.txt",
            "different/path",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid()),
            1024L);
        
        var file2 = new File(
            new FileId(Guid.NewGuid()),
            new ApplicationId(Guid.NewGuid()),
            "File 2",
            "Description",
            "file2.txt",
            "another-file.txt",
            "another/path",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid()),
            2048L);
        
        var files = new[] { file1, file2 };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByPathAndFileNameQueryObject(path, fileName);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldMatchPathCaseSensitively(
        string path,
        string fileName)
    {
        // Arrange
        var matchingFile = new File(
            new FileId(Guid.NewGuid()),
            new ApplicationId(Guid.NewGuid()),
            "Test File",
            "Description",
            "original.txt",
            fileName,
            path,
            DateTime.UtcNow,
            new UserId(Guid.NewGuid()),
            1024L);
        
        var differentCasePath = path.ToUpperInvariant();
        var differentCaseFile = new File(
            new FileId(Guid.NewGuid()),
            new ApplicationId(Guid.NewGuid()),
            "Different Case File",
            "Description",
            "different.txt",
            fileName,
            differentCasePath,
            DateTime.UtcNow,
            new UserId(Guid.NewGuid()),
            2048L);
        
        var files = new[] { matchingFile, differentCaseFile };
        var queryable = files.AsQueryable().BuildMock();
        
        var queryObject = new GetFileByPathAndFileNameQueryObject(path, fileName);
        
        // Act
        var result = queryObject.Apply(queryable).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(path, result[0].Path);
    }
}

