using System.Security.Claims;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Api.Security;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security.ClaimProviders;

public class TemplatePermissionsClaimProviderTests
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
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger);

        var result = await provider.GetClaimsAsync(principal);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenAppIdMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc")
        }));
        var sender = Substitute.For<ISender>();
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger);

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
        sender.Send(Arg.Any<GetTemplatePermissionsForUserByExternalProviderIdQuery>())
            .Returns(Task.FromResult(Result<IReadOnlyCollection<TemplatePermissionDto>>.Failure("err")));
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger);

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
        sender.Send(Arg.Any<GetTemplatePermissionsForUserByExternalProviderIdQuery>())
            .Returns(Task.FromResult(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(null)));
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger);

        var result = await provider.GetClaimsAsync(principal);

        Assert.Empty(result);
    }

    [Theory, AutoData]
    public async Task GetClaimsAsync_ShouldReturnClaims_WhenPermissionsReturned(Guid templateId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc"),
            new Claim("appid", "cid")
        }));
        var sender = Substitute.For<ISender>();
        var perms = new[]
        {
            new TemplatePermissionDto
            {
                TemplateId = templateId,
                AccessType = AccessType.Read,
                UserId = default
            }
        };
        sender.Send(Arg.Any<GetTemplatePermissionsForUserByExternalProviderIdQuery>())
            .Returns(Task.FromResult(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(perms)));
        var logger = Substitute.For<ILogger<TemplatePermissionsClaimProvider>>();
        var provider = new TemplatePermissionsClaimProvider(sender, logger);

        var result = (await provider.GetClaimsAsync(principal)).ToList();

        Assert.Single(result);
        Assert.Equal("permission", result[0].Type);
        Assert.Equal($"Template:{perms[0].TemplateId}:Read", result[0].Value);
    }
}