# Fix Local Database Script
# Run this to ensure the local SQLite database has all required tables

$ErrorActionPreference = "Stop"

Write-Host "=== Local Database Fix Script ===" -ForegroundColor Cyan
Write-Host ""

# Find the database file
$dbPath = "OneManVan.Web\AppData\OneManVan.db"

if (Test-Path $dbPath) {
    Write-Host "Found database at: $dbPath" -ForegroundColor Green
    
    # Check if CompanySettings table exists
    $checkQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='CompanySettings';"
    
    try {
        $result = sqlite3 $dbPath $checkQuery 2>$null
        
        if ($result -eq "CompanySettings") {
            Write-Host "CompanySettings table already exists!" -ForegroundColor Green
        } else {
            Write-Host "CompanySettings table is missing. Creating it..." -ForegroundColor Yellow
            
            $createSql = @"
CREATE TABLE IF NOT EXISTS CompanySettings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyName TEXT NOT NULL DEFAULT 'OneManVan',
    Tagline TEXT,
    Address TEXT,
    City TEXT,
    State TEXT,
    ZipCode TEXT,
    Email TEXT,
    Phone TEXT,
    Website TEXT,
    LogoBase64 TEXT,
    LogoFileName TEXT,
    TaxId TEXT,
    LicenseNumber TEXT,
    InsuranceNumber TEXT,
    PaymentTerms TEXT DEFAULT 'Payment due within 30 days of invoice date.',
    DocumentFooter TEXT DEFAULT 'Thank you for your business!',
    BankDetails TEXT,
    GoogleCalendarSettings TEXT,
    MapsApiKey TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

INSERT OR IGNORE INTO CompanySettings (Id, CompanyName, PaymentTerms, DocumentFooter)
VALUES (1, 'OneManVan', 'Payment due within 30 days of invoice date.', 'Thank you for your business!');
"@
            $createSql | sqlite3 $dbPath
            Write-Host "CompanySettings table created successfully!" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "sqlite3 command not found. You can either:" -ForegroundColor Yellow
        Write-Host "  1. Delete the database file and restart the app (it will be recreated)" -ForegroundColor White
        Write-Host "  2. Install sqlite3 and run this script again" -ForegroundColor White
        Write-Host ""
        Write-Host "To delete and recreate:" -ForegroundColor Cyan
        Write-Host "  Remove-Item '$dbPath'" -ForegroundColor White
    }
}
else {
    Write-Host "Database file not found at: $dbPath" -ForegroundColor Yellow
    Write-Host "This is normal - the database will be created when you run the app." -ForegroundColor White
    Write-Host ""
    Write-Host "Run the app with: dotnet run --project OneManVan.Web" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== Script Complete ===" -ForegroundColor Cyan
