namespace DfE.ExternalApplications.Api.Client.Security
{
    public interface ITokenAcquisitionService
    {
        Task<string> GetTokenAsync();
    }
}
