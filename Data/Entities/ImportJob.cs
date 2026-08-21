using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VocabularyService.Data.Entities;

[Table("import_jobs")]
public class ImportJob
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [Column("deck_id")]
    public Guid DeckId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "QUEUED"; // QUEUED, RUNNING, COMPLETED, FAILED

    [Column("total_rows")]
    public int TotalRows { get; set; }

    [Column("processed_rows")]
    public int ProcessedRows { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
