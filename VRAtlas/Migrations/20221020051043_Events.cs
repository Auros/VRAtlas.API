using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using VRAtlas.Models;

#nullable disable

namespace VRAtlas.Migrations
{
    /// <inheritdoc />
    public partial class Events : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Media = table.Column<ImageVariants>(type: "jsonb", nullable: false),
                    RSVP = table.Column<RSVP>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Groups_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContextEvent",
                columns: table => new
                {
                    ContextsId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextEvent", x => new { x.ContextsId, x.EventId });
                    table.ForeignKey(
                        name: "FK_ContextEvent_Contexts_ContextsId",
                        column: x => x.ContextsId,
                        principalTable: "Contexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContextEvent_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_ContextEvent_EventId",
                table: "ContextEvent",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OwnerId",
                table: "Events",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EventUser_StarsId",
                table: "EventUser",
                column: "StarsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContextEvent");

            migrationBuilder.DropTable(
                name: "EventUser");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
