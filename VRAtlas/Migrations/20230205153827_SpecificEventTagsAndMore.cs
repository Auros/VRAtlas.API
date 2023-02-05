using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace VRAtlas.Migrations
{
    /// <inheritdoc />
    public partial class SpecificEventTagsAndMore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventTag_Tags_TagsId",
                table: "EventTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventTag",
                table: "EventTag");

            migrationBuilder.RenameColumn(
                name: "TagsId",
                table: "EventTag",
                newName: "TagId");

            migrationBuilder.RenameIndex(
                name: "IX_EventTag_TagsId",
                table: "EventTag",
                newName: "IX_EventTag_TagId");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "EventTag",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Instant>(
                name: "StartTime",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Instant>(
                name: "EndTime",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventTag",
                table: "EventTag",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventTag_EventId",
                table: "EventTag",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventTag_Tags_TagId",
                table: "EventTag",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventTag_Tags_TagId",
                table: "EventTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventTag",
                table: "EventTag");

            migrationBuilder.DropIndex(
                name: "IX_EventTag_EventId",
                table: "EventTag");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "EventTag");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "EventTag",
                newName: "TagsId");

            migrationBuilder.RenameIndex(
                name: "IX_EventTag_TagId",
                table: "EventTag",
                newName: "IX_EventTag_TagsId");

            migrationBuilder.AlterColumn<Instant>(
                name: "StartTime",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L),
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Instant>(
                name: "EndTime",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L),
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventTag",
                table: "EventTag",
                columns: new[] { "EventId", "TagsId" });

            migrationBuilder.AddForeignKey(
                name: "FK_EventTag_Tags_TagsId",
                table: "EventTag",
                column: "TagsId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
