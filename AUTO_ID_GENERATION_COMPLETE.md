# Auto-ID Generation Implementation Summary

## ? What Was Added

### 1. **Model Changes**

#### Asset Model (`OneManVan.Shared/Models/Asset.cs`)
```csharp
/// <summary>
/// Auto-generated asset number (e.g., AST-0001).
/// </summary>
[MaxLength(20)]
public string? AssetNumber { get; set; }
```

#### InventoryItem Model (`OneManVan.Shared/Models/InventoryItem.cs`)
```csharp
/// <summary>
/// Auto-generated inventory number (e.g., INV-0001).
/// </summary>
[MaxLength(20)]
public string? InventoryNumber { get; set; }
```

---

### 2. **Import Service Changes**

#### Customer Import ? **COMPLETE**
- **Field**: `CustomerNumber`
- **Format**: `C-0001`, `C-0002`, etc.
- **Status**: Auto-generates when importing

#### Product Import ? **COMPLETE**
- **Field**: `SKU`
- **Format**: `PROD-0001`, `PROD-0002`, etc.
- **Status**: Auto-generates when importing if not provided

---

## ?? Entities Summary

| Entity | ID Field | Format | Auto-Gen | Status |
|--------|----------|--------|----------|--------|
| **Customer** | CustomerNumber | C-0001 | ? Yes | ? **Complete** |
| **Product** | SKU | PROD-0001 | ? Yes | ? **Complete** |
| **Asset** | AssetNumber | AST-0001 | ? When import implemented | ?? Field added |
| **Inventory** | InventoryNumber | INV-0001 | ? When import implemented | ?? Field added |
| **Job** | JobNumber | JOB-0001 | ? When import implemented | ?? Stub |
| **Invoice** | InvoiceNumber | INV-0001 | ? When import implemented | ?? Stub |
| **Estimate** | EstimateNumber | EST-0001 | ? When import implemented | ?? Stub |

---

## ?? Database Migration Required

You'll need to add database columns for the new fields:

### Migration SQL
```sql
-- Add AssetNumber to Assets table
ALTER TABLE Assets
ADD COLUMN AssetNumber NVARCHAR(20) NULL;

-- Add InventoryNumber to InventoryItems table
ALTER TABLE InventoryItems
ADD COLUMN InventoryNumber NVARCHAR(20) NULL;

-- Optional: Generate numbers for existing records
UPDATE Assets
SET AssetNumber = CONCAT('AST-', LPAD(Id, 4, '0'))
WHERE AssetNumber IS NULL;

UPDATE InventoryItems
SET InventoryNumber = CONCAT('INV-', LPAD(Id, 4, '0'))
WHERE InventoryNumber IS NULL;
```

---

## ?? Auto-Generation Pattern

When implementing import for other entities, follow this pattern:

```csharp
await using var db = await _dbFactory.CreateDbContextAsync();

// Get the next number for auto-generation
var existingNumbers = await db.[EntityTable]
    .Where(e => e.[NumberField] != null && e.[NumberField].StartsWith("[PREFIX]-"))
    .Select(e => e.[NumberField])
    .ToListAsync();

var nextNumber = 1;
if (existingNumbers.Any())
{
    nextNumber = existingNumbers
        .Select(n => int.TryParse(n?.Substring([PREFIX_LENGTH + 1]), out int num) ? num : 0)
        .DefaultIfEmpty(0)
        .Max() + 1;
}

// Inside the import loop for new records:
if (string.IsNullOrEmpty(entity.[NumberField]))
{
    entity.[NumberField] = $"[PREFIX]-{nextNumber:D4}";
    nextNumber++;
}
```

### Example for Jobs:
```csharp
var existingJobNumbers = await db.Jobs
    .Where(j => j.JobNumber != null && j.JobNumber.StartsWith("JOB-"))
    .Select(j => j.JobNumber)
    .ToListAsync();

var nextJobNumber = 1;
if (existingJobNumbers.Any())
{
    nextJobNumber = existingJobNumbers
        .Select(n => int.TryParse(n?.Substring(4), out int num) ? num : 0)
        .Max() + 1;
}

// In loop
if (string.IsNullOrEmpty(job.JobNumber))
{
    job.JobNumber = $"JOB-{nextJobNumber:D4}";
    nextJobNumber++;
}
```

---

## ?? Benefits

| Feature | Benefit |
|---------|---------|
| **Automatic IDs** | No manual ID entry required during import |
| **Sequential** | Continues from highest existing number |
| **No Conflicts** | Each import generates unique IDs |
| **Future-Proof** | Pattern ready for when other entity imports are implemented |

---

## ?? Next Steps

1. ? **Done**: Customer and Product imports have auto-generation
2. ?? **Deploy**: Run database migration to add new fields
3. ? **Future**: When implementing full import services for Jobs, Invoices, Estimates, Assets, and Inventory, use the pattern above

---

## ?? Testing

### Test Customer Import
```csv
Name,Email,Phone
John Doe,john@example.com,555-1234
Jane Smith,jane@example.com,555-5678
```
**Result**: `C-0001`, `C-0002`

### Test Product Import
```csv
Name,Manufacturer,ModelNumber,Price,Cost
HVAC Filter,FilterCo,F-123,25.00,10.00
Thermostat,ThermoTech,TH-456,150.00,75.00
```
**Result**: `PROD-0001`, `PROD-0002` (if SKU not provided)

---

*Auto-ID generation ensures consistent numbering across all imports!* ?
