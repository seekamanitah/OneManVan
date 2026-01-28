# OneManVan FSM - Data Relationships & Entity Reference Guide

**Purpose:** Complete reference for all database entities, relationships, and field names.  
**Use this guide when:** Building UI pages, creating forms, writing queries, ensuring consistency.

---

## ?? Entity Relationship Diagram (Conceptual)

```
                                    ???????????????
                                    ?   Company   ?
                                    ?  (B2B Hub)  ?
                                    ???????????????
                                           ? 1:M
                    ???????????????????????????????????????????????
                    ?                      ?                      ?
                    ?                      ?                      ?
            ?????????????          ?????????????          ?????????????????
            ? Customer  ????????????   Site    ????????????    Asset      ?
            ?(Contacts) ?   1:M    ?(Locations)?   1:M    ?  (Equipment)  ?
            ?????????????          ?????????????          ?????????????????
                  ?                      ?                        ?
                  ? 1:M                  ?                        ? 1:M
                  ?                      ?                        ?
        ???????????????????              ?              ???????????????????
        ?    Estimate     ????????????????              ? WarrantyClaim   ?
        ?   (Quotes)      ?                             ?  (Warranty)     ?
        ???????????????????                             ???????????????????
                 ?
                 ? 1:1 (converts to)
                 ?
        ???????????????????
        ?      Job        ?
        ? (Service Work)  ?
        ???????????????????
                 ?
                 ? 1:1 (generates)
                 ?
        ???????????????????          ???????????????????
        ?    Invoice      ????????????    Payment      ?
        ?   (Billing)     ?   1:M    ?  (Transactions) ?
        ???????????????????          ???????????????????

        ???????????????????          ???????????????????
        ?    Product      ???????????? InventoryItem   ?
        ?  (Catalog)      ?   1:1    ?   (Stock)       ?
        ???????????????????          ???????????????????

        ???????????????????????????????????????????????
        ?           ServiceAgreement                   ?
        ?    (Recurring Maintenance Contracts)         ?
        ???????????????????????????????????????????????

        ???????????????????????????????????????????????
        ?              QuickNote (NEW)                 ?
        ?       (Random notes, reminders)              ?
        ???????????????????????????????????????????????
```

---

## ?? Core Entities

### 1. Customer
**Table:** `Customers`  
**Purpose:** Contact/person who receives services

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `FirstName` | string(100) | ? | First name |
| `LastName` | string(100) | ? | Last name |
| `DisplayName` | string(200) | - | Computed: "FirstName LastName" |
| `Email` | string(200) | - | Email address |
| `Phone` | string(20) | - | Primary phone |
| `MobilePhone` | string(20) | - | Mobile/cell |
| `WorkPhone` | string(20) | - | Work phone |
| `HomeAddress` | string(300) | - | Home street address |
| `HomeCity` | string(100) | - | Home city |
| `HomeState` | string(50) | - | Home state |
| `HomeZipCode` | string(20) | - | Home ZIP |
| `Notes` | string(2000) | - | General notes |
| `PreferredContact` | enum | - | Email/Phone/Text |
| `CustomerType` | enum | - | Residential/Commercial |
| `IsActive` | bool | - | Active status |
| `CreatedAt` | DateTime | - | Created timestamp |
| `UpdatedAt` | DateTime | - | Last updated |

**Relationships:**
- `Sites` ? 1:M ? Site[]
- `Assets` ? 1:M ? Asset[] (optional, via Site preferred)
- `Estimates` ? 1:M ? Estimate[]
- `Jobs` ? 1:M ? Job[]
- `Invoices` ? 1:M ? Invoice[]

---

### 2. Company
**Table:** `Companies`  
**Purpose:** B2B organizations (property management, commercial accounts)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `Name` | string(200) | ? | Company name |
| `CompanyType` | enum | - | PropertyManagement/Commercial/Contractor |
| `TaxId` | string(20) | - | EIN/Tax ID |
| `Website` | string(200) | - | Website URL |
| `Phone` | string(20) | - | Main phone |
| `Email` | string(200) | - | Main email |
| `BillingAddress` | string(300) | - | Billing street |
| `BillingCity` | string(100) | - | Billing city |
| `BillingState` | string(50) | - | Billing state |
| `BillingZipCode` | string(20) | - | Billing ZIP |
| `Notes` | string(2000) | - | Notes |
| `IsActive` | bool | - | Active status |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Sites` ? 1:M ? Site[]
- `Assets` ? 1:M ? Asset[]
- `Customers` ? M:M ? CustomerCompanyRole[]

---

### 3. Site
**Table:** `Sites`  
**Purpose:** Physical service locations (addresses)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `CustomerId` | int | FK | Owner customer |
| `CompanyId` | int? | FK | Optional company owner |
| `SiteName` | string(100) | - | Friendly name ("Main Office") |
| `SiteNumber` | string(20) | - | Auto-generated (S-0001) |
| `Address` | string(300) | ? | Street address |
| `Address2` | string(50) | - | Unit/Suite |
| `City` | string(100) | - | City |
| `State` | string(50) | - | State |
| `ZipCode` | string(20) | - | ZIP code |
| `Latitude` | decimal(10,7) | - | GPS lat |
| `Longitude` | decimal(10,7) | - | GPS long |
| `PropertyType` | enum | - | Residential/Commercial/Industrial |
| `SquareFootage` | int? | - | Square feet |
| `YearBuilt` | int? | - | Year constructed |
| `GateCode` | string(20) | - | Gate access code |
| `LockboxCode` | string(20) | - | Lockbox code |
| `AccessInstructions` | string(1000) | - | Access notes |
| `SiteContactName` | string(100) | - | On-site contact |
| `SiteContactPhone` | string(20) | - | Contact phone |
| `HasDog` | bool | - | Dog on premises |
| `HasGate` | bool | - | Gated property |
| `IsPrimary` | bool | - | Primary site for customer |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Customer` ? M:1 ? Customer
- `Company` ? M:1 ? Company?
- `Assets` ? 1:M ? Asset[]
- `Jobs` ? 1:M ? Job[]

---

### 4. Asset
**Table:** `Assets`  
**Purpose:** HVAC equipment being serviced

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `CustomerId` | int? | FK | Direct customer (optional) |
| `SiteId` | int? | FK | Site location (preferred) |
| `CompanyId` | int? | FK | Company owner (optional) |
| `AssetName` | string(200) | - | Friendly name ("Main Floor AC") |
| `AssetTag` | string(50) | - | Barcode/QR tag |
| `Serial` | string(100) | ? | Serial number |
| `Brand` | string(100) | - | Manufacturer (Carrier, Trane) |
| `Model` | string(100) | - | Model number |
| `Nickname` | string(100) | - | Location nickname |
| `EquipmentType` | enum | - | AC/Furnace/HeatPump/etc |
| `FuelType` | enum | - | Gas/Electric/Propane |
| `BtuRating` | int? | - | BTU capacity |
| `TonnageX10` | int? | - | Tonnage * 10 (25 = 2.5 ton) |
| `SeerRating` | decimal(4,1) | - | SEER efficiency |
| `InstallDate` | DateTime? | - | Installation date |
| `WarrantyExpiration` | DateTime? | - | Warranty end date |
| `LastServiceDate` | DateTime? | - | Last service |
| `Condition` | enum | - | Excellent/Good/Fair/Poor |
| `Notes` | string(2000) | - | Notes |
| `IsActive` | bool | - | Active status |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Customer` ? M:1 ? Customer?
- `Site` ? M:1 ? Site?
- `Company` ? M:1 ? Company?
- `Jobs` ? 1:M ? Job[]
- `WarrantyClaims` ? 1:M ? WarrantyClaim[]

---

### 5. Estimate
**Table:** `Estimates`  
**Purpose:** Quotes/proposals for work

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `EstimateNumber` | string(20) | - | Auto-generated (E-2024-0001) |
| `CustomerId` | int | FK | Customer |
| `SiteId` | int? | FK | Service location |
| `AssetId` | int? | FK | Related asset |
| `Title` | string(200) | ? | Estimate title |
| `Description` | string(2000) | - | Detailed description |
| `Status` | enum | - | Draft/Sent/Approved/Declined/Expired |
| `Subtotal` | decimal(10,2) | - | Before tax |
| `TaxRate` | decimal(5,2) | - | Tax percentage |
| `TaxAmount` | decimal(10,2) | - | Tax amount |
| `Total` | decimal(10,2) | - | Grand total |
| `ValidUntil` | DateTime? | - | Expiration date |
| `SentAt` | DateTime? | - | When sent to customer |
| `ApprovedAt` | DateTime? | - | When approved |
| `Notes` | string(2000) | - | Internal notes |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Customer` ? M:1 ? Customer
- `Site` ? M:1 ? Site?
- `Asset` ? M:1 ? Asset?
- `EstimateLines` ? 1:M ? EstimateLine[]
- `Job` ? 1:1 ? Job? (when converted)

---

### 6. Job
**Table:** `Jobs`  
**Purpose:** Scheduled service work

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `JobNumber` | string(20) | - | Auto-generated (J-2024-0001) |
| `EstimateId` | int? | FK | Source estimate |
| `CustomerId` | int | FK | Customer |
| `SiteId` | int? | FK | Service location |
| `AssetId` | int? | FK | Related asset |
| `ServiceAgreementId` | int? | FK | Related agreement |
| `Title` | string(200) | ? | Job title |
| `Description` | string(2000) | - | Work description |
| `JobType` | enum | - | ServiceCall/Repair/Install/Maintenance |
| `Priority` | enum | - | Low/Normal/High/Emergency |
| `Status` | enum | - | Draft/Scheduled/InProgress/Completed/Cancelled |
| `ScheduledDate` | DateTime? | - | Scheduled date |
| `ScheduledEndDate` | DateTime? | - | Scheduled end |
| `ArrivalWindowStart` | TimeSpan? | - | Arrival window start |
| `ArrivalWindowEnd` | TimeSpan? | - | Arrival window end |
| `ActualStartTime` | DateTime? | - | When work started |
| `ActualEndTime` | DateTime? | - | When work ended |
| `TechnicianNotes` | string(2000) | - | Technician notes |
| `CustomerSignature` | string | - | Base64 signature |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Customer` ? M:1 ? Customer
- `Site` ? M:1 ? Site?
- `Asset` ? M:1 ? Asset?
- `Estimate` ? M:1 ? Estimate?
- `ServiceAgreement` ? M:1 ? ServiceAgreement?
- `TimeEntries` ? 1:M ? TimeEntry[]
- `Invoice` ? 1:1 ? Invoice?

---

### 7. Invoice
**Table:** `Invoices`  
**Purpose:** Billing documents

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `InvoiceNumber` | string(20) | - | Auto-generated (INV-2024-0001) |
| `JobId` | int? | FK | Source job |
| `CustomerId` | int | FK | Customer |
| `Status` | enum | - | Draft/Sent/Paid/Overdue/Cancelled |
| `InvoiceDate` | DateTime | - | Invoice date |
| `DueDate` | DateTime | - | Payment due date |
| `Subtotal` | decimal(10,2) | - | Before tax |
| `TaxRate` | decimal(5,2) | - | Tax percentage |
| `TaxAmount` | decimal(10,2) | - | Tax amount |
| `Total` | decimal(10,2) | - | Grand total |
| `AmountPaid` | decimal(10,2) | - | Total paid |
| `BalanceDue` | decimal(10,2) | - | Remaining balance |
| `Notes` | string(2000) | - | Invoice notes |
| `SentAt` | DateTime? | - | When sent |
| `PaidAt` | DateTime? | - | When fully paid |
| `IsDeleted` | bool | - | Soft delete flag |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Customer` ? M:1 ? Customer
- `Job` ? M:1 ? Job?
- `InvoiceLines` ? 1:M ? InvoiceLineItem[]
- `Payments` ? 1:M ? Payment[]

---

### 8. Product
**Table:** `Products`  
**Purpose:** Parts/materials catalog

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `Sku` | string(50) | - | SKU/Part number |
| `Name` | string(200) | ? | Product name |
| `Description` | string(1000) | - | Description |
| `Category` | string(100) | - | Category |
| `Brand` | string(100) | - | Brand/Manufacturer |
| `UnitCost` | decimal(10,2) | - | Cost price |
| `UnitPrice` | decimal(10,2) | - | Sell price |
| `IsActive` | bool | - | Active status |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `InventoryItem` ? 1:1 ? InventoryItem?

---

### 9. InventoryItem
**Table:** `InventoryItems`  
**Purpose:** Stock levels/warehouse inventory

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `ProductId` | int? | FK | Related product |
| `Name` | string(200) | ? | Item name |
| `Sku` | string(50) | - | SKU |
| `QuantityOnHand` | int | - | Current stock |
| `QuantityReserved` | int | - | Reserved for jobs |
| `ReorderLevel` | int | - | Reorder threshold |
| `ReorderQuantity` | int | - | Reorder amount |
| `Location` | string(100) | - | Warehouse location |
| `UnitCost` | decimal(10,2) | - | Cost price |
| `UnitPrice` | decimal(10,2) | - | Sell price |
| `LastRestockDate` | DateTime? | - | Last restock |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Product` ? M:1 ? Product?
- `InventoryLogs` ? 1:M ? InventoryLog[]

---

### 10. ServiceAgreement
**Table:** `ServiceAgreements`  
**Purpose:** Maintenance contracts

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `AgreementNumber` | string(20) | - | Auto-generated |
| `CustomerId` | int | FK | Customer |
| `SiteId` | int? | FK | Service location |
| `Name` | string(200) | ? | Agreement name |
| `Description` | string(2000) | - | Description |
| `AgreementType` | enum | - | Preventive/FullService/LaborOnly |
| `Status` | enum | - | Draft/Active/Expired/Cancelled |
| `StartDate` | DateTime | - | Start date |
| `EndDate` | DateTime | - | End date |
| `BillingFrequency` | enum | - | Monthly/Quarterly/Annual |
| `BillingAmount` | decimal(10,2) | - | Recurring charge |
| `VisitsPerYear` | int | - | Included visits |
| `VisitsUsed` | int | - | Visits consumed |
| `NextServiceDate` | DateTime? | - | Next scheduled |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Customer` ? M:1 ? Customer
- `Site` ? M:1 ? Site?
- `Jobs` ? 1:M ? Job[]

---

### 11. WarrantyClaim
**Table:** `WarrantyClaims`  
**Purpose:** Warranty repairs/claims

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `ClaimNumber` | string(50) | - | Auto-generated |
| `AssetId` | int | FK | Related asset |
| `JobId` | int? | FK | Related job |
| `ClaimDate` | DateTime | - | Claim date |
| `ResolvedDate` | DateTime? | - | Resolution date |
| `IssueDescription` | string(2000) | ? | Problem description |
| `Resolution` | string(2000) | - | Resolution notes |
| `PartsReplaced` | string(1000) | - | Parts replaced |
| `IsCoveredByWarranty` | bool | - | Covered? |
| `NotCoveredReason` | string(500) | - | If not covered, why |
| `RepairCost` | decimal(10,2) | - | Repair cost |
| `CustomerCharge` | decimal(10,2) | - | Customer charge |
| `Status` | enum | - | Pending/Approved/Denied/Completed |
| `CreatedAt` | DateTime | - | Created timestamp |

**Relationships:**
- `Asset` ? M:1 ? Asset
- `Job` ? M:1 ? Job?

---

### 12. QuickNote (NEW)
**Table:** `QuickNotes`  
**Purpose:** Random notes, reminders, scratchpad

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | int | PK | Primary key |
| `Title` | string(200) | - | Optional title |
| `Content` | string(MAX) | ? | Note content |
| `Category` | string(50) | - | Optional category |
| `IsPinned` | bool | - | Pin to top |
| `IsArchived` | bool | - | Archived/hidden |
| `Color` | string(20) | - | Card color |
| `CreatedAt` | DateTime | - | Created timestamp |
| `UpdatedAt` | DateTime | - | Last updated |

**No Relationships** - Standalone notes

---

## ?? Relationship Summary

| From | To | Type | FK Field |
|------|------|------|----------|
| Site | Customer | M:1 | CustomerId |
| Site | Company | M:1 | CompanyId |
| Asset | Customer | M:1 | CustomerId |
| Asset | Site | M:1 | SiteId |
| Asset | Company | M:1 | CompanyId |
| Estimate | Customer | M:1 | CustomerId |
| Estimate | Site | M:1 | SiteId |
| Estimate | Asset | M:1 | AssetId |
| Job | Customer | M:1 | CustomerId |
| Job | Site | M:1 | SiteId |
| Job | Asset | M:1 | AssetId |
| Job | Estimate | M:1 | EstimateId |
| Job | ServiceAgreement | M:1 | ServiceAgreementId |
| Invoice | Customer | M:1 | CustomerId |
| Invoice | Job | M:1 | JobId |
| Payment | Invoice | M:1 | InvoiceId |
| WarrantyClaim | Asset | M:1 | AssetId |
| WarrantyClaim | Job | M:1 | JobId |
| ServiceAgreement | Customer | M:1 | CustomerId |
| ServiceAgreement | Site | M:1 | SiteId |

---

## ?? Enum Reference

### JobStatus
- `Draft` = 0
- `Scheduled` = 1
- `InProgress` = 2
- `Completed` = 3
- `Cancelled` = 4

### InvoiceStatus
- `Draft` = 0
- `Sent` = 1
- `Paid` = 2
- `Overdue` = 3
- `Cancelled` = 4

### EstimateStatus
- `Draft` = 0
- `Sent` = 1
- `Approved` = 2
- `Declined` = 3
- `Expired` = 4

### ClaimStatus
- `Pending` = 0
- `Approved` = 1
- `Denied` = 2
- `Completed` = 3

### EquipmentType
- `Unknown` = 0
- `AirConditioner` = 1
- `Furnace` = 2
- `HeatPump` = 3
- `Boiler` = 4
- `MiniSplit` = 5
- `PackageUnit` = 6
- `Thermostat` = 7
- `AirHandler` = 8

### PropertyType
- `Unknown` = 0
- `Residential` = 1
- `Commercial` = 2
- `Industrial` = 3
- `MultiFamily` = 4

---

## ?? Naming Conventions

### Entity Properties
- Use `PascalCase` for all properties
- Use `Id` suffix for primary keys (not `ID`)
- Use `At` suffix for timestamps (`CreatedAt`, `UpdatedAt`)
- Use `Is` prefix for booleans (`IsActive`, `IsPinned`)

### Display Names
- Customer: `DisplayName` = "FirstName LastName"
- Asset: `AssetName` or `Nickname`
- Site: `SiteName`
- Auto-numbers: `EstimateNumber`, `JobNumber`, `InvoiceNumber`

### Foreign Keys
- Use `EntityId` pattern: `CustomerId`, `SiteId`, `AssetId`
- Navigation properties use entity name: `Customer`, `Site`, `Asset`

---

## ?? UI Page Mapping

| Entity | Web Page | Mobile Page | Status |
|--------|----------|-------------|--------|
| Dashboard | `/dashboard` | MainPage | ?/?? Modernize |
| Customer | `/customers` | CustomerListPage | ?/?? Modernize |
| Site | `/sites` | SiteListPage | ?/? Disabled |
| Asset | `/assets` | AssetListPage | ?/?? Modernize |
| Estimate | `/estimates` | EstimateListPage | ?/?? Modernize |
| Job | `/jobs` | JobListPage | ?/?? Modernize |
| Invoice | `/invoices` | InvoiceListPage | ?/?? Modernize |
| Product | `/products` | ProductListPage | ?/?? Modernize |
| Inventory | `/inventory` | InventoryListPage | ?/?? Modernize |
| Company | `/companies` | CompanyListPage | ?/?? Modernize |
| Agreement | `/agreements` | ServiceAgreementListPage | ?/?? Modernize |
| Warranty | `/warranties/claims` | WarrantyClaimsListPage | ?/? Disabled |
| Calendar | `/calendar` | CalendarPage | ?/? Disabled |
| Settings | `/settings` | SettingsPage | ?/? OK |
| **QuickNotes** | `/notes` (NEW) | NotesPage (Planned) | ??/?? Planned |

---

## ?? Include Patterns for EF Core

### Customer with all related data:
```csharp
await db.Customers
    .Include(c => c.Sites)
    .Include(c => c.Assets)
    .Include(c => c.Jobs)
    .ToListAsync();
```

### Job with full context:
```csharp
await db.Jobs
    .Include(j => j.Customer)
    .Include(j => j.Site)
    .Include(j => j.Asset)
    .Include(j => j.Estimate)
    .ToListAsync();
```

### Asset with warranty info:
```csharp
await db.Assets
    .Include(a => a.Customer)
    .Include(a => a.Site)
    .Include(a => a.WarrantyClaims)
    .ToListAsync();
```

---

*Last Updated: 2025-01-28*
