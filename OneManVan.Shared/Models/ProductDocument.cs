using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Document attachment for a product (brochures, manuals, spec sheets, etc.).
/// </summary>
public class ProductDocument
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    // === Document Info ===

    public ProductDocumentType DocumentType { get; set; } = ProductDocumentType.Other;

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Relative path from the documents storage folder.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type (e.g., application/pdf).
    /// </summary>
    [MaxLength(100)]
    public string? ContentType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // === Metadata ===

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // === Navigation Properties ===

    public Product Product { get; set; } = null!;

    // === Computed Properties ===

    [NotMapped]
    public string FileSizeDisplay
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:N1} KB";
            return $"{FileSize / (1024.0 * 1024.0):N1} MB";
        }
    }

    [NotMapped]
    public string DocumentTypeDisplay => DocumentType switch
    {
        ProductDocumentType.Brochure => "Brochure",
        ProductDocumentType.Manual => "Manual",
        ProductDocumentType.SpecSheet => "Spec Sheet",
        ProductDocumentType.Nomenclature => "Nomenclature",
        ProductDocumentType.InstallGuide => "Install Guide",
        ProductDocumentType.WiringDiagram => "Wiring Diagram",
        ProductDocumentType.PartsBreakdown => "Parts Breakdown",
        ProductDocumentType.WarrantyInfo => "Warranty Info",
        ProductDocumentType.ServiceBulletin => "Service Bulletin",
        ProductDocumentType.TroubleshootingGuide => "Troubleshooting",
        ProductDocumentType.Other => "Other",
        _ => "Document"
    };

    [NotMapped]
    public bool IsPdf => ContentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true ||
                         FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
}
