using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEduAiAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFallback",
                table: "ChatMessages",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KbHit",
                table: "ChatMessages",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "KbScore",
                table: "ChatMessages",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LatencyMs",
                table: "ChatMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelUsed",
                table: "ChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatMessageFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsHelpful = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessageFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessageFeedbacks_ChatMessages_ChatMessageId",
                        column: x => x.ChatMessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessageFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageFeedbacks_ChatMessageId",
                table: "ChatMessageFeedbacks",
                column: "ChatMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageFeedbacks_UserId",
                table: "ChatMessageFeedbacks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessageFeedbacks");

            migrationBuilder.DropColumn(
                name: "IsFallback",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "KbHit",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "KbScore",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "LatencyMs",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ModelUsed",
                table: "ChatMessages");
        }
    }
}
