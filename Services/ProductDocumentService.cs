using System.Diagnostics;
using System.IO;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Services;

/// <summary>
/// Service for managing product document file storage and retrieval.
/// </summary>
public class ProductDocumentService
{
    private readonly string _documentsBasePath;

    public ProductDocumentService()
    {
        // Store documents in AppData/OneManVan/ProductDocuments
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _documentsBasePath = Path.Combine(appDataPath, "OneManVan", "ProductDocuments");
        
        // Ensure directory exists
        Directory.CreateDirectory(_documentsBasePath);
    }

    /// <summary>
    /// Gets the full path to a document file.
    /// </summary>
    public string GetDocumentFullPath(ProductDocument document)
    {
        return Path.Combine(_documentsBasePath, document.FilePath);
    }

    /// <summary>
    /// Gets the full path to a product's document folder.
    /// </summary>
    public string GetProductDocumentFolder(int productId)
    {
        var folderPath = Path.Combine(_documentsBasePath, productId.ToString());
        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    /// <summary>
    /// Uploads a document file and creates a database record.
    /// </summary>
    public async Task<ProductDocument> UploadDocumentAsync(int productId, string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Source file not found.", sourceFilePath);

        var fileInfo = new FileInfo(sourceFilePath);
        var fileName = fileInfo.Name;
        var relativePath = Path.Combine(productId.ToString(), fileName);
        var destinationPath = Path.Combine(_documentsBasePath, relativePath);

        // Ensure product folder exists
        var productFolder = GetProductDocumentFolder(productId);

        // Handle duplicate filenames
        var counter = 1;
        while (File.Exists(destinationPath))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            fileName = $"{nameWithoutExt}_{counter}{extension}";
            relativePath = Path.Combine(productId.ToString(), fileName);
            destinationPath = Path.Combine(_documentsBasePath, relativePath);
            counter++;
        }

        // Copy file to storage
        File.Copy(sourceFilePath, destinationPath);

        // Determine document type from filename
        var documentType = InferDocumentType(fileName);

        // Determine content type
        var contentType = GetContentType(fileInfo.Extension);

        // Create database record
        var document = new ProductDocument
        {
            ProductId = productId,
            FileName = fileName,
            FilePath = relativePath,
            FileSize = fileInfo.Length,
            ContentType = contentType,
            DocumentType = documentType,
            UploadedAt = DateTime.UtcNow
        };

        App.DbContext.ProductDocuments.Add(document);
        await App.DbContext.SaveChangesAsync();

        return document;
    }

    /// <summary>
    /// Opens a document file using the system default application.
    /// </summary>
    public void OpenDocument(ProductDocument document)
    {
        var fullPath = GetDocumentFullPath(document);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Document file not found.", fullPath);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to open document: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a document file from storage.
    /// </summary>
    public void DeleteDocumentFile(ProductDocument document)
    {
        var fullPath = GetDocumentFullPath(document);

        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch
            {
                // Log but don't throw - file might be in use
            }
        }
    }

    /// <summary>
    /// Deletes a document and its database record.
    /// </summary>
    public async Task DeleteDocumentAsync(ProductDocument document)
    {
        DeleteDocumentFile(document);

        var dbDocument = await App.DbContext.ProductDocuments.FindAsync(document.Id);
        if (dbDocument != null)
        {
            App.DbContext.ProductDocuments.Remove(dbDocument);
            await App.DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Infers the document type from the filename.
    /// </summary>
    private static ProductDocumentType InferDocumentType(string fileName)
    {
        var lower = fileName.ToLowerInvariant();

        if (lower.Contains("brochure") || lower.Contains("flyer") || lower.Contains("marketing"))
            return ProductDocumentType.Brochure;

        if (lower.Contains("manual") || lower.Contains("operation") || lower.Contains("owner"))
            return ProductDocumentType.Manual;

        if (lower.Contains("spec") || lower.Contains("specification") || lower.Contains("datasheet"))
            return ProductDocumentType.SpecSheet;

        if (lower.Contains("nomenclature") || lower.Contains("naming") || lower.Contains("model"))
            return ProductDocumentType.Nomenclature;

        if (lower.Contains("install") || lower.Contains("installation") || lower.Contains("setup"))
            return ProductDocumentType.InstallGuide;

        if (lower.Contains("wiring") || lower.Contains("electrical") || lower.Contains("schematic"))
            return ProductDocumentType.WiringDiagram;

        if (lower.Contains("parts") || lower.Contains("breakdown") || lower.Contains("exploded"))
            return ProductDocumentType.PartsBreakdown;

        if (lower.Contains("warranty") || lower.Contains("guarantee"))
            return ProductDocumentType.WarrantyInfo;

        if (lower.Contains("bulletin") || lower.Contains("service") || lower.Contains("alert"))
            return ProductDocumentType.ServiceBulletin;

        if (lower.Contains("troubleshoot") || lower.Contains("diagnostic") || lower.Contains("repair"))
            return ProductDocumentType.TroubleshootingGuide;

        return ProductDocumentType.Other;
    }

    /// <summary>
    /// Gets the MIME content type for a file extension.
    /// </summary>
    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".html" or ".htm" => "text/html",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Gets all documents for a product.
    /// </summary>
    public async Task<List<ProductDocument>> GetProductDocumentsAsync(int productId)
    {
        return await App.DbContext.ProductDocuments
            .Where(d => d.ProductId == productId)
            .OrderBy(d => d.DocumentType)
            .ThenBy(d => d.FileName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets total storage used by product documents.
    /// </summary>
    public async Task<long> GetTotalStorageUsedAsync()
    {
        return await App.DbContext.ProductDocuments.SumAsync(d => d.FileSize);
    }

    /// <summary>
    /// Gets total storage used formatted as a string.
    /// </summary>
    public async Task<string> GetTotalStorageUsedDisplayAsync()
    {
        var bytes = await GetTotalStorageUsedAsync();
        
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:N1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):N1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):N2} GB";
    }
}
