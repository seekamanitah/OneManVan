using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneManVan.Shared.Models;

/// <summary>
/// Quick notes for random information, reminders, and scratchpad entries.
/// These are standalone notes not linked to any specific entity.
/// Use for: customer info to enter later, material lists, phone numbers, etc.
/// </summary>
public class QuickNote
{
    public int Id { get; set; }

    /// <summary>
    /// Optional title for the note.
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// The actual note content. Can be any text.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional category for organization (e.g., "Customer", "Material", "Reminder").
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Pin important notes to the top of the list.
    /// </summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Archive notes instead of deleting them.
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Optional color for visual organization (hex code like #FFE082).
    /// </summary>
    [MaxLength(20)]
    public string? Color { get; set; }

    /// <summary>
    /// When the note was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the note was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // === Entity Linking (Optional) ===

    /// <summary>
    /// Optional link to a customer for customer-specific notes.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Optional link to a site for site-specific notes.
    /// </summary>
    public int? SiteId { get; set; }

    /// <summary>
    /// Optional link to a job for job-specific notes.
    /// </summary>
    public int? JobId { get; set; }

    /// <summary>
    /// Optional link to an asset for asset-specific notes.
    /// </summary>
    public int? AssetId { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("SiteId")]
    public virtual Site? Site { get; set; }

    [ForeignKey("JobId")]
    public virtual Job? Job { get; set; }

    [ForeignKey("AssetId")]
    public virtual Asset? Asset { get; set; }

    // === Computed Properties ===

    /// <summary>
    /// Whether this note is linked to any entity.
    /// </summary>
    [NotMapped]
    public bool IsLinked => CustomerId.HasValue || SiteId.HasValue || JobId.HasValue || AssetId.HasValue;

    /// <summary>
    /// Display name for what this note is linked to.
    /// </summary>
    [NotMapped]
    public string? LinkedToDisplay
    {
        get
        {
            if (Customer != null) return $"Customer: {Customer.Name}";
            if (Site != null) return $"Site: {Site.SiteName}";
            if (Job != null) return $"Job: {Job.Title}";
            if (Asset != null) return $"Asset: {Asset.AssetName ?? Asset.Serial}";
            if (CustomerId.HasValue) return "Customer";
            if (SiteId.HasValue) return "Site";
            if (JobId.HasValue) return "Job";
            if (AssetId.HasValue) return "Asset";
            return null;
        }
    }

    /// <summary>
    /// Gets a preview of the content (first 100 chars).
    /// </summary>
    [NotMapped]
    public string ContentPreview => Content.Length > 100 
        ? Content.Substring(0, 100) + "..." 
        : Content;

    /// <summary>
    /// Display title - uses Title if set, otherwise first line of content.
    /// </summary>
    [NotMapped]
    public string DisplayTitle => !string.IsNullOrEmpty(Title) 
        ? Title 
        : Content.Split('\n').FirstOrDefault()?.Trim() ?? "Untitled Note";
}
