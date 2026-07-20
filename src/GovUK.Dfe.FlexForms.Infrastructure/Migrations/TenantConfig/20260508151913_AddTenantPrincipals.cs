using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GovUK.Dfe.FlexForms.Infrastructure.Migrations.TenantConfig
{
    /// <inheritdoc />
    public partial class AddTenantPrincipals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantPrincipals",
                schema: "tenantconfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalObjectId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrincipalType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPrincipals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantPrincipals_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenantconfig",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantPrincipals_PrincipalObjectId",
                schema: "tenantconfig",
                table: "TenantPrincipals",
                column: "PrincipalObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPrincipals_TenantId",
                schema: "tenantconfig",
                table: "TenantPrincipals",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantPrincipals",
                schema: "tenantconfig");
        }
    }
}
