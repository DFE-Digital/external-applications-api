//using AutoFixture;
//using AutoFixture.Xunit2;
//using DfE.CoreLibs.Caching.Helpers;
//using DfE.CoreLibs.Caching.Interfaces;
//using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
//using DfE.CoreLibs.Testing.AutoFixture.Attributes;
//using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
//using DfE.ExternalApplications.Domain.Entities;
//using DfE.ExternalApplications.Domain.Interfaces.Repositories;
//using DfE.ExternalApplications.Domain.ValueObjects;
//using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
//using MockQueryable;
//using NSubstitute;
//using NSubstitute.ExceptionExtensions;

//namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.TemplatePermissions;

//public class GetTemplatePermissionsForUserByUserIdQueryHandlerTests
//{
//    [Theory]
//    [CustomAutoData(typeof(UserCustomization), typeof(TemplatePermissionCustomization))]
//    public async Task Handle_ShouldReturnAllPermissions_ForExistingUser(
//        UserId userId,
//        TemplatePermissionCustomization tpCustom,
//        [Frozen] IEaRepository<TemplatePermission> repo,
//        [Frozen] ICacheService<IMemoryCacheType> cache)
//    {
//        // Arrange
//        tpCustom.OverrideUserId = userId;
//        var fixture = new Fixture().Customize(tpCustom);
//        var tp1 = fixture.Create<User>();
//        var tp2 = fixture.Create<User>();

//        var list = new List<User> { tp1, tp2 };
//        var mockQ = list.AsQueryable().BuildMock();
//        repo.Query().Returns(mockQ);

//        var cacheKey = $"Template_Permissions_ByUserId_{CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString())}";
//        cache
//            .GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(), nameof(GetTemplatePermissionsForUserByUserIdQueryHandler))
//            .Returns(call => { var f = call.Arg<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(); return f(); });

//        var handler = new GetTemplatePermissionsForUserByUserIdQueryHandler(repo, cache);
//        var result = await handler.Handle(new GetTemplatePermissionsForUserByUserIdQuery(userId), CancellationToken.None);

//        // Assert
//        Assert.True(result.IsSuccess);
//        Assert.Equal(2, result.Value!.Count);
//    }

//    [Theory]
//    [CustomAutoData(typeof(UserCustomization))]
//    public async Task Handle_ShouldReturnEmpty_WhenUserHasNoPermissions(
//        UserId userId,
//        [Frozen] IEaRepository<TemplatePermission> repo,
//        [Frozen] ICacheService<IMemoryCacheType> cache)
//    {
//        // Arrange
//        var list = new List<TemplatePermission>();
//        repo.Query().Returns(list.AsQueryable().BuildMock());

//        var cacheKey = $"Template_Permissions_ByUserId_{CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString())}";
//        cache
//            .GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(), nameof(GetTemplatePermissionsForUserByUserIdQueryHandler))
//            .Returns(call => { var f = call.Arg<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(); return f(); });

//        var handler = new GetTemplatePermissionsForUserByUserIdQueryHandler(repo, cache);
//        var result = await handler.Handle(new GetTemplatePermissionsForUserByUserIdQuery(userId), CancellationToken.None);

//        // Assert
//        Assert.True(result.IsSuccess);
//        Assert.Empty(result.Value!);
//    }

//    [Theory]
//    [CustomAutoData]
//    public async Task Handle_ShouldReturnFromCache(
//        UserId userId,
//        List<TemplatePermissionDto> cached,
//        [Frozen] IEaRepository<TemplatePermission> repo,
//        [Frozen] ICacheService<IMemoryCacheType> cache)
//    {
//        // Arrange
//        var readOnly = cached.AsReadOnly();
//        var cacheKey = $"Template_Permissions_ByUserId_{CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString())}";
//        cache
//            .GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(), nameof(GetTemplatePermissionsForUserByUserIdQueryHandler))
//            .Returns(Task.FromResult(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(readOnly)));

//        var handler = new GetTemplatePermissionsForUserByUserIdQueryHandler(repo, cache);
//        var result = await handler.Handle(new GetTemplatePermissionsForUserByUserIdQuery(userId), CancellationToken.None);

//        // Assert
//        Assert.True(result.IsSuccess);
//        Assert.Equal(readOnly.Count, result.Value!.Count);
//        repo.DidNotReceive().Query();
//    }

//    [Theory]
//    [CustomAutoData(typeof(UserCustomization))]
//    public async Task Handle_ShouldReturnFailure_WhenCacheThrows(
//        UserId userId,
//        [Frozen] IEaRepository<TemplatePermission> repo,
//        [Frozen] ICacheService<IMemoryCacheType> cache)
//    {
//        // Arrange
//        cache
//            .GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(), Arg.Any<string>())
//            .Throws(new Exception("Boom"));

//        var handler = new GetTemplatePermissionsForUserByUserIdQueryHandler(repo, cache);
//        var result = await handler.Handle(new GetTemplatePermissionsForUserByUserIdQuery(userId), CancellationToken.None);

//        // Assert
//        Assert.False(result.IsSuccess);
//        Assert.Contains("Boom", result.Error);
//        repo.DidNotReceive().Query();
//    }
//} 