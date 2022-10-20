using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRAtlas.Migrations
{
    /// <inheritdoc />
    public partial class EventStars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventUser");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EventStar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventStar_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventEventStar",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    StarsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEventStar", x => new { x.EventId, x.StarsId });
                    table.ForeignKey(
                        name: "FK_EventEventStar_EventStar_StarsId",
                        column: x => x.StarsId,
                        principalTable: "EventStar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEventStar_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Id",
                table: "Events",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventEventStar_StarsId",
                table: "EventEventStar",
                column: "StarsId");

            migrationBuilder.CreateIndex(
                name: "IX_EventStar_UserId",
                table: "EventStar",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventEventStar");

            migrationBuilder.DropTable(
                name: "EventStar");

            migrationBuilder.DropIndex(
                name: "IX_Events_Id",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Events");

            migrationBuilder.CreateTable(
                name: "EventUser",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    StarsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventUser", x => new { x.EventId, x.StarsId });
                    table.ForeignKey(
                        name: "FK_EventUser_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventUser_Users_StarsId",
                        column: x => x.StarsId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventUser_StarsId",
                table: "EventUser",
                column: "StarsId");
        }
    }
}
