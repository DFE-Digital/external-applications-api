using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DfE.ExternalApplications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedIndexs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemplateVersions_TemplateId",
                schema: "ea",
                table: "TemplateVersions");

            migrationBuilder.DropIndex(
                name: "IX_TemplatePermissions_UserId",
                schema: "ea",
                table: "TemplatePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_ApplicationId",
                schema: "ea",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_UserId",
                schema: "ea",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationResponses_ApplicationId",
                schema: "ea",
                table: "ApplicationResponses");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_TemplateId_CreatedOn",
                schema: "ea",
                table: "TemplateVersions",
                columns: new[] { "TemplateId", "CreatedOn" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePermissions_UserId_TemplateId",
                schema: "ea",
                table: "TemplatePermissions",
                columns: new[] { "UserId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ApplicationId_ResourceType",
                schema: "ea",
                table: "Permissions",
                columns: new[] { "ApplicationId", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_ResourceType_ApplicationId",
                schema: "ea",
                table: "Permissions",
                columns: new[] { "UserId", "ResourceType", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_ApplicationId_FileName",
                schema: "ea",
                table: "Files",
                columns: new[] { "ApplicationId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_Path_FileName",
                schema: "ea",
                table: "Files",
                columns: new[] { "Path", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicationReference",
                schema: "ea",
                table: "Applications",
                column: "ApplicationReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationResponses_ApplicationId_CreatedOn",
                schema: "ea",
                table: "ApplicationResponses",
                columns: new[] { "ApplicationId", "CreatedOn" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemplateVersions_TemplateId_CreatedOn",
                schema: "ea",
                table: "TemplateVersions");

            migrationBuilder.DropIndex(
                name: "IX_TemplatePermissions_UserId_TemplateId",
                schema: "ea",
                table: "TemplatePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_ApplicationId_ResourceType",
                schema: "ea",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_UserId_ResourceType_ApplicationId",
                schema: "ea",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Files_ApplicationId_FileName",
                schema: "ea",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_Path_FileName",
                schema: "ea",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Applications_ApplicationReference",
                schema: "ea",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationResponses_ApplicationId_CreatedOn",
                schema: "ea",
                table: "ApplicationResponses");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_TemplateId",
                schema: "ea",
                table: "TemplateVersions",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePermissions_UserId",
                schema: "ea",
                table: "TemplatePermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ApplicationId",
                schema: "ea",
                table: "Permissions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId",
                schema: "ea",
                table: "Permissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationResponses_ApplicationId",
                schema: "ea",
                table: "ApplicationResponses",
                column: "ApplicationId");
        }
    }
}
