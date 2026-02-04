using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Handles SQLite database migrations for the mobile app.
/// Checks current schema version and applies migrations as needed.
/// </summary>
public class DatabaseMigrationService
{
    private const int CURRENT_SCHEMA_VERSION = 2; // Increment when adding new migrations
    
    public static async Task<bool> EnsureDatabaseMigratedAsync(OneManVanDbContext context)
    {
        try
        {
            // Ensure database exists
            await context.Database.EnsureCreatedAsync();
            
            // Get current schema version
            var currentVersion = await GetSchemaVersionAsync(context);
            
            if (currentVersion >= CURRENT_SCHEMA_VERSION)
            {
                System.Diagnostics.Debug.WriteLine($"[Migration] Database schema is up-to-date (v{currentVersion})");
                return true;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Migration] Upgrading database from v{currentVersion} to v{CURRENT_SCHEMA_VERSION}");
            
            // Apply migrations based on current version
            if (currentVersion < 2)
            {
                await ApplyMigration_v2_JobWorkerTimeTracking(context);
            }
            
            // Update schema version
            await SetSchemaVersionAsync(context, CURRENT_SCHEMA_VERSION);
            
            System.Diagnostics.Debug.WriteLine($"[Migration] Database migration complete!");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Migration] Migration failed: {ex.Message}");
            return false;
        }
    }
    
    private static async Task<int> GetSchemaVersionAsync(OneManVanDbContext context)
    {
        try
        {
            // Check if schema_version table exists
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='schema_version'";
            var exists = await checkCmd.ExecuteScalarAsync();
            
            if (exists == null)
            {
                // Create schema_version table
                using var createCmd = connection.CreateCommand();
                createCmd.CommandText = @"
                    CREATE TABLE schema_version (
                        version INTEGER PRIMARY KEY,
                        applied_at TEXT NOT NULL
                    )";
                await createCmd.ExecuteNonQueryAsync();
                
                // Insert initial version
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO schema_version (version, applied_at) VALUES (1, @date)";
                insertCmd.Parameters.Add(new SqliteParameter("@date", DateTime.UtcNow.ToString("O")));
                await insertCmd.ExecuteNonQueryAsync();
                
                return 1;
            }
            
            // Get current version
            using var versionCmd = connection.CreateCommand();
            versionCmd.CommandText = "SELECT version FROM schema_version ORDER BY version DESC LIMIT 1";
            var version = await versionCmd.ExecuteScalarAsync();
            
            return version != null ? Convert.ToInt32(version) : 1;
        }
        catch
        {
            return 1; // Assume version 1 if table doesn't exist or error
        }
    }
    
    private static async Task SetSchemaVersionAsync(OneManVanDbContext context, int version)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO schema_version (version, applied_at) VALUES (@version, @date)";
        cmd.Parameters.Add(new SqliteParameter("@version", version));
        cmd.Parameters.Add(new SqliteParameter("@date", DateTime.UtcNow.ToString("O")));
        await cmd.ExecuteNonQueryAsync();
    }
    
    /// <summary>
    /// Migration v2: Add JobWorkers table and EmployeeId to TimeEntries
    /// </summary>
    private static async Task ApplyMigration_v2_JobWorkerTimeTracking(OneManVanDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        
        System.Diagnostics.Debug.WriteLine("[Migration] Applying v2: JobWorker Time Tracking");
        
        // Check if columns already exist (in case of partial migration)
        var employeeIdExists = await ColumnExistsAsync(connection, "TimeEntries", "EmployeeId");
        
        if (!employeeIdExists)
        {
            // Add EmployeeId to TimeEntries
            using var cmd1 = connection.CreateCommand();
            cmd1.CommandText = "ALTER TABLE TimeEntries ADD COLUMN EmployeeId INTEGER";
            await cmd1.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("[Migration] Added EmployeeId to TimeEntries");
        }
        
        // Create JobWorkers table if it doesn't exist
        using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = @"
            CREATE TABLE IF NOT EXISTS JobWorkers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                JobId INTEGER NOT NULL,
                EmployeeId INTEGER NOT NULL,
                Role TEXT,
                AssignedAt TEXT NOT NULL,
                CompletedAt TEXT,
                HourlyRate REAL,
                EstimatedHours REAL,
                ActualHours REAL,
                Notes TEXT,
                FOREIGN KEY (JobId) REFERENCES Jobs(Id),
                FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
            )";
        await cmd2.ExecuteNonQueryAsync();
        System.Diagnostics.Debug.WriteLine("[Migration] Created JobWorkers table");
        
        // Add InvoicePricing columns if they don't exist
        if (!await ColumnExistsAsync(connection, "Invoices", "CustomPriceTotal"))
        {
            using var cmd3 = connection.CreateCommand();
            cmd3.CommandText = "ALTER TABLE Invoices ADD COLUMN CustomPriceTotal REAL";
            await cmd3.ExecuteNonQueryAsync();
        }
        
        if (!await ColumnExistsAsync(connection, "Invoices", "CustomPriceDescription"))
        {
            using var cmd4 = connection.CreateCommand();
            cmd4.CommandText = "ALTER TABLE Invoices ADD COLUMN CustomPriceDescription TEXT";
            await cmd4.ExecuteNonQueryAsync();
        }
        
        // Add VendorId to Expenses if it doesn't exist
        if (!await ColumnExistsAsync(connection, "Expenses", "VendorId"))
        {
            using var cmd5 = connection.CreateCommand();
            cmd5.CommandText = "ALTER TABLE Expenses ADD COLUMN VendorId INTEGER";
            await cmd5.ExecuteNonQueryAsync();
        }
        
        System.Diagnostics.Debug.WriteLine("[Migration] v2 migration complete");
    }
    
    private static async Task<bool> ColumnExistsAsync(System.Data.Common.DbConnection connection, string tableName, string columnName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName})";
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var colName = reader.GetString(1); // Column name is at index 1
            if (colName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }
}
