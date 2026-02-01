// Database table checker and migration applier
using System;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        var dbPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "OneManVan.Web", "Data", "business.db"));
        var migrationsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Migrations"));
        
        if (!File.Exists(dbPath))
        {
            Console.WriteLine($"Database not found: {dbPath}");
            return;
        }
        
        Console.WriteLine($"Database: {dbPath}");
        Console.WriteLine();
        
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        
        // Get existing tables
        var tables = GetTables(conn);
        Console.WriteLine($"Found {tables.Count} tables");
        
        // Check for missing tables
        var expectedTables = new[] {
            "Employees", "EmployeeTimeLogs", "EmployeePerformanceNotes", "EmployeePayments"
        };
        
        var missing = expectedTables.Where(e => !tables.Any(t => t.Equals(e, StringComparison.OrdinalIgnoreCase))).ToList();
        
        if (missing.Count == 0)
        {
            Console.WriteLine("\nAll expected tables exist!");
            return;
        }
        
        Console.WriteLine($"\nMissing tables: {string.Join(", ", missing)}");
        
        // Apply migration if --apply flag is passed
        if (args.Contains("--apply"))
        {
            Console.WriteLine("\nApplying migrations directly...");
            
            // Create tables directly
            if (missing.Contains("Employees"))
            {
                Console.WriteLine("Creating Employees table...");
                ExecuteNonQuery(conn, @"
                    CREATE TABLE IF NOT EXISTS Employees (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FirstName TEXT NOT NULL,
                        LastName TEXT NOT NULL,
                        Type INTEGER NOT NULL DEFAULT 0,
                        Status INTEGER NOT NULL DEFAULT 0,
                        StartDate TEXT NOT NULL DEFAULT (datetime('now')),
                        TerminationDate TEXT,
                        Address TEXT,
                        City TEXT,
                        State TEXT,
                        ZipCode TEXT,
                        Phone TEXT,
                        Email TEXT,
                        EmergencyContactName TEXT,
                        EmergencyContactPhone TEXT,
                        EmergencyContactRelationship TEXT,
                        TaxId TEXT,
                        IsEin INTEGER NOT NULL DEFAULT 0,
                        PayRateType INTEGER NOT NULL DEFAULT 0,
                        PayRate REAL NOT NULL DEFAULT 0,
                        OvertimeMultiplier REAL NOT NULL DEFAULT 1.5,
                        OvertimeThresholdHours INTEGER NOT NULL DEFAULT 40,
                        PaymentMethod INTEGER NOT NULL DEFAULT 0,
                        PtoBalanceHours REAL NOT NULL DEFAULT 0,
                        PtoAccrualPerYear REAL NOT NULL DEFAULT 0,
                        LastPaidDate TEXT,
                        LastPaidAmount REAL,
                        LastPaidMethod TEXT,
                        CompanyName TEXT,
                        ContactPerson TEXT,
                        CompanyEin TEXT,
                        ServiceProvided TEXT,
                        InvoiceRequired INTEGER NOT NULL DEFAULT 0,
                        InsuranceOnFile INTEGER NOT NULL DEFAULT 0,
                        HasW4 INTEGER NOT NULL DEFAULT 0,
                        W4DocumentId INTEGER,
                        HasI9 INTEGER NOT NULL DEFAULT 0,
                        I9DocumentId INTEGER,
                        Has1099Setup INTEGER NOT NULL DEFAULT 0,
                        W9DocumentId INTEGER,
                        InsuranceCertDocumentId INTEGER,
                        BackgroundCheckDate TEXT,
                        BackgroundCheckResult TEXT,
                        DrugTestDate TEXT,
                        DrugTestResult TEXT,
                        Notes TEXT,
                        PhotoPath TEXT,
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                        UpdatedAt TEXT
                    )");
            }
            
            if (missing.Contains("EmployeeTimeLogs"))
            {
                Console.WriteLine("Creating EmployeeTimeLogs table...");
                ExecuteNonQuery(conn, @"
                    CREATE TABLE IF NOT EXISTS EmployeeTimeLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EmployeeId INTEGER NOT NULL,
                        JobId INTEGER,
                        Date TEXT NOT NULL,
                        HoursWorked REAL NOT NULL DEFAULT 0,
                        IsOvertime INTEGER NOT NULL DEFAULT 0,
                        StartTime TEXT,
                        EndTime TEXT,
                        BreakMinutes INTEGER NOT NULL DEFAULT 0,
                        Notes TEXT,
                        IsApproved INTEGER NOT NULL DEFAULT 0,
                        ApprovedBy TEXT,
                        ApprovedAt TEXT,
                        IsPaid INTEGER NOT NULL DEFAULT 0,
                        PaymentId INTEGER,
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE
                    )");
            }
            
            if (missing.Contains("EmployeePerformanceNotes"))
            {
                Console.WriteLine("Creating EmployeePerformanceNotes table...");
                ExecuteNonQuery(conn, @"
                    CREATE TABLE IF NOT EXISTS EmployeePerformanceNotes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EmployeeId INTEGER NOT NULL,
                        JobId INTEGER,
                        Date TEXT NOT NULL,
                        Category INTEGER NOT NULL DEFAULT 7,
                        Rating INTEGER,
                        Note TEXT NOT NULL,
                        CreatedBy TEXT,
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE
                    )");
            }
            
            if (missing.Contains("EmployeePayments"))
            {
                Console.WriteLine("Creating EmployeePayments table...");
                ExecuteNonQuery(conn, @"
                    CREATE TABLE IF NOT EXISTS EmployeePayments (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EmployeeId INTEGER NOT NULL,
                        PayDate TEXT NOT NULL,
                        GrossAmount REAL NOT NULL DEFAULT 0,
                        Deductions REAL NOT NULL DEFAULT 0,
                        NetAmount REAL NOT NULL DEFAULT 0,
                        PaymentMethod INTEGER NOT NULL DEFAULT 0,
                        PeriodStart TEXT,
                        PeriodEnd TEXT,
                        RegularHours REAL NOT NULL DEFAULT 0,
                        OvertimeHours REAL NOT NULL DEFAULT 0,
                        RegularPay REAL NOT NULL DEFAULT 0,
                        OvertimePay REAL NOT NULL DEFAULT 0,
                        ReferenceNumber TEXT,
                        Notes TEXT,
                        IsBonus INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE
                    )");
            }
            
            // Create indexes
            Console.WriteLine("Creating indexes...");
            ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_Employees_Status ON Employees(Status)");
            ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_Employees_Type ON Employees(Type)");
            ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_EmployeeTimeLogs_EmployeeId ON EmployeeTimeLogs(EmployeeId)");
            ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_EmployeeTimeLogs_Date ON EmployeeTimeLogs(Date)");
            ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_EmployeePerformanceNotes_EmployeeId ON EmployeePerformanceNotes(EmployeeId)");
            ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_EmployeePayments_EmployeeId ON EmployeePayments(EmployeeId)");
            
            Console.WriteLine("Migration complete!");
            
            // Re-check tables
            tables = GetTables(conn);
            Console.WriteLine($"\nAfter migration: {tables.Count} tables");
            missing = expectedTables.Where(e => !tables.Any(t => t.Equals(e, StringComparison.OrdinalIgnoreCase))).ToList();
            if (missing.Count == 0)
                Console.WriteLine("SUCCESS: All Employee tables now exist!");
            else
                Console.WriteLine($"Still missing: {string.Join(", ", missing)}");
        }
        else
        {
            Console.WriteLine("\nTo apply missing migrations, run with --apply flag");
        }
    }
    
    static void ExecuteNonQuery(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }
    
    static List<string> GetTables(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        using var reader = cmd.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read()) tables.Add(reader.GetString(0));
        return tables;
    }
}
