using System.Security.Claims;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Api.Security;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using Xunit;
using MockQueryable.NSubstitute;

namespace DfE.ExternalApplications.Api.Tests.Security.ClaimProviders;

public class TemplatePermissionsClaimProviderTests
{
    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenIssuerInvalid()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var userRepo = Substitute.For<IEaRepository<User>>();

        var provider = new TemplatePermissionsClaimProvider(sender, logger, userRepo);

        // Act
        var result = await provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenAppIdMissing()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc")
        }));
        var sender = Substitute.For<ISender>();
        var userRepo = Substitute.For<IEaRepository<User>>();

        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger, userRepo);

        // Act
        var result = await provider.GetClaimsAsync(principal);

        // Assert
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

        sender.Send(Arg.Is<GetTemplatePermissionsForUserByUserIdQuery>(q => q.UserId == userId))
            .Returns(Task.FromResult(Result<IReadOnlyCollection<TemplatePermissionDto>>.Failure("err")));
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger, userRepo);

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

        sender.Send(Arg.Is<GetTemplatePermissionsForUserByUserIdQuery>(q => q.UserId == userId))
            .Returns(Task.FromResult(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(null)));
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger, userRepo);

        // Act
        var result = await provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
    }

    [Theory, AutoData]
    public async Task GetClaimsAsync_ShouldReturnClaims_WhenPermissionsReturned(Guid templateId)
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
            new TemplatePermissionDto
            {
                TemplateId = templateId,
                AccessType = AccessType.Read,
                UserId = userId.Value
            }
        };
        sender.Send(Arg.Is<GetTemplatePermissionsForUserByUserIdQuery>(q => q.UserId == userId))
            .Returns(Task.FromResult(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(perms)));
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger, userRepo);

        // Act
        var result = (await provider.GetClaimsAsync(principal)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("permission", result[0].Type);
        Assert.Equal($"Template:{perms[0].TemplateId}:Read", result[0].Value);
    }
}