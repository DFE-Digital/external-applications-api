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
        public static void SeedTestData(ExternalApplicationsContext ctx)
        {
            var roleAdmin = new Role(new RoleId(Guid.NewGuid()), "Administrator");
            var roleSubmitter = new Role(new RoleId(Guid.NewGuid()), "Submitter");
            ctx.Roles.AddRange(roleAdmin, roleSubmitter);

            var now = DateTime.UtcNow;

            var aliceId = new UserId(Guid.NewGuid());
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

            var bobId = new UserId(Guid.NewGuid());
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

            var templateId = new TemplateId(Guid.NewGuid());
            var template = new Template(
                templateId,
                name: "Employee Onboarding",
                createdOn: now,
                createdBy: aliceId
            );
            ctx.Templates.Add(template);

            var templateVersionId = new TemplateVersionId(Guid.NewGuid());
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

            var applicationId = new ApplicationId(Guid.NewGuid());
            var application = new Domain.Entities.Application(
                applicationId,
                applicationReference: "APP-001",
                templateVersionId: templateVersionId,
                createdOn: now,
                createdBy: bobId,
                status: 1,
                lastModifiedOn: null,
                lastModifiedBy: null
            );
            ctx.Applications.Add(application);

            ctx.SaveChanges();

            var response1Id = new ResponseId(Guid.NewGuid());
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
                new PermissionId(Guid.NewGuid()),
                userId: bobId,
                applicationId: applicationId,
                resourceKey: "Application:Read",
                resourceType: ResourceType.Application,
                accessType: AccessType.Read,
                grantedOn: now,
                grantedBy: aliceId
            );

            var perm2 = new Permission(
                new PermissionId(Guid.NewGuid()),
                userId: bobId,
                applicationId: applicationId,
                resourceKey: "Application:Write",
                resourceType: ResourceType.Application,
                accessType: AccessType.Write,
                grantedOn: now,
                grantedBy: aliceId
            );

            var perm3 = new Permission(
                new PermissionId(Guid.NewGuid()),
                userId: aliceId,
                applicationId: applicationId,
                resourceKey: "Application:Write",
                resourceType: ResourceType.Application,
                accessType: AccessType.Write,
                grantedOn: now,
                grantedBy: aliceId
            );

            ctx.Permissions.AddRange(perm1, perm2, perm3);

            var templatePerm1 = new TemplatePermission(
                new TemplatePermissionId(Guid.NewGuid()),
                userId: bobId,
                templateId: templateId,
                accessType: AccessType.Read,
                grantedOn: now,
                grantedBy: aliceId
            );

            var templatePerm2 = new TemplatePermission(
                new TemplatePermissionId(Guid.NewGuid()),
                userId: aliceId,
                templateId: templateId,
                accessType: AccessType.Write,
                grantedOn: now,
                grantedBy: aliceId
            );
            ctx.TemplatePermissions.AddRange(templatePerm1, templatePerm2);

            var response2Id = new ResponseId(Guid.NewGuid());
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
