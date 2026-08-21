using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBookProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_book_progresses",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<string>(type: "text", nullable: false),
                    progress_percent = table.Column<float>(type: "real", nullable: false),
                    last_position_locator = table.Column<string>(type: "text", nullable: true),
                    last_chapter = table.Column<string>(type: "text", nullable: true),
                    is_finished = table.Column<bool>(type: "boolean", nullable: false),
                    last_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_book_progress_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_book_progress_projects",
                        column: x => x.project_id,
                        principalSchema: "internal",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_book_progress_project",
                schema: "internal",
                table: "user_book_progresses",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_book_progress",
                schema: "internal",
                table: "user_book_progresses",
                columns: new[] { "user_id", "book_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_book_progresses",
                schema: "internal");
        }
    }
}
