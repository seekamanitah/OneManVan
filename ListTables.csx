// Quick database table verification
// Run with: dotnet script ListTables.csx

#r "nuget: Microsoft.Data.Sqlite, 9.0.0"

using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "OneManVan.Web", "AppData", "OneManVan.db"
);

// Also check alternate location
var altDbPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "OneManVan.Web", "Data", "business.db"
);

string actualPath = null;
if (File.Exists(dbPath))
    actualPath = dbPath;
else if (File.Exists(altDbPath))
    actualPath = altDbPath;

if (actualPath == null)
{
    Console.WriteLine("Database not found at:");
    Console.WriteLine($"  - {dbPath}");
    Console.WriteLine($"  - {altDbPath}");
    return;
}

Console.WriteLine($"Database found at: {actualPath}");
Console.WriteLine();

using var connection = new SqliteConnection($"Data Source={actualPath}");
connection.Open();

using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";

using var reader = cmd.ExecuteReader();
Console.WriteLine("Tables in database:");
Console.WriteLine("==================");
int count = 0;
while (reader.Read())
{
    Console.WriteLine($"  - {reader.GetString(0)}");
    count++;
}
Console.WriteLine($"\nTotal: {count} tables");
