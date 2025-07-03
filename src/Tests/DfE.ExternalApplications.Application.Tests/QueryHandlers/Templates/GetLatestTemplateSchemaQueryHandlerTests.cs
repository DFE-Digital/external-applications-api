using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using MockQueryable.NSubstitute;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Security.Claims;
using MockQueryable;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Templates;

public class GetLatestTemplateSchemaQueryHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnSchema_WhenUserHasEmailAndPermission(
        Guid templateId,
        string emailName,
        TemplateSchemaDto templateSchema,
        UserCustomization userCustom,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ICacheService<IMemoryCacheType> cacheService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var email = $"{emailName}@example.com";
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideEmail = email;
        var user = new Fixture().Customize(userCustom).Create<User>();
        var userQ = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQ);

        permissionCheckerService.HasPermission(ResourceType.Template, templateId.ToString(), AccessType.Read)
            .Returns(true);

        mediator.Send(Arg.Is<GetLatestTemplateSchemaByUserIdQuery>(q => q.TemplateId == templateId && q.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Success(templateSchema));

        var cacheKey = $"TemplateSchema_PrincipalId_{CacheKeyHelper.GenerateHashedCacheKey(templateId.ToString())}_{email}";
        cacheService.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(), nameof(GetLatestTemplateSchemaQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<TemplateSchemaDto>>>>();
                return f();
            });

        var handler = new GetLatestTemplateSchemaQueryHandler(httpContextAccessor, userRepo, permissionCheckerService, cacheService, mediator);

        // Act
        var result = await handler.Handle(new GetLatestTemplateSchemaQuery(templateId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(templateSchema, result.Value);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnSchema_WhenUserHasExternalIdAndPermission(
        Guid templateId,
        string externalId,
        TemplateSchemaDto templateSchema,
        UserCustomization userCustom,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ICacheService<IMemoryCacheType> cacheService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("appid", externalId)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideExternalProviderId = externalId;
        var user = new Fixture().Customize(userCustom).Create<User>();
        var userQ = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQ);

        permissionCheckerService.HasPermission(ResourceType.Template, templateId.ToString(), AccessType.Read)
            .Returns(true);

        mediator.Send(Arg.Is<GetLatestTemplateSchemaByUserIdQuery>(q => q.TemplateId == templateId && q.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Success(templateSchema));

        var cacheKey = $"TemplateSchema_PrincipalId_{CacheKeyHelper.GenerateHashedCacheKey(templateId.ToString())}_{externalId}";
        cacheService.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(), nameof(GetLatestTemplateSchemaQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<TemplateSchemaDto>>>>();
                return f();
            });

        var handler = new GetLatestTemplateSchemaQueryHandler(httpContextAccessor, userRepo, permissionCheckerService, cacheService, mediator);

        // Act
        var result = await handler.Handle(new GetLatestTemplateSchemaQuery(templateId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(templateSchema, result.Value);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenNotAuthenticated(
        Guid templateId,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ICacheService<IMemoryCacheType> cacheService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetLatestTemplateSchemaQueryHandler(httpContextAccessor, userRepo, permissionCheckerService, cacheService, mediator);

        // Act
        var result = await handler.Handle(new GetLatestTemplateSchemaQuery(templateId), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenNoUserIdentifier(
        Guid templateId,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ICacheService<IMemoryCacheType> cacheService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetLatestTemplateSchemaQueryHandler(httpContextAccessor, userRepo, permissionCheckerService, cacheService, mediator);

        // Act
        var result = await handler.Handle(new GetLatestTemplateSchemaQuery(templateId), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotHavePermission(
        Guid templateId,
        string email,
        UserCustomization userCustom,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ICacheService<IMemoryCacheType> cacheService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        userCustom.OverrideEmail = email;
        var user = new Fixture().Customize(userCustom).Create<User>();
        var userQ = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQ);

        permissionCheckerService.HasPermission(ResourceType.Template, templateId.ToString(), AccessType.Read)
            .Returns(false);

        var cacheKey = $"TemplateSchema_PrincipalId_{CacheKeyHelper.GenerateHashedCacheKey(templateId.ToString())}_{email}";
        cacheService.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(), nameof(GetLatestTemplateSchemaQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<TemplateSchemaDto>>>>();
                return f();
            });

        var handler = new GetLatestTemplateSchemaQueryHandler(httpContextAccessor, userRepo, permissionCheckerService, cacheService, mediator);

        // Act
        var result = await handler.Handle(new GetLatestTemplateSchemaQuery(templateId), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to read this template", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFromCache(
        Guid templateId,
        string email,
        TemplateSchemaDto templateSchema,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ICacheService<IMemoryCacheType> cacheService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var cacheKey = $"TemplateSchema_PrincipalId_{CacheKeyHelper.GenerateHashedCacheKey(templateId.ToString())}_{email}";
        cacheService.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(), nameof(GetLatestTemplateSchemaQueryHandler))
            .Returns(Result<TemplateSchemaDto>.Success(templateSchema));

        var handler = new GetLatestTemplateSchemaQueryHandler(httpContextAccessor, userRepo, permissionCheckerService, cacheService, mediator);

        // Act
        var result = await handler.Handle(new GetLatestTemplateSchemaQuery(templateId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(templateSchema, result.Value);
        userRepo.DidNotReceive().Query();
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenCacheThrows(
        Guid templateId,
        string email,
        [Frozen] IHttpContextAccessor httpContextAccessor,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IPermissionCheckerService permissionCheckerService,
        [Frozen] ICacheService<IMemoryCacheType> cacheService,
        [Frozen] ISender mediator)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        cacheService.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(), Arg.Any<string>())
            .Throws(new Exception("Boom"));

        var handler = new GetLatestTemplateSchemaQueryHandler(httpContextAccessor, userRepo, permissionCheckerService, cacheService, mediator);

        // Act
        var result = await handler.Handle(new GetLatestTemplateSchemaQuery(templateId), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Boom", result.Error);
    }
} 