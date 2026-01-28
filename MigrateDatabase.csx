using Microsoft.Data.Sqlite;

var dbPath = @"C:\Users\tech\source\repos\TradeFlow\OneManVan.Web\AppData\onemanvan.db";
var connectionString = $"Data Source={dbPath}";

Console.WriteLine("Connecting to database...");

using var connection = new SqliteConnection(connectionString);
connection.Open();

Console.WriteLine("Connected! Executing migration...\n");

var sqlStatements = new[]
{
    "ALTER TABLE Estimates ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0",
    "ALTER TABLE Invoices ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0",
    "ALTER TABLE Jobs ADD COLUMN TaxIncluded INTEGER NOT NULL DEFAULT 0",
    "ALTER TABLE Companies ADD COLUMN ContactName TEXT",
    "ALTER TABLE Companies ADD COLUMN ContactCustomerId INTEGER REFERENCES Customers(Id)",
    "ALTER TABLE Sites ADD COLUMN SiteName TEXT",
    "ALTER TABLE Sites ADD COLUMN LocationDescription TEXT",
    "ALTER TABLE Sites ADD COLUMN CompanyId INTEGER REFERENCES Companies(Id)",
    "CREATE INDEX IF NOT EXISTS IX_Companies_ContactCustomerId ON Companies(ContactCustomerId)",
    "CREATE INDEX IF NOT EXISTS IX_Sites_CompanyId ON Sites(CompanyId)",
    "CREATE INDEX IF NOT EXISTS IX_Sites_SiteName ON Sites(SiteName)"
};

int success = 0, skipped = 0;

foreach (var sql in sqlStatements)
{
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    
    try
    {
        command.ExecuteNonQuery();
        Console.WriteLine($"? {sql.Substring(0, Math.Min(60, sql.Length))}...");
        success++;
    }
    catch (Exception ex)
    {
        if (ex.Message.Contains("duplicate column") || ex.Message.Contains("already exists"))
        {
            Console.WriteLine($"? Already exists: {sql.Substring(0, Math.Min(60, sql.Length))}...");
            skipped++;
        }
        else
        {
            Console.WriteLine($"? Error: {ex.Message}");
        }
    }
}

connection.Close();

Console.WriteLine($"\n========================================");
Console.WriteLine($"? MIGRATION COMPLETE!");
Console.WriteLine($"========================================");
Console.WriteLine($"? Executed: {success} statements");
Console.WriteLine($"? Skipped: {skipped} (already existed)");
Console.WriteLine($"\nRestart your Web app - the error should be fixed!");
