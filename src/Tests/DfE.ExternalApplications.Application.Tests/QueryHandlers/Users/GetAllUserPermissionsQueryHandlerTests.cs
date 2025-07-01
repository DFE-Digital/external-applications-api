using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using MockQueryable.NSubstitute;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Users;

public class GetAllUserPermissionsQueryHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization))]
    public async Task Handle_UserWithPermissions_ShouldReturnPermissions(
        UserId userId,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        // Arrange
        userCustom.OverrideId = userId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        // Add permissions to user
        var permissions = fixture.Customize(permCustom).CreateMany<Permission>().ToList();
        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, permissions);

        var userQueryable = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQueryable);

        var cacheKey = $"Permissions_All_UserId_{CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString())}";
        cacheService.GetOrAddAsync(
            cacheKey,
            Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
            nameof(GetAllUserPermissionsQueryHandler))
            .Returns(call =>
            {
                var func = call.Arg<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>();
                return func();
            });

        var handler = new GetAllUserPermissionsQueryHandler(userRepo, cacheService);

        // Act
        var result = await handler.Handle(new GetAllUserPermissionsQuery(userId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
        Assert.Equal(permissions.Count, result.Value.Count);
        Assert.All(result.Value, dto =>
        {
            var permission = permissions.First(p => p.ApplicationId?.Value == dto.ApplicationId);
            Assert.Equal(permission.ResourceType, dto.ResourceType);
            Assert.Equal(permission.ResourceKey, dto.ResourceKey);
            Assert.Equal(permission.AccessType, dto.AccessType);
        });
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_UserNotFound_ShouldReturnEmptyCollection(
        UserId userId,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        // Arrange
        var emptyQueryable = new List<User>().AsQueryable().BuildMock();
        userRepo.Query().Returns(emptyQueryable);

        var cacheKey = $"Permissions_All_UserId_{CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString())}";
        cacheService.GetOrAddAsync(
            cacheKey,
            Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
            nameof(GetAllUserPermissionsQueryHandler))
            .Returns(call =>
            {
                var func = call.Arg<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>();
                return func();
            });

        var handler = new GetAllUserPermissionsQueryHandler(userRepo, cacheService);

        // Act
        var result = await handler.Handle(new GetAllUserPermissionsQuery(userId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_Exception_ShouldReturnFailure(
        UserId userId,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        // Arrange
        userRepo.Query().Throws(new Exception("Test exception"));

        var cacheKey = $"Permissions_All_UserId_{CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString())}";
        cacheService.GetOrAddAsync(
            cacheKey,
            Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
            nameof(GetAllUserPermissionsQueryHandler))
            .Returns(call =>
            {
                var func = call.Arg<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>();
                return func();
            });

        var handler = new GetAllUserPermissionsQueryHandler(userRepo, cacheService);

        // Act
        var result = await handler.Handle(new GetAllUserPermissionsQuery(userId), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Test exception", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_CacheHit_ShouldReturnCachedResult(
        UserId userId,
        IReadOnlyCollection<UserPermissionDto> cachedPermissions,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        // Arrange
        var cacheKey = $"Permissions_All_UserId_{CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString())}";
        cacheService.GetOrAddAsync(
            cacheKey,
            Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
            nameof(GetAllUserPermissionsQueryHandler))
            .Returns(Result<IReadOnlyCollection<UserPermissionDto>>.Success(cachedPermissions));

        var handler = new GetAllUserPermissionsQueryHandler(userRepo, cacheService);

        // Act
        var result = await handler.Handle(new GetAllUserPermissionsQuery(userId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(cachedPermissions, result.Value);
        userRepo.DidNotReceive().Query();
    }
} 