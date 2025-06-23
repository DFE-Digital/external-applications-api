using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Client.Contracts;
using DfE.ExternalApplications.Infrastructure.Database;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Helpers;
using System.Net.Http.Headers;
using System.Security.Claims;
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
}