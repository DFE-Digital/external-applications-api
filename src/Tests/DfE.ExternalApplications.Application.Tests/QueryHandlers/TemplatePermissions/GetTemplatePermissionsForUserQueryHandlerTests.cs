using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.TemplatePermissions;

public class GetTemplatePermissionsForUserQueryHandlerTests
{
    [Theory, CustomAutoData(typeof(UserCustomization), typeof(TemplatePermissionCustomization))]
    public async Task Handle_ShouldReturnAllPermissions_ForExistingUser(
        string rawEmail,
        UserCustomization userCustom,
        TemplatePermissionCustomization tpCustom,
        [Frozen] IEaRepository<User> repo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        var normalized = rawEmail.Trim().ToLowerInvariant();
        userCustom.OverrideEmail = rawEmail;
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backingField = typeof(User)
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backingField.SetValue(user, new List<TemplatePermission>());
        var tp1 = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        var tp2 = new Fixture().Customize(tpCustom).Create<TemplatePermission>();
        ((List<TemplatePermission>)backingField.GetValue(user)!).AddRange([tp1, tp2]);

        var list = new List<User> { user };
        var mockQ = list.AsQueryable().BuildMock();
        repo.Query().Returns(mockQ);

        var cacheKey = $"Template_Permissions_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";
        cache
            .GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(), nameof(GetTemplatePermissionsForUserQueryHandler))
            .Returns(call => { var f = call.Arg<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(); return f(); });

        var handler = new GetTemplatePermissionsForUserQueryHandler(repo, cache);
        var result = await handler.Handle(new GetTemplatePermissionsForUserQuery(rawEmail), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Theory, CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnEmpty_WhenUserNotFound(
        string rawEmail,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> repo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        userCustom.OverrideEmail = "someone@example.com";
        var user = new Fixture().Customize(userCustom).Create<User>();
        var backingField = typeof(User)
            .GetField("_templatePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backingField.SetValue(user, new List<TemplatePermission>());
        var list = new List<User> { user };
        repo.Query().Returns(list.AsQueryable().BuildMock());

        var cacheKey = $"Template_Permissions_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";
        cache
            .GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(), nameof(GetTemplatePermissionsForUserQueryHandler))
            .Returns(call => { var f = call.Arg<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(); return f(); });

        var handler = new GetTemplatePermissionsForUserQueryHandler(repo, cache);
        var result = await handler.Handle(new GetTemplatePermissionsForUserQuery(rawEmail), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Theory, CustomAutoData]
    public async Task Handle_ShouldReturnFromCache(
        string rawEmail,
        List<TemplatePermissionDto> cached,
        [Frozen] IEaRepository<User> repo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        var readOnly = cached.AsReadOnly();
        var cacheKey = $"Template_Permissions_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";
        cache
            .GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(), nameof(GetTemplatePermissionsForUserQueryHandler))
            .Returns(Task.FromResult(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(readOnly)));

        var handler = new GetTemplatePermissionsForUserQueryHandler(repo, cache);
        var result = await handler.Handle(new GetTemplatePermissionsForUserQuery(rawEmail), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(readOnly.Count, result.Value!.Count);
        repo.DidNotReceive().Query();
    }

    [Theory, CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenCacheThrows(
        string rawEmail,
        [Frozen] IEaRepository<User> repo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        cache
            .GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<TemplatePermissionDto>>>>>(), Arg.Any<string>())
            .Throws(new Exception("Boom"));

        var handler = new GetTemplatePermissionsForUserQueryHandler(repo, cache);
        var result = await handler.Handle(new GetTemplatePermissionsForUserQuery(rawEmail), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Boom", result.Error);
        repo.DidNotReceive().Query();
    }
}