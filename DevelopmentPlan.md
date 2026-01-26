# OneManVan - Unified Development Plan

## Executive Summary

**OneManVan** (by MtnManApps) is an HVAC field service management application for residential contractors. It runs on **Windows desktop (WPF)** and **Android mobile (.NET MAUI)** with offline-first architecture, local SQLite storage, and robust backup/export capabilities.

### Vision Statement
An industry-competitive FSM platform that scales from solo operators to small teams—handling customer/asset tracking, estimates, scheduling, time logging, invoicing, and reporting—with offline-first reliability and full data sovereignty.

### Target User
- Solo HVAC contractor ? small teams (1-5 technicians)
- Residential and light commercial services
- Values data ownership, offline reliability, no recurring fees
- Needs deep HVAC-specific features (refrigerant tracking, warranty management)

---

## Core Principles

| Principle | Description |
|-----------|-------------|
| **Offline-First** | Every action succeeds locally; sync queues changes for later upload |
| **HVAC Native** | Deep trade-specific features (refrigerant types, efficiency ratings, warranty tracking) |
| **Scalable UI** | Handles 1000+ customers/assets with virtualized grids and filtering |
| **Data Sovereignty** | Your data stays yours, with one-tap backups and optional Docker sync |
| **Industry Competitive** | Feature parity with ServiceTitan, Zoho FSM, Housecall Pro |
| **Zero Recurring Cost** | Free self-hosted, no per-user fees |

---

## Technical Architecture

### Technology Stack
| Layer | Technology |
|-------|------------|
| **Framework** | .NET 10, C# 14 |
| **Desktop UI** | WPF with XAML |
| **Mobile UI** | .NET MAUI (shared ViewModels) |
| **Data Access** | Entity Framework Core |
| **Local DB** | SQLite (file in %LocalAppData%\OneManVan) |
| **Remote DB** | SQL Server (Docker container, optional) |
| **MVVM** | CommunityToolkit.Mvvm |
| **PDF Export** | iText7 |
| **Barcode Scan** | ZXing.Net.MAUI |
| **Payments** | Square SDK |
| **Calendar** | Microsoft.Graph (Google Calendar) |
| **Charts** | LiveCharts2 |

### Project Structure
```
OneManVan.sln
??? OneManVan/ (WPF Desktop)
?   ??? Pages/
?   ??? ViewModels/
?   ??? Services/
?   ??? Themes/
?   ??? Dialogs/
??? OneManVan.Shared/ (Shared Library)
?   ??? Models/
?   ??? Models/Enums/
?   ??? Data/
?   ??? Services/
??? OneManVan.Mobile/ (.NET MAUI)
    ??? Pages/
    ??? Services/
    ??? Converters/
```

---

## Development Phases

### Phase Overview
| Phase | Focus | Hours | Status |
|-------|-------|-------|--------|
| **Phase 1** | Core Foundation | 8-10 | ? COMPLETE |
| **Phase 2** | Universal Workflows | 10-12 | ? COMPLETE |
| **Phase 3** | Operations & Financial | 8-10 | ? COMPLETE |
| **Phase 4** | Insights & Customization | 6-8 | ? COMPLETE |
| **Phase 5** | Android Mobile App | 10-12 | ? COMPLETE |
| **Phase 6** | Data Model Enhancement | 17 | ? COMPLETE |
| **Phase 7** | UI Modernization | 29 | ? COMPLETE |
| **Phase 8** | Mobile Parity | 21 | ? COMPLETE |
| **Phase 9** | Service Agreements | 14 | ? COMPLETE |
| **Phase 10** | Advanced Features | 32 | ? Pending |
| **Phase 11** | Team Features | 25 | ?? Future |
| **Phase 12** | Products/Equipment Catalog | 20 | ? COMPLETE |
| **Total** | | **~200 hours** | |

---

## Phase 1-5: COMPLETE ?

See archived documentation for details on completed phases.

---

## Phase 6: Data Model Enhancement ? COMPLETE

### Goal
Enhance all core models to support industry-standard FSM features, particularly HVAC-specific fields critical for service operations.

### Deliverables

#### 6.1 New Enums ?
- [x] CustomerType (Residential, Commercial, PropertyManager, etc.)
- [x] CustomerStatus (Active, Inactive, Lead, VIP, DoNotService, Delinquent)
- [x] ContactMethod (Any, Phone, Email, Text)
- [x] PaymentTerms (COD, DueOnReceipt, Net15, Net30, Net45, Net60)
- [x] PropertyType (SingleFamily, MultiFamily, Commercial, etc.)
- [x] RefrigerantType (R22, R410A, R32, R454B, etc.)
- [x] EquipmentType (detailed HVAC equipment types)
- [x] FilterType (Standard, Pleated, HEPA, Electronic, etc.)
- [x] ThermostatType (Manual, Programmable, SmartWifi, Zoning)
- [x] AssetCondition (Excellent, Good, Fair, Poor, Failed)
- [x] AssetStatus (Active, Inactive, Replaced, Removed, etc.)
- [x] JobType (ServiceCall, Repair, Installation, Maintenance, etc.)
- [x] JobPriority (Low, Normal, High, Urgent, Emergency)
- [x] PhotoType (Before, After, Issue, DataPlate, Signature)
- [x] CommunicationType (Call, Email, Text, InPerson)

#### 6.2 Enhanced Customer Model ?
New fields:
- CustomerNumber (auto-generated)
- CompanyName (for commercial)
- CustomerType, CustomerStatus
- SecondaryPhone, SecondaryEmail
- EmergencyContact, EmergencyPhone
- PaymentTerms, CreditLimit, AccountBalance
- TaxExempt, TaxExemptId
- ReferralSource, ReferredBy
- PreferredContact, ServiceNotes
- PreferredServiceWindow
- Tags, LifetimeRevenue, TotalJobCount

#### 6.3 Enhanced Site Model ?
New fields:
- SiteNumber (auto-generated)
- Address2 (unit/suite)
- Latitude, Longitude, GooglePlaceId
- PropertyType, YearBuilt, SquareFootage, Stories
- GateCode, LockboxCode, AccessInstructions
- ParkingInstructions, HasDog, HasGate, PetNotes
- SiteContactName, SiteContactPhone, SiteContactEmail
- ServiceZone, TravelSurcharge, EstimatedDriveMinutes
- System locations (electrical, gas, water, HVAC, thermostat, filter)

#### 6.4 Enhanced Asset Model ?
New fields:
- AssetTag (barcode/QR)
- Nickname ("Upstairs Unit", "Main Floor")
- EquipmentType (detailed)
- TonnageX10 (store as int, divide by 10 for display)
- Seer2Rating, AfueRating, HspfRating, Hspf2Rating, EerRating
- Voltage, PhaseType
- **RefrigerantType** (CRITICAL for service)
- RefrigerantChargeOz
- FilterSize, FilterType, FilterChangeMonths, LastFilterChange, NextFilterDue
- ThermostatBrand, ThermostatModel, ThermostatType, HasWifi
- ManufactureDate
- LaborWarrantyYears, CompressorWarrantyYears
- HasExtendedWarranty, ExtendedWarrantyEnd
- PurchasePrice, EstimatedReplacementCost, ExpectedLifeYears
- ServiceIntervalMonths, NextServiceDue
- Condition, ConditionNotes
- DuctworkNotes, TechnicalNotes
- AssetStatus, DecommissionedDate, ReplacedByAssetId

#### 6.5 Enhanced Job Model ?
New fields:
- JobNumber (auto-generated: J-2024-0001)
- ServiceAgreementId, RecurringJobId
- JobType, JobPriority
- RequestedDate (customer requested)
- ArrivalWindowStart, ArrivalWindowEnd
- DispatchedAt, EnRouteAt, ArrivedAt
- ProblemDescription, Diagnosis, RecommendedWork
- InternalNotes vs CustomerNotes
- PreJobChecklist, PostJobChecklist (JSON)
- MaterialTotal, TripCharge, DiscountAmount, DiscountReason
- SignedByName, SignedAt, CustomerApproved
- RequiresFollowUp, FollowUpReason, FollowUpDate, FollowUpJobId
- GoogleCalendarEventId, OutlookEventId
- Tags, CreatedBy, LastModifiedBy

#### 6.6 New Supporting Models ?
- [x] ServiceHistory (per-asset service log)
- [x] JobPart (parts used on job, links to inventory)
- [x] JobPhoto, AssetPhoto, SitePhoto
- [x] CommunicationLog (customer communication history)
- [x] CustomerDocument (file attachments)
- [x] CustomerNote (activity log)

### Progress ? COMPLETE
| Task | Status |
|------|--------|
| Design enhanced models | ? Complete |
| Create new enums | ? Complete |
| Enhance Customer model | ? Complete |
| Enhance Site model | ? Complete |
| Enhance Asset model | ? Complete |
| Enhance Job model | ? Complete |
| Create supporting models | ? Complete |
| Update DbContext | ? Complete |
| Build verification | ? Complete |

---

## Phase 7: UI Modernization ? COMPLETE

### Goal
Replace list-based UIs with scalable DataGrids, add Kanban board, calendar view.

### Deliverables
| Task | Hours | Status |
|------|-------|--------|
| Customer list ? DataGrid with filters, pagination | 4 | ? Complete |
| Asset list ? DataGrid with warranty highlighting | 4 | ? Complete |
| Job Kanban board (drag-drop columns) | 6 | ? |
| Calendar scheduling view | 8 | ? |
| Bulk operations UI | 3 | ? |
| Virtual scrolling for large datasets | 2 | ? |
| Quick-action context menus | 2 | ? |

### UI Mockups
See EnhancementPlan.md for detailed wireframes.

---

## Phase 8: Mobile Parity ? COMPLETE

### Goal
Bring mobile app to feature parity with desktop for field operations.

### Deliverables
| Task | Hours | Status |
|------|-------|--------|
| Job workflow (start/complete/invoice) | 4 | ? Complete |
| Photo capture for assets/jobs | 3 | ? Complete |
| GPS time stamping | 2 | ? Complete |
| Barcode/QR scanning | 3 | ? Complete |
| Signature capture | 3 | ? Complete |
| Offline sync queue | 4 | ? Complete |
| Push notifications | 2 | ? Future |

### Features Implemented
- **Job Workflow**: Full status transitions (Scheduled ? En Route ? In Progress ? Completed ? Invoiced)
- **Signature Capture**: SignaturePad control with touch drawing, clear, and accept functionality
- **Photo Capture**: Camera and gallery support with photo type selection (Before, After, Issue)
- **Photo Gallery**: CollectionView displaying captured photos with type labels
- **GPS Timestamps**: Location capture on status transitions and photos
- **Barcode Scanning**: BarcodeScannerService with interface for ZXing integration
- **Offline Sync**: OfflineSyncService with queue management and connectivity monitoring
- **Sync Status**: SyncStatusIndicator and OfflineBanner controls for connection state
- **Parts Entry**: Quick add parts/materials with pricing during job completion
- **Enhanced Job List**: Today filter, improved stats bar, workflow buttons

### Files Created
**Services (`OneManVan.Mobile/Services/`):**
- `BarcodeScannerService.cs` - Barcode/QR scanning interface and implementation
- `OfflineSyncService.cs` - Offline queue management and sync processing
- `PhotoCaptureService.cs` - Photo capture with GPS metadata and database storage

**Controls (`OneManVan.Mobile/Controls/`):**
- `SignaturePad.cs` - Touch-based signature capture control
- `SyncStatusIndicator.cs` - Connection status and offline banner controls

**Pages Updated:**
- `Pages/JobDetailPage.xaml` - Enhanced with signature pad, photo gallery, barcode scan
- `Pages/JobDetailPage.xaml.cs` - Full workflow, parts entry, photo capture
- `Pages/JobListPage.xaml` - Offline banner, Today filter, improved UI
- `Pages/JobListPage.xaml.cs` - Sync support, enhanced filtering

**Configuration:**
- `MauiProgram.cs` - Registered new services in DI container

---

## Phase 9: Service Agreements ? COMPLETE

### Goal
Add maintenance contract management for recurring revenue.

### Deliverables
| Task | Hours | Status |
|------|-------|--------|
| ServiceAgreement model (Shared & WPF) | 2 | ? Complete |
| AgreementType, AgreementStatus, BillingFrequency enums | 1 | ? Complete |
| ServiceAgreement DbSet & EF configuration | 1 | ? Complete |
| ServiceAgreementsDataGridPage.xaml | 3 | ? Complete |
| ServiceAgreementsDataGridPage.xaml.cs with CRUD | 2 | ? Complete |
| AddEditServiceAgreementDialog | 2 | ? Complete |
| Maintenance job generation from agreements | 2 | ? Complete |
| MainShell navigation registration | 1 | ? Complete |

### Features Implemented
- **Contract Management**: Create, edit, renew, suspend, and cancel service agreements
- **Agreement Types**: Basic, Standard, Premium, Annual, Semi-Annual, Quarterly, Custom
- **Included Services**: AC tune-up, heating tune-up, filter replacement, refrigerant top-off, parts coverage
- **Visit Tracking**: Track visits used vs. included visits per year
- **Pricing**: Annual/monthly pricing, repair discounts, billing frequency options
- **Maintenance Scheduling**: Generate maintenance jobs from agreements with preferred spring/fall months
- **Smart Filtering**: Filter by status, type, active, expiring soon, due for service
- **Detail Panel**: Rich detail view showing customer, pricing, services, and next maintenance
- **Export**: CSV export functionality

### Files Created/Modified
**WPF Desktop (`OneManVan/`):**
- `Models/ServiceAgreement.cs` - Service agreement model
- `Models/Enums/AgreementEnums.cs` - Agreement enums
- `Data/OneManVanDbContext.cs` - Added ServiceAgreements DbSet
- `Pages/ServiceAgreementsDataGridPage.xaml` - DataGrid page
- `Pages/ServiceAgreementsDataGridPage.xaml.cs` - Page code-behind
- `Dialogs/AddEditServiceAgreementDialog.xaml` - Add/Edit dialog
- `Dialogs/AddEditServiceAgreementDialog.xaml.cs` - Dialog code-behind
- `MainShell.xaml` - Added navigation button
- `MainShell.xaml.cs` - Added navigation handler

**Shared Project (`OneManVan.Shared/`):**
- `Models/ServiceAgreement.cs` - Shared model
- `Models/Enums/AgreementEnums.cs` - Shared enums
- `Data/OneManVanDbContext.cs` - Updated DbContext

---

## Phase 10: Advanced Features ? PENDING

### Goal
Add enterprise-grade features for competitive parity.

### Deliverables
| Task | Hours | Status |
|------|-------|--------|
| Route optimization | 6 | ? |
| Customer portal (web) | 12 | ? |
| Email/SMS notifications | 4 | ? |
| QuickBooks integration | 6 | ? |
| Advanced reporting | 4 | ? |

---

## Phase 11: Team Features ?? FUTURE

### Goal
Support small teams (2-5 technicians).

### Deliverables
| Task | Hours | Status |
|------|-------|--------|
| Multi-user authentication | 8 | ?? |
| Technician dispatching | 6 | ?? |
| Role-based permissions | 4 | ?? |
| Team scheduling conflicts | 4 | ?? |
| Technician performance metrics | 3 | ?? |

---

## Phase 12: Products/Equipment Catalog ? COMPLETE

### Goal
Create a comprehensive HVAC equipment catalog separate from inventory. Products serve as **templates** with pre-filled manufacturer specifications that can be quickly applied to customer assets or added to estimates. This dramatically speeds up data entry and ensures accuracy.

### Key Differentiator: Products vs Inventory vs Assets
| Concept | Purpose | Example |
|---------|---------|---------|
| **Product** | Template/catalog entry with specs | "Carrier 24ACC636A003 - 3 Ton 16 SEER AC" |
| **Inventory** | Physical stock you have on hand | "2x Carrier 24ACC636A003 in warehouse" |
| **Asset** | Installed equipment at customer site | "Carrier 24ACC636A003, Serial: 1234ABC, at 123 Main St" |

### Deliverables

#### 12.1 Product Model & Database (4 hours)
| Task | Status |
|------|--------|
| Create Product model with HVAC specifications | ? |
| Create ProductDocument model for attachments | ? |
| Create ProductCategory enum | ? |
| Add DbContext configuration | ? |
| Database migration | ? |

**Product Model Fields:**
```
- Id, ProductNumber (auto: P-0001)
- Manufacturer, ModelNumber, ProductName
- Category (AirConditioner, Furnace, HeatPump, etc.)
- Description, Features (rich text)

HVAC Specifications:
- EquipmentType, FuelType, RefrigerantType
- TonnageX10, BtuRating (cooling/heating)
- SeerRating, Seer2Rating, EerRating
- AfueRating, HspfRating, Hspf2Rating
- Voltage, PhaseType, Amperage, MinCircuitAmpacity
- FilterSize, FilterType

Physical:
- WeightLbs, HeightIn, WidthIn, DepthIn
- IndoorUnit, OutdoorUnit (for split systems)

Pricing:
- MSRP, WholesaleCost, SuggestedSellPrice
- LaborHoursEstimate, InstallationNotes

Warranty:
- PartsWarrantyYears, CompressorWarrantyYears
- LaborWarrantyYears, RegistrationRequired

Status:
- IsActive, IsDiscontinued, DiscontinuedDate
- ReplacementProductId (successor model)

Metadata:
- ManufacturerPartNumber, UpcCode
- ManufacturerUrl, VideoUrl
- Tags, Notes
- CreatedAt, UpdatedAt
```

**ProductDocument Model:**
```
- Id, ProductId
- DocumentType (Brochure, Manual, SpecSheet, Nomenclature, InstallGuide, WiringDiagram)
- FileName, FilePath, FileSize
- Description
- UploadedAt
```

#### 12.2 Products Page - Desktop (5 hours)
| Task | Status |
|------|--------|
| ProductsPage.xaml with DataGrid | ? |
| Search by manufacturer, model, specs | ? |
| Filter by category, fuel type, tonnage | ? |
| Product detail panel with full specs | ? |
| Document viewer/downloader | ? |
| Add/Edit product dialog | ? |
| Import products from CSV | ? |

#### 12.3 Document Management (3 hours)
| Task | Status |
|------|--------|
| Document upload service | ? |
| PDF viewer integration | ? |
| Document storage in AppData folder | ? |
| Document type icons and badges | ? |
| Open/download document actions | ? |

#### 12.4 Quick-Add Integration (4 hours)
| Task | Status |
|------|--------|
| "Add from Product" button on Asset form | ? |
| Product picker dialog with search | ? |
| Auto-populate asset fields from product | ? |
| "Add to Estimate" from product catalog | ? |
| Product quick-add to estimate lines | ? |

#### 12.5 Products Page - Mobile (4 hours)
| Task | Status |
|------|--------|
| ProductListPage.xaml for MAUI | ? |
| Search and filter on mobile | ? |
| Product detail view | ? |
| Document list (open in external app) | ? |
| Quick-add to asset from field | ? |

### Workflow Examples

**Creating a New Asset (Before):**
1. Select customer/site
2. Manually enter: Brand, Model, BTU, SEER, Refrigerant, Voltage...
3. Look up specs in separate document
4. Prone to typos and missing data

**Creating a New Asset (After):**
1. Select customer/site
2. Click "Add from Product Catalog"
3. Search: "Carrier 3 ton" ? Select model
4. Serial number auto-focused (only field to enter!)
5. All specs pre-filled accurately

**Creating an Estimate (After):**
1. From Product Catalog, click "Add to Estimate"
2. Select customer
3. Product specs and suggested pricing auto-filled
4. Adjust quantity/pricing as needed

### UI Mockups

**Products Page Layout:**
```
???????????????????????????????????????????????????????????????
? ?? Products Catalog                    [Import] [+ Add]     ?
???????????????????????????????????????????????????????????????
? ?? [Search products...]  [Category ?] [Manufacturer ?]      ?
? Filters: ? All  ? AC  ? Furnace  ? Heat Pump  ? Mini Split ?
???????????????????????????????????????????????????????????????
? ? Model          ? Manufacturer ? Tons ? SEER ? Price ? ?  ?
? ????????????????????????????????????????????????????????????
? ? 24ACC636A003   ? Carrier      ? 3.0  ? 16   ? $2,450? ?  ?
? ? XC21-036-230   ? Trane        ? 3.0  ? 21   ? $4,200? ?  ?
? ? GSX140361      ? Goodman      ? 3.0  ? 14   ? $1,850? ?  ?
???????????????????????????????????????????????????????????????
? ?? Product Details ??????????????????????????????????????   ?
? ? Carrier 24ACC636A003                    [Edit] [??]   ?   ?
? ? Comfort™ 16 Central Air Conditioner                   ?   ?
? ?                                                       ?   ?
? ? Specifications          ? Documents                   ?   ?
? ? ???????????????????????????????????????????????????   ?   ?
? ? Cooling: 36,000 BTU    ? ?? Product Brochure.pdf     ?   ?
? ? SEER: 16               ? ?? Installation Manual.pdf   ?   ?
? ? Refrigerant: R-410A    ? ?? Spec Sheet.pdf           ?   ?
? ? Voltage: 208/230V      ? ?? Nomenclature Guide.pdf   ?   ?
? ? Min Circuit: 20A       ?                             ?   ?
? ? Weight: 195 lbs        ? [+ Upload Document]         ?   ?
? ?                        ?                             ?   ?
? ? Pricing                ? Warranty                    ?   ?
? ? ???????????????????????????????????????????????????   ?   ?
? ? MSRP: $2,899           ? Parts: 10 years             ?   ?
? ? Cost: $1,450           ? Compressor: 10 years        ?   ?
? ? Sell: $2,450           ? Labor: 1 year               ?   ?
? ? Labor Est: 4 hrs       ? Registration Required: Yes  ?   ?
? ?                        ?                             ?   ?
? ? [? Add to Estimate] [?? Create Asset from Product]   ?   ?
? ?????????????????????????????????????????????????????????   ?
???????????????????????????????????????????????????????????????
```

### Benefits
1. **Speed**: Create assets in seconds instead of minutes
2. **Accuracy**: Eliminates typos and missing data
3. **Consistency**: Same product = same specs every time
4. **Documentation**: All manuals/brochures in one place
5. **Pricing**: Maintain accurate pricing for estimates
6. **Training**: New techs can reference product specs easily

---

## Testing

### Testing Plan
A comprehensive testing plan has been created in `TestingPlan.md` covering:

| Stage | Focus Area | Priority |
|-------|-----------|----------|
| **Stage 1** | Database & Models | Critical |
| **Stage 2** | Core CRUD Operations | Critical |
| **Stage 3** | Navigation & Routing | High |
| **Stage 4** | Business Logic & Workflows | High |
| **Stage 5** | UI/UX & Data Display | Medium |
| **Stage 6** | Services & Integrations | Medium |
| **Stage 7** | Mobile App Testing | High |
| **Stage 8** | Edge Cases & Error Handling | Medium |
| **Stage 9** | Performance & Stress Testing | Low |
| **Stage 10** | Final Integration Testing | Critical |

### Test Data Seeder
A `TestDataSeederService` has been created in `Services/TestDataSeederService.cs` to generate:
- 10 Customers (mix of residential/commercial)
- 20+ Sites (2+ per customer)
- 30+ Assets (various equipment types)
- 50 Inventory Items
- 10 Estimates (various statuses)
- 15 Jobs (various statuses)
- 10 Invoices (various statuses)
- 5 Service Agreements
- 20 Products

### Running Tests
1. Clear existing data: `TestDataSeederService.ClearAllDataAsync()`
2. Seed test data: `TestDataSeederService.SeedAllTestDataAsync()`
3. Follow test cases in `TestingPlan.md`
4. Document defects in the testing plan's defect tracking section

---

*Last Updated: December 2024 - Phase 8 Complete, Testing Plan Added*
*Document Version: 2.3*

