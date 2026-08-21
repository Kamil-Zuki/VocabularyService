using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations;

/// <inheritdoc />
public partial class RenameLingqStatusToSaved : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Переименование статуса в БД: LINGQ → SAVED (понятнее в API и логах).
        migrationBuilder.Sql(
            "UPDATE internal.user_term_statuses SET status = 'SAVED' WHERE UPPER(TRIM(status)) = 'LINGQ';");

        migrationBuilder.CreateIndex(
            name: "IX_user_term_statuses_project_id",
            schema: "internal",
            table: "user_term_statuses",
            column: "project_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_term_statuses_project_term_id",
            schema: "internal",
            table: "user_term_statuses",
            column: "project_term_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_user_term_statuses_project_id",
            schema: "internal",
            table: "user_term_statuses");

        migrationBuilder.DropIndex(
            name: "IX_user_term_statuses_project_term_id",
            schema: "internal",
            table: "user_term_statuses");

        migrationBuilder.Sql(
            "UPDATE internal.user_term_statuses SET status = 'LINGQ' WHERE UPPER(TRIM(status)) = 'SAVED';");
    }
}
