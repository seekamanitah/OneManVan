using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Photo attachment for jobs.
/// </summary>
public class JobPhoto
{
    public int Id { get; set; }

    [Required]
    public int JobId { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    public PhotoType Type { get; set; } = PhotoType.Other;

    [MaxLength(500)]
    public string? Caption { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Longitude { get; set; }

    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;

    [NotMapped]
    public string TypeDisplay => Type switch
    {
        PhotoType.Before => "Before",
        PhotoType.After => "After",
        PhotoType.Issue => "Issue",
        PhotoType.DataPlate => "Data Plate",
        PhotoType.Signature => "Signature",
        PhotoType.Serial => "Serial Number",
        PhotoType.FilterSize => "Filter Size",
        PhotoType.Damage => "Damage",
        PhotoType.Installation => "Installation",
        _ => "Other"
    };
}

/// <summary>
/// Photo attachment for assets.
/// </summary>
public class AssetPhoto
{
    public int Id { get; set; }

    [Required]
    public int AssetId { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    public PhotoType Type { get; set; } = PhotoType.Other;

    [MaxLength(500)]
    public string? Caption { get; set; }

    public bool IsPrimary { get; set; } = false;

    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Asset Asset { get; set; } = null!;
}

/// <summary>
/// Photo attachment for sites.
/// </summary>
public class SitePhoto
{
    public int Id { get; set; }

    [Required]
    public int SiteId { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    [MaxLength(500)]
    public string? Caption { get; set; }

    public bool IsPrimary { get; set; } = false;

    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Site Site { get; set; } = null!;
}
