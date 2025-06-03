using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Users
{
    public class GetUserPermissionsQueryHandlerTests
    {
        [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization))]
        public async Task Handle_ShouldReturnAllPermissions_ForExistingUser(
            string rawEmail,
            UserCustomization userCustom,
            PermissionCustomization permCustom,
            [Frozen] IEaRepository<User> mockUserRepo,
            [Frozen] ICacheService<IMemoryCacheType> mockCacheService)
        {
            var normalizedEmail = rawEmail.Trim().ToLowerInvariant();

            userCustom.OverrideEmail = rawEmail;
            var fixture = new Fixture().Customize(userCustom);
            var userA = fixture.Create<User>();

            var backingField = typeof(User)
                .GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            backingField.SetValue(userA, new List<Permission>());

            var grantedBy = new UserId(Guid.NewGuid());
            userA.AddPermission(new ApplicationId(Guid.NewGuid()), "Key:Read", AccessType.Read, grantedBy, DateTime.UtcNow);
            userA.AddPermission(new ApplicationId(Guid.NewGuid()), "Key:Write", AccessType.Write, grantedBy, DateTime.UtcNow);

            var users = new List<User> { userA };
            var mockQ = users.AsQueryable().BuildMock();

            mockUserRepo.Query().Returns(mockQ);

            var cacheKey = $"Permissions_All_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";

            mockCacheService
                .GetOrAddAsync(
                    cacheKey,
                    Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
                    nameof(GetAllUserPermissionsQueryHandler)
                )
                .Returns(callInfo =>
                {
                    var factory = callInfo.Arg<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>();
                    return factory();
                });

            var handler = new GetAllUserPermissionsQueryHandler(mockUserRepo, mockCacheService);
            var result = await handler.Handle(new GetAllUserPermissionsQuery(rawEmail), CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);

            var dtoList = result.Value!;
            Assert.Equal(2, dtoList.Count);

            var expectedKeys = new[] { "Key:Read", "Key:Write" }.OrderBy(k => k);
            var actualKeys = dtoList.Select(d => d.ResourceKey).OrderBy(k => k);
            Assert.Equal(expectedKeys, actualKeys);

            var expectedTypes = new[] { AccessType.Read, AccessType.Write }.OrderBy(t => t);
            var actualTypes = dtoList.Select(d => d.AccessType).OrderBy(t => t);
            Assert.Equal(expectedTypes, actualTypes);

            // 6) Verify interactions
            mockUserRepo.Received(1).Query();
            await mockCacheService.Received(1)
                .GetOrAddAsync(
                    cacheKey,
                    Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
                    nameof(GetAllUserPermissionsQueryHandler)
                );
        }


        [Theory, CustomAutoData(typeof(UserCustomization))]
        public async Task Handle_ShouldReturnEmptyList_WhenUserNotFound(
            string rawEmail,
            UserCustomization userCustom,
            [Frozen] IEaRepository<User> mockUserRepo,
            [Frozen] ICacheService<IMemoryCacheType> mockCacheService)
        {
            userCustom.OverrideEmail = "someoneelse@example.com";
            var fixture = new Fixture().Customize(userCustom);
            var user = fixture.Create<User>();

            var backingField = typeof(User)
                .GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            backingField.SetValue(user, new List<Permission>());

            user.AddPermission(new ApplicationId(Guid.NewGuid()), "Other:Key", AccessType.Read, new UserId(Guid.NewGuid()), DateTime.UtcNow);

            var users = new List<User> { user };
            var mockQ = users.AsQueryable().BuildMock();

            mockUserRepo.Query().Returns(mockQ);

            var cacheKey = $"Permissions_All_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";

            mockCacheService
                .GetOrAddAsync(
                    cacheKey,
                    Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
                    nameof(GetAllUserPermissionsQueryHandler)
                )
                .Returns(callInfo =>
                {
                    var factory = callInfo.Arg<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>();
                    return factory();
                });

            var handler = new GetAllUserPermissionsQueryHandler(mockUserRepo, mockCacheService);
            var result = await handler.Handle(new GetAllUserPermissionsQuery(rawEmail), CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);

            mockUserRepo.Received(1).Query();
            await mockCacheService.Received(1)
                .GetOrAddAsync(
                    cacheKey,
                    Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
                    nameof(GetAllUserPermissionsQueryHandler)
                );
        }


        [Theory, CustomAutoData]
        public async Task Handle_ShouldReturnFromCache_AndNotCallRepository(
            string rawEmail,
            List<UserPermissionDto> cachedDtos,
            [Frozen] IEaRepository<User> mockUserRepo,
            [Frozen] ICacheService<IMemoryCacheType> mockCacheService)
        {
            var readOnlyDtos = cachedDtos.AsReadOnly();
            var cacheKey = $"Permissions_All_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";

            mockCacheService
                .GetOrAddAsync(
                    cacheKey,
                    Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
                    nameof(GetAllUserPermissionsQueryHandler)
                )
                .Returns(Task.FromResult(Result<IReadOnlyCollection<UserPermissionDto>>.Success(readOnlyDtos)));

            // Act: invoke the handler
            var handler = new GetAllUserPermissionsQueryHandler(mockUserRepo, mockCacheService);
            var result = await handler.Handle(new GetAllUserPermissionsQuery(rawEmail), CancellationToken.None);

            // Assert: it came from cache
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);

            var dtoList = result.Value!;
            Assert.Equal(readOnlyDtos.Count, dtoList.Count);

            for (int i = 0; i < readOnlyDtos.Count; i++)
            {
                Assert.Equal(readOnlyDtos[i].ResourceKey, dtoList.ElementAt(i).ResourceKey);
                Assert.Equal(readOnlyDtos[i].AccessType, dtoList.ElementAt(i).AccessType);
            }

            // Verify repository was never called
            mockUserRepo.DidNotReceive().Query();

            // Verify cache was called once with the correct arguments
            await mockCacheService.Received(1)
                .GetOrAddAsync(
                    cacheKey,
                    Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
                    nameof(GetAllUserPermissionsQueryHandler)
                );
        }

        [Theory, CustomAutoData(typeof(UserCustomization))]
        public async Task Handle_ShouldReturnFailure_WhenCacheThrows(
            string rawEmail,
            [Frozen] IEaRepository<User> mockUserRepo,
            [Frozen] ICacheService<IMemoryCacheType> mockCacheService)
        {
            mockCacheService
                .GetOrAddAsync(
                    Arg.Any<string>(),
                    Arg.Any<Func<Task<Result<IReadOnlyCollection<UserPermissionDto>>>>>(),
                    Arg.Any<string>())
                .Throws(new Exception("Boom"));

            var handler = new GetAllUserPermissionsQueryHandler(mockUserRepo, mockCacheService);

            // Act
            var result = await handler.Handle(new GetAllUserPermissionsQuery(rawEmail), CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Boom", result.Error);

            // Repository should not be invoked in this scenario
            mockUserRepo.DidNotReceive().Query();
        }
    }
}
