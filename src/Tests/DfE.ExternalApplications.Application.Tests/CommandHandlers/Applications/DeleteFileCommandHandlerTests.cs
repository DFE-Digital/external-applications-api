using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
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

public class DeleteFileCommandHandlerTests
{
    private readonly IEaRepository<File> _fileRepository;
    private readonly IEaRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEaRepository<Domain.Entities.Application> _applicationRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFileFactory _fileFactory;
    private readonly DeleteFileCommandHandler _handler;

    public DeleteFileCommandHandlerTests()
    {
        _fileRepository = Substitute.For<IEaRepository<File>>();
        _userRepository = Substitute.For<IEaRepository<User>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _applicationRepo = Substitute.For<IEaRepository<Domain.Entities.Application>>();
        _fileStorageService = Substitute.For<IFileStorageService>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _fileFactory = Substitute.For<IFileFactory>();

        _handler = new DeleteFileCommandHandler(
            _fileRepository,
            _userRepository,
            _unitOfWork,
            _applicationRepo,
            _fileStorageService,
            _permissionCheckerService,
            _httpContextAccessor,
            _fileFactory);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Success_When_Valid_Request(
        Domain.Entities.Application application,
        User user,
        File file,
        Guid fileId,
        Guid applicationId)
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
            new ApplicationId(applicationId), // Use the same ID as the parameter
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.IsApplicationOwner(applicationWithMatchingId, userWithMatchingEmail.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);
        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationId.ToString(), AccessType.Delete)
            .Returns(true);

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        _fileFactory.Received(1).DeleteFile(file);
        await _fileRepository.Received(1).RemoveAsync(file, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _fileStorageService.Received(1).DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_Not_Authenticated(
        Guid fileId,
        Guid applicationId)
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var command = new DeleteFileCommand(fileId, applicationId);

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
        Guid fileId,
        Guid applicationId)
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

        var command = new DeleteFileCommand(fileId, applicationId);

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
        Guid fileId,
        Guid applicationId)
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

        var applicationQueryable = new List<Domain.Entities.Application>().AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_User_Not_Owner_Or_Admin(
        Domain.Entities.Application application,
        User user,
        Guid fileId,
        Guid applicationId)
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

        var applicationQueryable = new List<Domain.Entities.Application> { application }.AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        _permissionCheckerService.IsApplicationOwner(application, userWithMatchingEmail.Id!.Value.ToString()).Returns(false);
        _permissionCheckerService.IsAdmin().Returns(false);

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Only the application owner or admin can remove files", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_User_No_Delete_Permission(
        Domain.Entities.Application application,
        User user,
        Guid fileId,
        Guid applicationId)
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
            new ApplicationId(applicationId), // Use the same ID as the parameter
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        _permissionCheckerService.IsApplicationOwner(applicationWithMatchingId, userWithMatchingEmail.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);
        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationId.ToString(), AccessType.Delete)
            .Returns(false);

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to delete this file", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_File_Not_Found(
        Domain.Entities.Application application,
        User user,
        Guid fileId,
        Guid applicationId)
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
            new ApplicationId(applicationId), // Use the same ID as the parameter
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File>().AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.IsApplicationOwner(applicationWithMatchingId, userWithMatchingEmail.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);
        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationId.ToString(), AccessType.Delete)
            .Returns(true);

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("File not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_Exception_Occurs(
        Domain.Entities.Application application,
        User user,
        File file,
        Guid fileId,
        Guid applicationId)
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
            new ApplicationId(applicationId), // Use the same ID as the parameter
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.IsApplicationOwner(applicationWithMatchingId, userWithMatchingEmail.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);
        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationId.ToString(), AccessType.Delete)
            .Returns(true);

        _fileStorageService.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Storage error"));

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Storage error", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Allow_Admin_To_Delete_File(
        Domain.Entities.Application application,
        User user,
        File file,
        Guid fileId,
        Guid applicationId)
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
            new ApplicationId(applicationId), // Use the same ID as the parameter
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.IsApplicationOwner(applicationWithMatchingId, userWithMatchingEmail.Id!.Value.ToString()).Returns(false);
        _permissionCheckerService.IsAdmin().Returns(true);
        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationId.ToString(), AccessType.Delete)
            .Returns(true);

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        _fileFactory.Received(1).DeleteFile(file);
        await _fileRepository.Received(1).RemoveAsync(file, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Use_Email_When_PrincipalId_Contains_At_Symbol(
        Domain.Entities.Application application,
        User user,
        File file,
        Guid fileId,
        Guid applicationId)
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
            new ApplicationId(applicationId), // Use the same ID as the parameter
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.IsApplicationOwner(applicationWithMatchingId, userWithMatchingEmail.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);
        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationId.ToString(), AccessType.Delete)
            .Returns(true);

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _userRepository.Query().Received(1);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Use_ExternalProviderId_When_PrincipalId_Does_Not_Contain_At_Symbol(
        Domain.Entities.Application application,
        User user,
        File file,
        Guid fileId,
        Guid applicationId)
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
        var userWithMatchingExternalId = new User(
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

        var queryable = new List<User> { userWithMatchingExternalId }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        // Create an application with the same ID as the applicationId parameter
        var applicationWithMatchingId = new Domain.Entities.Application(
            new ApplicationId(applicationId), // Use the same ID as the parameter
            application.ApplicationReference,
            application.TemplateVersionId,
            application.CreatedOn,
            application.CreatedBy,
            application.Status,
            application.LastModifiedOn,
            application.LastModifiedBy);

        var applicationQueryable = new List<Domain.Entities.Application> { applicationWithMatchingId }.AsQueryable().BuildMock();
        _applicationRepo.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _fileRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.IsApplicationOwner(applicationWithMatchingId, userWithMatchingExternalId.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);
        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, applicationId.ToString(), AccessType.Delete)
            .Returns(true);

        var command = new DeleteFileCommand(fileId, applicationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _userRepository.Query().Received(1);
    }
} 