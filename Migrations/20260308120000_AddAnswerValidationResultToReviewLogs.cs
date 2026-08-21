using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerValidationResultToReviewLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS answer_validation_result jsonb;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "answer_validation_result",
                schema: "internal",
                table: "review_logs");
        }
    }
}
