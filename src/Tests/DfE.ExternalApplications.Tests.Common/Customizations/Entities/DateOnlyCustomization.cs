using AutoFixture;

namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities;

public class DateOnlyCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DateOnly>(composer =>
            composer.FromFactory(() => DateOnly.FromDateTime(fixture.Create<DateTime>())));
    }
}