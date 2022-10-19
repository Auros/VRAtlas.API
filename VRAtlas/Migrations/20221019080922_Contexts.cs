using System;
using Microsoft.EntityFrameworkCore.Migrations;
using VRAtlas.Models;

#nullable disable

namespace VRAtlas.Migrations
{
    /// <inheritdoc />
    public partial class Contexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<ImageVariants>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contexts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contexts_Id",
                table: "Contexts",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Contexts_Name",
                table: "Contexts",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contexts");
        }
    }
}
