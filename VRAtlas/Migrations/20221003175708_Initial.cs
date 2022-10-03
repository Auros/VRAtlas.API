using System;
using Microsoft.EntityFrameworkCore.Migrations;
using VRAtlas.Models;

#nullable disable

namespace VRAtlas.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Identifiers = table.Column<PlatformIdentifiers>(type: "jsonb", nullable: false),
                    Icon = table.Column<ImageVariants>(type: "jsonb", nullable: false),
                    IconSourceUrl = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
