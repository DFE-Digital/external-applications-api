using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Infrastructure.Services;

public class DefaultApplicationReferenceProvider(IEaRepository<Application> applicationRepo) : IApplicationReferenceProvider
{
    public async Task<string> GenerateReferenceAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var latestApp = await applicationRepo.Query()
            .Where(a => a.CreatedOn.Date == today)
            .OrderByDescending(a => a.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        var currentNumber = 1;
        if (latestApp != null)
        {
            var parts = latestApp.ApplicationReference.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var number))
            {
                currentNumber = number + 1;
            }
        }

        // Generate new reference in format "APP-YYYYMMDD-NNN"
        return $"TRF-{today:yyyyMMdd}-{currentNumber:000}";
    }
} 