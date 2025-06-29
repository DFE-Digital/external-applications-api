namespace DfE.ExternalApplications.Domain.Services;

public interface IApplicationReferenceProvider
{
    Task<string> GenerateReferenceAsync(CancellationToken cancellationToken = default);
} 