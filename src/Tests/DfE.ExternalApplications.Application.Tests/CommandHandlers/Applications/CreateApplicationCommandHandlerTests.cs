using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class CreateApplicationCommandHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldCreateApplicationAndResponse_WhenValidRequestWithAppId(
        CreateApplicationCommand command,
        Template template,
        TemplateSchemaDto templateSchema,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "external-id";
        var claims = new List<Claim>
        {
            new("appid", externalId),
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            externalId);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(true);

        referenceProvider.GenerateReferenceAsync(Arg.Any<CancellationToken>())
            .Returns("APP-001");

        var application = new Domain.Entities.Application(
            new ApplicationId(Guid.NewGuid()),
            "APP-001",
            new TemplateVersionId(templateSchema.TemplateVersionId),
            DateTime.UtcNow,
            user.Id!);

        applicationFactory.CreateApplicationWithResponse(
            Arg.Any<ApplicationId>(),
            Arg.Any<string>(),
            Arg.Any<TemplateVersionId>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<UserId>())
            .Returns((application, null));

        mediator.Send(Arg.Any<GetLatestTemplateSchemaByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Success(templateSchema));

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("APP-001", result.Value.ApplicationReference);
        Assert.Equal(templateSchema.TemplateVersionId, result.Value.TemplateVersionId);

        await applicationRepo.Received(1)
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1)
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldCreateApplicationAndResponse_WhenValidRequestWithEmail(
        CreateApplicationCommand command,
        Template template,
        TemplateSchemaDto templateSchema,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
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

        permissionCheckerService.HasPermission(ResourceType.Template,command.TemplateId.ToString(), AccessType.Write).Returns(true);

        referenceProvider.GenerateReferenceAsync(Arg.Any<CancellationToken>())
            .Returns("APP-001");

        var application = new Domain.Entities.Application(
            new ApplicationId(Guid.NewGuid()),
            "APP-001",
            new TemplateVersionId(templateSchema.TemplateVersionId),
            DateTime.UtcNow,
            user.Id!);

        applicationFactory.CreateApplicationWithResponse(
            Arg.Any<ApplicationId>(),
            Arg.Any<string>(),
            Arg.Any<TemplateVersionId>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<UserId>())
            .Returns((application, null));

        mediator.Send(Arg.Any<GetLatestTemplateSchemaByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Success(templateSchema));

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("APP-001", result.Value.ApplicationReference);
        Assert.Equal(templateSchema.TemplateVersionId, result.Value.TemplateVersionId);

        await applicationRepo.Received(1)
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1)
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);

        await applicationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenNoUserIdentifier(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("other-claim", "some-value")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);

        await applicationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", "external-id"),
            new(ClaimTypes.Email, "nonexistent@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var users = new List<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);

        await applicationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotHavePermission(
        CreateApplicationCommand command,
        Template template,
        TemplateSchemaDto templateSchema,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "external-id";
        var claims = new List<Claim>
        {
            new("appid", externalId),
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            externalId);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(false);


        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to create applications for this template", result.Error);

        await applicationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenTemplateSchemaNotFound(
        CreateApplicationCommand command,
        Template template,
        TemplateSchemaDto templateSchema,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "external-id";
        var claims = new List<Claim>
        {
            new("appid", externalId),
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            externalId);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(true);

        mediator.Send(Arg.Any<GetLatestTemplateSchemaByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Failure("Template schema not found"));

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Template schema not found", result.Error);

        await applicationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenExceptionThrown(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "external-id";
        var claims = new List<Claim>
        {
            new("appid", externalId),
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            externalId);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(true);

        // Setup an exception to be thrown during the mediator call
        mediator.When(x => x.Send(Arg.Any<GetLatestTemplateSchemaByUserIdQuery>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new InvalidOperationException("Database connection failed"));

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Database connection failed", result.Error);

        await applicationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenHttpContextIsNull(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);

        await applicationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserIdentityNotAuthenticated(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationReferenceProvider referenceProvider,
        IApplicationFactory applicationFactory,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", "external-id"),
            new(ClaimTypes.Email, "test@example.com")
        };
        // Create an unauthenticated identity
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims)); // No authentication type = not authenticated
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            referenceProvider,
            applicationFactory,
            permissionCheckerService,
            mediator,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);

        await applicationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }
} 