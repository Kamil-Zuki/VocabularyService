using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddDeepSkillTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListeningLevel",
                schema: "internal",
                table: "user_term_statuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReadingLevel",
                schema: "internal",
                table: "user_term_statuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpeakingLevel",
                schema: "internal",
                table: "user_term_statuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WritingLevel",
                schema: "internal",
                table: "user_term_statuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetSkill",
                schema: "internal",
                table: "card_templates",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListeningLevel",
                schema: "internal",
                table: "user_term_statuses");

            migrationBuilder.DropColumn(
                name: "ReadingLevel",
                schema: "internal",
                table: "user_term_statuses");

            migrationBuilder.DropColumn(
                name: "SpeakingLevel",
                schema: "internal",
                table: "user_term_statuses");

            migrationBuilder.DropColumn(
                name: "WritingLevel",
                schema: "internal",
                table: "user_term_statuses");

            migrationBuilder.DropColumn(
                name: "TargetSkill",
                schema: "internal",
                table: "card_templates");
        }
    }
}
