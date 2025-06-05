using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates
{
    public class RoleTests
    {
        [Theory]
        [CustomAutoData(typeof(RoleCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
            string name)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new Role(null!, name));

            Assert.Equal("id", ex.ParamName);
        }

        [Theory]
        [CustomAutoData(typeof(RoleCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenNameIsNull(
            RoleId id)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new Role(id, null!));

            Assert.Equal("name", ex.ParamName);
        }
    }
}
