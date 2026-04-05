using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityFoundationPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Groups",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Groups",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Groups",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Groups",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Groups]
                SET [ShortDescription] = LEFT(LTRIM(RTRIM([Description])), 220)
                WHERE [ShortDescription] IS NULL OR [ShortDescription] = ''
                """);

            migrationBuilder.Sql("""
                UPDATE [Groups]
                SET [Slug] = CONCAT('community-', REPLACE(CONVERT(nvarchar(36), [Id]), '-', ''))
                WHERE [Slug] IS NULL OR [Slug] = ''
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ShortDescription",
                table: "Groups",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(220)",
                oldMaxLength: 220,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Groups",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_GroupId",
                table: "Posts",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Slug",
                table: "Groups",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Groups_GroupId",
                table: "Posts",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Groups_GroupId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_GroupId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Groups_Slug",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Groups");
        }
    }
}
