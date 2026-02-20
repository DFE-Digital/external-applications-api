using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class DeleteInfectedFileCommandHandlerTests
{
    private readonly IEaRepository<File> _fileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantAwareFileStorageService _fileStorageService;
    private readonly IFileFactory _fileFactory;
    private readonly DeleteInfectedFileCommandHandler _handler;

    public DeleteInfectedFileCommandHandlerTests()
    {
        _fileRepository = Substitute.For<IEaRepository<File>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _fileStorageService = Substitute.For<ITenantAwareFileStorageService>();
        _fileFactory = Substitute.For<IFileFactory>();

        _handler = new DeleteInfectedFileCommandHandler(
            _fileRepository,
            _unitOfWork,
            _fileStorageService,
            _fileFactory,
            NullLogger<DeleteInfectedFileCommandHandler>.Instance);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public async Task Handle_ShouldReturnSuccess_WhenFileFoundAndDeleted(
        File file,
        long fileSize,
        Guid fileIdGuid)
    {
        // Arrange
        var fileId = new FileId(fileIdGuid);
        var fileWithMatchingId = new File(
            fileId,
            file.ApplicationId,
            file.Name,
            file.Description,
            file.OriginalFileName,
            file.FileName,
            file.Path,
            file.UploadedOn,
            file.UploadedBy,
            fileSize);

        var fileQueryable = new List<File> { fileWithMatchingId }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        var command = new DeleteInfectedFileCommand(fileId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        _fileFactory.Received(1).DeleteFile(fileWithMatchingId);
        await _fileRepository.Received(1).RemoveAsync(fileWithMatchingId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenFileDoesNotExist()
    {
        // Arrange
        var fileId = new FileId(Guid.NewGuid());
        var fileQueryable = new List<File>().AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        var command = new DeleteInfectedFileCommand(fileId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("File not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public async Task Handle_ShouldStillDeleteFromDb_WhenStorageDeletionFails(
        File file,
        long fileSize,
        Guid fileIdGuid)
    {
        // Arrange
        var fileId = new FileId(fileIdGuid);
        var fileWithMatchingId = new File(
            fileId,
            file.ApplicationId,
            file.Name,
            file.Description,
            file.OriginalFileName,
            file.FileName,
            file.Path,
            file.UploadedOn,
            file.UploadedBy,
            fileSize);

        var fileQueryable = new List<File> { fileWithMatchingId }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        _fileStorageService.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Storage error"));

        var command = new DeleteInfectedFileCommand(fileId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _fileFactory.Received(1).DeleteFile(fileWithMatchingId);
        await _fileRepository.Received(1).RemoveAsync(fileWithMatchingId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenExceptionOccurs(
        File file,
        long fileSize,
        Guid fileIdGuid)
    {
        // Arrange
        var fileId = new FileId(fileIdGuid);
        var fileWithMatchingId = new File(
            fileId,
            file.ApplicationId,
            file.Name,
            file.Description,
            file.OriginalFileName,
            file.FileName,
            file.Path,
            file.UploadedOn,
            file.UploadedBy,
            fileSize);

        var fileQueryable = new List<File> { fileWithMatchingId }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        _fileRepository.RemoveAsync(Arg.Any<File>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        var command = new DeleteInfectedFileCommand(fileId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Database error", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(FileCustomization))]
    public async Task Handle_ShouldCallStorageDelete_WithCorrectPath(
        File file,
        long fileSize,
        Guid fileIdGuid)
    {
        // Arrange
        var fileId = new FileId(fileIdGuid);
        var fileWithMatchingId = new File(
            fileId,
            file.ApplicationId,
            file.Name,
            file.Description,
            file.OriginalFileName,
            file.FileName,
            file.Path,
            file.UploadedOn,
            file.UploadedBy,
            fileSize);

        var fileQueryable = new List<File> { fileWithMatchingId }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        var command = new DeleteInfectedFileCommand(fileId);
        var expectedStoragePath = $"{fileWithMatchingId.Path}/{fileWithMatchingId.FileName}";

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _fileStorageService.Received(1).DeleteAsync(expectedStoragePath, Arg.Any<CancellationToken>());
    }
}
