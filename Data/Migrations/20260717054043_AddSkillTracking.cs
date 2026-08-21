using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VocabularyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillTypes",
                schema: "internal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompletionThreshold = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSkillActivities",
                schema: "internal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SkillTypeId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSkillActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSkillActivities_SkillTypes",
                        column: x => x.SkillTypeId,
                        principalSchema: "internal",
                        principalTable: "SkillTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "internal",
                table: "SkillTypes",
                columns: new[] { "Id", "Code", "CompletionThreshold", "DisplayName", "Unit" },
                values: new object[,]
                {
                    { 1, "reading", 15, "Reading", "minutes" },
                    { 2, "listening", 10, "Listening", "minutes" },
                    { 3, "writing", 1, "Writing", "exercises" },
                    { 4, "speaking", 1, "Speaking", "exercises" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillTypes_Code",
                schema: "internal",
                table: "SkillTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSkillActivities_SkillTypeId",
                schema: "internal",
                table: "UserSkillActivities",
                column: "SkillTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkillActivities_UserId_ProjectId_Date",
                schema: "internal",
                table: "UserSkillActivities",
                columns: new[] { "UserId", "ProjectId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSkillActivities_UserId_ProjectId_Date_SkillTypeId",
                schema: "internal",
                table: "UserSkillActivities",
                columns: new[] { "UserId", "ProjectId", "Date", "SkillTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSkillActivities",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "SkillTypes",
                schema: "internal");
        }
    }
}
