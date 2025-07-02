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
using Xunit;

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
        var provider = new PermissionsClaimProvider(sender, logger);

        var result = await provider.GetClaimsAsync(principal);

        Assert.Empty(result);
        await sender.DidNotReceive().Send(Arg.Any<GetAllUserPermissionsByExternalProviderIdQuery>());
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
        var provider = new PermissionsClaimProvider(sender, logger);

        var result = await provider.GetClaimsAsync(principal);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenQueryFails()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetAllUserPermissionsByExternalProviderIdQuery>())
            .Returns(Task.FromResult(Result<IReadOnlyCollection<UserPermissionDto>>.Failure("err")));
        var logger = Substitute.For<ILogger<PermissionsClaimProvider>>();
        var provider = new PermissionsClaimProvider(sender, logger);

        var result = await provider.GetClaimsAsync(principal);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenNoPermissions()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetAllUserPermissionsByExternalProviderIdQuery>())
            .Returns(Task.FromResult(Result<IReadOnlyCollection<UserPermissionDto>>.Success(null)));
        var logger = Substitute.For<ILogger<PermissionsClaimProvider>>();
        var provider = new PermissionsClaimProvider(sender, logger);

        var result = await provider.GetClaimsAsync(principal);

        Assert.Empty(result);
    }

    [Theory, AutoData]
    public async Task GetClaimsAsync_ShouldReturnClaims_WhenPermissionsReturned(string key)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        var perms = new[]
        {
            new UserPermissionDto { ResourceType = ResourceType.Application, ResourceKey = key, AccessType = AccessType.Read }
        };
        sender.Send(Arg.Any<GetAllUserPermissionsByExternalProviderIdQuery>())
            .Returns(Task.FromResult(Result<IReadOnlyCollection<UserPermissionDto>>.Success(perms)));
        var logger = Substitute.For<ILogger<PermissionsClaimProvider>>();
        var provider = new PermissionsClaimProvider(sender, logger);

        var result = (await provider.GetClaimsAsync(principal)).ToList();

        Assert.Single(result);
        Assert.Equal("permission", result[0].Type);
        Assert.Equal($"Application:{key}:Read", result[0].Value);
    }
}