using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSkillProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSkillProgresses",
                schema: "internal",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillTypeId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalValue = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSkillProgresses", x => new { x.UserId, x.ProjectId, x.SkillTypeId });
                    table.ForeignKey(
                        name: "FK_UserSkillProgresses_Projects",
                        column: x => x.ProjectId,
                        principalSchema: "internal",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSkillProgresses_SkillTypes",
                        column: x => x.SkillTypeId,
                        principalSchema: "internal",
                        principalTable: "SkillTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSkillProgresses_ProjectId",
                schema: "internal",
                table: "UserSkillProgresses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkillProgresses_SkillTypeId",
                schema: "internal",
                table: "UserSkillProgresses",
                column: "SkillTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSkillProgresses",
                schema: "internal");
        }
    }
}
