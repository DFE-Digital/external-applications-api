﻿using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Helpers;
using System.Net.Http.Headers;
using System.Security.Claims;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class TokensControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Exchange_ShouldReturnToken_WhenAzureTokenValid(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITokensClient tokensClient,
        HttpClient httpClient)
    {
        var appid = Guid.NewGuid().ToString();

        factory.TestClaims = new List<Claim>
        {
            new Claim("iss", "windows.net"),
            new Claim("appid", appid),
            new Claim(ClaimTypes.Role, "API.Read"),
            new Claim(ClaimTypes.Role, "API.Write")
        };

        var dbContext = factory.GetDbContext<ExternalApplicationsContext>();

        await dbContext.Users
            .Where(x => x.Email == "alice@example.com")
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.ExternalProviderId, appid));

        var externalToken = TestExternalIdentityValidator.CreateToken("bob@example.com");

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "azure-token");

        var result = await tokensClient.ExchangeAsync(new ExchangeTokenDto(externalToken));

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(
            result.AccessToken,
            new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "21f3ed37-8443-4755-9ed2-c68ca86b4398",
                ValidateAudience = true,
                ValidAudience = "20dafd6d-79e5-4caf-8b72-d070dcc9716f",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes("iw5/ivfUWaCpj+n3TihlGUzRVna+KKu8IfLP52GdgNXlDcqt3+N2MM45rwQ=")),
                ValidateLifetime = false
            },
            out _);

        Assert.Equal("bob@example.com", principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value);
        Assert.Contains(principal.Claims,
            c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "API.Write");
    }

    private static string CreateTokenWithoutEmail()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(claims: claims, signingCredentials: creds);
        return handler.WriteToken(jwt);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Exchange_ShouldReturnUnauthorized_WhenAzureTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITokensClient tokensClient)
    {
        var externalToken = TestExternalIdentityValidator.CreateToken("bob@example.com");

        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => tokensClient.ExchangeAsync(new ExchangeTokenDto(externalToken)));

        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Exchange_ShouldReturnForbidden_WhenRoleMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITokensClient tokensClient,
        HttpClient httpClient)
    {
        factory.TestClaims = new List<Claim>
        {
            new Claim("appid", "app"),
            new Claim("iss", "windows.net")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "azure-token");

        var externalToken = TestExternalIdentityValidator.CreateToken("bob@example.com");

        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => tokensClient.ExchangeAsync(new ExchangeTokenDto(externalToken)));

        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Exchange_ShouldReturnServerError_WhenSubjectTokenInvalid(
        CustomWebApplicationDbContextFactory<Program> factory,
        ITokensClient tokensClient,
        HttpClient httpClient)
    {
        factory.TestClaims = new List<Claim>
        {
            new Claim("iss", "windows.net"),
            new Claim("appid", "app"),
            new Claim(ClaimTypes.Role, "API.Read"),
            new Claim(ClaimTypes.Role, "API.Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "azure-token");

        var invalidToken = CreateTokenWithoutEmail();

        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => tokensClient.ExchangeAsync(new ExchangeTokenDto(invalidToken)));

        Assert.Equal(500, ex.StatusCode);
    }
}