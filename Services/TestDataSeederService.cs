using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Services;

/// <summary>
/// Service for seeding comprehensive test data for testing purposes.
/// </summary>
public class TestDataSeederService
{
    private readonly OneManVanDbContext _dbContext;
    private readonly Random _random = new();

    public TestDataSeederService(OneManVanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Seeds all test data for comprehensive testing.
    /// </summary>
    public async Task SeedAllTestDataAsync()
    {
        await SeedCustomersAsync(10);
        await SeedAssetsAsync();
        await SeedInventoryAsync(50);
        await SeedEstimatesAsync(10);
        await SeedJobsAsync(15);
        await SeedInvoicesAsync(10);
        await SeedServiceAgreementsAsync(5);
        await SeedProductsAsync(20);
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        // Delete in reverse dependency order
        _dbContext.Payments.RemoveRange(_dbContext.Payments);
        _dbContext.Invoices.RemoveRange(_dbContext.Invoices);
        _dbContext.TimeEntries.RemoveRange(_dbContext.TimeEntries);
        _dbContext.Jobs.RemoveRange(_dbContext.Jobs);
        _dbContext.EstimateLines.RemoveRange(_dbContext.EstimateLines);
        _dbContext.Estimates.RemoveRange(_dbContext.Estimates);
        _dbContext.ServiceAgreements.RemoveRange(_dbContext.ServiceAgreements);
        _dbContext.InventoryLogs.RemoveRange(_dbContext.InventoryLogs);
        _dbContext.InventoryItems.RemoveRange(_dbContext.InventoryItems);
        _dbContext.Assets.RemoveRange(_dbContext.Assets);
        _dbContext.Sites.RemoveRange(_dbContext.Sites);
        _dbContext.Customers.RemoveRange(_dbContext.Customers);
        _dbContext.Products.RemoveRange(_dbContext.Products);
        
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedCustomersAsync(int count)
    {
        var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Emily", "Robert", "Lisa", "James", "Maria" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        var cities = new[] { "Denver", "Aurora", "Lakewood", "Boulder", "Fort Collins", "Colorado Springs", "Arvada", "Westminster", "Centennial", "Thornton" };
        var streets = new[] { "Main St", "Oak Ave", "Maple Dr", "Pine Ln", "Cedar Blvd", "Elm Way", "Spruce Ct", "Birch Rd", "Aspen Pl", "Willow St" };

        var customerCount = await _dbContext.Customers.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var firstName = firstNames[_random.Next(firstNames.Length)];
            var lastName = lastNames[_random.Next(lastNames.Length)];
            var city = cities[_random.Next(cities.Length)];
            var street = streets[_random.Next(streets.Length)];
            var houseNum = _random.Next(100, 9999);

            var customer = new Customer
            {
                CustomerNumber = $"C-{DateTime.Now.Year}-{(customerCount + i + 1):D4}",
                Name = $"{firstName} {lastName}",
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@email.com",
                Phone = $"(303) {_random.Next(200, 999)}-{_random.Next(1000, 9999)}",
                CustomerType = i % 5 == 0 ? CustomerType.Commercial : CustomerType.Residential,
                Status = (CustomerStatus)(i % 3), // Active, Inactive, Lead
                PaymentTerms = (PaymentTerms)(i % 4),
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 365))
            };

            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();

            // Add 1-3 sites per customer
            var siteCount = _random.Next(1, 4);
            for (int j = 0; j < siteCount; j++)
            {
                var site = new Site
                {
                    CustomerId = customer.Id,
                    Address = $"{houseNum + j * 100} {street}",
                    City = city,
                    State = "CO",
                    ZipCode = $"80{_random.Next(100, 999)}",
                    IsPrimary = j == 0,
                    CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 365))
                };

                _dbContext.Sites.Add(site);
            }
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedAssetsAsync()
    {
        var sites = await _dbContext.Sites.Include(s => s.Customer).ToListAsync();
        var brands = new[] { "Carrier", "Trane", "Lennox", "Rheem", "Goodman", "Bryant", "American Standard", "York", "Daikin", "Amana" };
        var models = new[] { "XC21", "XR15", "XV18", "SL18XC", "GSXC18", "24ACC6", "17SEER", "YZF", "DX16SA", "ASX16" };

        var assetCount = await _dbContext.Assets.CountAsync();
        int assetIndex = 0;

        foreach (var site in sites)
        {
            // Add 1-3 assets per site
            var count = _random.Next(1, 4);
            for (int i = 0; i < count; i++)
            {
                var brand = brands[_random.Next(brands.Length)];
                var model = models[_random.Next(models.Length)];
                var installYear = _random.Next(2010, 2024);

                var asset = new Asset
                {
                    AssetTag = $"A-{DateTime.Now.Year}-{(assetCount + assetIndex + 1):D5}",
                    CustomerId = site.CustomerId,
                    SiteId = site.Id,
                    Brand = brand,
                    Model = $"{model}-{_random.Next(24, 60):D3}",
                    Serial = $"{brand.Substring(0, 2).ToUpper()}{installYear}{_random.Next(10000, 99999)}",
                    FuelType = (FuelType)(i % 3),
                    UnitConfig = (UnitConfig)(i % 4),
                    EquipmentType = (EquipmentType)(i % 8 + 1), // Skip Unknown
                    RefrigerantType = i % 3 == 0 ? RefrigerantType.R22 : RefrigerantType.R410A,
                    TonnageX10 = _random.Next(15, 50), // 1.5 to 5 tons
                    BtuRating = _random.Next(18000, 60000),
                    SeerRating = _random.Next(13, 22),
                    InstallDate = new DateTime(installYear, _random.Next(1, 13), _random.Next(1, 28)),
                    WarrantyStartDate = new DateTime(installYear, _random.Next(1, 13), _random.Next(1, 28)),
                    WarrantyTermYears = 10,
                    Status = (AssetStatus)(i % 3),
                    Condition = (AssetCondition)(_random.Next(0, 5)),
                    FilterSize = new[] { "16x20x1", "16x25x1", "20x20x1", "20x25x1", "20x25x4" }[_random.Next(5)],
                    CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 365))
                };

                _dbContext.Assets.Add(asset);
                assetIndex++;
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedInventoryAsync(int count)
    {
        var categoryNames = new[] { "Refrigerant", "Filters", "Capacitors", "Contactors", "Thermostats", "Copper", "Misc Parts" };
        var itemNames = new Dictionary<string, string[]>
        {
            ["Refrigerant"] = new[] { "R-410A 25lb Jug", "R-22 30lb Jug", "R-32 10lb Can", "R-454B 25lb Jug" },
            ["Filters"] = new[] { "16x20x1 Pleated", "16x25x1 Pleated", "20x20x1 Pleated", "20x25x4 MERV 11" },
            ["Capacitors"] = new[] { "35/5 MFD 440V", "40/5 MFD 440V", "45/5 MFD 440V", "50/5 MFD 440V" },
            ["Contactors"] = new[] { "30A Contactor", "40A Contactor", "2-Pole Contactor", "3-Pole Contactor" },
            ["Thermostats"] = new[] { "Basic Digital", "Programmable 7-Day", "Honeywell T6 Pro", "Ecobee Smart" },
            ["Copper"] = new[] { "3/8\" Line Set 25'", "1/4\" Line Set 25'", "3/4\" Line Set 50'", "Copper Fittings Kit" },
            ["Misc Parts"] = new[] { "Condensate Pump", "Float Switch", "UV Light", "Hard Start Kit" }
        };

        var inventoryCount = await _dbContext.InventoryItems.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var categoryName = categoryNames[i % categoryNames.Length];
            var names = itemNames[categoryName];
            var name = names[i % names.Length];

            var cost = _random.Next(10, 200) + (_random.Next(100) / 100.0m);
            var price = cost * (1 + (_random.Next(20, 50) / 100.0m)); // 20-50% markup

            var item = new InventoryItem
            {
                Name = name,
                Sku = $"SKU-{(inventoryCount + i + 1):D5}",
                Description = $"{name} - {categoryName}",
                Category = (InventoryCategory)(i % 7), // Use enum values
                QuantityOnHand = _random.Next(0, 50),
                ReorderPoint = _random.Next(5, 15),
                Cost = Math.Round(cost, 2),
                Price = Math.Round(price, 2),
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 365))
            };

            _dbContext.InventoryItems.Add(item);
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedEstimatesAsync(int count)
    {
        var customers = await _dbContext.Customers.Include(c => c.Sites).ToListAsync();
        if (!customers.Any()) return;

        var titles = new[] { "AC Repair", "Furnace Maintenance", "New AC Installation", "Ductwork Repair", "Heat Pump Service", "Thermostat Upgrade", "System Replacement", "Seasonal Tune-Up" };
        var statuses = new[] { EstimateStatus.Draft, EstimateStatus.Sent, EstimateStatus.Accepted, EstimateStatus.Declined, EstimateStatus.Expired };

        for (int i = 0; i < count; i++)
        {
            var customer = customers[_random.Next(customers.Count)];
            var site = customer.Sites.FirstOrDefault();

            var estimate = new Estimate
            {
                CustomerId = customer.Id,
                SiteId = site?.Id,
                Title = titles[_random.Next(titles.Length)],
                Status = statuses[_random.Next(statuses.Length)],
                TaxRate = 7.0m,
                ExpiresAt = DateTime.Now.AddDays(30),
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 90))
            };

            _dbContext.Estimates.Add(estimate);
            await _dbContext.SaveChangesAsync();

            // Add 2-5 lines
            var lineCount = _random.Next(2, 6);
            for (int j = 0; j < lineCount; j++)
            {
                var lineType = (LineItemType)(j % 4);
                var qty = lineType == LineItemType.Labor ? _random.Next(1, 8) : _random.Next(1, 5);
                var rate = lineType == LineItemType.Labor ? 85m : _random.Next(25, 500);

                var line = new EstimateLine
                {
                    EstimateId = estimate.Id,
                    Type = lineType,
                    Description = $"{lineType} - Item {j + 1}",
                    Quantity = qty,
                    UnitPrice = rate,
                    Total = qty * rate
                };

                _dbContext.EstimateLines.Add(line);
            }
            await _dbContext.SaveChangesAsync();

            // Update estimate totals
            var lines = await _dbContext.EstimateLines.Where(l => l.EstimateId == estimate.Id).ToListAsync();
            estimate.SubTotal = lines.Sum(l => l.Total);
            estimate.TaxAmount = estimate.SubTotal * (estimate.TaxRate / 100);
            estimate.Total = estimate.SubTotal + estimate.TaxAmount;
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedJobsAsync(int count)
    {
        var customers = await _dbContext.Customers.Include(c => c.Sites).Include(c => c.Assets).ToListAsync();
        if (!customers.Any()) return;

        var titles = new[] { "AC Not Cooling", "Furnace Won't Start", "Scheduled Maintenance", "Thermostat Issue", "Strange Noise", "Water Leak", "No Heat", "Blowing Warm Air" };
        var statuses = new[] { JobStatus.Draft, JobStatus.Scheduled, JobStatus.EnRoute, JobStatus.InProgress, JobStatus.Completed, JobStatus.Closed, JobStatus.Cancelled };

        for (int i = 0; i < count; i++)
        {
            var customer = customers[_random.Next(customers.Count)];
            var site = customer.Sites.FirstOrDefault();
            var asset = customer.Assets.FirstOrDefault();
            var status = statuses[_random.Next(statuses.Length)];

            var job = new Job
            {
                CustomerId = customer.Id,
                SiteId = site?.Id,
                AssetId = asset?.Id,
                Title = titles[_random.Next(titles.Length)],
                Description = $"Customer reported issue: {titles[_random.Next(titles.Length)].ToLower()}",
                Status = status,
                EstimatedHours = _random.Next(1, 8),
                ScheduledDate = DateTime.Today.AddDays(_random.Next(-30, 30)).AddHours(_random.Next(8, 17)),
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 60))
            };

            if (status >= JobStatus.InProgress)
            {
                job.StartedAt = job.ScheduledDate?.AddMinutes(_random.Next(-15, 30));
            }

            if (status >= JobStatus.Completed)
            {
                job.CompletedAt = job.StartedAt?.AddHours(_random.Next(1, 6));
                job.WorkPerformed = "Diagnosed and repaired issue. System operating normally.";
                job.LaborTotal = _random.Next(85, 500);
                job.PartsTotal = _random.Next(0, 300);
                job.TaxAmount = (job.LaborTotal + job.PartsTotal) * 0.07m;
                job.Total = job.LaborTotal + job.PartsTotal + job.TaxAmount;
            }

            _dbContext.Jobs.Add(job);
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedInvoicesAsync(int count)
    {
        var customers = await _dbContext.Customers.ToListAsync();
        if (!customers.Any()) return;

        var statuses = new[] { InvoiceStatus.Draft, InvoiceStatus.Sent, InvoiceStatus.Paid, InvoiceStatus.Cancelled };

        for (int i = 0; i < count; i++)
        {
            var customer = customers[_random.Next(customers.Count)];
            var status = statuses[_random.Next(statuses.Length)];
            var labor = _random.Next(85, 500);
            var parts = _random.Next(0, 400);
            var subtotal = labor + parts;
            var tax = subtotal * 0.07m;
            var total = subtotal + tax;

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{(i + 1):D3}",
                CustomerId = customer.Id,
                Status = status,
                InvoiceDate = DateTime.Today.AddDays(-_random.Next(1, 60)),
                DueDate = DateTime.Today.AddDays(_random.Next(-30, 30)),
                LaborAmount = labor,
                PartsAmount = parts,
                SubTotal = subtotal,
                TaxRate = 7m,
                TaxAmount = tax,
                Total = total,
                AmountPaid = status == InvoiceStatus.Paid ? total : (status == InvoiceStatus.Sent ? _random.Next(0, (int)total) : 0),
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 60))
            };

            _dbContext.Invoices.Add(invoice);
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedServiceAgreementsAsync(int count)
    {
        var customers = await _dbContext.Customers.Include(c => c.Sites).ToListAsync();
        if (!customers.Any()) return;

        var names = new[] { "Basic Maintenance Plan", "Standard Service Agreement", "Premium Protection Plan", "Annual Tune-Up Package", "Comfort Club Membership" };

        for (int i = 0; i < count; i++)
        {
            var customer = customers[_random.Next(customers.Count)];
            var site = customer.Sites.FirstOrDefault();

            var agreement = new ServiceAgreement
            {
                AgreementNumber = $"SA-{DateTime.Now.Year}-{(i + 1):D4}",
                CustomerId = customer.Id,
                SiteId = site?.Id,
                Name = names[i % names.Length],
                Type = (AgreementType)(i % 4),
                Status = (AgreementStatus)(i % 3),
                StartDate = DateTime.Today.AddDays(-_random.Next(0, 180)),
                EndDate = DateTime.Today.AddDays(_random.Next(30, 365)),
                AnnualPrice = new[] { 199m, 299m, 399m, 499m }[i % 4],
                IncludedVisitsPerYear = new[] { 1, 2, 2, 4 }[i % 4],
                VisitsUsed = _random.Next(0, 2),
                RepairDiscountPercent = new[] { 10m, 15m, 20m, 25m }[i % 4],
                IncludesAcTuneUp = true,
                IncludesHeatingTuneUp = true,
                IncludesFilterReplacement = i % 2 == 0,
                PriorityService = i % 3 != 0,
                WaiveTripCharge = i % 2 == 0,
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 180))
            };

            _dbContext.ServiceAgreements.Add(agreement);
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedProductsAsync(int count)
    {
        // Skip if products already exist to avoid duplicates
        if (await _dbContext.Products.AnyAsync()) return;

        // Realistic product definitions with unique model numbers
        var productDefinitions = new[]
        {
            // Carrier Air Conditioners
            new { Manufacturer = "Carrier", Model = "24ACC636A003", Name = "Carrier Infinity 26 Air Conditioner", Category = ProductCategory.AirConditioner, Tonnage = 30, Seer = 26, Msrp = 7899m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Carrier", Model = "24ACC624A003", Name = "Carrier Infinity 24 Air Conditioner", Category = ProductCategory.AirConditioner, Tonnage = 24, Seer = 24, Msrp = 6499m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Carrier", Model = "24SCA536A003", Name = "Carrier Performance 17 AC", Category = ProductCategory.AirConditioner, Tonnage = 30, Seer = 17, Msrp = 4299m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Carrier", Model = "24ACC648A003", Name = "Carrier Infinity 21 Air Conditioner", Category = ProductCategory.AirConditioner, Tonnage = 48, Seer = 21, Msrp = 8999m, Refrigerant = RefrigerantType.R410A },
            
            // Trane Air Conditioners
            new { Manufacturer = "Trane", Model = "4TTX6024A1000A", Name = "Trane XV20i Variable Speed AC", Category = ProductCategory.AirConditioner, Tonnage = 24, Seer = 22, Msrp = 7199m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Trane", Model = "4TTX6036A1000A", Name = "Trane XV18i Air Conditioner", Category = ProductCategory.AirConditioner, Tonnage = 36, Seer = 18, Msrp = 5899m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Trane", Model = "4TTR6048A1000A", Name = "Trane XR15 Air Conditioner", Category = ProductCategory.AirConditioner, Tonnage = 48, Seer = 15, Msrp = 3999m, Refrigerant = RefrigerantType.R410A },
            
            // Lennox Air Conditioners
            new { Manufacturer = "Lennox", Model = "XC25-036-230", Name = "Lennox XC25 Premium AC", Category = ProductCategory.AirConditioner, Tonnage = 36, Seer = 26, Msrp = 8299m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Lennox", Model = "XC21-024-230", Name = "Lennox XC21 Air Conditioner", Category = ProductCategory.AirConditioner, Tonnage = 24, Seer = 21, Msrp = 6199m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Lennox", Model = "EL16XC1-036", Name = "Lennox Elite Series AC", Category = ProductCategory.AirConditioner, Tonnage = 36, Seer = 16, Msrp = 3899m, Refrigerant = RefrigerantType.R410A },
            
            // Gas Furnaces
            new { Manufacturer = "Carrier", Model = "59MN7A100V21-22", Name = "Carrier Infinity 98 Gas Furnace", Category = ProductCategory.GasFurnace, Tonnage = 0, Seer = 0, Msrp = 4599m, Refrigerant = RefrigerantType.Unknown },
            new { Manufacturer = "Carrier", Model = "59SC5A080E21-20", Name = "Carrier Comfort 80 Furnace", Category = ProductCategory.GasFurnace, Tonnage = 0, Seer = 0, Msrp = 2299m, Refrigerant = RefrigerantType.Unknown },
            new { Manufacturer = "Trane", Model = "S9X2D100U5PSA", Name = "Trane S9X2 96% AFUE Furnace", Category = ProductCategory.GasFurnace, Tonnage = 0, Seer = 0, Msrp = 3899m, Refrigerant = RefrigerantType.Unknown },
            new { Manufacturer = "Lennox", Model = "SLP99UH110XV60C", Name = "Lennox SLP99V Variable Furnace", Category = ProductCategory.GasFurnace, Tonnage = 0, Seer = 0, Msrp = 5199m, Refrigerant = RefrigerantType.Unknown },
            new { Manufacturer = "Goodman", Model = "GMVC961005CN", Name = "Goodman 96% AFUE Furnace", Category = ProductCategory.GasFurnace, Tonnage = 0, Seer = 0, Msrp = 1899m, Refrigerant = RefrigerantType.Unknown },
            
            // Heat Pumps
            new { Manufacturer = "Carrier", Model = "25VNA036A003", Name = "Carrier Infinity 24 Heat Pump", Category = ProductCategory.HeatPump, Tonnage = 36, Seer = 24, Msrp = 7499m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Trane", Model = "4TWV6036A1000B", Name = "Trane XV20i Heat Pump", Category = ProductCategory.HeatPump, Tonnage = 36, Seer = 20, Msrp = 6899m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Lennox", Model = "XP25-036-230", Name = "Lennox XP25 Heat Pump", Category = ProductCategory.HeatPump, Tonnage = 36, Seer = 23, Msrp = 7999m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Rheem", Model = "RP2036AJVCA", Name = "Rheem Prestige 20 SEER Heat Pump", Category = ProductCategory.HeatPump, Tonnage = 36, Seer = 20, Msrp = 5599m, Refrigerant = RefrigerantType.R410A },
            
            // Mini Splits
            new { Manufacturer = "Daikin", Model = "FTXS24LVJU", Name = "Daikin 24K BTU Mini Split", Category = ProductCategory.MiniSplit, Tonnage = 20, Seer = 19, Msrp = 2999m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Mitsubishi", Model = "MSZ-FH18NA", Name = "Mitsubishi Hyper Heat 18K", Category = ProductCategory.MiniSplit, Tonnage = 15, Seer = 22, Msrp = 3499m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "Fujitsu", Model = "AOU36RLXFZH", Name = "Fujitsu Halcyon 36K Multi-Zone", Category = ProductCategory.MiniSplit, Tonnage = 36, Seer = 18, Msrp = 4299m, Refrigerant = RefrigerantType.R410A },
            new { Manufacturer = "LG", Model = "LMU360HHV", Name = "LG Multi F 36K BTU Outdoor", Category = ProductCategory.MiniSplit, Tonnage = 36, Seer = 20, Msrp = 3899m, Refrigerant = RefrigerantType.R410A },
            
            // Air Handlers
            new { Manufacturer = "Carrier", Model = "FE4ANB006L00", Name = "Carrier Infinity Air Handler", Category = ProductCategory.AirHandler, Tonnage = 50, Seer = 0, Msrp = 2899m, Refrigerant = RefrigerantType.Unknown },
            new { Manufacturer = "Trane", Model = "TEM6A0C48H41SB", Name = "Trane XR Air Handler", Category = ProductCategory.AirHandler, Tonnage = 48, Seer = 0, Msrp = 1999m, Refrigerant = RefrigerantType.Unknown },
            new { Manufacturer = "Rheem", Model = "RHLLHM4821JA", Name = "Rheem Classic Air Handler", Category = ProductCategory.AirHandler, Tonnage = 48, Seer = 0, Msrp = 1699m, Refrigerant = RefrigerantType.Unknown },
        };

        var productCount = 0;
        foreach (var def in productDefinitions.Take(count))
        {
            productCount++;
            var cost = def.Msrp * 0.52m;
            var sellPrice = def.Msrp * 0.72m;

            var equipmentType = def.Category switch
            {
                ProductCategory.AirConditioner => EquipmentType.AirConditioner,
                ProductCategory.GasFurnace => EquipmentType.GasFurnace,
                ProductCategory.HeatPump => EquipmentType.HeatPump,
                ProductCategory.MiniSplit => EquipmentType.MiniSplit,
                ProductCategory.AirHandler => EquipmentType.AirHandler,
                _ => EquipmentType.Unknown
            };

            var product = new Product
            {
                ProductNumber = $"P-{productCount:D4}",
                Manufacturer = def.Manufacturer,
                ModelNumber = def.Model,
                ProductName = def.Name,
                Category = def.Category,
                Description = GenerateProductDescription(def.Category, def.Manufacturer, def.Seer, def.Tonnage),
                EquipmentType = equipmentType,
                RefrigerantType = def.Refrigerant,
                TonnageX10 = def.Tonnage,
                CoolingBtu = def.Tonnage > 0 ? def.Tonnage * 1200 : null,
                SeerRating = def.Seer > 0 ? def.Seer : null,
                Voltage = def.Category == ProductCategory.MiniSplit ? 208 : 240,
                Amperage = def.Tonnage > 0 ? (decimal)(def.Tonnage * 0.6) : 15m,
                MinCircuitAmpacity = def.Tonnage > 0 ? (decimal)(def.Tonnage * 0.7) : 20m,
                Msrp = def.Msrp,
                WholesaleCost = Math.Round(cost, 2),
                SuggestedSellPrice = Math.Round(sellPrice, 2),
                PartsWarrantyYears = 10,
                CompressorWarrantyYears = def.Category == ProductCategory.GasFurnace ? null : 10,
                HeatExchangerWarrantyYears = def.Category == ProductCategory.GasFurnace ? 20 : null,
                LaborWarrantyYears = 1,
                RegistrationRequired = true,
                LaborHoursEstimate = def.Category switch
                {
                    ProductCategory.AirConditioner => 6m,
                    ProductCategory.GasFurnace => 8m,
                    ProductCategory.HeatPump => 7m,
                    ProductCategory.MiniSplit => 4m,
                    ProductCategory.AirHandler => 5m,
                    _ => 4m
                },
                IsActive = true,
                IsDiscontinued = false,
                CreatedAt = DateTime.Now.AddDays(-_random.Next(30, 365))
            };

            _dbContext.Products.Add(product);
        }
        await _dbContext.SaveChangesAsync();
    }

    private string GenerateProductDescription(ProductCategory category, string manufacturer, int seer, int tonnage)
    {
        return category switch
        {
            ProductCategory.AirConditioner => $"High-efficiency {manufacturer} air conditioner with {seer} SEER rating. " +
                $"Features variable-speed technology for optimal comfort and energy savings. " +
                $"Rated at {tonnage / 10.0} tons cooling capacity. Compatible with most existing ductwork systems.",
            
            ProductCategory.GasFurnace => $"Premium {manufacturer} gas furnace with high AFUE efficiency rating. " +
                $"Features modulating gas valve and variable-speed blower for quiet, efficient operation. " +
                $"Includes advanced diagnostics and humidity control compatibility.",
            
            ProductCategory.HeatPump => $"Dual-function {manufacturer} heat pump providing both heating and cooling. " +
                $"Rated at {seer} SEER for cooling efficiency. {tonnage / 10.0} ton capacity. " +
                $"Ideal for moderate climates with year-round comfort.",
            
            ProductCategory.MiniSplit => $"Ductless {manufacturer} mini-split system with inverter technology. " +
                $"Perfect for room additions, garages, or homes without ductwork. " +
                $"Includes wireless remote and optional WiFi control. {seer} SEER rated.",
            
            ProductCategory.AirHandler => $"{manufacturer} air handler designed for split system applications. " +
                $"Features multi-speed blower motor and factory-installed filter rack. " +
                $"Compatible with matching outdoor condensing units.",
            
            _ => $"{manufacturer} HVAC equipment - contact for specifications."
        };
    }
}
