using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRAtlas.Migrations
{
    /// <inheritdoc />
    public partial class Crossposting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Identity",
                table: "Groups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Crosspost",
                table: "Events",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Identity",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Crosspost",
                table: "Events");
        }
    }
}
