namespace GovUK.Dfe.FlexForms.Domain.Services;

public interface IApplicationReferenceProvider
{
    Task<string> GenerateReferenceAsync(CancellationToken cancellationToken = default);
} 
