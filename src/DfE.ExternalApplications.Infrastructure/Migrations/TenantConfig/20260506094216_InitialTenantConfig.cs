using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DfE.ExternalApplications.Infrastructure.Migrations.TenantConfig
{
    /// <inheritdoc />
    public partial class InitialTenantConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenantconfig");

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "tenantconfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantFrontendOrigins",
                schema: "tenantconfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFrontendOrigins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantFrontendOrigins_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenantconfig",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantHostnames",
                schema: "tenantconfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Hostname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantHostnames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantHostnames_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenantconfig",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                schema: "tenantconfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSecret = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenantconfig",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantFrontendOrigins_Origin",
                schema: "tenantconfig",
                table: "TenantFrontendOrigins",
                column: "Origin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantFrontendOrigins_TenantId",
                schema: "tenantconfig",
                table: "TenantFrontendOrigins",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHostnames_Hostname",
                schema: "tenantconfig",
                table: "TenantHostnames",
                column: "Hostname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantHostnames_TenantId",
                schema: "tenantconfig",
                table: "TenantHostnames",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                schema: "tenantconfig",
                table: "Tenants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId_Category_Target",
                schema: "tenantconfig",
                table: "TenantSettings",
                columns: new[] { "TenantId", "Category", "Target" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantFrontendOrigins",
                schema: "tenantconfig");

            migrationBuilder.DropTable(
                name: "TenantHostnames",
                schema: "tenantconfig");

            migrationBuilder.DropTable(
                name: "TenantSettings",
                schema: "tenantconfig");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "tenantconfig");
        }
    }
}
