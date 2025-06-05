using AutoFixture;
using DfE.ExternalApplications.Domain.Common;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities
{
    public class GuidIdCustomization<TId> : ICustomization where TId : IStronglyTypedId
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register(() =>
            {
                var guid = fixture.Create<Guid>();
                // Use reflection or known ValueObject constructor that takes Guid
                return (TId)Activator.CreateInstance(typeof(TId), guid)!;
            });
        }
    }
}
