using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DfE.ExternalApplications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ea");

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "ea",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "ea",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "ea",
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskAssignmentLabels",
                schema: "ea",
                columns: table => new
                {
                    TaskAssignmentLabelsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaskId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignmentLabels", x => x.TaskAssignmentLabelsId);
                    table.ForeignKey(
                        name: "FK_TaskAssignmentLabels_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskAssignmentLabels_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                schema: "ea",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK_Templates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplatePermissions",
                schema: "ea",
                columns: table => new
                {
                    TemplatePermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessType = table.Column<byte>(type: "tinyint", nullable: false),
                    GrantedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    GrantedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplatePermissions", x => x.TemplatePermissionId);
                    table.ForeignKey(
                        name: "FK_TemplatePermissions_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "ea",
                        principalTable: "Templates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplatePermissions_Users_GrantedBy",
                        column: x => x.GrantedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplatePermissions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateVersions",
                schema: "ea",
                columns: table => new
                {
                    TemplateVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JsonSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVersions", x => x.TemplateVersionId);
                    table.ForeignKey(
                        name: "FK_TemplateVersions_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "ea",
                        principalTable: "Templates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateVersions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateVersions_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                schema: "ea",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationReference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_Applications_TemplateVersions_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalSchema: "ea",
                        principalTable: "TemplateVersions",
                        principalColumn: "TemplateVersionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Applications_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Applications_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationResponses",
                schema: "ea",
                columns: table => new
                {
                    ResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationResponses", x => x.ResponseId);
                    table.ForeignKey(
                        name: "FK_ApplicationResponses_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "ea",
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationResponses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationResponses_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "ea",
                columns: table => new
                {
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<byte>(type: "tinyint", nullable: false),
                    ResourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccessType = table.Column<byte>(type: "tinyint", nullable: false),
                    GrantedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    GrantedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                    table.ForeignKey(
                        name: "FK_Permissions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "ea",
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_GrantedBy",
                        column: x => x.GrantedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationResponses_ApplicationId",
                schema: "ea",
                table: "ApplicationResponses",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationResponses_CreatedBy",
                schema: "ea",
                table: "ApplicationResponses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationResponses_LastModifiedBy",
                schema: "ea",
                table: "ApplicationResponses",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CreatedBy",
                schema: "ea",
                table: "Applications",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_LastModifiedBy",
                schema: "ea",
                table: "Applications",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TemplateVersionId",
                schema: "ea",
                table: "Applications",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ApplicationId",
                schema: "ea",
                table: "Permissions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_GrantedBy",
                schema: "ea",
                table: "Permissions",
                column: "GrantedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId",
                schema: "ea",
                table: "Permissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                schema: "ea",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignmentLabels_CreatedBy",
                schema: "ea",
                table: "TaskAssignmentLabels",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignmentLabels_UserId",
                schema: "ea",
                table: "TaskAssignmentLabels",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePermissions_GrantedBy",
                schema: "ea",
                table: "TemplatePermissions",
                column: "GrantedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePermissions_TemplateId",
                schema: "ea",
                table: "TemplatePermissions",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePermissions_UserId",
                schema: "ea",
                table: "TemplatePermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedBy",
                schema: "ea",
                table: "Templates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_CreatedBy",
                schema: "ea",
                table: "TemplateVersions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_LastModifiedBy",
                schema: "ea",
                table: "TemplateVersions",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_TemplateId",
                schema: "ea",
                table: "TemplateVersions",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                schema: "ea",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "ea",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastModifiedBy",
                schema: "ea",
                table: "Users",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                schema: "ea",
                table: "Users",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationResponses",
                schema: "ea");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "ea");

            migrationBuilder.DropTable(
                name: "TaskAssignmentLabels",
                schema: "ea");

            migrationBuilder.DropTable(
                name: "TemplatePermissions",
                schema: "ea");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "ea");

            migrationBuilder.DropTable(
                name: "TemplateVersions",
                schema: "ea");

            migrationBuilder.DropTable(
                name: "Templates",
                schema: "ea");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "ea");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "ea");
        }
    }
}
