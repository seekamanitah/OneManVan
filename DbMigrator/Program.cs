using Microsoft.Data.Sqlite;

var dbPath = @"C:\Users\tech\source\repos\OneManVan\OneManVan.Web\AppData\onemanvan.db";
Console.WriteLine($"Database: {dbPath}\n");

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

var sqlStatements = new[]
{
    "ALTER TABLE Invoices ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0",
    "ALTER TABLE Invoices ADD COLUMN DeletedAt TEXT",
    "ALTER TABLE Invoices ADD COLUMN DeletedBy TEXT",
    "CREATE INDEX IF NOT EXISTS IX_Invoices_IsDeleted ON Invoices(IsDeleted)"
};

int success = 0, skipped = 0;

foreach (var sql in sqlStatements)
{
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    
    try
    {
        command.ExecuteNonQuery();
        Console.WriteLine($"? {sql[..Math.Min(70, sql.Length)]}");
        success++;
    }
    catch (Exception ex)
    {
        if (ex.Message.Contains("duplicate") || ex.Message.Contains("already exists"))
        {
            Console.WriteLine($"? Already exists: {sql[..Math.Min(70, sql.Length)]}");
            skipped++;
        }
        else
        {
            Console.WriteLine($"? ERROR: {ex.Message}");
        }
    }
}

Console.WriteLine($"\n? SOFT DELETE MIGRATION COMPLETE!");
Console.WriteLine($"? Executed: {success} | ? Skipped: {skipped}");
Console.WriteLine($"\nRestart your Web app now!");

