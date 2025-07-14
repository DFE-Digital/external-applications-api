using AutoFixture.Xunit2;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications;

public class GetMyApplicationsQueryHandlerTests
{
    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnApplications_WhenUserHasEmail(
        string emailName,
        List<ApplicationDto> applications,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ISender mediator)
    {
        // Arrange
        var email = $"{emailName}@example.com";
        var context = Substitute.For<HttpContext>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email) }, "TestAuth");
        identity.AddClaim(new Claim("authenticated", "true"));
        var principal = new ClaimsPrincipal(identity);
        context.User.Returns(principal);
        httpContextAccessor.HttpContext.Returns(context);

        mediator.Send(Arg.Is<GetApplicationsForUserQuery>(q => q.Email == email && q.IncludeSchema == false), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<ApplicationDto>>.Success(applications.AsReadOnly()));

        var handler = new GetMyApplicationsQueryHandler(httpContextAccessor, mediator);

        // Act
        var result = await handler.Handle(new GetMyApplicationsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(applications.Count, result.Value!.Count);
        await mediator.Received(1).Send(Arg.Is<GetApplicationsForUserQuery>(q => q.Email == email && q.IncludeSchema == false), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnApplications_WhenUserHasExternalId(
        string externalId,
        List<ApplicationDto> applications,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ISender mediator)
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim("appid", externalId) }, "TestAuth", ClaimTypes.Email, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        httpContextAccessor.HttpContext.Returns(context);

        mediator.Send(Arg.Is<GetApplicationsForUserByExternalProviderIdQuery>(q => q.ExternalProviderId == externalId && q.IncludeSchema == false), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<ApplicationDto>>.Success(applications.AsReadOnly()));

        var handler = new GetMyApplicationsQueryHandler(httpContextAccessor, mediator);

        // Act
        var result = await handler.Handle(new GetMyApplicationsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(applications.Count, result.Value!.Count);
        await mediator.Received(1).Send(Arg.Is<GetApplicationsForUserByExternalProviderIdQuery>(q => q.ExternalProviderId == externalId && q.IncludeSchema == false), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnApplicationsWithoutSchema_WhenIncludeSchemaIsFalse(
        string emailName,
        List<ApplicationDto> applications,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ISender mediator)
    {
        // Arrange
        var email = $"{emailName}@example.com";
        var context = Substitute.For<HttpContext>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email) }, "TestAuth");
        identity.AddClaim(new Claim("authenticated", "true"));
        var principal = new ClaimsPrincipal(identity);
        context.User.Returns(principal);
        httpContextAccessor.HttpContext.Returns(context);

        mediator.Send(Arg.Is<GetApplicationsForUserQuery>(q => q.Email == email && q.IncludeSchema == false), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<ApplicationDto>>.Success(applications.AsReadOnly()));

        var handler = new GetMyApplicationsQueryHandler(httpContextAccessor, mediator);

        // Act
        var result = await handler.Handle(new GetMyApplicationsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(applications.Count, result.Value!.Count);
        await mediator.Received(1).Send(Arg.Is<GetApplicationsForUserQuery>(q => q.Email == email && q.IncludeSchema == false), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnApplicationsWithSchema_WhenIncludeSchemaIsTrue(
        string emailName,
        List<ApplicationDto> applications,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ISender mediator)
    {
        // Arrange
        var email = $"{emailName}@example.com";
        var context = Substitute.For<HttpContext>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email) }, "TestAuth");
        identity.AddClaim(new Claim("authenticated", "true"));
        var principal = new ClaimsPrincipal(identity);
        context.User.Returns(principal);
        httpContextAccessor.HttpContext.Returns(context);

        mediator.Send(Arg.Is<GetApplicationsForUserQuery>(q => q.Email == email && q.IncludeSchema == true), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<ApplicationDto>>.Success(applications.AsReadOnly()));

        var handler = new GetMyApplicationsQueryHandler(httpContextAccessor, mediator);

        // Act
        var result = await handler.Handle(new GetMyApplicationsQuery(true), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(applications.Count, result.Value!.Count);
        await mediator.Received(1).Send(Arg.Is<GetApplicationsForUserQuery>(q => q.Email == email && q.IncludeSchema == true), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnApplicationsWithSchema_WhenUserHasExternalIdAndIncludeSchemaIsTrue(
        string externalId,
        List<ApplicationDto> applications,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ISender mediator)
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim("appid", externalId) }, "TestAuth", ClaimTypes.Email, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        httpContextAccessor.HttpContext.Returns(context);

        mediator.Send(Arg.Is<GetApplicationsForUserByExternalProviderIdQuery>(q => q.ExternalProviderId == externalId && q.IncludeSchema == true), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<ApplicationDto>>.Success(applications.AsReadOnly()));

        var handler = new GetMyApplicationsQueryHandler(httpContextAccessor, mediator);

        // Act
        var result = await handler.Handle(new GetMyApplicationsQuery(true), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(applications.Count, result.Value!.Count);
        await mediator.Received(1).Send(Arg.Is<GetApplicationsForUserByExternalProviderIdQuery>(q => q.ExternalProviderId == externalId && q.IncludeSchema == true), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenNotAuthenticated(
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ISender mediator)
    {
        // Arrange
        var context = Substitute.For<HttpContext>();
        httpContextAccessor.HttpContext.Returns(context);

        var handler = new GetMyApplicationsQueryHandler(httpContextAccessor, mediator);

        // Act
        var result = await handler.Handle(new GetMyApplicationsQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
        await mediator.DidNotReceive().Send(Arg.Any<IRequest<Result<IReadOnlyCollection<ApplicationDto>>>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenNoUserIdentifier(
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] ISender mediator)
    {
        // Arrange
        var identity = new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth", ClaimTypes.Email, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        httpContextAccessor.HttpContext.Returns(context);

        var handler = new GetMyApplicationsQueryHandler(httpContextAccessor, mediator);

        // Act
        var result = await handler.Handle(new GetMyApplicationsQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);
        await mediator.DidNotReceive().Send(Arg.Any<IRequest<Result<IReadOnlyCollection<ApplicationDto>>>>(), Arg.Any<CancellationToken>());
    }
} 