using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Services;

/// <summary>
/// Seeds sample HVAC data for demonstration and testing.
/// </summary>
public class DataSeederService
{
    private readonly OneManVanDbContext _context;

    public DataSeederService(OneManVanDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Seeds the database with sample HVAC data.
    /// </summary>
    public async Task SeedSampleDataAsync()
    {
        // Check if data already exists
        if (_context.Customers.Any())
            return;

        // Create customers
        var customers = CreateSampleCustomers();
        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();

        // Create sites for customers
        var sites = CreateSampleSites(customers);
        _context.Sites.AddRange(sites);
        await _context.SaveChangesAsync();

        // Create assets
        var assets = CreateSampleAssets(customers, sites);
        _context.Assets.AddRange(assets);
        await _context.SaveChangesAsync();

        // Create inventory
        var inventory = CreateSampleInventory();
        _context.InventoryItems.AddRange(inventory);
        await _context.SaveChangesAsync();

        // Create estimates
        var estimates = CreateSampleEstimates(customers, assets);
        _context.Estimates.AddRange(estimates);
        await _context.SaveChangesAsync();

        // Create jobs
        var jobs = CreateSampleJobs(customers, sites, assets, estimates);
        _context.Jobs.AddRange(jobs);
        await _context.SaveChangesAsync();

        // Create invoices
        var invoices = CreateSampleInvoices(customers, jobs);
        _context.Invoices.AddRange(invoices);
        await _context.SaveChangesAsync();
    }

    private static List<Customer> CreateSampleCustomers()
    {
        return
        [
            new Customer
            {
                Name = "John Smith",
                Email = "john.smith@email.com",
                Phone = "555-123-4567",
                Notes = "Prefers morning appointments. Has two dogs.",
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new Customer
            {
                Name = "Sarah Johnson",
                Email = "sarah.j@email.com",
                Phone = "555-234-5678",
                Notes = "Rental property owner. Quick response time important.",
                CreatedAt = DateTime.UtcNow.AddMonths(-4)
            },
            new Customer
            {
                Name = "Mike Williams",
                Email = "mike.w@email.com",
                Phone = "555-345-6789",
                Notes = "Business owner. Multiple commercial units.",
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new Customer
            {
                Name = "Emily Davis",
                Email = "emily.davis@email.com",
                Phone = "555-456-7890",
                Notes = "New homeowner. First-time HVAC maintenance.",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            },
            new Customer
            {
                Name = "Robert Brown",
                Email = "rbrown@email.com",
                Phone = "555-567-8901",
                Notes = "Elderly. Prefers phone calls over email.",
                CreatedAt = DateTime.UtcNow.AddDays(-14)
            }
        ];
    }

    private static List<Site> CreateSampleSites(List<Customer> customers)
    {
        return
        [
            // John Smith - 2 sites
            new Site
            {
                CustomerId = customers[0].Id,
                Address = "123 Main Street",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701",
                AccessNotes = "Primary residence. Gate code: 1234"
            },
            new Site
            {
                CustomerId = customers[0].Id,
                Address = "456 Oak Avenue",
                City = "Springfield",
                State = "IL",
                ZipCode = "62702",
                AccessNotes = "Vacation cabin. Key under mat."
            },
            // Sarah Johnson - 3 rental properties
            new Site
            {
                CustomerId = customers[1].Id,
                Address = "789 Elm Street",
                City = "Springfield",
                State = "IL",
                ZipCode = "62703",
                AccessNotes = "Rental property #1. Contact tenant before arriving."
            },
            new Site
            {
                CustomerId = customers[1].Id,
                Address = "101 Pine Road",
                City = "Springfield",
                State = "IL",
                ZipCode = "62704",
                AccessNotes = "Rental property #2. Attic access in hallway."
            },
            new Site
            {
                CustomerId = customers[1].Id,
                Address = "202 Cedar Lane",
                City = "Springfield",
                State = "IL",
                ZipCode = "62705",
                AccessNotes = "Rental property #3. Newer construction."
            },
            // Mike Williams
            new Site
            {
                CustomerId = customers[2].Id,
                Address = "303 Business Parkway",
                City = "Springfield",
                State = "IL",
                ZipCode = "62706",
                AccessNotes = "Main office building. Check in at front desk."
            },
            // Emily Davis
            new Site
            {
                CustomerId = customers[3].Id,
                Address = "404 Maple Drive",
                City = "Springfield",
                State = "IL",
                ZipCode = "62707",
                AccessNotes = "New construction home. Warranty active."
            },
            // Robert Brown
            new Site
            {
                CustomerId = customers[4].Id,
                Address = "505 Birch Street",
                City = "Springfield",
                State = "IL",
                ZipCode = "62708",
                AccessNotes = "Single-story ranch. Side entrance for equipment."
            }
        ];
    }

    private static List<Asset> CreateSampleAssets(List<Customer> customers, List<Site> sites)
    {
        return
        [
            // John Smith's primary residence
            new Asset
            {
                CustomerId = customers[0].Id,
                SiteId = sites[0].Id,
                Serial = "AC2019-001",
                Brand = "Carrier",
                Model = "24ACC636A003",
                FuelType = FuelType.Electric,
                UnitConfig = UnitConfig.Split,
                BtuRating = 36000,
                SeerRating = 16,
                InstallDate = DateTime.UtcNow.AddYears(-5),
                WarrantyStartDate = DateTime.UtcNow.AddYears(-5),
                WarrantyTermYears = 10,
                Notes = "Outdoor condenser unit. R-410A refrigerant."
            },
            new Asset
            {
                CustomerId = customers[0].Id,
                SiteId = sites[0].Id,
                Serial = "FN2019-002",
                Brand = "Carrier",
                Model = "59TP6A080E17--14",
                FuelType = FuelType.NaturalGas,
                UnitConfig = UnitConfig.Furnace,
                BtuRating = 80000,
                InstallDate = DateTime.UtcNow.AddYears(-5),
                WarrantyStartDate = DateTime.UtcNow.AddYears(-5),
                WarrantyTermYears = 10,
                Notes = "High-efficiency furnace. Filter size 16x25x1."
            },
            // Sarah Johnson's rentals
            new Asset
            {
                CustomerId = customers[1].Id,
                SiteId = sites[2].Id,
                Serial = "HP2021-003",
                Brand = "Trane",
                Model = "XR15",
                FuelType = FuelType.Electric,
                UnitConfig = UnitConfig.HeatPump,
                BtuRating = 24000,
                SeerRating = 15,
                InstallDate = DateTime.UtcNow.AddYears(-3),
                WarrantyStartDate = DateTime.UtcNow.AddYears(-3),
                WarrantyTermYears = 10,
                Notes = "Heat pump system. Dual-fuel backup available."
            },
            new Asset
            {
                CustomerId = customers[1].Id,
                SiteId = sites[3].Id,
                Serial = "MS2022-004",
                Brand = "Mitsubishi",
                Model = "MSZ-GL12NA",
                FuelType = FuelType.Electric,
                UnitConfig = UnitConfig.MiniSplit,
                BtuRating = 12000,
                SeerRating = 23,
                InstallDate = DateTime.UtcNow.AddYears(-2),
                WarrantyStartDate = DateTime.UtcNow.AddYears(-2),
                WarrantyTermYears = 12,
                Notes = "Ductless mini-split. Excellent efficiency."
            },
            // Mike Williams
            new Asset
            {
                CustomerId = customers[2].Id,
                SiteId = sites[5].Id,
                Serial = "RTU2020-005",
                Brand = "Lennox",
                Model = "LRP14AC048P",
                FuelType = FuelType.Electric,
                UnitConfig = UnitConfig.Packaged,
                BtuRating = 48000,
                SeerRating = 14,
                InstallDate = DateTime.UtcNow.AddYears(-4),
                WarrantyStartDate = DateTime.UtcNow.AddYears(-4),
                WarrantyTermYears = 5,
                Notes = "Rooftop unit. Access via ladder on north side."
            },
            // Emily Davis - new home
            new Asset
            {
                CustomerId = customers[3].Id,
                SiteId = sites[6].Id,
                Serial = "NEW2024-006",
                Brand = "Carrier",
                Model = "DERA036XXX",
                FuelType = FuelType.Electric,
                UnitConfig = UnitConfig.HeatPump,
                BtuRating = 36000,
                SeerRating = 20,
                InstallDate = DateTime.UtcNow.AddMonths(-6),
                WarrantyStartDate = DateTime.UtcNow.AddMonths(-6),
                WarrantyTermYears = 10,
                Notes = "New high-efficiency heat pump. Builder warranty."
            },
            // Robert Brown - older unit
            new Asset
            {
                CustomerId = customers[4].Id,
                SiteId = sites[7].Id,
                Serial = "OLD2010-007",
                Brand = "Rheem",
                Model = "RGDA-075A",
                FuelType = FuelType.NaturalGas,
                UnitConfig = UnitConfig.Furnace,
                BtuRating = 75000,
                InstallDate = DateTime.UtcNow.AddYears(-14),
                WarrantyStartDate = DateTime.UtcNow.AddYears(-14),
                WarrantyTermYears = 10,
                Notes = "Older unit. Warranty expired. Consider replacement."
            }
        ];
    }

    private static List<InventoryItem> CreateSampleInventory()
    {
        return
        [
            new InventoryItem
            {
                Name = "16x25x1 Pleated Filter",
                Description = "Standard pleated air filter, MERV 8",
                Sku = "FILT-16251",
                Category = InventoryCategory.Filters,
                QuantityOnHand = 50,
                Cost = 5.00m,
                Price = 15.00m,
                ReorderPoint = 10,
                FilterSize = "16x25x1"
            },
            new InventoryItem
            {
                Name = "20x20x1 Pleated Filter",
                Description = "Standard pleated air filter, MERV 8",
                Sku = "FILT-20201",
                Category = InventoryCategory.Filters,
                QuantityOnHand = 35,
                Cost = 5.50m,
                Price = 16.00m,
                ReorderPoint = 10,
                FilterSize = "20x20x1"
            },
            new InventoryItem
            {
                Name = "R-410A Refrigerant (25 lb)",
                Description = "R-410A refrigerant cylinder",
                Sku = "REF-410A-25",
                Category = InventoryCategory.Refrigerants,
                QuantityOnHand = 8,
                Cost = 125.00m,
                Price = 200.00m,
                ReorderPoint = 3,
                RefrigerantType = "R-410A"
            },
            new InventoryItem
            {
                Name = "Capacitor 35/5 MFD",
                Description = "Dual run capacitor for AC units",
                Sku = "CAP-355",
                Category = InventoryCategory.Capacitors,
                QuantityOnHand = 15,
                Cost = 12.00m,
                Price = 45.00m,
                ReorderPoint = 5
            },
            new InventoryItem
            {
                Name = "Contactor 30A",
                Description = "Single pole contactor for AC systems",
                Sku = "CONT-30A",
                Category = InventoryCategory.Contactors,
                QuantityOnHand = 10,
                Cost = 18.00m,
                Price = 55.00m,
                ReorderPoint = 3
            },
            new InventoryItem
            {
                Name = "Blower Motor 1/2 HP",
                Description = "Direct drive blower motor",
                Sku = "MOT-BLW-05",
                Category = InventoryCategory.Motors,
                QuantityOnHand = 4,
                Cost = 85.00m,
                Price = 175.00m,
                ReorderPoint = 2,
                BtuMinCompatibility = 40000,
                BtuMaxCompatibility = 80000
            },
            new InventoryItem
            {
                Name = "Flame Sensor",
                Description = "Universal flame sensor rod",
                Sku = "SENS-FLAME",
                Category = InventoryCategory.Electrical,
                QuantityOnHand = 12,
                Cost = 8.00m,
                Price = 35.00m,
                ReorderPoint = 5
            },
            new InventoryItem
            {
                Name = "Thermostat - Honeywell T6",
                Description = "Programmable thermostat with WiFi",
                Sku = "TSTAT-T6",
                Category = InventoryCategory.Thermostats,
                QuantityOnHand = 6,
                Cost = 75.00m,
                Price = 150.00m,
                ReorderPoint = 2
            }
        ];
    }

    private static List<Estimate> CreateSampleEstimates(List<Customer> customers, List<Asset> assets)
    {
        var estimates = new List<Estimate>
        {
            new Estimate
            {
                CustomerId = customers[0].Id,
                AssetId = assets[0].Id,
                Title = "AC Tune-Up and Filter Replacement",
                Description = "Annual maintenance service for split system",
                TaxRate = 7.0m,
                Status = EstimateStatus.Accepted,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            },
            new Estimate
            {
                CustomerId = customers[1].Id,
                AssetId = assets[2].Id,
                Title = "Heat Pump Repair - Capacitor Replacement",
                Description = "Replace failed run capacitor on outdoor unit",
                TaxRate = 7.0m,
                Status = EstimateStatus.Sent,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                ExpiresAt = DateTime.UtcNow.AddDays(23)
            },
            new Estimate
            {
                CustomerId = customers[4].Id,
                Title = "Furnace Replacement Quote",
                Description = "Replace 14-year-old furnace with new high-efficiency model",
                TaxRate = 7.0m,
                Status = EstimateStatus.Draft,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExpiresAt = DateTime.UtcNow.AddDays(28)
            }
        };

        // Add line items
        estimates[0].Lines =
        [
            new EstimateLine { Description = "AC Tune-Up Service", Type = LineItemType.Labor, Quantity = 1, UnitPrice = 150, SortOrder = 0 },
            new EstimateLine { Description = "16x25x1 Pleated Filter", Type = LineItemType.Part, Quantity = 2, UnitPrice = 15, SortOrder = 1 },
            new EstimateLine { Description = "Coil Cleaning Solution", Type = LineItemType.Material, Quantity = 1, UnitPrice = 25, SortOrder = 2 }
        ];
        estimates[0].RecalculateTotals();

        estimates[1].Lines =
        [
            new EstimateLine { Description = "Diagnostic Service Call", Type = LineItemType.Labor, Quantity = 1, UnitPrice = 89, SortOrder = 0 },
            new EstimateLine { Description = "Capacitor 35/5 MFD", Type = LineItemType.Part, Quantity = 1, UnitPrice = 45, SortOrder = 1 },
            new EstimateLine { Description = "Installation Labor", Type = LineItemType.Labor, Quantity = 0.5m, UnitPrice = 95, SortOrder = 2 }
        ];
        estimates[1].RecalculateTotals();

        estimates[2].Lines =
        [
            new EstimateLine { Description = "High-Efficiency Furnace (96% AFUE)", Type = LineItemType.Equipment, Quantity = 1, UnitPrice = 2500, SortOrder = 0 },
            new EstimateLine { Description = "Installation Labor", Type = LineItemType.Labor, Quantity = 8, UnitPrice = 95, SortOrder = 1 },
            new EstimateLine { Description = "Permit Fee", Type = LineItemType.Fee, Quantity = 1, UnitPrice = 150, SortOrder = 2 },
            new EstimateLine { Description = "Haul Away Old Unit", Type = LineItemType.Service, Quantity = 1, UnitPrice = 75, SortOrder = 3 }
        ];
        estimates[2].RecalculateTotals();

        return estimates;
    }

    private static List<Job> CreateSampleJobs(List<Customer> customers, List<Site> sites, List<Asset> assets, List<Estimate> estimates)
    {
        return
        [
            new Job
            {
                CustomerId = customers[0].Id,
                SiteId = sites[0].Id,
                AssetId = assets[0].Id,
                EstimateId = estimates[0].Id,
                Title = "AC Tune-Up - Smith Residence",
                Description = "Annual AC maintenance and filter replacement",
                Status = JobStatus.Completed,
                ScheduledDate = DateTime.UtcNow.AddDays(-14),
                StartedAt = DateTime.UtcNow.AddDays(-14),
                CompletedAt = DateTime.UtcNow.AddDays(-14),
                EstimatedHours = 1.5m,
                LaborTotal = estimates[0].SubTotal * 0.7m,
                PartsTotal = estimates[0].SubTotal * 0.3m,
                TaxAmount = estimates[0].TaxAmount,
                Total = estimates[0].Total,
                Notes = "Completed successfully. System running well."
            },
            new Job
            {
                CustomerId = customers[1].Id,
                SiteId = sites[2].Id,
                AssetId = assets[2].Id,
                Title = "Heat Pump Repair",
                Description = "Customer reports AC not cooling properly",
                Status = JobStatus.Scheduled,
                ScheduledDate = DateTime.UtcNow.AddDays(3),
                EstimatedHours = 2m,
                Notes = "Bring capacitor and multimeter."
            },
            new Job
            {
                CustomerId = customers[2].Id,
                SiteId = sites[5].Id,
                AssetId = assets[4].Id,
                Title = "Commercial RTU Maintenance",
                Description = "Quarterly maintenance on rooftop unit",
                Status = JobStatus.InProgress,
                ScheduledDate = DateTime.Today,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                EstimatedHours = 3m,
                Notes = "Currently on site."
            }
        ];
    }

    private static List<Invoice> CreateSampleInvoices(List<Customer> customers, List<Job> jobs)
    {
        return
        [
            new Invoice
            {
                CustomerId = customers[0].Id,
                JobId = jobs[0].Id,
                InvoiceNumber = Invoice.GenerateInvoiceNumber(0),
                LaborAmount = jobs[0].LaborTotal,
                PartsAmount = jobs[0].PartsTotal,
                SubTotal = jobs[0].LaborTotal + jobs[0].PartsTotal,
                TaxRate = 7.0m,
                TaxAmount = jobs[0].TaxAmount,
                Total = jobs[0].Total,
                AmountPaid = jobs[0].Total,
                Status = InvoiceStatus.Paid,
                DueDate = DateTime.UtcNow.AddDays(-7),
                PaidAt = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow.AddDays(-14)
            }
        ];
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        _context.Payments.RemoveRange(_context.Payments);
        _context.Invoices.RemoveRange(_context.Invoices);
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        _context.Jobs.RemoveRange(_context.Jobs);
        _context.EstimateLines.RemoveRange(_context.EstimateLines);
        _context.Estimates.RemoveRange(_context.Estimates);
        _context.InventoryLogs.RemoveRange(_context.InventoryLogs);
        _context.InventoryItems.RemoveRange(_context.InventoryItems);
        _context.CustomFields.RemoveRange(_context.CustomFields);
        _context.Assets.RemoveRange(_context.Assets);
        _context.Sites.RemoveRange(_context.Sites);
        _context.Customers.RemoveRange(_context.Customers);
        
        await _context.SaveChangesAsync();
    }
}
