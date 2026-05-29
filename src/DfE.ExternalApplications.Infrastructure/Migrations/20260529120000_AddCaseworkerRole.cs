using DfE.ExternalApplications.Domain.Common;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DfE.ExternalApplications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseworkerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "ea",
                table: "Roles",
                columns: new[] { "RoleId", "Name" },
                values: new object[] { RoleConstants.CaseworkerRoleId, RoleNames.Caseworker });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "ea",
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: RoleConstants.CaseworkerRoleId);
        }
    }
}
