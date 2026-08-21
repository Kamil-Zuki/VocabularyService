using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewLogSnapshotColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS step_before integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS step_after integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS reps_before integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS reps_after integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS lapses_before integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS lapses_after integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS elapsed_days_before integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS elapsed_days_after integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS scheduled_days_before integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS scheduled_days_after integer NOT NULL DEFAULT 0;

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS last_review_before timestamp with time zone NOT NULL DEFAULT now();

                ALTER TABLE internal.review_logs
                ADD COLUMN IF NOT EXISTS last_review_after timestamp with time zone NOT NULL DEFAULT now();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "step_before", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "step_after", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "reps_before", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "reps_after", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "lapses_before", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "lapses_after", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "elapsed_days_before", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "elapsed_days_after", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "scheduled_days_before", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "scheduled_days_after", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "last_review_before", schema: "internal", table: "review_logs");
            migrationBuilder.DropColumn(name: "last_review_after", schema: "internal", table: "review_logs");
        }
    }
}
