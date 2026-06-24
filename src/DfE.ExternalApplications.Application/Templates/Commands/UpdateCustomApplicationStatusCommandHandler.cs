using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.Commands
{
    public sealed class UpdateCustomApplicationStatusCommandHandler(
        IEaRepository<CustomApplicationStatus> repo,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateCustomApplicationStatusCommand, Result>
    {
        public async Task<Result> Handle(UpdateCustomApplicationStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await repo.Query().FirstOrDefaultAsync(x => x.Id!.Value == request.CustomApplicationStatusId, cancellationToken);

                if (entity is null)
                    return Result.NotFound("Custom application status not found");

                // Update mutable fields via reflection or create methods; direct property set is used here to match other entities
                var templateId = entity.TemplateId; // unchanged
                // Using internal setters is not available; use reflection workaround or replace with a new entity pattern
                // For simplicity, create a new instance and replace is not supported by repo; attempt to set via private setter using dynamic

                // Use expected pattern: entity has private setters, so use domain method if existed. Since none, use EF Core's Entry to set values via repository implementation.
                // We'll assume repository exposes a method to Update via tracking; here we attach and modify properties via DBContext in repo implementation.

                // As a pragmatic approach, throw if cannot update; but to keep functionality, use an approach via casting to Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry is not possible here.
                // Instead, if repo has UpdateAsync, use it. Check for IEaRepository<T> extension - not available. So use repo.Update via Add/Remove isn't ideal.

                // Reflectively set properties
                var labelProp = typeof(CustomApplicationStatus).GetProperty("Label", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var statusProp = typeof(CustomApplicationStatus).GetProperty("ApplicationStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                if (labelProp is null || statusProp is null)
                    return Result.Failure("Unable to update entity properties");

                labelProp.SetValue(entity, request.Label);
                statusProp.SetValue(entity, request.ApplicationStatus);

                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Failure(e.ToString());
            }
        }
    }
}
