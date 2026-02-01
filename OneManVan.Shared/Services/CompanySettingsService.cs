using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing company settings and seeding/resetting data.
/// </summary>
public class CompanySettingsService
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;

    public CompanySettingsService(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Company Settings

    /// <summary>
    /// Gets the company settings (creates default if not exists).
    /// </summary>
    public async Task<CompanySettings> GetCompanySettingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var settings = await context.CompanySettings.FirstOrDefaultAsync();
        
        if (settings == null)
        {
            settings = new CompanySettings
            {
                CompanyName = "My Company",
                Email = "contact@mycompany.com"
            };
            context.CompanySettings.Add(settings);
            await context.SaveChangesAsync();
        }
        
        return settings;
    }

    /// <summary>
    /// Updates company settings.
    /// </summary>
    public async Task UpdateCompanySettingsAsync(CompanySettings settings)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var existing = await context.CompanySettings.FirstOrDefaultAsync();
        
        if (existing == null)
        {
            settings.Id = 1;
            settings.CreatedAt = DateTime.UtcNow;
            settings.UpdatedAt = DateTime.UtcNow;
            context.CompanySettings.Add(settings);
        }
        else
        {
            existing.CompanyName = settings.CompanyName;
            existing.Tagline = settings.Tagline;
            existing.Address = settings.Address;
            existing.City = settings.City;
            existing.State = settings.State;
            existing.ZipCode = settings.ZipCode;
            existing.Email = settings.Email;
            existing.Phone = settings.Phone;
            existing.Website = settings.Website;
            existing.LogoBase64 = settings.LogoBase64;
            existing.LogoFileName = settings.LogoFileName;
            existing.TaxId = settings.TaxId;
            existing.LicenseNumber = settings.LicenseNumber;
            existing.InsuranceNumber = settings.InsuranceNumber;
            existing.PaymentTerms = settings.PaymentTerms;
            existing.DocumentFooter = settings.DocumentFooter;
            existing.BankDetails = settings.BankDetails;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        
        await context.SaveChangesAsync();
    }

    #endregion

    #region Seed Data

    /// <summary>
    /// Seeds demo/test data to all tables.
    /// </summary>
    public async Task<SeedResult> SeedAllDataAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new SeedResult();
        
        try
        {
            // Seed Customers
            if (!await context.Customers.AnyAsync())
            {
                var customers = GenerateSampleCustomers();
                context.Customers.AddRange(customers);
                result.CustomersCreated = customers.Count;
            }
            
            await context.SaveChangesAsync();
            
            // Seed Sites (requires customers)
            var customerIds = await context.Customers.Select(c => c.Id).ToListAsync();
            if (!await context.Sites.AnyAsync() && customerIds.Any())
            {
                var sites = GenerateSampleSites(customerIds);
                context.Sites.AddRange(sites);
                result.SitesCreated = sites.Count;
            }
            
            await context.SaveChangesAsync();
            
            // Seed Products
            if (!await context.Products.AnyAsync())
            {
                var products = GenerateSampleProducts();
                context.Products.AddRange(products);
                result.ProductsCreated = products.Count;
            }
            
            await context.SaveChangesAsync();
            
            // Seed Inventory Items
            if (!await context.InventoryItems.AnyAsync())
            {
                var inventory = GenerateSampleInventory();
                context.InventoryItems.AddRange(inventory);
                result.InventoryCreated = inventory.Count;
            }
            
            await context.SaveChangesAsync();
            
            // Seed Assets (requires customers)
            if (!await context.Assets.AnyAsync() && customerIds.Any())
            {
                var assets = GenerateSampleAssets(customerIds);
                context.Assets.AddRange(assets);
                result.AssetsCreated = assets.Count;
            }
            
            await context.SaveChangesAsync();
            
            // Seed Jobs (requires customers)
            if (!await context.Jobs.AnyAsync() && customerIds.Any())
            {
                var jobs = GenerateSampleJobs(customerIds);
                context.Jobs.AddRange(jobs);
                result.JobsCreated = jobs.Count;
            }
            
            await context.SaveChangesAsync();
            
            // Seed Invoices (requires customers)
            if (!await context.Invoices.AnyAsync() && customerIds.Any())
            {
                var invoices = GenerateSampleInvoices(customerIds);
                context.Invoices.AddRange(invoices);
                result.InvoicesCreated = invoices.Count;
            }
            
            await context.SaveChangesAsync();
            
            // Seed Estimates (requires customers)
            if (!await context.Estimates.AnyAsync() && customerIds.Any())
            {
                var estimates = GenerateSampleEstimates(customerIds);
                context.Estimates.AddRange(estimates);
                result.EstimatesCreated = estimates.Count;
            }
            
            await context.SaveChangesAsync();
            
            // Seed Service Agreements (requires customers)
            if (!await context.ServiceAgreements.AnyAsync() && customerIds.Any())
            {
                var agreements = GenerateSampleServiceAgreements(customerIds);
                context.ServiceAgreements.AddRange(agreements);
                result.Message += $" {agreements.Count} service agreements created.";
            }
            
            await context.SaveChangesAsync();
            
            // Seed Warranty Claims (requires assets)
            var assetIds = await context.Assets.Select(a => a.Id).ToListAsync();
            if (!await context.WarrantyClaims.AnyAsync() && assetIds.Any())
            {
                var claims = GenerateSampleWarrantyClaims(assetIds);
                context.WarrantyClaims.AddRange(claims);
                result.Message += $" {claims.Count} warranty claims created.";
            }
            
            await context.SaveChangesAsync();
            
            // Seed QuickNotes
            if (!await context.QuickNotes.AnyAsync())
            {
                var notes = GenerateSampleQuickNotes();
                context.QuickNotes.AddRange(notes);
                result.Message += $" {notes.Count} quick notes created.";
            }
            
            await context.SaveChangesAsync();
            
            result.Success = true;
            result.Message = "Sample data seeded successfully!";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error seeding data: {ex.Message}";
        }
        
        return result;
    }

    /// <summary>
    /// Resets all data in the database (except company settings).
    /// </summary>
    public async Task<bool> ResetAllDataAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Delete in order to avoid FK constraints
        context.InvoiceLineItems.RemoveRange(context.InvoiceLineItems);
        context.EstimateLines.RemoveRange(context.EstimateLines);
        context.JobParts.RemoveRange(context.JobParts);
        context.TimeEntries.RemoveRange(context.TimeEntries);
        context.Payments.RemoveRange(context.Payments);
        context.WarrantyClaims.RemoveRange(context.WarrantyClaims);
        context.QuickNotes.RemoveRange(context.QuickNotes);
        context.ServiceHistory.RemoveRange(context.ServiceHistory);
        context.CommunicationLogs.RemoveRange(context.CommunicationLogs);
        context.CustomerNotes.RemoveRange(context.CustomerNotes);
        context.CustomerDocuments.RemoveRange(context.CustomerDocuments);
        context.JobPhotos.RemoveRange(context.JobPhotos);
        context.AssetPhotos.RemoveRange(context.AssetPhotos);
        context.SitePhotos.RemoveRange(context.SitePhotos);
        context.ProductDocuments.RemoveRange(context.ProductDocuments);
        context.InventoryLogs.RemoveRange(context.InventoryLogs);
        
        await context.SaveChangesAsync();
        
        context.Invoices.RemoveRange(context.Invoices);
        context.Estimates.RemoveRange(context.Estimates);
        context.Jobs.RemoveRange(context.Jobs);
        context.ServiceAgreements.RemoveRange(context.ServiceAgreements);
        context.Assets.RemoveRange(context.Assets);
        context.Sites.RemoveRange(context.Sites);
        context.Products.RemoveRange(context.Products);
        context.InventoryItems.RemoveRange(context.InventoryItems);
        context.ManufacturerRegistrations.RemoveRange(context.ManufacturerRegistrations);
        context.Companies.RemoveRange(context.Companies);
        context.Customers.RemoveRange(context.Customers);
        
        await context.SaveChangesAsync();
        
        return true;
    }

    #endregion

    #region Sample Data Generators

    private List<Customer> GenerateSampleCustomers()
    {
        return new List<Customer>
        {
            new() { Name = "John Smith", Email = "john.smith@email.com", Phone = "(555) 123-4567", HomeAddress = "123 Main St, Anytown, ST 12345", Status = CustomerStatus.Active },
            new() { Name = "Jane Doe", Email = "jane.doe@email.com", Phone = "(555) 234-5678", HomeAddress = "456 Oak Ave, Springfield, ST 23456", Status = CustomerStatus.Active },
            new() { Name = "Robert Johnson", Email = "robert.j@email.com", Phone = "(555) 345-6789", HomeAddress = "789 Pine Rd, Metropolis, ST 34567", Status = CustomerStatus.Active },
            new() { Name = "Emily Davis", Email = "emily.d@email.com", Phone = "(555) 456-7890", HomeAddress = "321 Elm St, Gotham, ST 45678", Status = CustomerStatus.Active },
            new() { Name = "Michael Wilson", Email = "mike.w@email.com", Phone = "(555) 567-8901", HomeAddress = "654 Maple Dr, Star City, ST 56789", Status = CustomerStatus.Active },
            new() { Name = "Sarah Brown", Email = "sarah.b@email.com", Phone = "(555) 678-9012", HomeAddress = "987 Cedar Ln, Central City, ST 67890", Status = CustomerStatus.Active },
            new() { Name = "David Miller", Email = "david.m@email.com", Phone = "(555) 789-0123", HomeAddress = "159 Birch Way, Coast City, ST 78901", Status = CustomerStatus.Active },
            new() { Name = "Lisa Anderson", Email = "lisa.a@email.com", Phone = "(555) 890-1234", HomeAddress = "753 Walnut Blvd, Keystone, ST 89012", Status = CustomerStatus.Active },
        };
    }

    private List<Site> GenerateSampleSites(List<int> customerIds)
    {
        var sites = new List<Site>();
        
        // Sample addresses with GPS coordinates (realistic US locations)
        var sampleLocations = new[]
        {
            ("123 Main Street", "Springfield", "IL", "62701", 39.7817m, -89.6501m),
            ("456 Oak Avenue", "Columbus", "OH", "43215", 39.9612m, -82.9988m),
            ("789 Pine Road", "Austin", "TX", "78701", 30.2672m, -97.7431m),
            ("321 Maple Drive", "Denver", "CO", "80202", 39.7392m, -104.9903m),
            ("654 Cedar Lane", "Phoenix", "AZ", "85001", 33.4484m, -112.0740m),
            ("987 Birch Way", "Atlanta", "GA", "30303", 33.7490m, -84.3880m),
            ("159 Elm Street", "Seattle", "WA", "98101", 47.6062m, -122.3321m),
            ("753 Walnut Boulevard", "Miami", "FL", "33101", 25.7617m, -80.1918m),
        };
        
        for (int i = 0; i < Math.Min(customerIds.Count, sampleLocations.Length); i++)
        {
            var loc = sampleLocations[i];
            sites.Add(new Site
            {
                CustomerId = customerIds[i],
                SiteName = i == 0 ? "Primary Residence" : $"Location #{i + 1}",
                Address = loc.Item1,
                City = loc.Item2,
                State = loc.Item3,
                ZipCode = loc.Item4,
                Latitude = loc.Item5,
                Longitude = loc.Item6,
                IsPrimary = true,
                Notes = "Primary service location"
            });
        }
        
        return sites;
    }

    private List<Product> GenerateSampleProducts()
    {
        return new List<Product>
        {
            new() { ProductName = "Thermostat - Basic", Description = "Non-programmable thermostat", Manufacturer = "Honeywell", ModelNumber = "T4", Category = ProductCategory.Thermostat },
            new() { ProductName = "Thermostat - Smart", Description = "WiFi enabled smart thermostat", Manufacturer = "Nest", ModelNumber = "Learning", Category = ProductCategory.Thermostat },
            new() { ProductName = "Air Filter Pack", Description = "Pack of 5 standard filters", Manufacturer = "Generic", ModelNumber = "FLT-16201", Category = ProductCategory.Accessories },
            new() { ProductName = "AC Unit 2 Ton", Description = "2 Ton Air Conditioner", Manufacturer = "Carrier", ModelNumber = "24ACC336A003", Category = ProductCategory.AirConditioner },
            new() { ProductName = "Heat Pump 3 Ton", Description = "3 Ton Heat Pump", Manufacturer = "Trane", ModelNumber = "XR15", Category = ProductCategory.HeatPump },
            new() { ProductName = "Gas Furnace 80K BTU", Description = "80,000 BTU Gas Furnace", Manufacturer = "Lennox", ModelNumber = "ML180UH", Category = ProductCategory.GasFurnace },
        };
    }

    private List<InventoryItem> GenerateSampleInventory()
    {
        return new List<InventoryItem>
        {
            new() { Name = "Air Filter 16x20x1", Description = "Standard furnace filter", Price = 12.99m, Cost = 6.50m, Sku = "FLT-16201", QuantityOnHand = 25, ReorderPoint = 10 },
            new() { Name = "Air Filter 20x25x1", Description = "Large furnace filter", Price = 15.99m, Cost = 8.00m, Sku = "FLT-20251", QuantityOnHand = 20, ReorderPoint = 10 },
            new() { Name = "Capacitor 35/5 MFD", Description = "Dual run capacitor", Price = 45.00m, Cost = 18.00m, Sku = "CAP-355", QuantityOnHand = 8, ReorderPoint = 5 },
            new() { Name = "Contactor 40A", Description = "AC contactor", Price = 35.00m, Cost = 12.00m, Sku = "CNT-40A", QuantityOnHand = 6, ReorderPoint = 4 },
            new() { Name = "Thermostat Wire 50ft", Description = "18/5 thermostat cable", Price = 28.00m, Cost = 15.00m, Sku = "WRE-185", QuantityOnHand = 12, ReorderPoint = 5 },
            new() { Name = "Refrigerant R-410A (25lb)", Description = "R-410A cylinder", Price = 175.00m, Cost = 95.00m, Sku = "REF-410", QuantityOnHand = 4, ReorderPoint = 2 },
        };
    }

    private List<Asset> GenerateSampleAssets(List<int> customerIds)
    {
        var assets = new List<Asset>();
        var brands = new[] { "Carrier", "Trane", "Lennox", "Rheem", "Goodman", "Bryant" };
        var random = new Random();
        
        foreach (var customerId in customerIds.Take(6))
        {
            assets.Add(new Asset
            {
                CustomerId = customerId,
                AssetName = "HVAC System",
                Brand = brands[random.Next(brands.Length)],
                Model = $"XR{random.Next(10, 20)}-{random.Next(24, 60)}",
                Serial = $"SN{random.Next(100000, 999999)}",
                InstallDate = DateTime.Today.AddYears(-random.Next(1, 10)),
                WarrantyExpiration = DateTime.Today.AddYears(random.Next(1, 5)),
                Status = AssetStatus.Active,
                Notes = "Primary HVAC unit"
            });
        }
        
        return assets;
    }

    private List<Job> GenerateSampleJobs(List<int> customerIds)
    {
        var jobs = new List<Job>();
        var random = new Random(42); // Fixed seed for reproducible data
        var counter = 1;
        
        var jobTemplates = new[]
        {
            ("AC Not Cooling", "Customer reports AC not blowing cold air", JobType.ServiceCall, JobPriority.High),
            ("Annual Maintenance", "Scheduled annual HVAC maintenance", JobType.Maintenance, JobPriority.Normal),
            ("Thermostat Replacement", "Install new smart thermostat", JobType.Installation, JobPriority.Normal),
            ("No Heat", "Furnace not producing heat", JobType.ServiceCall, JobPriority.Emergency),
            ("Filter Replacement", "Replace all air filters", JobType.Maintenance, JobPriority.Low),
            ("Duct Cleaning", "Full duct system cleaning", JobType.ServiceCall, JobPriority.Normal),
            ("New System Install", "Full HVAC system replacement", JobType.Installation, JobPriority.High),
            ("Refrigerant Recharge", "Low refrigerant - needs recharge", JobType.ServiceCall, JobPriority.Normal),
        };
        
        // Create jobs spread across past, today, and future
        for (int i = 0; i < Math.Min(customerIds.Count * 2, 12); i++)
        {
            var customerId = customerIds[i % customerIds.Count];
            var template = jobTemplates[i % jobTemplates.Length];
            var daysOffset = i < 4 ? -random.Next(1, 30) : // Past jobs
                            i < 8 ? random.Next(0, 3) :    // Near future
                            random.Next(4, 14);             // Future
            
            var status = daysOffset < 0 ? JobStatus.Completed :
                        daysOffset == 0 ? JobStatus.InProgress :
                        JobStatus.Scheduled;
            
            var scheduledDate = DateTime.Today.AddDays(daysOffset).AddHours(8 + (i % 8));
            
            jobs.Add(new Job
            {
                CustomerId = customerId,
                JobNumber = $"J-{DateTime.Now.Year}-{counter++:D4}",
                Title = template.Item1,
                Description = template.Item2,
                Status = status,
                Priority = template.Item4,
                JobType = template.Item3,
                ScheduledDate = scheduledDate,
                ScheduledEndDate = scheduledDate.AddHours(random.Next(1, 4)),
                EstimatedHours = random.Next(1, 4),
                TaxRate = 7.0m,
                LaborTotal = status == JobStatus.Completed ? random.Next(100, 300) : 0,
                PartsTotal = status == JobStatus.Completed ? random.Next(50, 200) : 0,
                Total = status == JobStatus.Completed ? random.Next(150, 500) : 0,
                CompletedAt = status == JobStatus.Completed ? scheduledDate.AddHours(2) : null
            });
        }
        
        return jobs;
    }

    private List<Invoice> GenerateSampleInvoices(List<int> customerIds)
    {
        var invoices = new List<Invoice>();
        var statuses = new[] { InvoiceStatus.Paid, InvoiceStatus.Sent, InvoiceStatus.Draft };
        var random = new Random();
        var counter = 1;
        
        foreach (var customerId in customerIds.Take(5))
        {
            var subTotal = random.Next(150, 800);
            var taxRate = 7.0m;
            var taxAmount = subTotal * (taxRate / 100);
            var total = subTotal + taxAmount;
            var status = statuses[random.Next(statuses.Length)];
            
            invoices.Add(new Invoice
            {
                CustomerId = customerId,
                InvoiceNumber = $"INV-{counter++:D5}",
                InvoiceDate = DateTime.Today.AddDays(-random.Next(0, 60)),
                DueDate = DateTime.Today.AddDays(random.Next(-30, 30)),
                SubTotal = subTotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total,
                AmountPaid = status == InvoiceStatus.Paid ? total : 0,
                Status = status
            });
        }
        
        return invoices;
    }

    private List<Estimate> GenerateSampleEstimates(List<int> customerIds)
    {
        var estimates = new List<Estimate>();
        var statuses = new[] { EstimateStatus.Accepted, EstimateStatus.Sent, EstimateStatus.Draft };
        var random = new Random();
        var counter = 1;
        
        foreach (var customerId in customerIds.Take(4))
        {
            var subTotal = random.Next(500, 5000);
            var taxRate = 7.0m;
            var taxAmount = subTotal * (taxRate / 100);
            
            estimates.Add(new Estimate
            {
                CustomerId = customerId,
                Title = $"EST-{counter++:D5}",
                Description = "System replacement estimate",
                CreatedAt = DateTime.Now.AddDays(-random.Next(0, 30)),
                ExpiresAt = DateTime.Now.AddDays(30),
                SubTotal = subTotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = subTotal + taxAmount,
                Status = statuses[random.Next(statuses.Length)]
            });
        }
        
        return estimates;
    }

    private List<ServiceAgreement> GenerateSampleServiceAgreements(List<int> customerIds)
    {
        var agreements = new List<ServiceAgreement>();
        var random = new Random(42);
        var counter = 1;
        
        var agreementTypes = new[]
        {
            ("Annual Maintenance Plan", AgreementType.Annual, 2, 299.99m),
            ("Premium Protection Plan", AgreementType.Premium, 4, 599.99m),
            ("Basic Service Agreement", AgreementType.Basic, 1, 149.99m),
        };
        
        foreach (var customerId in customerIds.Take(4))
        {
            var template = agreementTypes[counter % agreementTypes.Length];
            var startDate = DateTime.Today.AddMonths(-random.Next(0, 6));
            
            agreements.Add(new ServiceAgreement
            {
                CustomerId = customerId,
                AgreementNumber = $"SA-{DateTime.Now.Year}-{counter++:D4}",
                Name = template.Item1,
                Type = template.Item2,
                Status = AgreementStatus.Active,
                StartDate = startDate,
                EndDate = startDate.AddYears(1),
                IncludedVisitsPerYear = template.Item3,
                VisitsUsed = random.Next(0, template.Item3),
                AnnualPrice = template.Item4,
                NextMaintenanceDue = DateTime.Today.AddDays(random.Next(7, 60)),
                Description = "Auto-generated service agreement"
            });
        }
        
        return agreements;
    }

    private List<WarrantyClaim> GenerateSampleWarrantyClaims(List<int> assetIds)
    {
        var claims = new List<WarrantyClaim>();
        var random = new Random(42);
        
        var claimReasons = new[]
        {
            ("Compressor failure", "Replaced under manufacturer warranty"),
            ("Control board malfunction", "Board replaced, warranty approved"),
            ("Fan motor seized", "Motor replaced under parts warranty"),
        };
        
        for (int i = 0; i < Math.Min(assetIds.Count, 3); i++)
        {
            var reason = claimReasons[i];
            var claimDate = DateTime.Today.AddDays(-random.Next(10, 60));
            
            claims.Add(new WarrantyClaim
            {
                AssetId = assetIds[i],
                ClaimNumber = $"CLM-{DateTime.Now.Year}-{i + 1:D4}",
                ClaimDate = claimDate,
                IssueDescription = reason.Item1,
                Resolution = reason.Item2,
                Status = i == 0 ? ClaimStatus.Completed : (i == 1 ? ClaimStatus.Approved : ClaimStatus.Pending),
                IsCoveredByWarranty = true,
                RepairCost = random.Next(200, 800),
                CustomerCharge = 0,
                ResolvedDate = i < 2 ? claimDate.AddDays(random.Next(3, 14)) : null
            });
        }
        
        return claims;
    }

    private List<QuickNote> GenerateSampleQuickNotes()
    {
        return new List<QuickNote>
        {
            new()
            {
                Title = "Order filters for next week",
                Content = "Need to order 20x25x1 filters from supplier. Running low.",
                IsPinned = true,
                Color = "#FFF3CD"
            },
            new()
            {
                Title = "Follow up with Johnson residence",
                Content = "Call back about the quote for new AC unit installation.",
                IsPinned = true,
                Color = "#D1E7DD"
            },
            new()
            {
                Title = "Training reminder",
                Content = "New refrigerant handling certification due next month.",
                IsPinned = false,
                Color = "#CFE2FF"
            },
            new()
            {
                Title = "Van maintenance",
                Content = "Schedule oil change for service van before end of month.",
                IsPinned = false,
                Color = "#F8D7DA"
            },
        };
    }

    #endregion
}

/// <summary>
/// Result of seeding operation.
/// </summary>
public class SeedResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int CustomersCreated { get; set; }
    public int SitesCreated { get; set; }
    public int ProductsCreated { get; set; }
    public int InventoryCreated { get; set; }
    public int AssetsCreated { get; set; }
    public int JobsCreated { get; set; }
    public int InvoicesCreated { get; set; }
    public int EstimatesCreated { get; set; }
    
    public int TotalCreated => CustomersCreated + SitesCreated + ProductsCreated + 
        InventoryCreated + AssetsCreated + JobsCreated + InvoicesCreated + EstimatesCreated;
}
