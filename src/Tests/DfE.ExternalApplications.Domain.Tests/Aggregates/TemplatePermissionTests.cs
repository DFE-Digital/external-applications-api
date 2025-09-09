using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates
{
    public class TemplatePermissionTests
    {
        [Theory]
        [CustomAutoData(typeof(TemplatePermissionCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
            UserId userId,
            TemplateId templateId,
            AccessType type,
            DateTime grantedOn,
            UserId grantedBy)
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new TemplatePermission(
                    null!,
                    userId,
                    templateId,
                    type,
                    grantedOn,
                    grantedBy));

            Assert.Equal("id", ex.ParamName);
        }

        [Theory]
        [CustomAutoData(typeof(TemplatePermissionCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenUserIdIsNull(
            TemplatePermissionId id,
            TemplateId templateId,
            AccessType type,
            DateTime grantedOn,
            UserId grantedBy)
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new TemplatePermission(
                    id,
                    null!,
                    templateId,
                    type,
                    grantedOn,
                    grantedBy));

            Assert.Equal("userId", ex.ParamName);
        }

        [Theory]
        [CustomAutoData(typeof(TemplatePermissionCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenTemplateIdIsNull(
            TemplatePermissionId id,
            UserId userId,
            AccessType type,
            DateTime grantedOn,
            UserId grantedBy)
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new TemplatePermission(
                    id,
                    userId,
                    null!,
                    type,
                    grantedOn,
                    grantedBy));

            Assert.Equal("templateId", ex.ParamName);
        }
    }
}
