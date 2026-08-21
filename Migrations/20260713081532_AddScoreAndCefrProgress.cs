using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreAndCefrProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScorePercent",
                table: "UserLessonProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimeSpentSeconds",
                table: "UserLessonProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserCefrProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CefrLevel = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CompletedLessons = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalLessons = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsLevelCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LevelCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCefrProgresses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCefrProgresses_UserId_CefrLevel",
                table: "UserCefrProgresses",
                columns: new[] { "UserId", "CefrLevel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCefrProgresses");

            migrationBuilder.DropColumn(
                name: "ScorePercent",
                table: "UserLessonProgresses");

            migrationBuilder.DropColumn(
                name: "TimeSpentSeconds",
                table: "UserLessonProgresses");
        }
    }
}
