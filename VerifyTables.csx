using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneManVan.Shared.Data;

// Quick verification of database tables
// Run from solution root: dotnet run --project Tools/VerifyTables

var builder = Host.CreateDefaultBuilder(args);

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "OneManVan.Web", "Data", "business.db");

if (!File.Exists(dbPath))
{
    Console.WriteLine($"Database not found at: {dbPath}");
    return;
}

Console.WriteLine($"Database: {dbPath}");
Console.WriteLine($"Size: {new FileInfo(dbPath).Length / 1024.0:N1} KB");
Console.WriteLine();

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";

using var reader = cmd.ExecuteReader();

var tables = new List<string>();
while (reader.Read())
{
    tables.Add(reader.GetString(0));
}

Console.WriteLine($"Found {tables.Count} tables:");
Console.WriteLine(new string('=', 50));

// Expected tables from DbContext
var expectedTables = new[]
{
    "Customers", "Companies", "Sites", "Assets",
    "CustomFields", "SchemaDefinitions", 
    "CustomFieldDefinitions", "CustomFieldChoices",
    "AssetOwners", "CustomerCompanyRoles",
    "Estimates", "EstimateLines", 
    "InventoryItems", "InventoryLogs",
    "Jobs", "TimeEntries", "JobParts",
    "Invoices", "InvoiceLineItems", "Payments",
    "Products", "ProductDocuments",
    "ManufacturerRegistrations",
    "WarrantyClaims",
    "ServiceHistory", "JobPhotos", "AssetPhotos", "SitePhotos",
    "CommunicationLogs", "CustomerDocuments", "CustomerNotes",
    "Documents",
    "Employees", "EmployeeTimeLogs", "EmployeePerformanceNotes", "EmployeePayments",
    "ServiceAgreements",
    "QuickNotes",
    "CompanySettings",
    "MaterialLists", "MaterialListSystems", "MaterialListItems",
    "MaterialListTemplates", "MaterialListTemplateItems"
};

var missing = expectedTables.Where(e => !tables.Any(t => t.Equals(e, StringComparison.OrdinalIgnoreCase))).ToList();
var present = expectedTables.Where(e => tables.Any(t => t.Equals(e, StringComparison.OrdinalIgnoreCase))).ToList();

Console.WriteLine();
Console.WriteLine($"Present ({present.Count}/{expectedTables.Length}):");
foreach (var t in present.OrderBy(x => x))
{
    Console.WriteLine($"  [OK] {t}");
}

if (missing.Any())
{
    Console.WriteLine();
    Console.WriteLine($"Missing ({missing.Count}):");
    foreach (var t in missing.OrderBy(x => x))
    {
        Console.WriteLine($"  [!!] {t}");
    }
}
else
{
    Console.WriteLine();
    Console.WriteLine("All expected tables are present!");
}
