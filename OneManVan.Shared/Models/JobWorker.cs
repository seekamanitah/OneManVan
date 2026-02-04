using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneManVan.Shared.Models;

/// <summary>
/// Represents an employee assigned to work on a job.
/// Enables crew management and per-worker time tracking.
/// </summary>
public class JobWorker
{
    public int Id { get; set; }

    /// <summary>
    /// The job this worker is assigned to.
    /// </summary>
    [Required]
    public int JobId { get; set; }

    /// <summary>
    /// The employee assigned to the job.
    /// </summary>
    [Required]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Role on this job (e.g., Lead, Helper, Apprentice).
    /// </summary>
    [MaxLength(50)]
    public string? Role { get; set; }

    /// <summary>
    /// Override hourly rate for this job, if different from employee's default.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? HourlyRateOverride { get; set; }

    /// <summary>
    /// Whether this worker is currently clocked in on the job.
    /// </summary>
    public bool IsClockedIn { get; set; } = false;

    /// <summary>
    /// The current active time entry for this worker (if clocked in).
    /// </summary>
    public int? ActiveTimeEntryId { get; set; }

    /// <summary>
    /// When this worker was assigned to the job.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total hours worked on this job by this worker.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalHoursWorked { get; set; } = 0;

    /// <summary>
    /// Total pay earned on this job by this worker.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPayEarned { get; set; } = 0;

    /// <summary>
    /// Notes about this worker's assignment.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("JobId")]
    public virtual Job Job { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("ActiveTimeEntryId")]
    public virtual TimeEntry? ActiveTimeEntry { get; set; }

    // Computed properties
    [NotMapped]
    public decimal EffectiveHourlyRate => HourlyRateOverride ?? Employee?.PayRate ?? 0;

    [NotMapped]
    public string DisplayName => Employee?.DisplayName ?? "Unknown";

    [NotMapped]
    public string RoleDisplay => Role ?? "Worker";
}
