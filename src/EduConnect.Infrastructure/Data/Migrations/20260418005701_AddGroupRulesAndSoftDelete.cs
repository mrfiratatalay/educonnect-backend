using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupRulesAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Groups",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "RulesJson",
                table: "Groups",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "RulesJson",
                table: "Groups");
        }
    }
}
