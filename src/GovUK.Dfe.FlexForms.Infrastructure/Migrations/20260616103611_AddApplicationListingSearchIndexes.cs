using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GovUK.Dfe.FlexForms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationListingSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Applications_CreatedOn",
                schema: "ea",
                table: "Applications",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Status_LastModifiedOn",
                schema: "ea",
                table: "Applications",
                columns: new[] { "Status", "LastModifiedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_CreatedOn",
                schema: "ea",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_Status_LastModifiedOn",
                schema: "ea",
                table: "Applications");
        }
    }
}
