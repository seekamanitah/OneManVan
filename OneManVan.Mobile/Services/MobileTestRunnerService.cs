using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using System.Text;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Simple test runner for mobile app functionality.
/// Executes database and CRUD tests on device.
/// </summary>
public class MobileTestRunnerService
{
    private readonly OneManVanDbContext _db;
    private readonly StringBuilder _report;
    private int _passCount;
    private int _failCount;

    public MobileTestRunnerService(OneManVanDbContext db)
    {
        _db = db;
        _report = new StringBuilder();
    }

    /// <summary>
    /// Runs all mobile tests and returns a report.
    /// </summary>
    public async Task<MobileTestResult> RunAllTestsAsync()
    {
        _report.Clear();
        _passCount = 0;
        _failCount = 0;

        _report.AppendLine("# Mobile App Test Report");
        _report.AppendLine($"**Executed**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _report.AppendLine();

        // Database Tests
        _report.AppendLine("## Database Tests");
        await TestDatabaseConnection();
        await TestTablesExist();

        // Customer CRUD
        _report.AppendLine();
        _report.AppendLine("## Customer CRUD Tests");
        await TestCustomerCrud();

        // Job CRUD
        _report.AppendLine();
        _report.AppendLine("## Job CRUD Tests");
        await TestJobCrud();

        // Invoice CRUD
        _report.AppendLine();
        _report.AppendLine("## Invoice CRUD Tests");
        await TestInvoiceCrud();

        // Inventory CRUD
        _report.AppendLine();
        _report.AppendLine("## Inventory CRUD Tests");
        await TestInventoryCrud();

        // Estimate CRUD
        _report.AppendLine();
        _report.AppendLine("## Estimate CRUD Tests");
        await TestEstimateCrud();

        // Asset CRUD
        _report.AppendLine();
        _report.AppendLine("## Asset CRUD Tests");
        await TestAssetCrud();

        // Summary
        _report.AppendLine();
        _report.AppendLine("## Summary");
        _report.AppendLine($"- **Passed**: {_passCount}");
        _report.AppendLine($"- **Failed**: {_failCount}");
        _report.AppendLine($"- **Total**: {_passCount + _failCount}");
        var passRate = _passCount + _failCount > 0 
            ? (_passCount * 100.0 / (_passCount + _failCount)) 
            : 0;
        _report.AppendLine($"- **Pass Rate**: {passRate:F1}%");

        return new MobileTestResult
        {
            Report = _report.ToString(),
            PassCount = _passCount,
            FailCount = _failCount,
            PassRate = passRate
        };
    }

    private async Task TestDatabaseConnection()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            LogResult("DB-001", "Database connection", canConnect);
        }
        catch (Exception ex)
        {
            LogResult("DB-001", $"Database connection: {ex.Message}", false);
        }
    }

    private async Task TestTablesExist()
    {
        try
        {
            // Try to query each major table
            await _db.Customers.AnyAsync();
            await _db.Jobs.AnyAsync();
            await _db.Invoices.AnyAsync();
            await _db.InventoryItems.AnyAsync();
            await _db.Estimates.AnyAsync();
            await _db.Assets.AnyAsync();
            LogResult("DB-002", "All tables accessible", true);
        }
        catch (Exception ex)
        {
            LogResult("DB-002", $"Tables check failed: {ex.Message}", false);
        }
    }

    private async Task TestCustomerCrud()
    {
        Customer? customer = null;
        try
        {
            // Create
            customer = new Customer
            {
                Name = "Test Customer Mobile",
                Email = "test@mobile.com",
                Phone = "(303) 555-TEST",
                CustomerType = CustomerType.Residential,
                Status = CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
            LogResult("CUS-001", "Create customer", customer.Id > 0);

            // Read
            var loaded = await _db.Customers.FindAsync(customer.Id);
            LogResult("CUS-002", "Read customer", loaded != null);

            // Update
            customer.Name = "Updated Customer Mobile";
            await _db.SaveChangesAsync();
            var updated = await _db.Customers.FindAsync(customer.Id);
            LogResult("CUS-003", "Update customer", updated?.Name == "Updated Customer Mobile");

            // Delete
            _db.Customers.Remove(customer);
            await _db.SaveChangesAsync();
            var deleted = await _db.Customers.FindAsync(customer.Id);
            LogResult("CUS-004", "Delete customer", deleted == null);
            customer = null;
        }
        catch (Exception ex)
        {
            LogResult("CUS-XXX", $"Customer CRUD error: {ex.Message}", false);
        }
        finally
        {
            // Cleanup if needed
            if (customer != null)
            {
                try
                {
                    _db.Customers.Remove(customer);
                    await _db.SaveChangesAsync();
                }
                catch { }
            }
        }
    }

    private async Task TestJobCrud()
    {
        Customer? customer = null;
        Job? job = null;
        try
        {
            // Setup customer
            customer = new Customer { Name = "Job Test Customer", CreatedAt = DateTime.UtcNow };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            // Create job
            job = new Job
            {
                CustomerId = customer.Id,
                Title = "Test Job Mobile",
                Status = JobStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };
            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();
            LogResult("JOB-001", "Create job", job.Id > 0);

            // Update status
            job.Status = JobStatus.Scheduled;
            job.ScheduledDate = DateTime.Today.AddDays(1);
            await _db.SaveChangesAsync();
            LogResult("JOB-002", "Schedule job", job.Status == JobStatus.Scheduled);

            // Start job
            job.Status = JobStatus.InProgress;
            job.StartedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            LogResult("JOB-003", "Start job", job.StartedAt.HasValue);

            // Complete job
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            LogResult("JOB-004", "Complete job", job.CompletedAt.HasValue);
        }
        catch (Exception ex)
        {
            LogResult("JOB-XXX", $"Job CRUD error: {ex.Message}", false);
        }
        finally
        {
            // Cleanup
            try
            {
                if (job != null) _db.Jobs.Remove(job);
                if (customer != null) _db.Customers.Remove(customer);
                await _db.SaveChangesAsync();
            }
            catch { }
        }
    }

    private async Task TestInvoiceCrud()
    {
        Customer? customer = null;
        Invoice? invoice = null;
        try
        {
            // Setup customer
            customer = new Customer { Name = "Invoice Test Customer", CreatedAt = DateTime.UtcNow };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            // Create invoice
            invoice = new Invoice
            {
                CustomerId = customer.Id,
                InvoiceNumber = $"INV-TEST-{DateTime.Now.Ticks}",
                Status = InvoiceStatus.Draft,
                Total = 500m,
                DueDate = DateTime.Today.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            LogResult("INV-001", "Create invoice", invoice.Id > 0);

            // Send invoice
            invoice.Status = InvoiceStatus.Sent;
            invoice.SentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            LogResult("INV-002", "Send invoice", invoice.Status == InvoiceStatus.Sent);

            // Add payment
            invoice.AmountPaid = 500m;
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            LogResult("INV-003", "Pay invoice", invoice.IsPaid);

            // Test balance
            var balanceDue = invoice.BalanceDue;
            LogResult("INV-004", "Balance calculation", balanceDue == 0);
        }
        catch (Exception ex)
        {
            LogResult("INV-XXX", $"Invoice CRUD error: {ex.Message}", false);
        }
        finally
        {
            // Cleanup
            try
            {
                if (invoice != null) _db.Invoices.Remove(invoice);
                if (customer != null) _db.Customers.Remove(customer);
                await _db.SaveChangesAsync();
            }
            catch { }
        }
    }

    private async Task TestInventoryCrud()
    {
        InventoryItem? item = null;
        try
        {
            // Create
            item = new InventoryItem
            {
                Name = "Test Part Mobile",
                Sku = $"SKU-TEST-{DateTime.Now.Ticks}",
                QuantityOnHand = 10,
                Cost = 25m,
                Price = 45m,
                ReorderPoint = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.InventoryItems.Add(item);
            await _db.SaveChangesAsync();
            LogResult("INVT-001", "Create inventory item", item.Id > 0);

            // Read
            var loaded = await _db.InventoryItems.FindAsync(item.Id);
            LogResult("INVT-002", "Read inventory item", loaded != null);

            // Update - restock
            item.QuantityOnHand = 20;
            item.LastRestockedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            LogResult("INVT-003", "Restock item", item.QuantityOnHand == 20);

            // Test low stock
            item.QuantityOnHand = 3;
            await _db.SaveChangesAsync();
            LogResult("INVT-004", "Low stock detection", item.IsLowStock);

            // Test margin calculation
            var margin = item.ProfitMargin;
            LogResult("INVT-005", "Profit margin calc", margin > 0);
        }
        catch (Exception ex)
        {
            LogResult("INVT-XXX", $"Inventory CRUD error: {ex.Message}", false);
        }
        finally
        {
            // Cleanup
            try
            {
                if (item != null)
                {
                    _db.InventoryItems.Remove(item);
                    await _db.SaveChangesAsync();
                }
            }
            catch { }
        }
    }

    private async Task TestEstimateCrud()
    {
        Customer? customer = null;
        Estimate? estimate = null;
        try
        {
            // Setup customer
            customer = new Customer { Name = "Estimate Test Customer", CreatedAt = DateTime.UtcNow };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            // Create estimate
            estimate = new Estimate
            {
                CustomerId = customer.Id,
                Title = "Test Estimate Mobile",
                Status = EstimateStatus.Draft,
                TaxRate = 8m,
                CreatedAt = DateTime.UtcNow
            };
            _db.Estimates.Add(estimate);
            await _db.SaveChangesAsync();
            LogResult("EST-001", "Create estimate", estimate.Id > 0);

            // Update totals
            estimate.SubTotal = 500m;
            estimate.TaxAmount = 40m;
            estimate.Total = 540m;
            await _db.SaveChangesAsync();
            LogResult("EST-002", "Update estimate totals", estimate.Total == 540m);

            // Send estimate
            estimate.Status = EstimateStatus.Sent;
            estimate.SentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            LogResult("EST-003", "Send estimate", estimate.Status == EstimateStatus.Sent);

            // Accept estimate
            estimate.Status = EstimateStatus.Accepted;
            estimate.AcceptedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            LogResult("EST-004", "Accept estimate", estimate.Status == EstimateStatus.Accepted);
        }
        catch (Exception ex)
        {
            LogResult("EST-XXX", $"Estimate CRUD error: {ex.Message}", false);
        }
        finally
        {
            // Cleanup
            try
            {
                if (estimate != null) _db.Estimates.Remove(estimate);
                if (customer != null) _db.Customers.Remove(customer);
                await _db.SaveChangesAsync();
            }
            catch { }
        }
    }

    private async Task TestAssetCrud()
    {
        Customer? customer = null;
        Asset? asset = null;
        try
        {
            // Setup customer
            customer = new Customer { Name = "Asset Test Customer", CreatedAt = DateTime.UtcNow };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            // Create asset
            asset = new Asset
            {
                CustomerId = customer.Id,
                Serial = $"SN-TEST-{DateTime.Now.Ticks}",
                Brand = "Carrier",
                Model = "24ACC636",
                EquipmentType = EquipmentType.AirConditioner,
                RefrigerantType = RefrigerantType.R410A,
                CreatedAt = DateTime.UtcNow
            };
            _db.Assets.Add(asset);
            await _db.SaveChangesAsync();
            LogResult("AST-001", "Create asset", asset.Id > 0);

            // Read with customer
            var loaded = await _db.Assets
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == asset.Id);
            LogResult("AST-002", "Read asset with customer", loaded?.Customer != null);

            // Update
            asset.Nickname = "Main AC Unit";
            asset.InstallDate = DateTime.Today.AddYears(-2);
            await _db.SaveChangesAsync();
            LogResult("AST-003", "Update asset", asset.Nickname == "Main AC Unit");
        }
        catch (Exception ex)
        {
            LogResult("AST-XXX", $"Asset CRUD error: {ex.Message}", false);
        }
        finally
        {
            // Cleanup
            try
            {
                if (asset != null) _db.Assets.Remove(asset);
                if (customer != null) _db.Customers.Remove(customer);
                await _db.SaveChangesAsync();
            }
            catch { }
        }
    }

    private void LogResult(string testId, string testCase, bool passed)
    {
        var icon = passed ? "?" : "?";
        _report.AppendLine($"- [{icon}] **{testId}**: {testCase}");
        if (passed) _passCount++; else _failCount++;
    }
}

/// <summary>
/// Result of mobile test run.
/// </summary>
public class MobileTestResult
{
    public string Report { get; set; } = string.Empty;
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public double PassRate { get; set; }
    public bool AllPassed => FailCount == 0;
}
