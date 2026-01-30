using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Communication log for tracking customer interactions.
/// </summary>
public class CommunicationLog
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    public int? JobId { get; set; }

    public CommunicationType Type { get; set; } = CommunicationType.PhoneCall;

    public CommunicationDirection Direction { get; set; } = CommunicationDirection.Outbound;

    [MaxLength(200)]
    public string? Subject { get; set; }

    [MaxLength(4000)]
    public string? Content { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? RecordedBy { get; set; }

    /// <summary>
    /// Duration in minutes for phone calls.
    /// </summary>
    public int? DurationMinutes { get; set; }

    [MaxLength(200)]
    public string? Outcome { get; set; }

    public bool RequiresFollowUp { get; set; } = false;

    public DateTime? FollowUpDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Customer Customer { get; set; } = null!;
    public Job? Job { get; set; }

    [NotMapped]
    public string TypeDisplay => Type switch
    {
        CommunicationType.PhoneCall => "?? Phone Call",
        CommunicationType.Email => "?? Email",
        CommunicationType.Text => "?? Text Message",
        CommunicationType.InPerson => "?? In Person",
        CommunicationType.Voicemail => "?? Voicemail",
        CommunicationType.Letter => "?? Letter",
        _ => "Other"
    };

    [NotMapped]
    public string DirectionDisplay => Direction == CommunicationDirection.Inbound 
        ? "?? Inbound" 
        : "?? Outbound";
}

/// <summary>
/// Document attachment for customers.
/// </summary>
public class CustomerDocument
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MimeType { get; set; }

    public long? FileSizeBytes { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? UploadedBy { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;

    [NotMapped]
    public string FileSizeDisplay
    {
        get
        {
            if (!FileSizeBytes.HasValue) return "Unknown";
            var size = FileSizeBytes.Value;
            if (size < 1024) return $"{size} B";
            if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
            return $"{size / (1024.0 * 1024.0):F1} MB";
        }
    }
}

/// <summary>
/// Activity log entry for customer timeline.
/// </summary>
public class CustomerNote
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    public int? JobId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    public bool IsPinned { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
    public Job? Job { get; set; }
}
