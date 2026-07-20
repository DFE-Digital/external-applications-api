using AutoFixture;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Users.Queries;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using MockQueryable.NSubstitute;
using NSubstitute;
using System.Security.Claims;
using MockQueryable;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryHandlers.Users;

public class GetMyPermissionsQueryHandlerTests
{
    [Theory]
    [CustomAutoData]
    public async Task Handle_NotAuthenticated_ShouldReturnFailure(
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ISender mediator)
    {
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetMyPermissionsQueryHandler(httpContextAccessor, userRepo, mediator);

        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_NoUserIdentifier_ShouldReturnFailure(
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ISender mediator)
    {
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new List<Claim>(), "Test");
        httpContext.User = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetMyPermissionsQueryHandler(httpContextAccessor, userRepo, mediator);

        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_UserByEmail_NotFound_ShouldReturnFailure(
        string email,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ISender mediator)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, email)], "Test"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        userRepo.Query().Returns(new List<User>().AsQueryable().BuildMock());

        var handler = new GetMyPermissionsQueryHandler(httpContextAccessor, userRepo, mediator);

        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_UserByExternalId_NotFound_ShouldReturnFailure(
        string externalProviderId,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ISender mediator)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("appid", externalProviderId)], "Test"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        userRepo.Query().Returns(new List<User>().AsQueryable().BuildMock());

        var handler = new GetMyPermissionsQueryHandler(httpContextAccessor, userRepo, mediator);

        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ContributorWithoutUserRead_ShouldReturnPermissions(
        string emailName,
        UserCustomization userCustom,
        UserAuthorizationDto authorizationData,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ISender mediator)
    {
        var email = $"{emailName}@example.com";
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, email)], "Test"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideEmail = email;
        var user = new Fixture().Customize(userCustom).Create<User>();
        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());

        mediator.Send(Arg.Is<GetAllUserPermissionsQuery>(q => q.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(Result<UserAuthorizationDto>.Success(authorizationData));

        var handler = new GetMyPermissionsQueryHandler(httpContextAccessor, userRepo, mediator);

        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(authorizationData, result.Value);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ValidRequest_WithEmail_ShouldReturnPermissions(
        string emailName,
        UserCustomization userCustom,
        UserAuthorizationDto authorizationData,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ISender mediator)
    {
        var email = $"{emailName}@example.com";
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, email)], "Test"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideEmail = email;
        var user = new Fixture().Customize(userCustom).Create<User>();
        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());

        mediator.Send(Arg.Is<GetAllUserPermissionsQuery>(q => q.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(Result<UserAuthorizationDto>.Success(authorizationData));

        var handler = new GetMyPermissionsQueryHandler(httpContextAccessor, userRepo, mediator);

        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(authorizationData, result.Value);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ValidRequest_WithExternalId_ShouldReturnPermissions(
        string externalProviderId,
        UserCustomization userCustom,
        UserAuthorizationDto authorizationData,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ISender mediator)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("appid", externalProviderId)], "Test"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideExternalProviderId = externalProviderId;
        var user = new Fixture().Customize(userCustom).Create<User>();
        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());

        mediator.Send(Arg.Is<GetAllUserPermissionsQuery>(q => q.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(Result<UserAuthorizationDto>.Success(authorizationData));

        var handler = new GetMyPermissionsQueryHandler(httpContextAccessor, userRepo, mediator);

        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(authorizationData, result.Value);
    }
}
