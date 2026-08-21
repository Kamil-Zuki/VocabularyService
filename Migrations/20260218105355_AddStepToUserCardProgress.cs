using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace VocabularyService.Migrations
{
    /// <inheritdoc />
    public partial class AddStepToUserCardProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            UpFull(migrationBuilder);
        }

        private void UpFull(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "internal");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "author_profiles",
                schema: "internal",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    social_links = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    badges = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    stats_cache = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("author_profiles_pkey", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "deleted_objects",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("deleted_objects_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    source_lang = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    target_lang = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    fsrs_settings = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{\"w\": [], \"maximum_interval\": 36500, \"request_retention\": 0.9}'::jsonb"),
                    stats = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{\"total_lemmas\": 0, \"mature_lemmas\": 0}'::jsonb"),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("projects_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                schema: "internal",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rollover_hour = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    max_streak = table.Column<int>(type: "integer", nullable: false),
                    last_study_date = table.Column<DateOnly>(type: "date", nullable: true),
                    daily_goal_new = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    daily_goal_review = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    interface_language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValueSql: "'en'::character varying"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_settings_pkey", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "decks",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_deck_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    cover_image_url = table.Column<string>(type: "text", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    contribution_policy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'OPEN'::character varying"),
                    license_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'PRIVATE'::character varying"),
                    forked_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    card_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("decks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_decks_parent",
                        column: x => x.parent_deck_id,
                        principalSchema: "internal",
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_decks_projects",
                        column: x => x.project_id,
                        principalSchema: "internal",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "study_sessions",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cards_reviewed = table.Column<int>(type: "integer", nullable: false),
                    duration_sec = table.Column<int>(type: "integer", nullable: false),
                    new_learned = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'ACTIVE'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("study_sessions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_projects",
                        column: x => x.project_id,
                        principalSchema: "internal",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deck_subscriptions",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_synced_version = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    subscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_accessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("deck_subscriptions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_subs_decks",
                        column: x => x.deck_id,
                        principalSchema: "internal",
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deck_versions",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    change_description = table.Column<string>(type: "text", nullable: false),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_ref = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("deck_versions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_deck_versions_decks",
                        column: x => x.deck_id,
                        principalSchema: "internal",
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description_html = table.Column<string>(type: "text", nullable: true),
                    cover_image_url = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValueSql: "'USD'::bpchar"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'DRAFT'::character varying"),
                    average_rating = table.Column<float>(type: "real", nullable: false),
                    review_count = table.Column<int>(type: "integer", nullable: false),
                    sales_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("products_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_decks",
                        column: x => x.linked_deck_id,
                        principalSchema: "internal",
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_reviews",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    author_reply = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_reviews_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviews_products",
                        column: x => x.product_id,
                        principalSchema: "internal",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_entitlements",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    external_order_id = table.Column<string>(type: "text", nullable: true),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_entitlements_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_entitlements_decks",
                        column: x => x.deck_id,
                        principalSchema: "internal",
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_entitlements_products",
                        column: x => x.product_id,
                        principalSchema: "internal",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cards",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sentence = table.Column<string>(type: "text", nullable: false),
                    translation = table.Column<string>(type: "text", nullable: false),
                    target_word = table.Column<string>(type: "text", nullable: false),
                    target_index = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    source_meta = table.Column<string>(type: "jsonb", nullable: true),
                    media = table.Column<string>(type: "jsonb", nullable: true),
                    synonyms = table.Column<string>(type: "jsonb", nullable: true),
                    lemma_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('english'::regconfig, sentence)", stored: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("cards_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_cards_decks",
                        column: x => x.deck_id,
                        principalSchema: "internal",
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contributions",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    target_deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'PENDING'::character varying"),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("contributions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_contributions_cards",
                        column: x => x.target_card_id,
                        principalSchema: "internal",
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_contributions_decks",
                        column: x => x.target_deck_id,
                        principalSchema: "internal",
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_lemmas",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    pos_tag = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'NEW'::character varying"),
                    main_card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("project_lemmas_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_lemmas_main_card",
                        column: x => x.main_card_id,
                        principalSchema: "internal",
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lemmas_projects",
                        column: x => x.project_id,
                        principalSchema: "internal",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_logs",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    state_before = table.Column<short>(type: "smallint", nullable: false),
                    state_after = table.Column<short>(type: "smallint", nullable: false),
                    due_before = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_after = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stability_before = table.Column<float>(type: "real", nullable: false),
                    stability_after = table.Column<float>(type: "real", nullable: false),
                    difficulty_before = table.Column<float>(type: "real", nullable: false),
                    difficulty_after = table.Column<float>(type: "real", nullable: false),
                    review_duration_ms = table.Column<int>(type: "integer", nullable: false),
                    user_answer = table.Column<string>(type: "text", nullable: true),
                    answer_validation_result = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("review_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_logs_cards",
                        column: x => x.card_id,
                        principalSchema: "internal",
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_card_progress",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<short>(type: "smallint", nullable: false),
                    step = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    stability = table.Column<float>(type: "real", nullable: false),
                    difficulty = table.Column<float>(type: "real", nullable: false),
                    due = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    elapsed_days = table.Column<int>(type: "integer", nullable: false),
                    scheduled_days = table.Column<int>(type: "integer", nullable: false),
                    reps = table.Column<int>(type: "integer", nullable: false),
                    lapses = table.Column<int>(type: "integer", nullable: false),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    last_review = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_card_progress_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_progress_cards",
                        column: x => x.card_id,
                        principalSchema: "internal",
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_progress_projects",
                        column: x => x.project_id,
                        principalSchema: "internal",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_cards_deck_id",
                schema: "internal",
                table: "cards",
                column: "deck_id");

            migrationBuilder.CreateIndex(
                name: "idx_cards_search",
                schema: "internal",
                table: "cards",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_cards_lemma_id",
                schema: "internal",
                table: "cards",
                column: "lemma_id");

            migrationBuilder.CreateIndex(
                name: "idx_contributions_author",
                schema: "internal",
                table: "contributions",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "idx_contributions_pending",
                schema: "internal",
                table: "contributions",
                column: "target_deck_id",
                filter: "((status)::text = 'PENDING'::text)");

            migrationBuilder.CreateIndex(
                name: "IX_contributions_target_card_id",
                schema: "internal",
                table: "contributions",
                column: "target_card_id");

            migrationBuilder.CreateIndex(
                name: "idx_subs_user",
                schema: "internal",
                table: "deck_subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_deck_subscriptions_deck_id",
                schema: "internal",
                table: "deck_subscriptions",
                column: "deck_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_deck_sub",
                schema: "internal",
                table: "deck_subscriptions",
                columns: new[] { "user_id", "deck_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_deck_versions_deck_id",
                schema: "internal",
                table: "deck_versions",
                column: "deck_id");

            migrationBuilder.CreateIndex(
                name: "idx_decks_parent_deck_id",
                schema: "internal",
                table: "decks",
                column: "parent_deck_id");

            migrationBuilder.CreateIndex(
                name: "idx_decks_project_id",
                schema: "internal",
                table: "decks",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_decks_public",
                schema: "internal",
                table: "decks",
                column: "is_public",
                filter: "(is_public = true)");

            migrationBuilder.CreateIndex(
                name: "idx_deleted_sync",
                schema: "internal",
                table: "deleted_objects",
                columns: new[] { "user_id", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "idx_reviews_product",
                schema: "internal",
                table: "product_reviews",
                columns: new[] { "product_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_products_author",
                schema: "internal",
                table: "products",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "idx_products_status",
                schema: "internal",
                table: "products",
                column: "status",
                filter: "((status)::text = 'PUBLISHED'::text)");

            migrationBuilder.CreateIndex(
                name: "IX_products_linked_deck_id",
                schema: "internal",
                table: "products",
                column: "linked_deck_id");

            migrationBuilder.CreateIndex(
                name: "idx_lemmas_text",
                schema: "internal",
                table: "project_lemmas",
                columns: new[] { "project_id", "text" });

            migrationBuilder.CreateIndex(
                name: "IX_project_lemmas_main_card_id",
                schema: "internal",
                table: "project_lemmas",
                column: "main_card_id");

            migrationBuilder.CreateIndex(
                name: "uq_project_lemma",
                schema: "internal",
                table: "project_lemmas",
                columns: new[] { "project_id", "text", "pos_tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_projects_user_id",
                schema: "internal",
                table: "projects",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_logs_session_created",
                schema: "internal",
                table: "review_logs",
                columns: new[] { "session_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_logs_user_date",
                schema: "internal",
                table: "review_logs",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_review_logs_card_id",
                schema: "internal",
                table: "review_logs",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "idx_sessions_heatmap",
                schema: "internal",
                table: "study_sessions",
                columns: new[] { "user_id", "project_id", "end_time" });

            migrationBuilder.CreateIndex(
                name: "IX_study_sessions_project_id",
                schema: "internal",
                table: "study_sessions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_progress_card_id",
                schema: "internal",
                table: "user_card_progress",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "idx_progress_queue_gen",
                schema: "internal",
                table: "user_card_progress",
                columns: new[] { "user_id", "project_id", "state", "due" },
                filter: "(is_suspended = false)");

            migrationBuilder.CreateIndex(
                name: "IX_user_card_progress_project_id",
                schema: "internal",
                table: "user_card_progress",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_entitlements_check",
                schema: "internal",
                table: "user_entitlements",
                columns: new[] { "user_id", "deck_id" },
                filter: "(is_active = true)");

            migrationBuilder.CreateIndex(
                name: "IX_user_entitlements_deck_id",
                schema: "internal",
                table: "user_entitlements",
                column: "deck_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_entitlements_product_id",
                schema: "internal",
                table: "user_entitlements",
                column: "product_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "step",
                schema: "internal",
                table: "user_card_progress");
        }

        private void DownFull(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cards_decks",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "fk_cards_lemmas",
                schema: "internal",
                table: "cards");

            migrationBuilder.DropTable(
                name: "author_profiles",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "contributions",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "deck_subscriptions",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "deck_versions",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "deleted_objects",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "product_reviews",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "review_logs",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "study_sessions",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "user_card_progress",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "user_entitlements",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "user_settings",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "products",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "decks",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "project_lemmas",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "cards",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "internal");
        }
    }
}
