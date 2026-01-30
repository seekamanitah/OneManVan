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

    // === Computed Properties ===

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
