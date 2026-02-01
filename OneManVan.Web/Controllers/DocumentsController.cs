using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Services;

namespace OneManVan.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;
    private readonly DocumentService _documentService;

    public DocumentsController(
        IDbContextFactory<OneManVanDbContext> contextFactory,
        DocumentService documentService)
    {
        _contextFactory = contextFactory;
        _documentService = documentService;
    }

    /// <summary>
    /// Gets document content for inline viewing (PDF, images).
    /// </summary>
    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetContent(int id)
    {
        var result = await _documentService.GetFileContentAsync(id);
        if (result == null || result.Value.Content == null)
            return NotFound();

        var (content, fileName, contentType) = result.Value;
        
        // Return inline for viewing
        return File(content!, contentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Downloads document as attachment.
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var result = await _documentService.GetFileContentAsync(id);
        if (result == null || result.Value.Content == null)
            return NotFound();

        var (content, fileName, contentType) = result.Value;
        
        // Force download
        return File(content!, contentType, fileName);
    }
}
