using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Api.Client.Security;
using DfE.ExternalApplications.Client.Contracts;

namespace DfE.ExternalApplications.Api.Client.Extensions;

public static class TokensClientExtensions
{
    public static async Task<ExchangeTokenDto> ExchangeAndStoreAsync(
        this ITokensClient client,
        string idpToken,
        IInternalUserTokenStore tokenStore,
        CancellationToken cancellationToken = default)
    {
        var request = new ExchangeTokenDto(idpToken);
        var response = await client.ExchangeAsync(request, cancellationToken);
        if (!string.IsNullOrEmpty(response.AccessToken))
        {
            tokenStore.SetToken(response.AccessToken!);
        }
        return response;
    }
}