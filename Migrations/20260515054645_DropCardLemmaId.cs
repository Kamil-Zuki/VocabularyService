using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class DropCardLemmaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cards_lemmas",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropIndex(
                name: "IX_cards_lemma_id",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "lemma_id",
                schema: "internal",
                table: "cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "lemma_id",
                schema: "internal",
                table: "cards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cards_lemma_id",
                schema: "internal",
                table: "cards",
                column: "lemma_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cards_lemmas",
                schema: "internal",
                table: "cards",
                column: "lemma_id",
                principalSchema: "internal",
                principalTable: "project_lemmas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
