using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Api.Security;
using DfE.ExternalApplications.Application.Users.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using System.Security.Claims;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using Xunit;
using MockQueryable.NSubstitute;

namespace DfE.ExternalApplications.Api.Tests.Security.ClaimProviders;

public class PermissionsClaimProviderTests
{
    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenIssuerInvalid()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        var logger = Substitute.For<ILogger<PermissionsClaimProvider>>();
        var userRepo = Substitute.For<IEaRepository<User>>();

        var provider = new PermissionsClaimProvider(sender, logger, userRepo);

        var result = await provider.GetClaimsAsync(principal);

        Assert.Empty(result);
        await sender.DidNotReceive().Send(Arg.Any<GetAllUserPermissionsQuery>());
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenAppIdMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc")
        }));
        var sender = Substitute.For<ISender>();
        var logger = Substitute.For<ILogger<PermissionsClaimProvider>>();
        var userRepo = Substitute.For<IEaRepository<User>>();

        var provider = new PermissionsClaimProvider(sender, logger, userRepo);

        var result = await provider.GetClaimsAsync(principal);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenQueryFails()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        var userRepo = Substitute.For<IEaRepository<User>>();

        // Create a user with the matching external provider ID
        var userId = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var user = new User(
            id: userId,
            roleId: roleId,
            name: "Test User",
            email: "test@example.com",
            createdOn: DateTime.UtcNow,
            createdBy: null,
            lastModifiedOn: null,
            lastModifiedBy: null,
            externalProviderId: "cid"
        );
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        sender.Send(Arg.Is<GetAllUserPermissionsQuery>(q => q.UserId == userId))
            .Returns(Task.FromResult(Result<IReadOnlyCollection<UserPermissionDto>>.Failure("err")));
        var logger = Substitute.For<ILogger<PermissionsClaimProvider>>();
        var provider = new PermissionsClaimProvider(sender, logger, userRepo);

        // Act
        var result = await provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenNoPermissions()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        var userRepo = Substitute.For<IEaRepository<User>>();

        // Create a user with the matching external provider ID
        var userId = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var user = new User(
            id: userId,
            roleId: roleId,
            name: "Test User",
            email: "test@example.com",
            createdOn: DateTime.UtcNow,
            createdBy: null,
            lastModifiedOn: null,
            lastModifiedBy: null,
            externalProviderId: "cid"
        );
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        sender.Send(Arg.Any<GetAllUserPermissionsQuery>())
            .Returns(Task.FromResult(Result<IReadOnlyCollection<UserPermissionDto>>.Success(null)));
        var logger = Substitute.For<ILogger<PermissionsClaimProvider>>();
        var provider = new PermissionsClaimProvider(sender, logger, userRepo);

        // Act
        var result = await provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
    }

    [Theory, AutoData]
    public async Task GetClaimsAsync_ShouldReturnClaims_WhenPermissionsReturned(string key)
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        var userRepo = Substitute.For<IEaRepository<User>>();

        // Create a user with the matching external provider ID
        var userId = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var user = new User(
            id: userId,
            roleId: roleId,
            name: "Test User",
            email: "test@example.com",
            createdOn: DateTime.UtcNow,
            createdBy: null,
            lastModifiedOn: null,
            lastModifiedBy: null,
            externalProviderId: "cid"
        );
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var perms = new[]
        {
            new UserPermissionDto { ResourceType = ResourceType.Application, ResourceKey = key, AccessType = AccessType.Read }
        };
        sender.Send(Arg.Is<GetAllUserPermissionsQuery>(q => q.UserId == userId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyCollection<UserPermissionDto>>.Success(perms)));
        var logger = Substitute.For<ILogger<PermissionsClaimProvider>>();
        var provider = new PermissionsClaimProvider(sender, logger, userRepo);

        // Act
        var result = (await provider.GetClaimsAsync(principal)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("permission", result[0].Type);
        Assert.Equal($"Application:{key}:Read", result[0].Value);
    }
}