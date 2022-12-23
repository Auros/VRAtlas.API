using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace VRAtlas.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentSocialPlatformUsername = table.Column<string>(type: "text", nullable: false),
                    CurrentSocialPlatformProfilePicture = table.Column<string>(type: "text", nullable: false),
                    SynchronizeUsernameWithSocialPlatform = table.Column<bool>(type: "boolean", nullable: false),
                    SynchronizeProfilePictureWithSocialPlatform = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Picture = table.Column<Guid>(type: "uuid", nullable: false),
                    SocialId = table.Column<string>(type: "text", nullable: false),
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_UserMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "UserMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Id",
                table: "Users",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MetadataId",
                table: "Users",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SocialId",
                table: "Users",
                column: "SocialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UserMetadata");
        }
    }
}
