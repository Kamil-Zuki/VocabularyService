using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VocabularyService.Migrations;

/// <inheritdoc />
public partial class AddAgentPersistenceTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS internal.agent_threads (
                id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
                user_id uuid NOT NULL,
                project_id uuid NOT NULL,
                title text NULL,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                updated_at timestamp with time zone NOT NULL DEFAULT now(),
                archived_at timestamp with time zone NULL,
                CONSTRAINT fk_agent_threads_projects FOREIGN KEY (project_id)
                    REFERENCES internal.projects (id)
            );

            CREATE INDEX IF NOT EXISTS idx_agent_threads_user_project_updated
                ON internal.agent_threads (user_id, project_id, updated_at DESC);

            CREATE TABLE IF NOT EXISTS internal.agent_messages (
                id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
                thread_id uuid NOT NULL,
                role character varying(16) NOT NULL,
                content text NOT NULL,
                metadata_json jsonb NULL,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT fk_agent_messages_threads FOREIGN KEY (thread_id)
                    REFERENCES internal.agent_threads (id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_agent_messages_thread_created
                ON internal.agent_messages (thread_id, created_at);

            CREATE TABLE IF NOT EXISTS internal.agent_runs (
                id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
                thread_id uuid NOT NULL,
                status character varying(16) NOT NULL,
                model text NULL,
                started_at timestamp with time zone NOT NULL DEFAULT now(),
                completed_at timestamp with time zone NULL,
                error text NULL,
                CONSTRAINT fk_agent_runs_threads FOREIGN KEY (thread_id)
                    REFERENCES internal.agent_threads (id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_agent_runs_thread_id
                ON internal.agent_runs (thread_id);

            CREATE TABLE IF NOT EXISTS internal.agent_tool_calls (
                id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
                run_id uuid NOT NULL,
                tool_name text NOT NULL,
                input_json jsonb NOT NULL DEFAULT '{}'::jsonb,
                output_json jsonb NOT NULL DEFAULT '{}'::jsonb,
                status character varying(16) NOT NULL,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT fk_agent_tool_calls_runs FOREIGN KEY (run_id)
                    REFERENCES internal.agent_runs (id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_agent_tool_calls_run_created
                ON internal.agent_tool_calls (run_id, created_at);

            CREATE TABLE IF NOT EXISTS internal.agent_domain_decisions (
                id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
                run_id uuid NOT NULL,
                allowed boolean NOT NULL,
                category character varying(32) NOT NULL,
                reason text NULL,
                user_text_preview text NULL,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT fk_agent_domain_decisions_runs FOREIGN KEY (run_id)
                    REFERENCES internal.agent_runs (id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_agent_domain_decisions_run_id
                ON internal.agent_domain_decisions (run_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS internal.agent_domain_decisions;
            DROP TABLE IF EXISTS internal.agent_tool_calls;
            DROP TABLE IF EXISTS internal.agent_runs;
            DROP TABLE IF EXISTS internal.agent_messages;
            DROP TABLE IF EXISTS internal.agent_threads;
            """);
    }
}
