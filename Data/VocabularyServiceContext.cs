using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Data;

public partial class VocabularyServiceContext : DbContext
{
    public VocabularyServiceContext()
    {
    }

    public VocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuthorProfile> AuthorProfiles { get; set; }

    public virtual DbSet<Card> Cards { get; set; }

    public virtual DbSet<Note> Notes { get; set; }

    public virtual DbSet<ImportJob> ImportJobs { get; set; }

    public virtual DbSet<NoteType> NoteTypes { get; set; }

    public virtual DbSet<NoteField> NoteFields { get; set; }

    public virtual DbSet<CardTemplate> CardTemplates { get; set; }

    public virtual DbSet<Contribution> Contributions { get; set; }

    public virtual DbSet<Deck> Decks { get; set; }

    public virtual DbSet<DeckSubscription> DeckSubscriptions { get; set; }

    public virtual DbSet<DeckVersion> DeckVersions { get; set; }

    public virtual DbSet<DeletedObject> DeletedObjects { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductReview> ProductReviews { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectLemma> ProjectLemmas { get; set; }

    public virtual DbSet<ProjectTerm> ProjectTerms { get; set; }

    public virtual DbSet<UserTermStatus> UserTermStatuses { get; set; }

    public virtual DbSet<ReviewLog> ReviewLogs { get; set; }

    public virtual DbSet<StudySession> StudySessions { get; set; }

    public virtual DbSet<UserCardProgress> UserCardProgresses { get; set; }

    public virtual DbSet<UserEntitlement> UserEntitlements { get; set; }

    public virtual DbSet<UserSetting> UserSettings { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<UserLessonProgress> UserLessonProgresses { get; set; }

    public virtual DbSet<UserCefrProgress> UserCefrProgresses { get; set; }

    public virtual DbSet<SkillAssessmentLog> SkillAssessmentLogs { get; set; }

    public virtual DbSet<SkillType> SkillTypes { get; set; }

    public virtual DbSet<UserSkillActivity> UserSkillActivities { get; set; }

    public virtual DbSet<UserSkillProgress> UserSkillProgresses { get; set; }

    public virtual DbSet<UserBookProgress> UserBookProgresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<ImportJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("import_jobs_pkey");
            entity.ToTable("import_jobs", "internal");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.DeckId).HasColumnName("deck_id");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValueSql("'QUEUED'::character varying").HasColumnName("status");
            entity.Property(e => e.TotalRows).HasColumnName("total_rows");
            entity.Property(e => e.ProcessedRows).HasColumnName("processed_rows");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        });

        modelBuilder.Entity<AuthorProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("author_profiles_pkey");

            entity.ToTable("author_profiles", "internal");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.Badges)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("badges")
                .HasConversion(GetJsonConverter<List<string>>());

            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.SocialLinks)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("social_links")
                .HasConversion(GetJsonConverter<SocialLinks>());

            entity.Property(e => e.StatsCache)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("stats_cache")
                .HasConversion(GetJsonConverter<AuthorStatsCache>());

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cards_pkey");

            entity.ToTable("cards", "internal");

            entity.HasIndex(e => e.DeckId, "idx_cards_deck_id");

            entity.HasIndex(e => e.SearchVector, "idx_cards_search").HasMethod("gin");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.DeckId).HasColumnName("deck_id");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.ProjectTermId).HasColumnName("project_term_id");
            entity.Property(e => e.SearchDocument).HasColumnName("search_document");
            entity.Property(e => e.SearchVector)
                .HasComputedColumnSql("to_tsvector('english'::regconfig, COALESCE(search_document, ''::text))", true)
                .HasColumnName("search_vector");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Deck).WithMany(p => p.Cards)
                .HasForeignKey(d => d.DeckId)
                .HasConstraintName("fk_cards_decks");

            entity.HasOne(d => d.ProjectTerm).WithMany(p => p.Cards)
                .HasForeignKey(d => d.ProjectTermId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_cards_project_terms");

            entity.Property(e => e.NoteId).HasColumnName("note_id");
            entity.Property(e => e.CardTemplateId).HasColumnName("card_template_id");

            entity.HasIndex(e => e.NoteId);

            entity.HasOne(d => d.Note).WithMany(p => p.Cards)
                .HasForeignKey(d => d.NoteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_cards_notes");

            entity.HasOne(d => d.CardTemplate).WithMany()
                .HasForeignKey(d => d.CardTemplateId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_cards_card_templates");
        });

        modelBuilder.Entity<NoteType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("note_types_pkey");

            entity.ToTable("note_types", "internal");

            entity.HasIndex(e => new { e.ProjectId, e.Name }, "ux_note_types_project_name").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Version).HasColumnName("version").HasDefaultValue(1);
            entity.Property(e => e.Css).HasColumnName("css");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Project).WithMany(p => p.NoteTypes)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_note_types_projects");
        });

        modelBuilder.Entity<NoteField>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("note_fields_pkey");

            entity.ToTable("note_fields", "internal");

            entity.HasIndex(e => new { e.NoteTypeId, e.FieldKey }, "ux_note_fields_type_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.NoteTypeId).HasColumnName("note_type_id");
            entity.Property(e => e.FieldKey).HasColumnName("field_key");
            entity.Property(e => e.Label).HasColumnName("label");
            entity.Property(e => e.FieldType).HasColumnName("field_type");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.Required).HasColumnName("required");
            entity.Property(e => e.Archived).HasColumnName("archived");
            entity.Property(e => e.ConfigJson)
                .HasColumnType("jsonb")
                .HasColumnName("config_json");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.NoteType).WithMany(p => p.NoteFields)
                .HasForeignKey(d => d.NoteTypeId)
                .HasConstraintName("fk_note_fields_note_types");
        });

        modelBuilder.Entity<CardTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("card_templates_pkey");

            entity.ToTable("card_templates", "internal");

            entity.HasIndex(e => new { e.NoteTypeId, e.TemplateKey }, "ux_card_templates_type_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.NoteTypeId).HasColumnName("note_type_id");
            entity.Property(e => e.TemplateKey).HasColumnName("template_key");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.FrontTemplate).HasColumnName("front_template");
            entity.Property(e => e.BackTemplate).HasColumnName("back_template");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.NoteType).WithMany(p => p.CardTemplates)
                .HasForeignKey(d => d.NoteTypeId)
                .HasConstraintName("fk_card_templates_note_types");
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notes_pkey");

            entity.ToTable("notes", "internal");

            entity.HasIndex(e => e.DeckId, "idx_notes_deck_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.DeckId).HasColumnName("deck_id");
            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.NoteTypeId).HasColumnName("note_type_id");
            entity.Property(e => e.FieldValues)
                .HasColumnType("jsonb")
                .HasColumnName("field_values")
                .HasDefaultValueSql("'{}'::jsonb")
                .HasConversion(GetJsonConverter<Dictionary<string, NoteFieldValue>>());
            entity.Property(e => e.ProjectTermId).HasColumnName("project_term_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Deck).WithMany(p => p.Notes)
                .HasForeignKey(d => d.DeckId)
                .HasConstraintName("fk_notes_decks");

            entity.HasOne(d => d.NoteType).WithMany(p => p.Notes)
                .HasForeignKey(d => d.NoteTypeId)
                .HasConstraintName("fk_notes_note_types");

            entity.HasOne(d => d.ProjectTerm).WithMany()
                .HasForeignKey(d => d.ProjectTermId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_notes_project_terms");
        });

        modelBuilder.Entity<Contribution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("contributions_pkey");

            entity.ToTable("contributions", "internal");

            entity.HasIndex(e => e.AuthorId, "idx_contributions_author");

            entity.HasIndex(e => e.TargetDeckId, "idx_contributions_pending").HasFilter("((status)::text = 'PENDING'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload")
                .HasConversion(GetJsonConverter<ContributionPayload>());

            entity.Property(e => e.ResolutionComment).HasColumnName("resolution_comment");
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TargetCardId).HasColumnName("target_card_id");
            entity.Property(e => e.TargetDeckId).HasColumnName("target_deck_id");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.TargetCard).WithMany(p => p.Contributions)
                .HasForeignKey(d => d.TargetCardId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_contributions_cards");

            entity.HasOne(d => d.TargetDeck).WithMany(p => p.Contributions)
                .HasForeignKey(d => d.TargetDeckId)
                .HasConstraintName("fk_contributions_decks");
        });

        modelBuilder.Entity<Deck>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("decks_pkey");

            entity.ToTable("decks", "internal");

            entity.HasIndex(e => e.ParentDeckId, "idx_decks_parent_deck_id");

            entity.HasIndex(e => e.ProjectId, "idx_decks_project_id");

            entity.HasIndex(e => e.IsPublic, "idx_decks_public").HasFilter("(is_public = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CardCount).HasColumnName("card_count");
            entity.Property(e => e.ContributionPolicy)
                .HasMaxLength(20)
                .HasDefaultValueSql("'OPEN'::character varying")
                .HasColumnName("contribution_policy");
            entity.Property(e => e.CoverImageUrl).HasColumnName("cover_image_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ForkedFromId).HasColumnName("forked_from_id");
            entity.Property(e => e.IsPublic).HasColumnName("is_public");
            entity.Property(e => e.LicenseType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PRIVATE'::character varying")
                .HasColumnName("license_type");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.ParentDeckId).HasColumnName("parent_deck_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ParentDeck).WithMany(p => p.InverseParentDeck)
                .HasForeignKey(d => d.ParentDeckId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_decks_parent");

            entity.HasOne(d => d.Project).WithMany(p => p.Decks)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_decks_projects");
        });

        modelBuilder.Entity<DeckSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("deck_subscriptions_pkey");

            entity.ToTable("deck_subscriptions", "internal");

            entity.HasIndex(e => e.UserId, "idx_subs_user");

            entity.HasIndex(e => new { e.UserId, e.DeckId }, "uq_user_deck_sub").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.DeckId).HasColumnName("deck_id");
            entity.Property(e => e.LastAccessedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_accessed_at");
            entity.Property(e => e.LastSyncedVersion)
                .HasDefaultValue(0)
                .HasColumnName("last_synced_version");
            entity.Property(e => e.SubscribedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("subscribed_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Deck).WithMany(p => p.DeckSubscriptions)
                .HasForeignKey(d => d.DeckId)
                .HasConstraintName("fk_subs_decks");
        });

        modelBuilder.Entity<DeckVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("deck_versions_pkey");

            entity.ToTable("deck_versions", "internal");

            entity.HasIndex(e => e.DeckId, "idx_deck_versions_deck_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ChangeDescription).HasColumnName("change_description");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeckId).HasColumnName("deck_id");
            entity.Property(e => e.ModifiedByUserId).HasColumnName("modified_by_user_id");
            entity.Property(e => e.SnapshotRef).HasColumnName("snapshot_ref");
            entity.Property(e => e.VersionNumber).HasColumnName("version_number");

            entity.HasOne(d => d.Deck).WithMany(p => p.DeckVersions)
                .HasForeignKey(d => d.DeckId)
                .HasConstraintName("fk_deck_versions_decks");
        });

        modelBuilder.Entity<DeletedObject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("deleted_objects_pkey");

            entity.ToTable("deleted_objects", "internal");

            entity.HasIndex(e => new { e.UserId, e.DeletedAt }, "idx_deleted_sync");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.DeletedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("deleted_at");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(20)
                .HasColumnName("entity_type");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("products_pkey");

            entity.ToTable("products", "internal");

            entity.HasIndex(e => e.AuthorId, "idx_products_author");

            entity.HasIndex(e => e.Status, "idx_products_status").HasFilter("((status)::text = 'PUBLISHED'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.AverageRating).HasColumnName("average_rating");
            entity.Property(e => e.CoverImageUrl).HasColumnName("cover_image_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'USD'::bpchar")
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.DescriptionHtml).HasColumnName("description_html");
            entity.Property(e => e.LinkedDeckId).HasColumnName("linked_deck_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.ReviewCount).HasColumnName("review_count");
            entity.Property(e => e.SalesCount).HasColumnName("sales_count");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.LinkedDeck).WithMany(p => p.Products)
                .HasForeignKey(d => d.LinkedDeckId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_products_decks");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_reviews_pkey");

            entity.ToTable("product_reviews", "internal");

            entity.HasIndex(e => new { e.ProductId, e.CreatedAt }, "idx_reviews_product").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AuthorReply).HasColumnName("author_reply");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsVerified)
                .HasDefaultValue(true)
                .HasColumnName("is_verified");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_reviews_products");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("projects_pkey");

            entity.ToTable("projects", "internal");

            entity.HasIndex(e => e.UserId, "idx_projects_user_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FsrsSettings)
                .HasDefaultValueSql("'{\"w\": [], \"maximum_interval\": 36500, \"request_retention\": 0.9}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("fsrs_settings")
                .HasConversion(GetJsonConverter<FsrsSettings>());

            entity.Property(e => e.TtsSettings)
                .HasColumnType("jsonb")
                .HasColumnName("tts_settings")
                .HasConversion(GetJsonConverter<TtsSettings>());

            entity.Property(e => e.IsArchived).HasColumnName("is_archived");
            entity.Property(e => e.SourceLang)
                .HasMaxLength(5)
                .HasColumnName("source_lang");
            entity.Property(e => e.Stats)
                .HasDefaultValueSql("'{\"total_lemmas\": 0, \"mature_lemmas\": 0}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("stats")
                .HasConversion(GetJsonConverter<ProjectStats>());

            entity.Property(e => e.TargetLang)
                .HasMaxLength(5)
                .HasColumnName("target_lang");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<ProjectLemma>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_lemmas_pkey");

            entity.ToTable("project_lemmas", "internal");

            entity.HasIndex(e => new { e.ProjectId, e.Text }, "idx_lemmas_text");

            entity.HasIndex(e => new { e.ProjectId, e.Text, e.PosTag }, "uq_project_lemma").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.MainCardId).HasColumnName("main_card_id");
            entity.Property(e => e.PosTag)
                .HasMaxLength(10)
                .HasColumnName("pos_tag");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'NEW'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.MainCard).WithMany(p => p.ProjectLemmas)
                .HasForeignKey(d => d.MainCardId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_lemmas_main_card");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectLemmas)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_lemmas_projects");
        });

        modelBuilder.Entity<ProjectTerm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_terms_pkey");
            entity.ToTable("project_terms", "internal");
            entity.HasIndex(e => new { e.ProjectId, e.NormalizedText, e.Type }, "uq_project_terms_norm_type").IsUnique();
            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.NormalizedText).HasColumnName("normalized_text");
            entity.Property(e => e.Type).HasMaxLength(16).HasColumnName("type");
            entity.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTerms)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_project_terms_projects");
        });

        modelBuilder.Entity<UserTermStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_term_statuses_pkey");
            entity.ToTable("user_term_statuses", "internal");
            entity.HasIndex(e => new { e.UserId, e.ProjectTermId }, "uq_user_term_status").IsUnique();
            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectTermId).HasColumnName("project_term_id");
            entity.Property(e => e.Status).HasMaxLength(16).HasColumnName("status");
            entity.Property(e => e.Meaning).HasColumnName("meaning");
            entity.Property(e => e.FirstSentence).HasColumnName("first_sentence");
            entity.Property(e => e.FirstSourceTitle).HasColumnName("first_source_title");
            entity.Property(e => e.FirstSourceUrl).HasColumnName("first_source_url");
            entity.Property(e => e.LastSeenAt).HasColumnName("last_seen_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.HasOne(d => d.ProjectTerm).WithMany(p => p.UserTermStatuses)
                .HasForeignKey(d => d.ProjectTermId)
                .HasConstraintName("fk_user_term_statuses_terms");
            entity.HasOne(d => d.Project).WithMany(p => p.UserTermStatuses)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_user_term_statuses_projects");
        });

        modelBuilder.Entity<ReviewLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("review_logs_pkey");

            entity.ToTable("review_logs", "internal");

            entity.HasIndex(e => new { e.SessionId, e.CreatedAt }, "idx_logs_session_created").IsDescending(false, true);

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_logs_user_date");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DueAfter).HasColumnName("due_after");
            entity.Property(e => e.DueBefore).HasColumnName("due_before");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReviewDurationMs).HasColumnName("review_duration_ms");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.StateAfter).HasColumnName("state_after");
            entity.Property(e => e.StateBefore).HasColumnName("state_before");
            entity.Property(e => e.StepBefore).HasColumnName("step_before");
            entity.Property(e => e.StepAfter).HasColumnName("step_after");
            entity.Property(e => e.RepsBefore).HasColumnName("reps_before");
            entity.Property(e => e.RepsAfter).HasColumnName("reps_after");
            entity.Property(e => e.LapsesBefore).HasColumnName("lapses_before");
            entity.Property(e => e.LapsesAfter).HasColumnName("lapses_after");
            entity.Property(e => e.ElapsedDaysBefore).HasColumnName("elapsed_days_before");
            entity.Property(e => e.ElapsedDaysAfter).HasColumnName("elapsed_days_after");
            entity.Property(e => e.ScheduledDaysBefore).HasColumnName("scheduled_days_before");
            entity.Property(e => e.ScheduledDaysAfter).HasColumnName("scheduled_days_after");
            entity.Property(e => e.LastReviewBefore).HasColumnName("last_review_before");
            entity.Property(e => e.LastReviewAfter).HasColumnName("last_review_after");
            entity.Property(e => e.StabilityBefore).HasColumnName("stability_before");
            entity.Property(e => e.StabilityAfter).HasColumnName("stability_after");
            entity.Property(e => e.DifficultyBefore).HasColumnName("difficulty_before");
            entity.Property(e => e.DifficultyAfter).HasColumnName("difficulty_after");
            entity.Property(e => e.UserAnswer).HasColumnName("user_answer");
            entity.Property(e => e.AnswerValidationResult)
                .HasColumnType("jsonb")
                .HasColumnName("answer_validation_result");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Card).WithMany(p => p.ReviewLogs)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("fk_logs_cards");
        });

        modelBuilder.Entity<StudySession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("study_sessions_pkey");

            entity.ToTable("study_sessions", "internal");

            entity.HasIndex(e => new { e.UserId, e.ProjectId, e.EndTime }, "idx_sessions_heatmap");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CardsReviewed).HasColumnName("cards_reviewed");
            entity.Property(e => e.DeckId).HasColumnName("deck_id");
            entity.Property(e => e.DurationSec).HasColumnName("duration_sec");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.NewLearned).HasColumnName("new_learned");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Project).WithMany(p => p.StudySessions)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_sessions_projects");
        });

        modelBuilder.Entity<UserCardProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_card_progress_pkey");

            entity.ToTable("user_card_progress", "internal");

            entity.HasIndex(e => e.CardId, "idx_progress_card_id");

            entity.HasIndex(e => new { e.UserId, e.ProjectId, e.State, e.Due }, "idx_progress_queue_gen").HasFilter("(is_suspended = false)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.Difficulty).HasColumnName("difficulty");
            entity.Property(e => e.Due).HasColumnName("due");
            entity.Property(e => e.ElapsedDays).HasColumnName("elapsed_days");
            entity.Property(e => e.IsSuspended).HasColumnName("is_suspended");
            entity.Property(e => e.Lapses).HasColumnName("lapses");
            entity.Property(e => e.LastReview)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_review");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Reps).HasColumnName("reps");
            entity.Property(e => e.ScheduledDays).HasColumnName("scheduled_days");
            entity.Property(e => e.Stability).HasColumnName("stability");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Step).HasColumnName("step").HasDefaultValue(0);
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Card).WithMany(p => p.UserCardProgresses)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("fk_progress_cards");

            entity.HasOne(d => d.Project).WithMany(p => p.UserCardProgresses)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_progress_projects");
        });

        modelBuilder.Entity<UserBookProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_book_progress_pkey");

            entity.ToTable("user_book_progresses", "internal");

            entity.HasIndex(e => new { e.UserId, e.BookId }, "uq_user_book_progress").IsUnique();
            entity.HasIndex(e => e.ProjectId, "idx_user_book_progress_project");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.ProgressPercent).HasColumnName("progress_percent");
            entity.Property(e => e.LastPositionLocator).HasColumnName("last_position_locator");
            entity.Property(e => e.LastChapter).HasColumnName("last_chapter");
            entity.Property(e => e.IsFinished).HasColumnName("is_finished");
            entity.Property(e => e.LastReadAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_read_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Project).WithMany(p => p.UserBookProgresses)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_user_book_progress_projects");
        });

        modelBuilder.Entity<UserEntitlement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_entitlements_pkey");

            entity.ToTable("user_entitlements", "internal");

            entity.HasIndex(e => new { e.UserId, e.DeckId }, "idx_entitlements_check").HasFilter("(is_active = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.DeckId).HasColumnName("deck_id");
            entity.Property(e => e.ExternalOrderId).HasColumnName("external_order_id");
            entity.Property(e => e.GrantedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("granted_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Source)
                .HasMaxLength(20)
                .HasColumnName("source");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Deck).WithMany(p => p.UserEntitlements)
                .HasForeignKey(d => d.DeckId)
                .HasConstraintName("fk_entitlements_decks");

            entity.HasOne(d => d.Product).WithMany(p => p.UserEntitlements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_entitlements_products");
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_settings_pkey");

            entity.ToTable("user_settings", "internal");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CurrentStreak).HasColumnName("current_streak");
            entity.Property(e => e.DailyGoalNew)
                .HasDefaultValue(20)
                .HasColumnName("daily_goal_new");
            entity.Property(e => e.DailyGoalReview)
                .HasDefaultValue(100)
                .HasColumnName("daily_goal_review");
            entity.Property(e => e.InterfaceLanguage)
                .HasMaxLength(5)
                .HasDefaultValueSql("'en'::character varying")
                .HasColumnName("interface_language");
            entity.Property(e => e.LastStudyDate).HasColumnName("last_study_date");
            entity.Property(e => e.MaxStreak).HasColumnName("max_streak");
            entity.Property(e => e.RolloverHour)
                .HasDefaultValue(4)
                .HasColumnName("rollover_hour");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Lessons");
            entity.ToTable("Lessons");

            entity.HasIndex(e => new { e.CefrLevel, e.OrderIndex }, "IX_Lessons_CefrLevel_OrderIndex");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.Category).HasColumnName("Category");
            entity.Property(e => e.Difficulty).HasColumnName("Difficulty");
            entity.Property(e => e.SystemPrompt).HasColumnName("SystemPrompt");
            entity.Property(e => e.ContentMarkdown).HasColumnName("ContentMarkdown");
            entity.Property(e => e.ColorCssClass).HasColumnName("ColorCssClass");
            entity.Property(e => e.CefrLevel).HasColumnName("CefrLevel").HasDefaultValue("B1");
            entity.Property(e => e.OrderIndex).HasColumnName("OrderIndex").HasDefaultValue(0);
            entity.Property(e => e.UnlocksAfterLessonId).HasColumnName("UnlocksAfterLessonId");
            entity.Property(e => e.TargetSkills).HasColumnName("TargetSkills").HasDefaultValue("R,W");
            entity.Property(e => e.EstimatedMinutes).HasColumnName("EstimatedMinutes").HasDefaultValue(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("UpdatedAt");
        });

        modelBuilder.Entity<UserLessonProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_UserLessonProgresses");
            entity.ToTable("UserLessonProgresses");

            entity.HasIndex(e => e.LessonId, "IX_UserLessonProgresses_LessonId");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.LessonId).HasColumnName("LessonId");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.AgentThreadId).HasColumnName("AgentThreadId");
            entity.Property(e => e.ScorePercent).HasColumnName("ScorePercent").HasDefaultValue(0);
            entity.Property(e => e.TimeSpentSeconds).HasColumnName("TimeSpentSeconds").HasDefaultValue(0);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("now()").HasColumnName("StartedAt");
            entity.Property(e => e.CompletedAt).HasColumnName("CompletedAt");

            entity.HasOne(d => d.Lesson).WithMany(p => p.UserProgresses)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("FK_UserLessonProgresses_Lessons_LessonId");
        });

        modelBuilder.Entity<UserCefrProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_UserCefrProgresses");
            entity.ToTable("UserCefrProgresses");

            entity.HasIndex(e => new { e.UserId, e.CefrLevel }, "IX_UserCefrProgresses_UserId_CefrLevel").IsUnique();

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.CefrLevel).HasColumnName("CefrLevel").HasMaxLength(8);
            entity.Property(e => e.CompletedLessons).HasColumnName("CompletedLessons").HasDefaultValue(0);
            entity.Property(e => e.TotalLessons).HasColumnName("TotalLessons").HasDefaultValue(0);
            entity.Property(e => e.IsLevelCompleted).HasColumnName("IsLevelCompleted").HasDefaultValue(false);
            entity.Property(e => e.LevelCompletedAt).HasColumnName("LevelCompletedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<SkillAssessmentLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SkillAssessmentLogs");
            entity.ToTable("SkillAssessmentLogs", "internal");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectId");
            entity.Property(e => e.Skill).HasColumnName("Skill").HasMaxLength(50);
            entity.Property(e => e.Score).HasColumnName("Score");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");

            entity.HasIndex(e => new { e.UserId, e.ProjectId, e.Skill }, "IX_SkillAssessmentLogs_UserId_ProjectId_Skill");
        });

        modelBuilder.Entity<SkillType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SkillTypes");
            entity.ToTable("SkillTypes", "internal");

            entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("Code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("DisplayName").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Unit).HasColumnName("Unit").HasMaxLength(50).IsRequired();
            entity.Property(e => e.CompletionThreshold).HasColumnName("CompletionThreshold");

            entity.HasIndex(e => e.Code, "IX_SkillTypes_Code").IsUnique();

            // Seed data — 4 base skills
            entity.HasData(
                new SkillType { Id = 1, Code = "reading",   DisplayName = "Reading",   Unit = "minutes",   CompletionThreshold = 15 },
                new SkillType { Id = 2, Code = "listening", DisplayName = "Listening", Unit = "minutes",   CompletionThreshold = 10 },
                new SkillType { Id = 3, Code = "writing",   DisplayName = "Writing",   Unit = "exercises", CompletionThreshold = 1  },
                new SkillType { Id = 4, Code = "speaking",  DisplayName = "Speaking",  Unit = "exercises", CompletionThreshold = 1  }
            );
        });

        modelBuilder.Entity<UserSkillActivity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_UserSkillActivities");
            entity.ToTable("UserSkillActivities", "internal");

            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectId");
            entity.Property(e => e.Date).HasColumnName("Date");
            entity.Property(e => e.SkillTypeId).HasColumnName("SkillTypeId");
            entity.Property(e => e.Value).HasColumnName("Value").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("now()");

            // Unique constraint for upsert (ON CONFLICT DO UPDATE)
            entity.HasIndex(e => new { e.UserId, e.ProjectId, e.Date, e.SkillTypeId },
                "IX_UserSkillActivities_UserId_ProjectId_Date_SkillTypeId").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.ProjectId, e.Date },
                "IX_UserSkillActivities_UserId_ProjectId_Date");

            entity.HasOne(e => e.SkillType)
                .WithMany(st => st.UserSkillActivities)
                .HasForeignKey(e => e.SkillTypeId)
                .HasConstraintName("FK_UserSkillActivities_SkillTypes");
        });

        modelBuilder.Entity<UserSkillProgress>(entity =>
        {
            // Composite PK — one row per (user, project, skill)
            entity.HasKey(e => new { e.UserId, e.ProjectId, e.SkillTypeId })
                  .HasName("PK_UserSkillProgresses");

            entity.ToTable("UserSkillProgresses", "internal");

            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectId");
            entity.Property(e => e.SkillTypeId).HasColumnName("SkillTypeId");
            entity.Property(e => e.Level).HasColumnName("Level").HasDefaultValue(0);
            entity.Property(e => e.TotalValue).HasColumnName("TotalValue").HasDefaultValue(0);
            entity.Property(e => e.Metadata)
                  .HasColumnName("Metadata")
                  .HasColumnType("jsonb")
                  .HasConversion(
                      v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(v, (JsonSerializerOptions?)null));
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("now()");

            entity.HasOne(e => e.SkillType)
                  .WithMany()
                  .HasForeignKey(e => e.SkillTypeId)
                  .HasConstraintName("FK_UserSkillProgresses_SkillTypes");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .HasConstraintName("FK_UserSkillProgresses_Projects");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    private static ValueConverter<T, string> GetJsonConverter<T>()
    {
        return new ValueConverter<T, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            v => JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions)null)
        );
    }
}


