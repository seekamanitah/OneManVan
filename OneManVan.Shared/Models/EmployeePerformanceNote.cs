using System.ComponentModel.DataAnnotations;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Performance note for tracking employee quality, reliability, and job-specific feedback.
/// Date-stamped entries that build up over time.
/// </summary>
public class EmployeePerformanceNote
{
    public int Id { get; set; }

    /// <summary>
    /// The employee this note is about.
    /// </summary>
    [Required]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Optional job this note relates to.
    /// </summary>
    public int? JobId { get; set; }

    /// <summary>
    /// Date of the observation/note.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Category of the performance note.
    /// </summary>
    public PerformanceCategory Category { get; set; } = PerformanceCategory.General;

    /// <summary>
    /// Optional 1-5 rating.
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// The note content.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Who created this note.
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // === Navigation Properties ===

    public Employee Employee { get; set; } = null!;

    public Job? Job { get; set; }

    // === Computed Properties ===

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string CategoryDisplay => Category switch
    {
        PerformanceCategory.Quality => "Quality",
        PerformanceCategory.Reliability => "Reliability",
        PerformanceCategory.Speed => "Speed",
        PerformanceCategory.Attitude => "Attitude",
        PerformanceCategory.JobPerformance => "Job Performance",
        PerformanceCategory.Issue => "Issue",
        PerformanceCategory.Positive => "Positive",
        PerformanceCategory.General => "General",
        _ => Category.ToString()
    };

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string RatingDisplay => Rating.HasValue ? new string('*', Rating.Value) : "";
}
