using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTargetPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Notifications', 'TargetPath') IS NULL
                BEGIN
                    ALTER TABLE [Notifications] ADD [TargetPath] nvarchar(500) NULL;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Notifications', 'TargetPath') IS NOT NULL
                BEGIN
                    ALTER TABLE [Notifications] DROP COLUMN [TargetPath];
                END
                """);
        }
    }
}
