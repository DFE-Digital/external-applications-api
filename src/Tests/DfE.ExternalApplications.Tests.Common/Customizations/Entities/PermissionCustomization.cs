using AutoFixture;
using AutoFixture.Kernel;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
namespace DfE.ExternalApplications.Tests.Common.Customizations.Entities
{
    /// <summary>
    /// Allows overriding specific Permission fields (e.g. ApplicationId, ResourceKey, AccessType).
    /// </summary>
    public class PermissionCustomization : ICustomization
    {
        public PermissionId? OverrideId { get; set; }
        public UserId? OverrideUserId { get; set; }
        public ApplicationId? OverrideAppId { get; set; }
        public string? OverrideResourceKey { get; set; }
        public AccessType? OverrideAccessType { get; set; }
        public DateTime? OverrideGrantedOn { get; set; }
        public UserId? OverrideGrantedBy { get; set; }

        public void Customize(IFixture fixture)
        {
            fixture.Customize<Permission>(composer =>
                composer.FromFactory(new MethodInvoker(
                    new GreedyConstructorQuery())));

            fixture.Customize<Permission>(composer => composer
                .FromFactory(() =>
                {
                    var id = OverrideId ?? new PermissionId(fixture.Create<Guid>());
                    var userId = OverrideUserId ?? new UserId(fixture.Create<Guid>());
                    var appId = OverrideAppId ?? new ApplicationId(fixture.Create<Guid>());
                    var resourceKey = OverrideResourceKey ?? fixture.Create<string>();
                    var accessType = OverrideAccessType ?? fixture.Create<AccessType>();
                    var grantedOn = OverrideGrantedOn ?? fixture.Create<DateTime>();
                    var grantedBy = OverrideGrantedBy ?? new UserId(fixture.Create<Guid>());

                    // Call the public constructor
                    return new Permission(
                        id,
                        userId,
                        appId,
                        resourceKey,
                        accessType,
                        grantedOn,
                        grantedBy);
                }));
        }
    }
}
