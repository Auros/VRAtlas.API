using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRAtlas.Migrations
{
    /// <inheritdoc />
    public partial class EventStarTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "EventStar",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "EventStar");
        }
    }
}
