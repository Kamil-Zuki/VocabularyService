using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTermsAndUserStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS internal.project_terms (
                    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
                    project_id uuid NOT NULL REFERENCES internal.projects(id) ON DELETE CASCADE,
                    text text NOT NULL,
                    normalized_text text NOT NULL,
                    type character varying(16) NOT NULL DEFAULT 'WORD',
                    language character varying(16),
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    updated_at timestamp with time zone NOT NULL DEFAULT now()
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uq_project_terms_norm_type
                    ON internal.project_terms (project_id, normalized_text, type);

                CREATE TABLE IF NOT EXISTS internal.user_term_statuses (
                    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
                    user_id uuid NOT NULL,
                    project_id uuid NOT NULL REFERENCES internal.projects(id) ON DELETE CASCADE,
                    project_term_id uuid NOT NULL REFERENCES internal.project_terms(id) ON DELETE CASCADE,
                    status character varying(16) NOT NULL DEFAULT 'NEW',
                    meaning text,
                    first_sentence text,
                    first_source_title text,
                    first_source_url text,
                    last_seen_at timestamp with time zone,
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    updated_at timestamp with time zone NOT NULL DEFAULT now()
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uq_user_term_status
                    ON internal.user_term_statuses (user_id, project_term_id);

                ALTER TABLE internal.cards
                ADD COLUMN IF NOT EXISTS project_term_id uuid REFERENCES internal.project_terms(id) ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE internal.cards DROP COLUMN IF EXISTS project_term_id;
                DROP TABLE IF EXISTS internal.user_term_statuses;
                DROP TABLE IF EXISTS internal.project_terms;
                """);
        }
    }
}
