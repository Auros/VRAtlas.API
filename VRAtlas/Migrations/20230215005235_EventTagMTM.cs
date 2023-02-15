using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRAtlas.Migrations
{
    /// <inheritdoc />
    public partial class EventTagMTM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventTag_Events_EventId",
                table: "EventTag");

            migrationBuilder.DropForeignKey(
                name: "FK_EventTag_Tags_TagId",
                table: "EventTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventTag",
                table: "EventTag");

            migrationBuilder.RenameTable(
                name: "EventTag",
                newName: "EventTags");

            migrationBuilder.RenameIndex(
                name: "IX_EventTag_TagId",
                table: "EventTags",
                newName: "IX_EventTags_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_EventTag_EventId",
                table: "EventTags",
                newName: "IX_EventTags_EventId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventTags",
                table: "EventTags",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventTags_Events_EventId",
                table: "EventTags",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventTags_Tags_TagId",
                table: "EventTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventTags_Events_EventId",
                table: "EventTags");

            migrationBuilder.DropForeignKey(
                name: "FK_EventTags_Tags_TagId",
                table: "EventTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventTags",
                table: "EventTags");

            migrationBuilder.RenameTable(
                name: "EventTags",
                newName: "EventTag");

            migrationBuilder.RenameIndex(
                name: "IX_EventTags_TagId",
                table: "EventTag",
                newName: "IX_EventTag_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_EventTags_EventId",
                table: "EventTag",
                newName: "IX_EventTag_EventId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventTag",
                table: "EventTag",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventTag_Events_EventId",
                table: "EventTag",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventTag_Tags_TagId",
                table: "EventTag",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
