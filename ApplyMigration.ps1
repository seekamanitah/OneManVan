# ApplyMigration.ps1
$dbPath = "onemanvan.db"
$sqlPath = "Migrations\AddTaxIncludedAndEnhancements.sql"

if (-not (Test-Path $dbPath)) {
    Write-Host "ERROR: Database not found. Searching..." -ForegroundColor Red
    $found = Get-ChildItem -Recurse -Filter "onemanvan.db" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $dbPath = $found.FullName
        Write-Host "Found database at: $dbPath" -ForegroundColor Green
    } else {
        Write-Host "Database not found anywhere!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Reading SQL migration script..." -ForegroundColor Yellow
$sql = Get-Content $sqlPath -Raw

Write-Host "Executing migration on database: $dbPath" -ForegroundColor Yellow

# Try using SQLite command if available
try {
    $sql | sqlite3 $dbPath
    Write-Host "✅ Migration completed successfully!" -ForegroundColor Green
} catch {
    Write-Host "sqlite3 not found, trying alternative method..." -ForegroundColor Yellow
    
    # Alternative: Write SQL to temp file and use .NET
    $tempFile = [System.IO.Path]::GetTempFileName()
    $sql | Out-File -FilePath $tempFile -Encoding UTF8
    
    # Execute using connection string
    $connectionString = "Data Source=$dbPath"
    
    Add-Type -AssemblyName "System.Data"
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $sql
        $command.ExecuteNonQuery() | Out-Null
        Write-Host "✅ Migration completed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "❌ Migration failed: $_" -ForegroundColor Red
    } finally {
        $connection.Close()
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

Write-Host "`nRestart your Web application now." -ForegroundColor Cyan