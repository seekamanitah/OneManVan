using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for managing database operations: reset, seed demo data, and cleanup.
/// </summary>
public class DatabaseManagementService
{
    /// <summary>
    /// Completely erases the database and recreates schema.
    /// </summary>
    public async Task<bool> ResetDatabaseAsync(OneManVanDbContext dbContext)
    {
        try
        {
            // Delete the database
            await dbContext.Database.EnsureDeletedAsync();
            
            // Recreate with fresh schema
            await dbContext.Database.EnsureCreatedAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ResetDatabase failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clears all data from tables but preserves schema.
    /// </summary>
    public async Task<bool> ClearAllDataAsync(OneManVanDbContext dbContext)
    {
        try
        {
            // Remove all data in proper order (respecting foreign keys)
            dbContext.InvoiceLineItems.RemoveRange(dbContext.InvoiceLineItems);
            dbContext.Invoices.RemoveRange(dbContext.Invoices);
            dbContext.Jobs.RemoveRange(dbContext.Jobs);
            dbContext.Estimates.RemoveRange(dbContext.Estimates);
            dbContext.ServiceAgreements.RemoveRange(dbContext.ServiceAgreements);
            dbContext.ManufacturerRegistrations.RemoveRange(dbContext.ManufacturerRegistrations);
            dbContext.ProductDocuments.RemoveRange(dbContext.ProductDocuments);
            dbContext.Products.RemoveRange(dbContext.Products);
            dbContext.AssetOwners.RemoveRange(dbContext.AssetOwners);
            dbContext.Assets.RemoveRange(dbContext.Assets);
            dbContext.CustomerCompanyRoles.RemoveRange(dbContext.CustomerCompanyRoles);
            dbContext.Companies.RemoveRange(dbContext.Companies);
            dbContext.Customers.RemoveRange(dbContext.Customers);
            dbContext.InventoryItems.RemoveRange(dbContext.InventoryItems);
            dbContext.CustomFieldChoices.RemoveRange(dbContext.CustomFieldChoices);
            dbContext.SchemaDefinitions.RemoveRange(dbContext.SchemaDefinitions);
            
            await dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClearAllData failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Seeds database with comprehensive demo data for testing.
    /// </summary>
    public async Task<bool> SeedDemoDataAsync(OneManVanDbContext dbContext)
    {
        try
        {
            // Check if data already exists
            if (await dbContext.Customers.AnyAsync())
            {
                return false; // Don't seed if data exists
            }

            // Create demo companies
            var companies = new[]
            {
                new Company
                {
                    Name = "ABC Property Management",
                    CompanyType = CompanyType.PropertyManagement,
                    Phone = "(555) 100-1000",
                    Email = "contact@abcproperties.com",
                    BillingAddress = "100 Main Street",
                    BillingCity = "Portland",
                    BillingState = "OR",
                    BillingZipCode = "97201",
                    TaxId = "12-3456789"
                },
                new Company
                {
                    Name = "XYZ Commercial Services",
                    CompanyType = CompanyType.Customer,
                    Phone = "(555) 200-2000",
                    Email = "info@xyzcommercial.com",
                    BillingAddress = "200 Business Park Dr",
                    BillingCity = "Portland",
                    BillingState = "OR",
                    BillingZipCode = "97202"
                },
                new Company
                {
                    Name = "Tech Innovations LLC",
                    CompanyType = CompanyType.Partner,
                    Phone = "(555) 300-3000",
                    Email = "hello@techinnovations.com"
                }
            };
            dbContext.Companies.AddRange(companies);
            await dbContext.SaveChangesAsync();

            // Create demo customers
            var customers = new[]
            {
                new Customer
                {
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "john.smith@email.com",
                    Phone = "(555) 111-1111",
                    AccountBalance = 0m
                },
                new Customer
                {
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Email = "sarah.j@email.com",
                    Phone = "(555) 222-2222",
                    AccountBalance = 250.00m
                },
                new Customer
                {
                    FirstName = "Michael",
                    LastName = "Brown",
                    Email = "mbrown@email.com",
                    Phone = "(555) 333-3333",
                    AccountBalance = -150.00m
                },
                new Customer
                {
                    FirstName = "Emily",
                    LastName = "Davis",
                    Email = "emily.davis@email.com",
                    Phone = "(555) 444-4444",
                    AccountBalance = 0m
                },
                new Customer
                {
                    FirstName = "David",
                    LastName = "Wilson",
                    Email = "d.wilson@email.com",
                    Phone = "(555) 555-5555",
                    AccountBalance = 500.00m
                }
            };
            dbContext.Customers.AddRange(customers);
            await dbContext.SaveChangesAsync();

            // Link some customers to companies
            dbContext.CustomerCompanyRoles.AddRange(new[]
            {
                new CustomerCompanyRole
                {
                    CustomerId = customers[0].Id,
                    CompanyId = companies[0].Id,
                    Role = "Tenant"
                },
                new CustomerCompanyRole
                {
                    CustomerId = customers[1].Id,
                    CompanyId = companies[0].Id,
                    Role = "Property Manager"
                },
                new CustomerCompanyRole
                {
                    CustomerId = customers[2].Id,
                    CompanyId = companies[1].Id,
                    Role = "Facilities Director"
                }
            });
            await dbContext.SaveChangesAsync();

            // Create demo assets (simplified with only required fields)
            var assets = new[]
            {
                new Asset
                {
                    CustomerId = customers[0].Id,
                    AssetName = "HVAC System - Main Floor",
                    Description = "Carrier HVAC CA-2400, installed 2021",
                    Serial = "SN-001-2020",
                    Brand = "Carrier",
                    Model = "CA-2400"
                },
                new Asset
                {
                    CustomerId = customers[0].Id,
                    AssetName = "Water Heater - Basement",
                    Description = "Rheem RH-50G Water Heater",
                    Serial = "WH-002-2021",
                    Brand = "Rheem",
                    Model = "RH-50G"
                },
                new Asset
                {
                    CustomerId = customers[1].Id,
                    AssetName = "HVAC - Rooftop Unit 1",
                    Description = "Trane TR-3000 HVAC System",
                    Serial = "SN-003-2019",
                    Brand = "Trane",
                    Model = "TR-3000"
                },
                new Asset
                {
                    CustomerId = customers[2].Id,
                    AssetName = "Furnace - Mechanical Room",
                    Description = "Lennox LX-1800 Furnace",
                    Serial = "FN-004-2022",
                    Brand = "Lennox",
                    Model = "LX-1800"
                }
            };
            dbContext.Assets.AddRange(assets);
            await dbContext.SaveChangesAsync();

            // Link assets to companies (using polymorphic pattern)
            dbContext.AssetOwners.AddRange(new[]
            {
                new AssetOwner
                {
                    AssetId = assets[0].Id,
                    OwnerType = "Company",
                    OwnerId = companies[0].Id,
                    OwnershipType = "Managed"
                },
                new AssetOwner
                {
                    AssetId = assets[2].Id,
                    OwnerType = "Company",
                    OwnerId = companies[0].Id,
                    OwnershipType = "Owned"
                }
            });
            await dbContext.SaveChangesAsync();

            // Create demo products (simplified with only required fields)
            var products = new[]
            {
                new Product
                {
                    ProductName = "Air Filter - 16x20",
                    Category = ProductCategory.Accessories,
                    Manufacturer = "FilterPro",
                    ModelNumber = "FP-1620",
                    Description = "High efficiency air filter"
                },
                new Product
                {
                    ProductName = "Thermostat - Smart WiFi",
                    Category = ProductCategory.Thermostat,
                    Manufacturer = "Nest",
                    ModelNumber = "NEST-E",
                    Description = "Smart thermostat with WiFi connectivity"
                },
                new Product
                {
                    ProductName = "3-Ton Heat Pump",
                    Category = ProductCategory.HeatPump,
                    Manufacturer = "Carrier",
                    ModelNumber = "CA-HP-30",
                    Description = "High-efficiency 3-ton heat pump system"
                },
                new Product
                {
                    ProductName = "80k BTU Gas Furnace",
                    Category = ProductCategory.GasFurnace,
                    Manufacturer = "Lennox",
                    ModelNumber = "LX-G80",
                    Description = "80,000 BTU gas furnace with 96% AFUE"
                }
            };
            dbContext.Products.AddRange(products);
            await dbContext.SaveChangesAsync();

            // Create 2 demo jobs (minimal fields only)
            var job1 = new Job
            {
                CustomerId = customers[0].Id,
                Title = "HVAC Repair - Main Floor",
                Description = "Repair compressor and replace filter",
                ScheduledDate = DateTime.Now.AddDays(2)
            };
            
            var job2 = new Job
            {
                CustomerId = customers[1].Id,
                Title = "Annual Maintenance - Service Agreement",
                Description = "Annual HVAC maintenance check",
                ScheduledDate = DateTime.Now.AddDays(5)
            };
            
            dbContext.Jobs.Add(job1);
            dbContext.Jobs.Add(job2);
            await dbContext.SaveChangesAsync();

            // Create 2 demo estimates (minimal fields only)
            var estimate1 = new Estimate
            {
                CustomerId = customers[0].Id,
                Title = "HVAC Inspection",
                Description = "HVAC system inspection and repair",
                CreatedAt = DateTime.Now.AddDays(-7),
                TaxRate = 7.0m,
                TaxAmount = 59.50m
            };
            
            var estimate2 = new Estimate
            {
                CustomerId = customers[1].Id,
                Title = "Water Heater Replacement",
                Description = "Water heater replacement",
                CreatedAt = DateTime.Now.AddDays(-3),
                TaxRate = 7.0m,
                TaxAmount = 84.00m
            };
            
            dbContext.Estimates.Add(estimate1);
            dbContext.Estimates.Add(estimate2);
            await dbContext.SaveChangesAsync();

            // Create 2 demo invoices (minimal fields only)
            var invoice1 = new Invoice
            {
                CustomerId = customers[2].Id,
                JobId = job1.Id,
                InvoiceNumber = "INV-2024-001",
                InvoiceDate = DateTime.Now.AddDays(-1),
                DueDate = DateTime.Now.AddDays(29),
                TaxRate = 7.0m,
                TaxAmount = 19.25m
            };
            
            var invoice2 = new Invoice
            {
                CustomerId = customers[4].Id,
                InvoiceNumber = "INV-2024-002",
                InvoiceDate = DateTime.Now.AddDays(-15),
                DueDate = DateTime.Now.AddDays(15),
                TaxRate = 7.0m,
                TaxAmount = 31.50m
            };
            
            dbContext.Invoices.Add(invoice1);
            dbContext.Invoices.Add(invoice2);
            await dbContext.SaveChangesAsync();

            // Create 1 demo service agreement (minimal fields only)
            var agreement = new ServiceAgreement
            {
                CustomerId = customers[1].Id,
                Description = "Quarterly maintenance visits",
                StartDate = DateTime.Now.AddMonths(-6),
                EndDate = DateTime.Now.AddMonths(6)
            };
            
            dbContext.ServiceAgreements.Add(agreement);
            await dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SeedDemoData failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets database statistics (record counts).
    /// </summary>
    public async Task<DatabaseStats> GetDatabaseStatsAsync(OneManVanDbContext dbContext)
    {
        return new DatabaseStats
        {
            CustomerCount = await dbContext.Customers.CountAsync(),
            CompanyCount = await dbContext.Companies.CountAsync(),
            AssetCount = await dbContext.Assets.CountAsync(),
            ProductCount = await dbContext.Products.CountAsync(),
            JobCount = await dbContext.Jobs.CountAsync(),
            EstimateCount = await dbContext.Estimates.CountAsync(),
            InvoiceCount = await dbContext.Invoices.CountAsync(),
            ServiceAgreementCount = await dbContext.ServiceAgreements.CountAsync()
        };
    }
}

/// <summary>
/// Database statistics model.
/// </summary>
public class DatabaseStats
{
    public int CustomerCount { get; set; }
    public int CompanyCount { get; set; }
    public int AssetCount { get; set; }
    public int ProductCount { get; set; }
    public int JobCount { get; set; }
    public int EstimateCount { get; set; }
    public int InvoiceCount { get; set; }
    public int ServiceAgreementCount { get; set; }

    public int TotalRecords =>
        CustomerCount + CompanyCount + AssetCount + ProductCount +
        JobCount + EstimateCount + InvoiceCount + ServiceAgreementCount;
}


