using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Document entity for storing service manuals, technical guides, specifications,
/// and custom business documents.
/// </summary>
public class Document
{
    public int Id { get; set; }

    /// <summary>
    /// Document title/name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the document.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Document category for organization.
    /// </summary>
    public DocumentCategory Category { get; set; } = DocumentCategory.General;

    /// <summary>
    /// Comma-separated tags for searching (e.g., "carrier,furnace,installation").
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Searchable keywords extracted from document or manually added.
    /// </summary>
    [MaxLength(2000)]
    public string? SearchKeywords { get; set; }

    // === File Information ===

    /// <summary>
    /// Original filename with extension.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Stored file path (relative to documents folder).
    /// </summary>
    [MaxLength(500)]
    public string? FilePath { get; set; }

    /// <summary>
    /// File content stored as bytes (for smaller documents or DB storage mode).
    /// </summary>
    public byte[]? FileContent { get; set; }

    /// <summary>
    /// MIME type (e.g., application/pdf, image/png).
    /// </summary>
    [MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// File extension (e.g., .pdf, .docx).
    /// </summary>
    [MaxLength(20)]
    public string? FileExtension { get; set; }

    // === Organization ===

    /// <summary>
    /// Optional folder/subfolder path for organization.
    /// </summary>
    [MaxLength(200)]
    public string? FolderPath { get; set; }

    /// <summary>
    /// Sort order within category.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this is a custom document created by the business.
    /// </summary>
    public bool IsCustomDocument { get; set; } = false;

    /// <summary>
    /// Whether this document is a printable template.
    /// </summary>
    public bool IsPrintable { get; set; } = true;

    /// <summary>
    /// Whether this is a favorite/pinned document.
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// Whether the document is active (soft delete).
    /// </summary>
    public bool IsActive { get; set; } = true;

    // === Associations ===

    /// <summary>
    /// Optional: Link to specific manufacturer.
    /// </summary>
    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Optional: Link to specific model/product.
    /// </summary>
    [MaxLength(100)]
    public string? ModelNumber { get; set; }

    /// <summary>
    /// Optional: Equipment type this document relates to.
    /// </summary>
    [MaxLength(100)]
    public string? EquipmentType { get; set; }

    /// <summary>
    /// Optional: Associated product ID.
    /// </summary>
    public int? ProductId { get; set; }

    // === Tracking ===

    /// <summary>
    /// Number of times document has been viewed/downloaded.
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// Last time document was accessed.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    // === Navigation ===

    public Product? Product { get; set; }

    // === Computed Properties ===

    [NotMapped]
    public string FileSizeDisplay
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:N1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):N1} MB";
        }
    }

    [NotMapped]
    public string CategoryDisplay => Category switch
    {
        DocumentCategory.ServiceManual => "Service Manual",
        DocumentCategory.InstallationGuide => "Installation Guide",
        DocumentCategory.TechnicalSpec => "Technical Specification",
        DocumentCategory.WiringDiagram => "Wiring Diagram",
        DocumentCategory.PartsManual => "Parts Manual",
        DocumentCategory.WarrantyInfo => "Warranty Information",
        DocumentCategory.SafetyData => "Safety Data Sheet",
        DocumentCategory.MaintenanceGuide => "Maintenance Guide",
        DocumentCategory.TroubleshootingGuide => "Troubleshooting Guide",
        DocumentCategory.CustomerForm => "Customer Form",
        DocumentCategory.BusinessTemplate => "Business Template",
        DocumentCategory.Checklist => "Checklist",
        DocumentCategory.General => "General",
        _ => Category.ToString()
    };

    [NotMapped]
    public List<string> TagList => string.IsNullOrWhiteSpace(Tags)
        ? new List<string>()
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    [NotMapped]
    public string FileTypeIcon => FileExtension?.ToLower() switch
    {
        ".pdf" => "bi-file-earmark-pdf text-danger",
        ".doc" or ".docx" => "bi-file-earmark-word text-primary",
        ".xls" or ".xlsx" => "bi-file-earmark-excel text-success",
        ".ppt" or ".pptx" => "bi-file-earmark-ppt text-warning",
        ".png" or ".jpg" or ".jpeg" or ".gif" => "bi-file-earmark-image text-info",
        ".txt" => "bi-file-earmark-text",
        ".zip" or ".rar" => "bi-file-earmark-zip text-secondary",
        _ => "bi-file-earmark"
    };

    /// <summary>
    /// Checks if the search query matches this document.
    /// </summary>
    public bool MatchesSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;

        var q = query.ToLower();
        return Name.ToLower().Contains(q) ||
               (Description?.ToLower().Contains(q) ?? false) ||
               (Tags?.ToLower().Contains(q) ?? false) ||
               (SearchKeywords?.ToLower().Contains(q) ?? false) ||
               (Manufacturer?.ToLower().Contains(q) ?? false) ||
               (ModelNumber?.ToLower().Contains(q) ?? false) ||
               (EquipmentType?.ToLower().Contains(q) ?? false);
    }
}
