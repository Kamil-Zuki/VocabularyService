using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations;

/// <inheritdoc />
public partial class ExtendReviewLogForUndo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS step_before integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS step_after integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS reps_before integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS reps_after integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS lapses_before integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS lapses_after integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS elapsed_days_before integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS elapsed_days_after integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS scheduled_days_before integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS scheduled_days_after integer NOT NULL DEFAULT 0;
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS last_review_before timestamp with time zone NOT NULL DEFAULT NOW();
            ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS last_review_after timestamp with time zone NOT NULL DEFAULT NOW();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS last_review_after;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS last_review_before;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS scheduled_days_after;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS scheduled_days_before;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS elapsed_days_after;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS elapsed_days_before;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS lapses_after;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS lapses_before;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS reps_after;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS reps_before;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS step_after;
            ALTER TABLE internal.review_logs DROP COLUMN IF EXISTS step_before;
            """);
    }
}
