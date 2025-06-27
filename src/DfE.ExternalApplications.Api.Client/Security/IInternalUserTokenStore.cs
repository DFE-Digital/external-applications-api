namespace DfE.ExternalApplications.Api.Client.Security
{
    public interface IInternalUserTokenStore
    {
        string? GetToken();
        void SetToken(string token);
        void ClearToken();
    }
}
