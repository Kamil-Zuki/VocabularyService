using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShadowingAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shadowing_attempts",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_book_id = table.Column<string>(type: "text", nullable: true),
                    sentence_text = table.Column<string>(type: "text", nullable: false),
                    tts_audio_url = table.Column<string>(type: "text", nullable: false),
                    user_recording_url = table.Column<string>(type: "text", nullable: true),
                    self_rating = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("shadowing_attempts_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_shadowing_attempts_cards",
                        column: x => x.card_id,
                        principalSchema: "internal",
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_shadowing_card_id",
                schema: "internal",
                table: "shadowing_attempts",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "idx_shadowing_user_id",
                schema: "internal",
                table: "shadowing_attempts",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shadowing_attempts",
                schema: "internal");
        }
    }
}
