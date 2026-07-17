using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DfE.ExternalApplications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateIsLive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLive",
                schema: "ea",
                table: "Templates",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLive",
                schema: "ea",
                table: "Templates");
        }
    }
}
