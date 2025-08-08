using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using DfE.ExternalApplications.Utils.File;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using MockQueryable;
using NSubstitute.ExceptionExtensions;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class UploadFileCommandHandlerTests
{
    private readonly IEaRepository<File> _uploadRepository;
    private readonly IEaRepository<Domain.Entities.Application> _applicationRepository;
    private readonly IEaRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileFactory _fileFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly UploadFileCommandHandler _handler;

    public UploadFileCommandHandlerTests()
    {
        _uploadRepository = Substitute.For<IEaRepository<File>>();
        _applicationRepository = Substitute.For<IEaRepository<Domain.Entities.Application>>();
        _userRepository = Substitute.For<IEaRepository<User>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _fileStorageService = Substitute.For<IFileStorageService>();
        _fileFactory = Substitute.For<IFileFactory>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();

        _handler = new UploadFileCommandHandler(
            _uploadRepository,
            _applicationRepository,
            _userRepository,
            _unitOfWork,
            _fileStorageService,
            _fileFactory,
            _httpContextAccessor,
            _permissionCheckerService);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Success_When_Valid_Request(
        Domain.Entities.Application application,
        User user,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent,
        File uploadedFile)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the same email as in the HTTP context
        var userWithMatchingEmail = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            "test@example.com", // Match the email in the HTTP context
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy,
            user.ExternalProviderId,
            user.Permissions);

        var queryable = new List<User> { userWithMatchingEmail }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        // Create an application with the same ID as the applicationId parameter
        var applicationWithMatchingId = new Domain.Entities.Application(
            applicationId, // Use the same ID as the parameter
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var uploadQueryable = new List<File>().AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(uploadQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationWithMatchingId.Id!.Value.ToString(), AccessType.Write)
            .Returns(true);

        _fileFactory.CreateUpload(
            Arg.Any<FileId>(),
            Arg.Any<ApplicationId>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<UserId>(),
            Arg.Any<long>())
            .Returns(uploadedFile);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(uploadedFile.Id!.Value, result.Value.Id);
        Assert.Equal(uploadedFile.ApplicationId.Value, result.Value.ApplicationId);
        Assert.Equal(uploadedFile.UploadedBy.Value, result.Value.UploadedBy);
        Assert.Equal(uploadedFile.Name, result.Value.Name);
        Assert.Equal(uploadedFile.Description, result.Value.Description);
        Assert.Equal(uploadedFile.OriginalFileName, result.Value.OriginalFileName);
        Assert.Equal(uploadedFile.FileName, result.Value.FileName);
        Assert.Equal(uploadedFile.UploadedOn, result.Value.UploadedOn);

        await _uploadRepository.Received(1).AddAsync(uploadedFile, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _fileStorageService.Received(1).UploadAsync(Arg.Any<string>(), fileContent, Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_Not_Authenticated(
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_User_Not_Found(
        Domain.Entities.Application application,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var queryable = new List<User>().AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_Application_Not_Found(
        User user,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the same email as in the HTTP context
        var userWithMatchingEmail = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            "test@example.com", // Match the email in the HTTP context
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy,
            user.ExternalProviderId,
            user.Permissions);

        var userQueryable = new List<User> { userWithMatchingEmail }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(userQueryable);

        // Application does NOT exist
        var applicationQueryable = new List<Domain.Entities.Application>().AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_User_No_Permission(
        Domain.Entities.Application application,
        User user,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the same email as in the HTTP context
        var userWithMatchingEmail = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            "test@example.com", // Match the email in the HTTP context
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy,
            user.ExternalProviderId,
            user.Permissions);

        var queryable = new List<User> { userWithMatchingEmail }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        // Application with matching ID
        var applicationWithMatchingId = new Domain.Entities.Application(
            applicationId,
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationWithMatchingId.Id!.Value.ToString(), AccessType.Write)
            .Returns(false);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to upload files for this application", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_File_Already_Exists(
        Domain.Entities.Application application,
        User user,
        File existingFile,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the same email as in the HTTP context
        var userWithMatchingEmail = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            "test@example.com", // Match the email in the HTTP context
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy,
            user.ExternalProviderId,
            user.Permissions);

        var queryable = new List<User> { userWithMatchingEmail }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        // Application with matching ID
        var applicationWithMatchingId = new Domain.Entities.Application(
            applicationId,
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        // Create a file with the correct ApplicationId and FileName that the handler will look for
        var hashedFileName = FileNameHasher.HashFileName(originalFileName);
        var existingFileWithMatchingProperties = new File(
            existingFile.Id!,
            applicationId, // Use the applicationId parameter
            existingFile.Name,
            existingFile.Description,
            existingFile.OriginalFileName,
            hashedFileName, // Use the hashed filename that the handler generates
            existingFile.Path,
            existingFile.UploadedOn,
            existingFile.UploadedBy,
            existingFile.FileSize);

        var uploadQueryable = new List<File> { existingFileWithMatchingProperties }.AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(uploadQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationWithMatchingId.Id!.Value.ToString(), AccessType.Write)
            .Returns(true);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The file already exist", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_Exception_Occurs(
        Domain.Entities.Application application,
        User user,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the same email as in the HTTP context
        var userWithMatchingEmail = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            "test@example.com", // Match the email in the HTTP context
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy,
            user.ExternalProviderId,
            user.Permissions);

        var userQueryable = new List<User> { userWithMatchingEmail }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(userQueryable);

        // Application with matching ID
        var applicationWithMatchingId = new Domain.Entities.Application(
            applicationId,
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        // Simulate exception in file storage
        _fileStorageService
            .When(x => x.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new Exception("Storage error"));

        // Allow user to have permission to upload files
        _permissionCheckerService.HasPermission(
            ResourceType.ApplicationFiles,
            applicationWithMatchingId.Id!.Value.ToString(),
            AccessType.Write
        ).Returns(true);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Storage error", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Use_Email_When_PrincipalId_Contains_At_Symbol(
        Domain.Entities.Application application,
        User user,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent,
        File uploadedFile)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the same email as in the HTTP context
        var userWithMatchingEmail = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            "test@example.com", // Match the email in the HTTP context
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy,
            user.ExternalProviderId,
            user.Permissions);

        var queryable = new List<User> { userWithMatchingEmail }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        // Application with matching ID
        var applicationWithMatchingId = new Domain.Entities.Application(
            applicationId,
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var uploadQueryable = new List<File>().AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(uploadQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationWithMatchingId.Id!.Value.ToString(), AccessType.Write)
            .Returns(true);

        _fileFactory.CreateUpload(
            Arg.Any<FileId>(),
            Arg.Any<ApplicationId>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<UserId>(),
            Arg.Any<long>())
            .Returns(uploadedFile);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _userRepository.Received(1).Query();
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Use_ExternalProviderId_When_PrincipalId_Does_Not_Contain_At_Symbol(
        Domain.Entities.Application application,
        User user,
        ApplicationId applicationId,
        string name,
        string description,
        string originalFileName,
        Stream fileContent,
        File uploadedFile)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("azp", "external-provider-id")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the same external provider ID as in the HTTP context
        var userWithMatchingExternalProviderId = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            user.Email,
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy,
            "external-provider-id", // Match the external provider ID in the HTTP context
            user.Permissions);

        var queryable = new List<User> { userWithMatchingExternalProviderId }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        // Application with matching ID
        var applicationWithMatchingId = new Domain.Entities.Application(
            applicationId,
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var uploadQueryable = new List<File>().AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(uploadQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationWithMatchingId.Id!.Value.ToString(), AccessType.Write)
            .Returns(true);

        _fileFactory.CreateUpload(
            Arg.Any<FileId>(),
            Arg.Any<ApplicationId>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<UserId>(),
            Arg.Any<long>())
            .Returns(uploadedFile);

        var command = new UploadFileCommand(applicationId, name, description, originalFileName, fileContent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _userRepository.Received(1).Query();
    }
} 