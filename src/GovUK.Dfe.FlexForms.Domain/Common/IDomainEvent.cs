using MediatR;

namespace GovUK.Dfe.FlexForms.Domain.Common
{
    public interface IDomainEvent : INotification
    {
        DateTime OccurredOn { get; }
    }
}
