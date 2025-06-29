//using DfE.CoreLibs.Testing.AutoFixture.Attributes;
//using DfE.ExternalApplications.Application.Applications.Commands;
//using DfE.ExternalApplications.Domain.Entities;
//using DfE.ExternalApplications.Domain.Interfaces;
//using DfE.ExternalApplications.Domain.Interfaces.Repositories;
//using DfE.ExternalApplications.Domain.ValueObjects;
//using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
//using Microsoft.AspNetCore.Http;
//using NSubstitute;
//using System.Security.Claims;

//namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

//public class CreateApplicationCommandHandlerTests
//{
//    [Theory]
//    [CustomAutoData(typeof(ApplicationCustomization))]
//    public async Task Handle_ShouldCreateApplicationAndResponse_WhenValidRequest(
//        CreateApplicationCommand command,
//        UserId userId,
//        IEaRepository<Domain.Entities.Application> applicationRepo,
//        IUnitOfWork unitOfWork)
//    {
//        // Arrange
//        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
//        var httpContext = new DefaultHttpContext();
//        var claims = new List<Claim>
//        {
//            new(ClaimTypes.NameIdentifier, userId.Value.ToString())
//        };
//        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
//        httpContextAccessor.HttpContext.Returns(httpContext);

//        var handler = new CreateApplicationCommandHandler(
//            applicationRepo,
//            httpContextAccessor,
//            unitOfWork);

//        // Act
//        var result = await handler.Handle(command, CancellationToken.None);

//        // Assert
//        Assert.True(result.IsSuccess);
//        Assert.NotNull(result.Value);
//        Assert.Equal(command.ApplicationReference, result.Value.ApplicationReference);
//        Assert.Equal(command.TemplateVersionId.Value, result.Value.TemplateVersionId);

//        await applicationRepo.Received(1)
//            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
//        await unitOfWork.Received(1)
//            .CommitAsync(Arg.Any<CancellationToken>());
//    }

//    [Theory]
//    [CustomAutoData(typeof(ApplicationCustomization))]
//    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
//        CreateApplicationCommand command,
//        IEaRepository<Domain.Entities.Application> applicationRepo,
//        IUnitOfWork unitOfWork)
//    {
//        // Arrange
//        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
//        var httpContext = new DefaultHttpContext();
//        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
//        httpContextAccessor.HttpContext.Returns(httpContext);

//        var handler = new CreateApplicationCommandHandler(
//            applicationRepo,
//            httpContextAccessor,
//            unitOfWork);

//        // Act
//        var result = await handler.Handle(command, CancellationToken.None);

//        // Assert
//        Assert.False(result.IsSuccess);
//        Assert.Equal("Not authenticated", result.Error);

//        await applicationRepo.DidNotReceive()
//            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
//        await unitOfWork.DidNotReceive()
//            .CommitAsync(Arg.Any<CancellationToken>());
//    }

//    [Theory]
//    [CustomAutoData(typeof(ApplicationCustomization))]
//    public async Task Handle_ShouldReturnFailure_WhenUserIdNotFound(
//        CreateApplicationCommand command,
//        IEaRepository<Domain.Entities.Application> applicationRepo,
//        IUnitOfWork unitOfWork)
//    {
//        // Arrange
//        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
//        var httpContext = new DefaultHttpContext();
//        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "TestAuth"));
//        httpContextAccessor.HttpContext.Returns(httpContext);

//        var handler = new CreateApplicationCommandHandler(
//            applicationRepo,
//            httpContextAccessor,
//            unitOfWork);

//        // Act
//        var result = await handler.Handle(command, CancellationToken.None);

//        // Assert
//        Assert.False(result.IsSuccess);
//        Assert.Equal("User ID not found", result.Error);

//        await applicationRepo.DidNotReceive()
//            .AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
//        await unitOfWork.DidNotReceive()
//            .CommitAsync(Arg.Any<CancellationToken>());
//    }
//} 