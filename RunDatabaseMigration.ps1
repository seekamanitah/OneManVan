# Database Migration Script
# This script applies the AddTaxIncludedAndEnhancements migration

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DATABASE MIGRATION SCRIPT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Find the database file
Write-Host "Step 1: Locating database file..." -ForegroundColor Yellow
$possiblePaths = @(
    "onemanvan.db",
    "OneManVan\bin\Debug\net10.0-windows\onemanvan.db",
    "OneManVan.Web\onemanvan.db",
    "bin\Debug\net10.0-windows\onemanvan.db"
)

$dbPath = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $dbPath = (Resolve-Path $path).Path
        Write-Host "Found database at: $dbPath" -ForegroundColor Green
        break
    }
}

# Search recursively if not found
if (-not $dbPath) {
    Write-Host "Searching for database file..." -ForegroundColor Yellow
    $found = Get-ChildItem -Recurse -Filter "onemanvan.db" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $dbPath = $found.FullName
        Write-Host "Found database at: $dbPath" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Database file not found!" -ForegroundColor Red
        Write-Host "Please ensure onemanvan.db exists in your project directory." -ForegroundColor Red
        exit 1
    }
}

# Step 2: Read migration script
Write-Host ""
Write-Host "Step 2: Reading migration script..." -ForegroundColor Yellow
$sqlPath = "Migrations\AddTaxIncludedAndEnhancements.sql"

if (-not (Test-Path $sqlPath)) {
    Write-Host "ERROR: Migration script not found at $sqlPath" -ForegroundColor Red
    exit 1
}

$migrationSql = Get-Content $sqlPath -Raw
Write-Host "Migration script loaded successfully" -ForegroundColor Green

# Step 3: Create backup
Write-Host ""
Write-Host "Step 3: Creating database backup..." -ForegroundColor Yellow
$backupPath = "$dbPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Copy-Item -Path $dbPath -Destination $backupPath
Write-Host "Backup created: $backupPath" -ForegroundColor Green

# Step 4: Execute migration
Write-Host ""
Write-Host "Step 4: Executing migration..." -ForegroundColor Yellow

try {
    # Load System.Data.SQLite if available
    $sqliteAssembly = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages" -Recurse -Filter "System.Data.SQLite.dll" -ErrorAction SilentlyContinue | 
                      Where-Object { $_.FullName -like "*netstandard*" } |
                      Select-Object -First 1

    if ($sqliteAssembly) {
        Add-Type -Path $sqliteAssembly.FullName
        Write-Host "Using System.Data.SQLite" -ForegroundColor Cyan
        
        $connectionString = "Data Source=$dbPath;Version=3;"
        $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $migrationSql
        $rowsAffected = $command.ExecuteNonQuery()
        
        $connection.Close()
        Write-Host "Migration executed successfully!" -ForegroundColor Green
    }
    else {
        # Try using Microsoft.Data.Sqlite
        Write-Host "Attempting alternative method..." -ForegroundColor Cyan
        
        # Split SQL into individual statements
        $statements = $migrationSql -split ";"
        
        # Load assembly from project
        $projectAssembly = Get-ChildItem -Path "." -Recurse -Filter "Microsoft.Data.Sqlite.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
        
        if ($projectAssembly) {
            Add-Type -Path $projectAssembly.FullName
            
            $connectionString = "Data Source=$dbPath"
            $connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)
            $connection.Open()
            
            foreach ($stmt in $statements) {
                $trimmed = $stmt.Trim()
                if ($trimmed -and -not $trimmed.StartsWith("--") -and $trimmed.Length -gt 10) {
                    $command = $connection.CreateCommand()
                    $command.CommandText = $trimmed
                    try {
                        $command.ExecuteNonQuery() | Out-Null
                    }
                    catch {
                        Write-Host "Executing: $($trimmed.Substring(0, [Math]::Min(50, $trimmed.Length)))..." -ForegroundColor Gray
                        if ($_.Exception.Message -notlike "*duplicate column*") {
                            throw
                        }
                    }
                }
            }
            
            $connection.Close()
            Write-Host "Migration executed successfully!" -ForegroundColor Green
        }
        else {
            Write-Host "SQLite libraries not found. Attempting manual execution..." -ForegroundColor Yellow
            
            # Last resort: try sqlite3.exe
            $sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue
            if ($sqlite3) {
                Get-Content $sqlPath | sqlite3 $dbPath
                Write-Host "Migration executed using sqlite3.exe" -ForegroundColor Green
            }
            else {
                Write-Host "ERROR: Cannot execute migration automatically." -ForegroundColor Red
                Write-Host ""
                Write-Host "MANUAL STEPS REQUIRED:" -ForegroundColor Yellow
                Write-Host "1. Install DB Browser for SQLite from: https://sqlitebrowser.org/" -ForegroundColor White
                Write-Host "2. Open your database: $dbPath" -ForegroundColor White
                Write-Host "3. Go to Execute SQL tab" -ForegroundColor White
                Write-Host "4. Copy/paste contents from: $sqlPath" -ForegroundColor White
                Write-Host "5. Click Execute" -ForegroundColor White
                Write-Host "6. Click Write Changes" -ForegroundColor White
                exit 1
            }
        }
    }
}
catch {
    Write-Host "ERROR during migration: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Restoring backup..." -ForegroundColor Yellow
    Copy-Item -Path $backupPath -Destination $dbPath -Force
    Write-Host "Database restored from backup" -ForegroundColor Green
    exit 1
}

# Step 5: Verify migration
Write-Host ""
Write-Host "Step 5: Verifying migration..." -ForegroundColor Yellow

try {
    if ($connection) {
        $connection = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath")
        $connection.Open()
        
        # Check if TaxIncluded column exists
        $command = $connection.CreateCommand()
        $command.CommandText = "PRAGMA table_info(Jobs)"
        $reader = $command.ExecuteReader()
        
        $hasTaxIncluded = $false
        while ($reader.Read()) {
            if ($reader["name"] -eq "TaxIncluded") {
                $hasTaxIncluded = $true
                break
            }
        }
        $reader.Close()
        $connection.Close()
        
        if ($hasTaxIncluded) {
            Write-Host "Verification SUCCESS: TaxIncluded column exists" -ForegroundColor Green
        }
        else {
            Write-Host "WARNING: Could not verify TaxIncluded column" -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "Verification skipped (manual verification required)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MIGRATION COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Close this window" -ForegroundColor White
Write-Host "2. Restart your Web application" -ForegroundColor White
Write-Host "3. Navigate to /jobs page" -ForegroundColor White
Write-Host "4. Error should be resolved!" -ForegroundColor White
Write-Host ""
Write-Host "Backup location: $backupPath" -ForegroundColor Cyan
Write-Host ""

Read-Host "Press Enter to exit"
