using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing the document library.
/// </summary>
public class DocumentService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public DocumentService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region CRUD Operations

    /// <summary>
    /// Gets all active documents.
    /// </summary>
    public async Task<List<Document>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Documents
            .Where(d => d.IsActive)
            .OrderBy(d => d.Category)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets documents by category.
    /// </summary>
    public async Task<List<Document>> GetByCategoryAsync(DocumentCategory category)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Documents
            .Where(d => d.IsActive && d.Category == category)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets favorite/pinned documents.
    /// </summary>
    public async Task<List<Document>> GetFavoritesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Documents
            .Where(d => d.IsActive && d.IsFavorite)
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets recently accessed documents.
    /// </summary>
    public async Task<List<Document>> GetRecentAsync(int count = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Documents
            .Where(d => d.IsActive && d.LastAccessedAt != null)
            .OrderByDescending(d => d.LastAccessedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Searches documents by query string.
    /// </summary>
    public async Task<List<Document>> SearchAsync(string query, DocumentCategory? category = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var q = query.ToLower();
        
        var queryable = context.Documents
            .Where(d => d.IsActive);

        if (category.HasValue)
        {
            queryable = queryable.Where(d => d.Category == category.Value);
        }

        // Search across multiple fields
        queryable = queryable.Where(d =>
            d.Name.ToLower().Contains(q) ||
            (d.Description != null && d.Description.ToLower().Contains(q)) ||
            (d.Tags != null && d.Tags.ToLower().Contains(q)) ||
            (d.SearchKeywords != null && d.SearchKeywords.ToLower().Contains(q)) ||
            (d.Manufacturer != null && d.Manufacturer.ToLower().Contains(q)) ||
            (d.ModelNumber != null && d.ModelNumber.ToLower().Contains(q)) ||
            (d.EquipmentType != null && d.EquipmentType.ToLower().Contains(q))
        );

        return await queryable
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    public async Task<Document?> GetByIdAsync(int id, bool incrementViewCount = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var document = await context.Documents
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document != null && incrementViewCount)
        {
            document.ViewCount++;
            document.LastAccessedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return document;
    }

    // Allowed file extensions for upload
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
        ".mp4", ".avi", ".mov", ".wmv",
        ".zip", ".rar", ".7z"
    };

    // Maximum file size: 50 MB
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    /// <summary>
    /// Validates a file for upload.
    /// </summary>
    public (bool IsValid, string ErrorMessage) ValidateFile(string fileName, long fileSize, byte[]? content = null)
    {
        // Check file name
        if (string.IsNullOrWhiteSpace(fileName))
            return (false, "File name is required.");
        
        // Sanitize file name - prevent path traversal
        var sanitizedName = Path.GetFileName(fileName);
        if (sanitizedName != fileName || sanitizedName.Contains(".."))
            return (false, "Invalid file name.");
        
        // Check extension
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            return (false, $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
        
        // Check file size
        if (fileSize <= 0)
            return (false, "File is empty.");
        
        if (fileSize > MaxFileSizeBytes)
            return (false, $"File size ({fileSize / (1024 * 1024)}MB) exceeds maximum allowed size ({MaxFileSizeBytes / (1024 * 1024)}MB).");
        
        // Check content for executable signatures (magic bytes)
        if (content != null && content.Length >= 4)
        {
            // Check for executable signatures
            if (IsExecutable(content))
                return (false, "Executable files are not allowed.");
        }
        
        return (true, string.Empty);
    }

    private static bool IsExecutable(byte[] content)
    {
        // Windows executable (MZ header)
        if (content.Length >= 2 && content[0] == 0x4D && content[1] == 0x5A)
            return true;
        
        // ELF executable (Linux)
        if (content.Length >= 4 && content[0] == 0x7F && content[1] == 0x45 && content[2] == 0x4C && content[3] == 0x46)
            return true;
        
        // Shell script
        if (content.Length >= 2 && content[0] == 0x23 && content[1] == 0x21) // #!
            return true;
        
        return false;
    }

    /// <summary>
    /// Creates a new document with validation.
    /// </summary>
    public async Task<Document> CreateAsync(Document document)
    {
        // Validate file
        var validation = ValidateFile(document.FileName, document.FileSizeBytes, document.FileContent);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        document.CreatedAt = DateTime.UtcNow;
        document.FileName = Path.GetFileName(document.FileName); // Sanitize
        document.FileExtension = Path.GetExtension(document.FileName)?.ToLower();
        
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        
        return document;
    }

    /// <summary>
    /// Updates a document.
    /// </summary>
    public async Task<Document> UpdateAsync(Document document)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        document.UpdatedAt = DateTime.UtcNow;
        
        context.Documents.Update(document);
        await context.SaveChangesAsync();
        
        return document;
    }

    /// <summary>
    /// Soft deletes a document.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var document = await context.Documents.FindAsync(id);
        if (document == null) return false;
        
        document.IsActive = false;
        document.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Permanently deletes a document.
    /// </summary>
    public async Task<bool> PermanentDeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var document = await context.Documents.FindAsync(id);
        if (document == null) return false;
        
        context.Documents.Remove(document);
        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Favorites & Organization

    /// <summary>
    /// Toggles favorite status.
    /// </summary>
    public async Task<bool> ToggleFavoriteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var document = await context.Documents.FindAsync(id);
        if (document == null) return false;
        
        document.IsFavorite = !document.IsFavorite;
        document.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return document.IsFavorite;
    }

    /// <summary>
    /// Gets all unique manufacturers from documents.
    /// </summary>
    public async Task<List<string>> GetManufacturersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Documents
            .Where(d => d.IsActive && d.Manufacturer != null)
            .Select(d => d.Manufacturer!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all unique equipment types from documents.
    /// </summary>
    public async Task<List<string>> GetEquipmentTypesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Documents
            .Where(d => d.IsActive && d.EquipmentType != null)
            .Select(d => d.EquipmentType!)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();
    }

    /// <summary>
    /// Gets document count by category.
    /// </summary>
    public async Task<Dictionary<DocumentCategory, int>> GetCategoryCountsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Documents
            .Where(d => d.IsActive)
            .GroupBy(d => d.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);
    }

    #endregion

    #region File Operations

    /// <summary>
    /// Gets document content for download.
    /// </summary>
    public async Task<(byte[]? Content, string FileName, string ContentType)?> GetFileContentAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var document = await context.Documents.FindAsync(id);
        if (document == null) return null;

        // Update access tracking
        document.ViewCount++;
        document.LastAccessedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return (document.FileContent, document.FileName, document.ContentType);
    }

    #endregion
}
