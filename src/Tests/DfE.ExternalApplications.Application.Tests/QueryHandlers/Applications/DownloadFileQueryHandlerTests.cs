using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using DfE.ExternalApplications.Utils.File;
using MockQueryable;
using NSubstitute.ExceptionExtensions;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using File = DfE.ExternalApplications.Domain.Entities.File;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications;

public class DownloadFileQueryHandlerTests
{
    private readonly IEaRepository<File> _uploadRepository;
    private readonly IEaRepository<User> _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEaRepository<Domain.Entities.Application> _applicationRepository;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DownloadFileQueryHandler _handler;

    public DownloadFileQueryHandlerTests()
    {
        _uploadRepository = Substitute.For<IEaRepository<File>>();
        _userRepository = Substitute.For<IEaRepository<User>>();
        _fileStorageService = Substitute.For<IFileStorageService>();
        _applicationRepository = Substitute.For<IEaRepository<Domain.Entities.Application>>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        _handler = new DownloadFileQueryHandler(
            _uploadRepository,
            _userRepository,
            _fileStorageService,
            _applicationRepository,
            _permissionCheckerService,
            _httpContextAccessor);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Success_When_Valid_Request(
        Domain.Entities.Application application,
        User user,
        File file,
        Stream fileStream,
        Guid fileId,
        ApplicationId applicationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var queryable = new List<User> { user }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        var applicationQueryable = new List<Domain.Entities.Application> { application }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Read)
            .Returns(true);

        _fileStorageService.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fileStream);

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(fileStream, result.Value.FileStream);
        Assert.Equal(file.OriginalFileName, result.Value.FileName);
        Assert.Equal(file.OriginalFileName.GetContentType(), result.Value.ContentType);

        await _fileStorageService.Received(1).DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_Not_Authenticated(
        Guid fileId,
        ApplicationId applicationId)
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_User_Not_Found(
        Domain.Entities.Application application,
        Guid fileId,
        ApplicationId applicationId)
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

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_Application_Not_Found(
        User user,
        Guid fileId,
        ApplicationId applicationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var queryable = new List<User> { user }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        var applicationQueryable = new List<Domain.Entities.Application>().AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_User_No_Read_Permission(
        Domain.Entities.Application application,
        User user,
        Guid fileId,
        ApplicationId applicationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var queryable = new List<User> { user }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        var applicationQueryable = new List<Domain.Entities.Application> { application }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Read)
            .Returns(false);

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to download this file", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Return_Failure_When_File_Not_Found(
        Domain.Entities.Application application,
        User user,
        Guid fileId,
        ApplicationId applicationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var queryable = new List<User> { user }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        var applicationQueryable = new List<Domain.Entities.Application> { application }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File>().AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Read)
            .Returns(true);

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

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
        ApplicationId applicationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var queryable = new List<User> { user }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        var applicationQueryable = new List<Domain.Entities.Application> { application }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Read)
            .Returns(true);

        _fileStorageService.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Storage error"));

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Storage error", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_Should_Use_Email_When_PrincipalId_Contains_At_Symbol(
        Domain.Entities.Application application,
        User user,
        File file,
        Stream fileStream,
        Guid fileId,
        ApplicationId applicationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var queryable = new List<User> { user }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        var applicationQueryable = new List<Domain.Entities.Application> { application }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Read)
            .Returns(true);

        _fileStorageService.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fileStream);

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

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
        Stream fileStream,
        Guid fileId,
        ApplicationId applicationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("azp", "external-provider-id")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var queryable = new List<User> { user }.AsQueryable().BuildMock();
        _userRepository.Query().Returns(queryable);

        var applicationQueryable = new List<Domain.Entities.Application> { application }.AsQueryable().BuildMock();
        _applicationRepository.Query().Returns(applicationQueryable);

        var fileQueryable = new List<File> { file }.AsQueryable().BuildMock();
        _uploadRepository.Query().Returns(fileQueryable);

        _permissionCheckerService.HasPermission(ResourceType.ApplicationFiles, application.Id!.Value.ToString(), AccessType.Read)
            .Returns(true);

        _fileStorageService.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fileStream);

        var query = new DownloadFileQuery(fileId, applicationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _userRepository.Query().Received(1);
    }
} 