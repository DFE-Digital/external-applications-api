using System;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Domain.Common;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DfE.ExternalApplications.Infrastructure.Migrations
{
    public partial class AddCustomApplicationStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomApplicationStatuses",
                schema: "ea",
                columns: table => new
                {
                    CustomApplicationStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationStatus = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomApplicationStatuses", x => x.CustomApplicationStatusId);
                    table.ForeignKey(
                        name: "FK_CustomApplicationStatuses_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "ea",
                        principalTable: "Templates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomApplicationStatuses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "ea",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomApplicationStatuses_TemplateId_ApplicationStatus",
                schema: "ea",
                table: "CustomApplicationStatuses",
                columns: new[] { "TemplateId", "ApplicationStatus" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomApplicationStatuses",
                schema: "ea");
        }
    }
}
