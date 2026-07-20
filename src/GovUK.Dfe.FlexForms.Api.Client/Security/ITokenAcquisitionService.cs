using System.Threading.Tasks;

namespace GovUK.Dfe.FlexForms.Api.Client.Security
{
    public interface ITokenAcquisitionService
    {
        Task<string> GetTokenAsync();
    }
}
