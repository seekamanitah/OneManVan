using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Data;

namespace OneManVan.Services;

/// <summary>
/// Service for searching across all entities in the application.
/// </summary>
public class GlobalSearchService
{
    private readonly OneManVanDbContext _context;

    public GlobalSearchService(OneManVanDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Searches all entities for the given query string.
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="maxResults">Maximum results per category</param>
    /// <returns>Search results grouped by category</returns>
    public async Task<GlobalSearchResults> SearchAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new GlobalSearchResults();

        var searchLower = query.ToLower().Trim();
        var results = new GlobalSearchResults();

        // Search customers
        results.Customers = await _context.Customers
            .Where(c => c.Name.ToLower().Contains(searchLower) ||
                       (c.Email != null && c.Email.ToLower().Contains(searchLower)) ||
                       (c.Phone != null && c.Phone.Contains(searchLower)) ||
                       (c.Notes != null && c.Notes.ToLower().Contains(searchLower)))
            .Take(maxResults)
            .Select(c => new SearchResult
            {
                Id = c.Id,
                Title = c.Name,
                Subtitle = c.Email ?? c.Phone ?? "",
                Category = "Customers",
                Icon = "",
                EntityType = nameof(Customer)
            })
            .AsNoTracking()
            .ToListAsync();

        // Search assets
        results.Assets = await _context.Assets
            .Include(a => a.Customer)
            .Where(a => a.Serial.ToLower().Contains(searchLower) ||
                       (a.Brand != null && a.Brand.ToLower().Contains(searchLower)) ||
                       (a.Model != null && a.Model.ToLower().Contains(searchLower)) ||
                       (a.Notes != null && a.Notes.ToLower().Contains(searchLower)))
            .Take(maxResults)
            .Select(a => new SearchResult
            {
                Id = a.Id,
                Title = $"{a.Brand} {a.Model}".Trim(),
                Subtitle = $"S/N: {a.Serial} | {a.Customer.Name}",
                Category = "Assets",
                Icon = "",
                EntityType = nameof(Asset)
            })
            .AsNoTracking()
            .ToListAsync();

        // Search estimates
        results.Estimates = await _context.Estimates
            .Include(e => e.Customer)
            .Where(e => e.Title.ToLower().Contains(searchLower) ||
                       (e.Description != null && e.Description.ToLower().Contains(searchLower)) ||
                       e.Customer.Name.ToLower().Contains(searchLower))
            .Take(maxResults)
            .Select(e => new SearchResult
            {
                Id = e.Id,
                Title = e.Title,
                Subtitle = $"${e.Total:N2} | {e.Customer.Name}",
                Category = "Estimates",
                Icon = "",
                EntityType = nameof(Estimate)
            })
            .AsNoTracking()
            .ToListAsync();

        // Search jobs
        results.Jobs = await _context.Jobs
            .Include(j => j.Customer)
            .Where(j => j.Title.ToLower().Contains(searchLower) ||
                       (j.Description != null && j.Description.ToLower().Contains(searchLower)) ||
                       (j.Notes != null && j.Notes.ToLower().Contains(searchLower)) ||
                       j.Customer.Name.ToLower().Contains(searchLower))
            .Take(maxResults)
            .Select(j => new SearchResult
            {
                Id = j.Id,
                Title = j.Title,
                Subtitle = $"{j.StatusDisplay} | {j.Customer.Name}",
                Category = "Jobs",
                Icon = "",
                EntityType = nameof(Job)
            })
            .AsNoTracking()
            .ToListAsync();

        // Search invoices
        results.Invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.InvoiceNumber.ToLower().Contains(searchLower) ||
                       (i.Notes != null && i.Notes.ToLower().Contains(searchLower)) ||
                       i.Customer.Name.ToLower().Contains(searchLower))
            .Take(maxResults)
            .Select(i => new SearchResult
            {
                Id = i.Id,
                Title = i.InvoiceNumber,
                Subtitle = $"${i.Total:N2} | {i.Customer.Name}",
                Category = "Invoices",
                Icon = "",
                EntityType = nameof(Invoice)
            })
            .AsNoTracking()
            .ToListAsync();

        // Search inventory
        results.Inventory = await _context.InventoryItems
            .Where(i => i.Name.ToLower().Contains(searchLower) ||
                       (i.Description != null && i.Description.ToLower().Contains(searchLower)) ||
                       (i.Sku != null && i.Sku.ToLower().Contains(searchLower)) ||
                       (i.PartNumber != null && i.PartNumber.ToLower().Contains(searchLower)))
            .Take(maxResults)
            .Select(i => new SearchResult
            {
                Id = i.Id,
                Title = i.Name,
                Subtitle = $"${i.Price:N2} | {i.QuantityOnHand} in stock",
                Category = "Inventory",
                Icon = "",
                EntityType = nameof(InventoryItem)
            })
            .AsNoTracking()
            .ToListAsync();

        // Search sites
        results.Sites = await _context.Sites
            .Include(s => s.Customer)
            .Where(s => s.Address.ToLower().Contains(searchLower) ||
                       (s.City != null && s.City.ToLower().Contains(searchLower)) ||
                       (s.ZipCode != null && s.ZipCode.Contains(searchLower)) ||
                       s.Customer.Name.ToLower().Contains(searchLower))
            .Take(maxResults)
            .Select(s => new SearchResult
            {
                Id = s.Id,
                Title = s.Address,
                Subtitle = $"{s.City}, {s.State} | {s.Customer.Name}",
                Category = "Sites",
                Icon = "",
                EntityType = nameof(Site)
            })
            .AsNoTracking()
            .ToListAsync();

        return results;
    }

    /// <summary>
    /// Quick search for autocomplete suggestions.
    /// </summary>
    public async Task<List<SearchResult>> QuickSearchAsync(string query, int maxResults = 10)
    {
        var results = await SearchAsync(query, 3);
        return results.AllResults.Take(maxResults).ToList();
    }
}

/// <summary>
/// Container for global search results grouped by category.
/// </summary>
public class GlobalSearchResults
{
    public List<SearchResult> Customers { get; set; } = [];
    public List<SearchResult> Assets { get; set; } = [];
    public List<SearchResult> Estimates { get; set; } = [];
    public List<SearchResult> Jobs { get; set; } = [];
    public List<SearchResult> Invoices { get; set; } = [];
    public List<SearchResult> Inventory { get; set; } = [];
    public List<SearchResult> Sites { get; set; } = [];

    /// <summary>
    /// Gets all results flattened into a single list.
    /// </summary>
    public IEnumerable<SearchResult> AllResults => 
        Customers.Concat(Assets)
                 .Concat(Estimates)
                 .Concat(Jobs)
                 .Concat(Invoices)
                 .Concat(Inventory)
                 .Concat(Sites);

    /// <summary>
    /// Total count of all results.
    /// </summary>
    public int TotalCount => 
        Customers.Count + Assets.Count + Estimates.Count + 
        Jobs.Count + Invoices.Count + Inventory.Count + Sites.Count;

    /// <summary>
    /// Whether there are any results.
    /// </summary>
    public bool HasResults => TotalCount > 0;
}

/// <summary>
/// Individual search result item.
/// </summary>
public class SearchResult
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = "";
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Display text combining icon and title.
    /// </summary>
    public string DisplayText => $"{Icon} {Title}";
}
