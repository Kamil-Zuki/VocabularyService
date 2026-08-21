using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillAssessmentLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillAssessmentLogs",
                schema: "internal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Skill = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillAssessmentLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssessmentLogs_UserId_ProjectId_Skill",
                schema: "internal",
                table: "SkillAssessmentLogs",
                columns: new[] { "UserId", "ProjectId", "Skill" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillAssessmentLogs",
                schema: "internal");
        }
    }
}
