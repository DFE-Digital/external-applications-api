using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using MockQueryable.NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class AddApplicationResponseCommandHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAddResponseVersion_WhenValidRequest(
        Guid applicationId,
        string responseBody,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationRepository applicationRepository,
        IApplicationResponseAppender responseAppender)
    {
        // Arrange
        var encodedBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(responseBody));
        var command = new AddApplicationResponseCommand(applicationId, encodedBody);
        
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", email, DateTime.UtcNow, null, null, null);
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(true);

        var appDomainId = new ApplicationId(command.ApplicationId);
        var now = DateTime.UtcNow;
        var newResponse = new ApplicationResponse(new ResponseId(Guid.NewGuid()), appDomainId, responseBody, now, user.Id!);
        var domainEvent = new DfE.ExternalApplications.Domain.Events.ApplicationResponseAddedEvent(appDomainId, newResponse.Id!, user.Id!, now);
        responseAppender.Create(appDomainId, responseBody, user.Id!, Arg.Any<DateTime?>())
            .Returns(new ApplicationResponseAppendResult(now, newResponse, domainEvent));

        applicationRepository.AppendResponseVersionAsync(appDomainId, newResponse, now, user.Id!, Arg.Any<CancellationToken>())
            .Returns(("APP-001", newResponse));

        var mediator = Substitute.For<IMediator>();
        var handler = new AddApplicationResponseCommandHandler(userRepo, httpContextAccessor, permissionCheckerService, applicationRepository, responseAppender, mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.ApplicationId, result.Value.ApplicationId);
        Assert.Equal(responseBody, result.Value.ResponseBody);
        Assert.Equal(user.Id!.Value, result.Value.CreatedBy);
        await mediator.Received(1).Publish(domainEvent, Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAddResponseVersion_WhenValidRequestWithExternalId(
        Guid applicationId,
        string responseBody,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationRepository applicationRepository,
        IApplicationResponseAppender responseAppender)
    {
        // Arrange
        var encodedBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(responseBody));
        var command = new AddApplicationResponseCommand(applicationId, encodedBody);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "external-id";
        var claims = new List<Claim> { new("appid", externalId), new(ClaimTypes.Email, "test@example.com") };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, externalId);
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(true);

        var appDomainId = new ApplicationId(command.ApplicationId);
        var now = DateTime.UtcNow;
        var newResponse = new ApplicationResponse(new ResponseId(Guid.NewGuid()), appDomainId, responseBody, now, user.Id!);
        var domainEvent = new DfE.ExternalApplications.Domain.Events.ApplicationResponseAddedEvent(appDomainId, newResponse.Id!, user.Id!, now);
        responseAppender.Create(appDomainId, responseBody, user.Id!, Arg.Any<DateTime?>())
            .Returns(new ApplicationResponseAppendResult(now, newResponse, domainEvent));

        applicationRepository.AppendResponseVersionAsync(appDomainId, newResponse, now, user.Id!, Arg.Any<CancellationToken>())
            .Returns(("APP-001", newResponse));

        var mediator = Substitute.For<IMediator>();
        var handler = new AddApplicationResponseCommandHandler(userRepo, httpContextAccessor, permissionCheckerService, applicationRepository, responseAppender, mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.ApplicationId, result.Value.ApplicationId);
        Assert.Equal(responseBody, result.Value.ResponseBody);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
        AddApplicationResponseCommand command,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationRepository applicationRepository,
        IApplicationResponseAppender responseAppender)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        httpContextAccessor.HttpContext.Returns(httpContext);

        var mediator = Substitute.For<IMediator>();
        var handler = new AddApplicationResponseCommandHandler(
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationRepository,
            responseAppender,
            mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound(
        AddApplicationResponseCommand command,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationRepository applicationRepository,
        IApplicationResponseAppender responseAppender)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var users = Array.Empty<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var mediator = Substitute.For<IMediator>();
        var handler = new AddApplicationResponseCommandHandler(
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationRepository,
            responseAppender,
            mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserLacksPermission(
        AddApplicationResponseCommand command,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationRepository applicationRepository,
        IApplicationResponseAppender responseAppender)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            email,
            DateTime.UtcNow,
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(false);

        var mediator = Substitute.For<IMediator>();
        var handler = new AddApplicationResponseCommandHandler(
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationRepository,
            responseAppender,
            mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to update this application", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenApplicationNotFound(
        AddApplicationResponseCommand command,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationRepository applicationRepository,
        IApplicationResponseAppender responseAppender)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            email,
            DateTime.UtcNow,
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(true);

        // Ensure base64 is valid so we hit the "not found" branch after decode.
        command = command with
        {
            ResponseBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("some body"))
        };

        responseAppender.Create(Arg.Any<ApplicationId>(), Arg.Any<string>(), Arg.Any<UserId>(), Arg.Any<DateTime?>())
            .Returns(ci =>
            {
                var appId = (ApplicationId)ci[0]!;
                var body = (string)ci[1]!;
                var createdBy = (UserId)ci[2]!;
                var now = DateTime.UtcNow;
                var response = new ApplicationResponse(new ResponseId(Guid.NewGuid()), appId, body, now, createdBy);
                var evt = new DfE.ExternalApplications.Domain.Events.ApplicationResponseAddedEvent(appId, response.Id!, createdBy, now);
                return new ApplicationResponseAppendResult(now, response, evt);
            });

        applicationRepository.AppendResponseVersionAsync(
                Arg.Any<ApplicationId>(),
                Arg.Any<ApplicationResponse>(),
                Arg.Any<DateTime>(),
                Arg.Any<UserId>(),
                Arg.Any<CancellationToken>())
            .Returns((ValueTuple<string, ApplicationResponse>?)null);

        var mediator = Substitute.For<IMediator>();
        var handler = new AddApplicationResponseCommandHandler(
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationRepository,
            responseAppender,
            mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenBodyIsInvalidBase64(
        Guid applicationId,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationRepository applicationRepository,
        IApplicationResponseAppender responseAppender)
    {
        // Arrange
        var command = new AddApplicationResponseCommand(applicationId, "this is not base64");
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", email, DateTime.UtcNow, null, null, null);
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(true);

        var mediator = Substitute.For<IMediator>();
        var handler = new AddApplicationResponseCommandHandler(
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationRepository,
            responseAppender,
            mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid Base64 format for ResponseBody", result.Error);
    }
} 