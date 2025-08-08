using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using System;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Domain.Tests.Factories;

public class FileFactoryTests
{
    private readonly FileFactory _factory = new();

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void CreateUpload_Should_Create_File_With_Valid_Parameters(
        FileId id,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        string fileName,
        long fileSize,
        string path,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        // Act
        var file = _factory.CreateUpload(id, applicationId, name, description, originalFileName, fileName, path, uploadedOn, uploadedBy, fileSize);

        // Assert
        Assert.Equal(id, file.Id);
        Assert.Equal(applicationId, file.ApplicationId);
        Assert.Equal(name, file.Name);
        Assert.Equal(description, file.Description);
        Assert.Equal(originalFileName, file.OriginalFileName);
        Assert.Equal(fileName, file.FileName);
        Assert.Equal(path, file.Path);
        Assert.Equal(uploadedOn, file.UploadedOn);
        Assert.Equal(uploadedBy, file.UploadedBy);
        Assert.False(file.IsDeleted);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void DeleteFile_Should_Call_Delete_On_File_And_Add_Domain_Event(
        FileId id,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        string fileName, 
        long fileSize,
        string path,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        // Arrange
        var file = new File(id, applicationId, name, description, originalFileName, fileName, path, uploadedOn, uploadedBy, fileSize);

        // Act
        _factory.DeleteFile(file);

        // Assert
        Assert.True(file.IsDeleted);
        Assert.Single(file.DomainEvents);
        Assert.IsType<FileDeletedEvent>(file.DomainEvents.First());
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void CreateUpload_Should_Throw_ArgumentNullException_When_Id_Is_Null(
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        string fileName,
        long fileSize,
        string path,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _factory.CreateUpload(null!, applicationId, name, description, originalFileName, fileName, path, uploadedOn, uploadedBy, fileSize));
        Assert.Equal("id", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void CreateUpload_Should_Throw_ArgumentNullException_When_ApplicationId_Is_Null(
        FileId id,
        string name,
        string description,
        string originalFileName,
        string fileName,
        string path,
        long fileSize,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _factory.CreateUpload(id, null!, name, description, originalFileName, fileName, path, uploadedOn, uploadedBy, fileSize));
        Assert.Equal("applicationId", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void CreateUpload_Should_Throw_ArgumentNullException_When_Name_Is_Null(
        FileId id,
        ApplicationId applicationId,
        string description,
        string originalFileName,
        string fileName,
        string path,
        long fileSize,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _factory.CreateUpload(id, applicationId, null!, description, originalFileName, fileName, path, uploadedOn, uploadedBy, fileSize));
        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void CreateUpload_Should_Throw_ArgumentNullException_When_OriginalFileName_Is_Null(
        FileId id,
        ApplicationId applicationId,
        string name,
        string description,
        string fileName,
        string path,
        long fileSize,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _factory.CreateUpload(id, applicationId, name, description, null!, fileName, path, uploadedOn, uploadedBy, fileSize));
        Assert.Equal("originalFileName", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void CreateUpload_Should_Throw_ArgumentNullException_When_FileName_Is_Null(
        FileId id,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        long fileSize,
        string path,
        DateTime uploadedOn,
        UserId uploadedBy)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _factory.CreateUpload(id, applicationId, name, description, originalFileName, null!, path, uploadedOn, uploadedBy, fileSize));
        Assert.Equal("fileName", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void CreateUpload_Should_Throw_ArgumentNullException_When_UploadedBy_Is_Null(
        FileId id,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        string fileName,
        long fileSize,
        string path,
        DateTime uploadedOn)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _factory.CreateUpload(id, applicationId, name, description, originalFileName, fileName, path, uploadedOn, null!, fileSize));
        Assert.Equal("uploadedBy", exception.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public void DeleteFile_Should_Throw_NullReferenceException_When_File_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() => _factory.DeleteFile(null!));
    }
} 