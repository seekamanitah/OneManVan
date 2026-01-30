using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneManVan.Shared.Models;

/// <summary>
/// Time entry for tracking work hours on a job.
/// </summary>
public class TimeEntry
{
    public int Id { get; set; }

    [Required]
    public int JobId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? GpsLatStart { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? GpsLonStart { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? GpsLatEnd { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? GpsLonEnd { get; set; }

    public TimeEntryType EntryType { get; set; } = TimeEntryType.Labor;

    [Column(TypeName = "decimal(10,2)")]
    public decimal HourlyRate { get; set; } = 85m;

    public bool IsBillable { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Job Job { get; set; } = null!;

    public TimeSpan? Duration => EndTime.HasValue 
        ? EndTime.Value - StartTime 
        : DateTime.UtcNow - StartTime;

    public decimal DurationHours => Duration.HasValue 
        ? (decimal)Duration.Value.TotalHours 
        : 0;

    public decimal Amount => IsBillable ? DurationHours * HourlyRate : 0;

    public bool IsActive => !EndTime.HasValue;

    public string DurationDisplay
    {
        get
        {
            if (!Duration.HasValue) return "0:00";
            var d = Duration.Value;
            return d.TotalHours >= 1 
                ? $"{(int)d.TotalHours}:{d.Minutes:D2}" 
                : $"0:{d.Minutes:D2}";
        }
    }

    public string TypeDisplay => EntryType switch
    {
        TimeEntryType.Labor => "Labor",
        TimeEntryType.Travel => "Travel",
        TimeEntryType.Break => "Break",
        _ => "Other"
    };

    public void ClockOut(decimal? gpsLat = null, decimal? gpsLon = null)
    {
        EndTime = DateTime.UtcNow;
        GpsLatEnd = gpsLat;
        GpsLonEnd = gpsLon;
    }
}

/// <summary>
/// Type of time entry for categorization.
/// </summary>
public enum TimeEntryType
{
    Labor = 0,
    Travel = 1,
    Break = 2
}
