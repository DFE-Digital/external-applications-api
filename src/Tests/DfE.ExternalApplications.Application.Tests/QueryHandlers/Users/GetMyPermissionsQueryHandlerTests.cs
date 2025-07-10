using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using MockQueryable.NSubstitute;
using NSubstitute;
using System.Security.Claims;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Users;

public class GetMyPermissionsQueryHandlerTests
{
    [Theory]
    [CustomAutoData]
    public async Task Handle_NotAuthenticated_ShouldReturnFailure(
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetMyPermissionsQueryHandler(
            httpContextAccessor,
            userRepo,
            permissionCheckerService,
            mediator);

        // Act
        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_NoUserIdentifier_ShouldReturnFailure(
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new List<Claim>(), "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        httpContext.User = claimsPrincipal;
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetMyPermissionsQueryHandler(
            httpContextAccessor,
            userRepo,
            permissionCheckerService,
            mediator);

        // Act
        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_UserByEmail_NotFound_ShouldReturnFailure(
        string email,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        httpContext.User = claimsPrincipal;
        httpContextAccessor.HttpContext.Returns(httpContext);

        var emptyQueryable = new List<User>().AsQueryable().BuildMock();
        userRepo.Query().Returns(emptyQueryable);

        var handler = new GetMyPermissionsQueryHandler(
            httpContextAccessor,
            userRepo,
            permissionCheckerService,
            mediator);

        // Act
        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_UserByExternalId_NotFound_ShouldReturnFailure(
        string externalProviderId,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", externalProviderId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        httpContext.User = claimsPrincipal;
        httpContextAccessor.HttpContext.Returns(httpContext);

        var emptyQueryable = new List<User>().AsQueryable().BuildMock();
        userRepo.Query().Returns(emptyQueryable);

        var handler = new GetMyPermissionsQueryHandler(
            httpContextAccessor,
            userRepo,
            permissionCheckerService,
            mediator);

        // Act
        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_NoPermission_ShouldReturnFailure(
        string emailName,
        UserCustomization userCustom,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var email = $"{emailName}@example.com";
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        httpContext.User = claimsPrincipal;
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideEmail = email;
        var user = new Fixture().Customize(userCustom).Create<User>();
        var userQueryable = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQueryable);

        permissionCheckerService.HasPermission(ResourceType.User, user.Id!.Value.ToString(), AccessType.Read)
            .Returns(false);

        var handler = new GetMyPermissionsQueryHandler(
            httpContextAccessor,
            userRepo,
            permissionCheckerService,
            mediator);

        // Act
        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to view permissions.", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ValidRequest_WithEmail_ShouldReturnPermissions(
        string emailName,
        UserCustomization userCustom,
        UserAuthorizationDto authorizationData,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var email = $"{emailName}@example.com";
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        httpContext.User = claimsPrincipal;
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideEmail = email;
        var user = new Fixture().Customize(userCustom).Create<User>();
        var userQueryable = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQueryable);

        permissionCheckerService.HasPermission(ResourceType.User, user.Id!.Value.ToString(), AccessType.Read)
            .Returns(true);

        mediator.Send(Arg.Is<GetAllUserPermissionsQuery>(q => q.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(Result<UserAuthorizationDto>.Success(authorizationData));

        var handler = new GetMyPermissionsQueryHandler(
            httpContextAccessor,
            userRepo,
            permissionCheckerService,
            mediator);

        // Act
        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        // Assert
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
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", externalProviderId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        httpContext.User = claimsPrincipal;
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideExternalProviderId = externalProviderId;
        var user = new Fixture().Customize(userCustom).Create<User>();
        var userQueryable = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQueryable);

        permissionCheckerService.HasPermission(ResourceType.User, user.Id!.Value.ToString(), AccessType.Read)
            .Returns(true);

        mediator.Send(Arg.Is<GetAllUserPermissionsQuery>(q => q.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(Result<UserAuthorizationDto>.Success(authorizationData));

        var handler = new GetMyPermissionsQueryHandler(
            httpContextAccessor,
            userRepo,
            permissionCheckerService,
            mediator);

        // Act
        var result = await handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(authorizationData, result.Value);
    }
}