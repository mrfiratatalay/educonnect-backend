using EduConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduConnect.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260405193000_AddPostBookmarksAndViews")]
    public partial class AddPostBookmarksAndViews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[PostBookmarks]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PostBookmarks]
                    (
                        [Id] uniqueidentifier NOT NULL,
                        [PostId] uniqueidentifier NOT NULL,
                        [UserId] uniqueidentifier NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [UpdatedAtUtc] datetime2 NULL,
                        CONSTRAINT [PK_PostBookmarks] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PostBookmarks_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [dbo].[Posts] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_PostBookmarks_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
                    );
                END

                IF OBJECT_ID(N'[dbo].[PostBookmarks]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE name = N'IX_PostBookmarks_PostId_UserId'
                         AND object_id = OBJECT_ID(N'[dbo].[PostBookmarks]')
                   )
                BEGIN
                    CREATE UNIQUE INDEX [IX_PostBookmarks_PostId_UserId]
                    ON [dbo].[PostBookmarks] ([PostId], [UserId]);
                END

                IF OBJECT_ID(N'[dbo].[PostBookmarks]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE name = N'IX_PostBookmarks_UserId'
                         AND object_id = OBJECT_ID(N'[dbo].[PostBookmarks]')
                   )
                BEGIN
                    CREATE INDEX [IX_PostBookmarks_UserId]
                    ON [dbo].[PostBookmarks] ([UserId]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[PostViews]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PostViews]
                    (
                        [Id] uniqueidentifier NOT NULL,
                        [PostId] uniqueidentifier NOT NULL,
                        [UserId] uniqueidentifier NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [UpdatedAtUtc] datetime2 NULL,
                        CONSTRAINT [PK_PostViews] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PostViews_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [dbo].[Posts] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_PostViews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
                    );
                END

                IF OBJECT_ID(N'[dbo].[PostViews]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE name = N'IX_PostViews_PostId_UserId'
                         AND object_id = OBJECT_ID(N'[dbo].[PostViews]')
                   )
                BEGIN
                    CREATE UNIQUE INDEX [IX_PostViews_PostId_UserId]
                    ON [dbo].[PostViews] ([PostId], [UserId]);
                END

                IF OBJECT_ID(N'[dbo].[PostViews]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE name = N'IX_PostViews_UserId'
                         AND object_id = OBJECT_ID(N'[dbo].[PostViews]')
                   )
                BEGIN
                    CREATE INDEX [IX_PostViews_UserId]
                    ON [dbo].[PostViews] ([UserId]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[PostViews]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[PostViews];
                END

                IF OBJECT_ID(N'[dbo].[PostBookmarks]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[PostBookmarks];
                END
                """);
        }
    }
}
