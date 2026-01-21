using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Infrastructure.Database;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using Microsoft.EntityFrameworkCore;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Infrastructure.Repositories;

public sealed class ApplicationRepository(ExternalApplicationsContext dbContext)
    : EaRepository<Application>(dbContext), IApplicationRepository
{
    public async Task<(string ApplicationReference, ApplicationResponse Response)?> AppendResponseVersionAsync(
        ApplicationId applicationId,
        ApplicationResponse response,
        DateTime lastModifiedOn,
        UserId lastModifiedBy,
        CancellationToken cancellationToken)
    {
        // Minimal read: only fetch the reference for return payload + existence check.
        var applicationReference = await DbContext.Applications
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => a.ApplicationReference)
            .SingleOrDefaultAsync(cancellationToken);

        if (applicationReference is null)
            return null;

        await using var tx = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        DbContext.ApplicationResponses.Add(response);

        // Update last-modified tracking without loading the Application aggregate graph.
        await DbContext.Applications
            .Where(a => a.Id == applicationId)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.Status, ApplicationStatus.InProgress)
                    .SetProperty(a => a.LastModifiedOn, lastModifiedOn)
                    .SetProperty(a => a.LastModifiedBy, lastModifiedBy),
                cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (applicationReference, response);
    }
}


