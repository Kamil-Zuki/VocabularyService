using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumFieldsToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CefrLevel",
                table: "Lessons",
                type: "text",
                nullable: false,
                defaultValue: "B1");

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UnlocksAfterLessonId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetSkills",
                table: "Lessons",
                type: "text",
                nullable: false,
                defaultValue: "R,W");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CefrLevel_OrderIndex",
                table: "Lessons",
                columns: new[] { "CefrLevel", "OrderIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_CefrLevel_OrderIndex",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CefrLevel",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "UnlocksAfterLessonId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "TargetSkills",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "Lessons");
        }
    }
}
