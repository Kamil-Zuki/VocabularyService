using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingReviewLogColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS stability_before real NOT NULL DEFAULT 0;
                ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS stability_after real NOT NULL DEFAULT 0;
                ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS difficulty_before real NOT NULL DEFAULT 0;
                ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS difficulty_after real NOT NULL DEFAULT 0;
                ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS user_answer text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_answer",
                schema: "internal",
                table: "review_logs");

            migrationBuilder.DropColumn(
                name: "difficulty_after",
                schema: "internal",
                table: "review_logs");

            migrationBuilder.DropColumn(
                name: "difficulty_before",
                schema: "internal",
                table: "review_logs");

            migrationBuilder.DropColumn(
                name: "stability_after",
                schema: "internal",
                table: "review_logs");

            migrationBuilder.DropColumn(
                name: "stability_before",
                schema: "internal",
                table: "review_logs");
        }
    }
}
