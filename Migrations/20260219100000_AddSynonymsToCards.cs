using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddSynonymsToCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE internal.cards
                ADD COLUMN IF NOT EXISTS synonyms jsonb;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "synonyms",
                schema: "internal",
                table: "cards");
        }
    }
}
