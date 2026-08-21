using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <summary>
    /// Убираем дублирующие колонки с cards: контент только в notes.field_values.
    /// search_document — денормализованный текст для FTS; search_vector пересчитан от него.
    /// </summary>
    public partial class CardNoteOnlyDropLegacyColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cards_notes",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropIndex(
                name: "idx_cards_search",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "internal",
                table: "cards");

            migrationBuilder.AddColumn<string>(
                name: "search_document",
                schema: "internal",
                table: "cards",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE internal.cards c
                SET search_document = trim(both ' ' FROM regexp_replace(COALESCE(
                  (
                    SELECT concat_ws(' ',
                      NULLIF(trim(coalesce(n.field_values #>> '{Expression,string}')), ''),
                      NULLIF(trim(coalesce(n.field_values #>> '{Word,string}')), ''),
                      NULLIF(trim(coalesce(n.field_values #>> '{Translation,string}')), ''),
                      NULLIF(trim(coalesce(n.field_values #>> '{Definition,string}')), ''),
                      NULLIF(trim(coalesce(n.field_values #>> '{Example,string}')), '')
                    )
                    FROM internal.notes n
                    WHERE n.id = c.note_id
                  ),
                  concat_ws(' ',
                    NULLIF(trim(c.sentence), ''),
                    NULLIF(trim(c.target_word), ''),
                    NULLIF(trim(c.translation), '')
                  )
                ), '\s+', ' ', 'g'))
                WHERE true;
                """);

            migrationBuilder.Sql("""
                UPDATE internal.cards SET search_document = '' WHERE search_document IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "search_document",
                schema: "internal",
                table: "cards",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "lexicon",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "media",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "sentence",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "source_meta",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "synonyms",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "target_index",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "target_word",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "translation",
                schema: "internal",
                table: "cards");

            migrationBuilder.Sql("""
                DELETE FROM internal.cards WHERE note_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "note_id",
                schema: "internal",
                table: "cards",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE internal.cards ADD COLUMN search_vector tsvector
                GENERATED ALWAYS AS (
                  to_tsvector('english'::regconfig, COALESCE(search_document, ''::text))
                ) STORED;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX idx_cards_search ON internal.cards USING gin (search_vector);
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_cards_notes",
                schema: "internal",
                table: "cards",
                column: "note_id",
                principalSchema: "internal",
                principalTable: "notes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cards_notes",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropIndex(
                name: "idx_cards_search",
                schema: "internal",
                table: "cards");

            migrationBuilder.Sql("ALTER TABLE internal.cards DROP COLUMN search_vector;");

            migrationBuilder.AlterColumn<Guid>(
                name: "note_id",
                schema: "internal",
                table: "cards",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "translation",
                schema: "internal",
                table: "cards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "target_word",
                schema: "internal",
                table: "cards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "target_index",
                schema: "internal",
                table: "cards",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "synonyms",
                schema: "internal",
                table: "cards",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_meta",
                schema: "internal",
                table: "cards",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sentence",
                schema: "internal",
                table: "cards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "media",
                schema: "internal",
                table: "cards",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lexicon",
                schema: "internal",
                table: "cards",
                type: "jsonb",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "search_document",
                schema: "internal",
                table: "cards");

            migrationBuilder.Sql("""
                ALTER TABLE internal.cards ADD COLUMN search_vector tsvector
                GENERATED ALWAYS AS (to_tsvector('english'::regconfig, sentence)) STORED;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX idx_cards_search ON internal.cards USING gin (search_vector);
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_cards_notes",
                schema: "internal",
                table: "cards",
                column: "note_id",
                principalSchema: "internal",
                principalTable: "notes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
