using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddAnkiLikeNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "card_template_id",
                schema: "internal",
                table: "cards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "note_id",
                schema: "internal",
                table: "cards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "note_types",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    css = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("note_types_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_note_types_projects",
                        column: x => x.project_id,
                        principalSchema: "internal",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "card_templates",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    note_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    front_template = table.Column<string>(type: "text", nullable: false),
                    back_template = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("card_templates_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_card_templates_note_types",
                        column: x => x.note_type_id,
                        principalSchema: "internal",
                        principalTable: "note_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_fields",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    note_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_key = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    field_type = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    archived = table.Column<bool>(type: "boolean", nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("note_fields_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_note_fields_note_types",
                        column: x => x.note_type_id,
                        principalSchema: "internal",
                        principalTable: "note_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_values = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    project_term_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("notes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_notes_decks",
                        column: x => x.deck_id,
                        principalSchema: "internal",
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_notes_note_types",
                        column: x => x.note_type_id,
                        principalSchema: "internal",
                        principalTable: "note_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_notes_project_terms",
                        column: x => x.project_term_id,
                        principalSchema: "internal",
                        principalTable: "project_terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cards_card_template_id",
                schema: "internal",
                table: "cards",
                column: "card_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_cards_note_id",
                schema: "internal",
                table: "cards",
                column: "note_id");

            migrationBuilder.CreateIndex(
                name: "ux_card_templates_type_key",
                schema: "internal",
                table: "card_templates",
                columns: new[] { "note_type_id", "template_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_note_fields_type_key",
                schema: "internal",
                table: "note_fields",
                columns: new[] { "note_type_id", "field_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_note_types_project_name",
                schema: "internal",
                table: "note_types",
                columns: new[] { "project_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notes_deck_id",
                schema: "internal",
                table: "notes",
                column: "deck_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_note_type_id",
                schema: "internal",
                table: "notes",
                column: "note_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_project_term_id",
                schema: "internal",
                table: "notes",
                column: "project_term_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cards_card_templates",
                schema: "internal",
                table: "cards",
                column: "card_template_id",
                principalSchema: "internal",
                principalTable: "card_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_cards_notes",
                schema: "internal",
                table: "cards",
                column: "note_id",
                principalSchema: "internal",
                principalTable: "notes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                -- Baseline schema (20260218105355) never added lexicon; data backfill below requires it.
                ALTER TABLE internal.cards ADD COLUMN IF NOT EXISTS lexicon jsonb;
                ALTER TABLE internal.cards ADD COLUMN IF NOT EXISTS project_term_id uuid;

                INSERT INTO internal.note_types (id, project_id, name, version, created_at, updated_at)
                SELECT uuid_generate_v4(), p.id, 'Sentence Mining', 1, now(), now()
                FROM internal.projects p
                ON CONFLICT (project_id, name) DO NOTHING;

                INSERT INTO internal.note_fields (id, note_type_id, field_key, label, field_type, sort_order, required, archived, created_at, updated_at)
                SELECT uuid_generate_v4(), nt.id, v.field_key, v.label, v.field_type, v.sort_order, v.req, false, now(), now()
                FROM internal.note_types nt
                CROSS JOIN (VALUES
                  ('Expression', 'Expression', 'textarea', 0, true),
                  ('Word', 'Word', 'text', 1, true),
                  ('Translation', 'Translation', 'textarea', 2, true),
                  ('Transcription', 'Transcription', 'text', 3, false),
                  ('WordTypes', 'Word types', 'text', 4, false),
                  ('Definition', 'Definition', 'textarea', 5, false),
                  ('Example', 'Example / context', 'textarea', 6, false),
                  ('Synonyms', 'Synonyms', 'tags', 7, false),
                  ('Antonyms', 'Antonyms', 'textarea', 8, false),
                  ('Notes', 'Notes', 'textarea', 9, false),
                  ('SourceTitle', 'Source title', 'text', 10, false),
                  ('SourceUrl', 'Source URL', 'url', 11, false),
                  ('Image', 'Image', 'image', 12, false),
                  ('Audio', 'Audio', 'audio', 13, false)
                ) AS v(field_key, label, field_type, sort_order, req)
                WHERE nt.name = 'Sentence Mining'
                ON CONFLICT (note_type_id, field_key) DO NOTHING;

                INSERT INTO internal.card_templates (id, note_type_id, template_key, name, front_template, back_template, sort_order, enabled, created_at, updated_at)
                SELECT uuid_generate_v4(), nt.id, 'default', 'Default',
                  '{{Expression}}',
                  '{{Word}}' || E'\n\n' || '{{Translation}}' || E'\n\n' || '{{Definition}}' || E'\n\n' || '{{Example}}' || E'\n\n' || '{{Synonyms}}' || E'\n\n' || '{{Antonyms}}' || E'\n\n' || '{{Notes}}',
                  0, true, now(), now()
                FROM internal.note_types nt
                WHERE nt.name = 'Sentence Mining'
                ON CONFLICT (note_type_id, template_key) DO NOTHING;

                CREATE TEMP TABLE tmp_anki_card_map ON COMMIT DROP AS
                SELECT
                  c.id AS card_id,
                  uuid_generate_v4() AS note_id,
                  nt.id AS note_type_id,
                  ct.id AS card_template_id
                FROM internal.cards c
                JOIN internal.decks d ON d.id = c.deck_id
                JOIN internal.note_types nt ON nt.project_id = d.project_id AND nt.name = 'Sentence Mining'
                JOIN internal.card_templates ct ON ct.note_type_id = nt.id AND ct.template_key = 'default';

                INSERT INTO internal.notes (id, deck_id, creator_id, note_type_id, field_values, project_term_id, created_at, updated_at)
                SELECT
                  m.note_id,
                  c.deck_id,
                  c.creator_id,
                  m.note_type_id,
                  jsonb_strip_nulls(jsonb_build_object(
                    'Expression', jsonb_build_object('string', c.sentence),
                    'Word', jsonb_build_object('string', c.target_word),
                    'Translation', jsonb_build_object('string', c.translation),
                    'Transcription', jsonb_build_object('string', COALESCE(c.lexicon->>'transcription', '')),
                    'WordTypes', jsonb_build_object('string', COALESCE(c.lexicon->>'word_types', '')),
                    'Definition', jsonb_build_object('string', COALESCE(c.lexicon->>'definition', '')),
                    'Example', jsonb_build_object('string', COALESCE(c.lexicon->>'example', '')),
                    'Antonyms', jsonb_build_object('string', COALESCE(c.lexicon->>'antonyms', '')),
                    'Notes', jsonb_build_object('string', COALESCE(c.lexicon->>'notes', '')),
                    'Synonyms', CASE
                      WHEN c.synonyms IS NULL OR jsonb_typeof(c.synonyms) <> 'array' THEN '{"strings":[]}'::jsonb
                      ELSE jsonb_build_object('strings', c.synonyms)
                    END,
                    'SourceTitle', jsonb_build_object('string', COALESCE(c.source_meta->>'title', '')),
                    'SourceUrl', jsonb_build_object('string', COALESCE(c.source_meta->>'url', '')),
                    'Image', jsonb_build_object('string', COALESCE(nullif(trim(COALESCE(c.media, '{}'::jsonb)->>'image_id'), ''), nullif(trim(COALESCE(c.media, '{}'::jsonb)->>'image_url'), ''), '')),
                    'Audio', jsonb_build_object('string', COALESCE(nullif(trim(COALESCE(c.media, '{}'::jsonb)->>'audio_id'), ''), nullif(trim(COALESCE(c.media, '{}'::jsonb)->>'audio_url'), ''), ''))
                  )),
                  c.project_term_id,
                  c.created_at,
                  c.updated_at
                FROM internal.cards c
                JOIN tmp_anki_card_map m ON m.card_id = c.id;

                UPDATE internal.cards c
                SET note_id = m.note_id,
                    card_template_id = m.card_template_id
                FROM tmp_anki_card_map m
                WHERE c.id = m.card_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cards_card_templates",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "fk_cards_notes",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropTable(
                name: "card_templates",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "note_fields",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "note_types",
                schema: "internal");

            migrationBuilder.DropIndex(
                name: "IX_cards_card_template_id",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropIndex(
                name: "IX_cards_note_id",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "card_template_id",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "note_id",
                schema: "internal",
                table: "cards");
        }
    }
}
