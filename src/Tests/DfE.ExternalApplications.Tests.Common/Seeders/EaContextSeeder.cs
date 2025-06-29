using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Infrastructure.Database;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Tests.Common.Seeders
{
    public static class EaContextSeeder
    {
        public const string AliceId = "c3cbbc7a-07d0-471c-a544-7bcf3237adaa";
        public const string BobId = "ba6e447b-5e20-45cf-8a6e-133efeb586c2";
        public const string ApplicationId = "9816b822-56e3-4b65-852a-00a4a3294e11";
        public const string PermissionId1 = "02ce58d2-051f-4fe7-a545-d63acca93752";
        public const string PermissionId2 = "02ce58d2-051f-4fe7-a545-d63acca93753";
        public const string PermissionId3 = "02ce58d2-051f-4fe7-a545-d63acca93754";
        public const string TemplateId = "7c911fbd-10ab-4561-b30a-adea4f8ce939";
        public const string TemplateVersionId = "ea4c4969-be95-46a8-813b-942af93a90be";
        public const string ResponseId = "f08a670f-ae0f-407d-ba3d-935d3e642362";
        public const string ResponseId2 = "f08a670f-ae0f-407d-ba3d-935d3e642363";
        public const string TemplatePermissionId1 = "08a25ca0-5890-4bd5-a441-552ec9c13ee1";
        public const string TemplatePermissionId2 = "08a25ca0-5890-4bd5-a441-552ec9c13ee2";
        public const string AdminRoleId = "e0546efd-d85e-452a-8232-06a84d1b8513";
        public const string SubmitterRoleId = "a5d3d871-ce57-47b9-807d-de5c1551f9f2";

        public static void SeedTestData(ExternalApplicationsContext ctx)
        {
            var roleAdmin = new Role(new RoleId(new Guid(AdminRoleId)), "Administrator");
            var roleSubmitter = new Role(new RoleId(new Guid(SubmitterRoleId)), "Submitter");
            ctx.Roles.AddRange(roleAdmin, roleSubmitter);

            var now = DateTime.UtcNow;

            var aliceId = new UserId(new Guid(AliceId));
            var alice = new User(
                aliceId,
                roleAdmin.Id,
                name: "Alice Anderson",
                email: "alice@example.com",
                createdOn: now,
                createdBy: null,
                lastModifiedOn: null,
                lastModifiedBy: null,
                initialPermissions: null
            );

            var bobId = new UserId(new Guid(BobId));
            var bob = new User(
                bobId,
                roleSubmitter.Id,
                name: "Bob Brown",
                email: "bob@example.com",
                createdOn: now,
                createdBy: null,
                lastModifiedOn: null,
                lastModifiedBy: null,
                initialPermissions: null
            );

            ctx.Users.AddRange(alice, bob);
            ctx.SaveChanges();

            var templateId = new TemplateId(new Guid(TemplateId));
            var template = new Template(
                templateId,
                name: "Employee Onboarding",
                createdOn: now,
                createdBy: aliceId
            );
            ctx.Templates.Add(template);

            var templateVersionId = new TemplateVersionId(new Guid(TemplateVersionId));
            var templateVersion = new TemplateVersion(
                templateVersionId,
                templateId,
                versionNumber: "v1.0",
                jsonSchema: "{ \"type\": \"object\", \"properties\": { \"name\": { \"type\": \"string\" } } }",
                createdOn: now,
                createdBy: aliceId,
                lastModifiedOn: null,
                lastModifiedBy: null
            );
            ctx.TemplateVersions.Add(templateVersion);

            ctx.SaveChanges();

            var applicationId = new ApplicationId(new Guid(ApplicationId));
            var application = new Domain.Entities.Application(
                applicationId,
                applicationReference: "APP-001",
                templateVersionId: templateVersionId,
                createdOn: now,
                createdBy: bobId,
                status: ApplicationStatus.InProgress,
                lastModifiedOn: null,
                lastModifiedBy: null
            );
            ctx.Applications.Add(application);

            ctx.SaveChanges();

            var response1Id = new ResponseId(new Guid(ResponseId));
            var response1 = new ApplicationResponse(
                response1Id,
                applicationId: applicationId,
                responseBody: "Initial submission received",
                createdOn: now,
                createdBy: aliceId,
                lastModifiedOn: null,
                lastModifiedBy: null
            );
            ctx.ApplicationResponses.Add(response1);

            var perm1 = new Permission(
                new PermissionId(new Guid(PermissionId1)),
                userId: bobId,
                applicationId: applicationId,
                resourceKey: applicationId.Value.ToString(),
                resourceType: ResourceType.Application,
                accessType: AccessType.Read,
                grantedOn: now,
                grantedBy: aliceId
            );

            var perm2 = new Permission(
                new PermissionId(new Guid(PermissionId2)),
                userId: bobId,
                applicationId: applicationId,
                resourceKey: applicationId.Value.ToString(),
                resourceType: ResourceType.Application,
                accessType: AccessType.Write,
                grantedOn: now,
                grantedBy: aliceId
            );

            var perm3 = new Permission(
                new PermissionId(new Guid(PermissionId3)),
                userId: aliceId,
                applicationId: applicationId,
                resourceKey: applicationId.Value.ToString(),
                resourceType: ResourceType.Application,
                accessType: AccessType.Read,
                grantedOn: now,
                grantedBy: aliceId
            );

            ctx.Permissions.AddRange(perm1, perm2, perm3);

            var templatePerm1 = new TemplatePermission(
                new TemplatePermissionId(new Guid(TemplatePermissionId1)),
                userId: bobId,
                templateId: templateId,
                accessType: AccessType.Read,
                grantedOn: now,
                grantedBy: aliceId
            );

            var templatePerm2 = new TemplatePermission(
                new TemplatePermissionId(new Guid(TemplatePermissionId2)),
                userId: aliceId,
                templateId: templateId,
                accessType: AccessType.Write,
                grantedOn: now,
                grantedBy: aliceId
            );
            ctx.TemplatePermissions.AddRange(templatePerm1, templatePerm2);

            var response2Id = new ResponseId(new Guid(ResponseId2));
            var response2 = new ApplicationResponse(
                response2Id,
                applicationId: applicationId,
                responseBody: "Reviewed and approved",
                createdOn: now.AddMinutes(5),
                createdBy: bobId,
                lastModifiedOn: null,
                lastModifiedBy: null
            );
            ctx.ApplicationResponses.Add(response2);



            ctx.SaveChanges();
        }
    }
}
