using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTtsSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tts_settings",
                schema: "internal",
                table: "projects",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tts_settings",
                schema: "internal",
                table: "projects");
        }
    }
}
