# MAUI Blazor Hybrid App - Audit & Gap Analysis

**Audit Date:** 2025-02-01  
**Auditor:** AI Agent  
**Target:** Achieve feature parity with Blazor Web app  

---

## Executive Summary

The MAUI Blazor Hybrid app (`OneManVan.MauiBlazor`) is significantly behind the Web app in features, UI/UX, and service registrations. The app has a minimal structure with only basic layout components and no actual page content. Immediate work is needed to bring it to feature parity.

---

## 1. Project Structure Comparison

### 1.1 Current MAUI Blazor Structure

```
OneManVan.MauiBlazor/
??? App.xaml / App.xaml.cs
??? MainPage.xaml / MainPage.xaml.cs (BlazorWebView host)
??? MauiProgram.cs
??? _Imports.razor
??? Components/
?   ??? Routes.razor
?   ??? Layout/
?       ??? MainLayout.razor
?       ??? NavMenu.razor
??? wwwroot/
?   ??? index.html
??? Platforms/
    ??? Android/
        ??? AndroidManifest.xml
        ??? MainActivity.cs
```

### 1.2 Web App Structure (Reference)

```
OneManVan.Web/
??? Program.cs
??? _Imports.razor
??? Components/
?   ??? App.razor
?   ??? Routes.razor
?   ??? Layout/
?   ?   ??? MainLayout.razor
?   ?   ??? MainLayout.razor.css
?   ?   ??? NavMenu.razor
?   ??? Shared/
?   ?   ??? DeleteConfirmationModal.razor
?   ?   ??? ExportButton.razor
?   ?   ??? ImportButton.razor
?   ?   ??? PdfOptionsModal.razor
?   ?   ??? PhoneNumberInput.razor
?   ?   ??? PwaInstallBanner.razor
?   ?   ??? SearchableDropdown.razor
?   ?   ??? ThemeToggle.razor
?   ??? Pages/
?   ?   ??? Dashboard.razor
?   ?   ??? Home.razor
?   ?   ??? Assets/
?   ?   ??? Calendar/
?   ?   ??? Companies/
?   ?   ??? Customers/
?   ?   ??? Documents/
?   ?   ??? Employees/
?   ?   ??? Estimates/
?   ?   ??? Expenses/
?   ?   ??? Inventory/
?   ?   ??? Invoices/
?   ?   ??? Jobs/
?   ?   ??? MaterialLists/
?   ?   ??? Notes/
?   ?   ??? Products/
?   ?   ??? Routes/
?   ?   ??? ServiceAgreements/
?   ?   ??? Settings/
?   ?   ??? Sites/
?   ?   ??? Warranties/
?   ??? Account/ (Authentication pages)
```

---

## 2. Missing Pages (Critical Gap)

### 2.1 Pages Present in Web but Missing in MAUI

| Category | Page | Priority |
|----------|------|----------|
| **Core** | Dashboard.razor | HIGH |
| **Core** | Home.razor | HIGH |
| **Customers** | CustomerList.razor | HIGH |
| **Customers** | CustomerDetail.razor | HIGH |
| **Customers** | CustomerEdit.razor | HIGH |
| **Jobs** | JobList.razor | HIGH |
| **Jobs** | JobDetail.razor | HIGH |
| **Jobs** | JobEdit.razor | HIGH |
| **Invoices** | InvoiceList.razor | HIGH |
| **Invoices** | InvoiceDetail.razor | HIGH |
| **Invoices** | InvoiceEdit.razor | HIGH |
| **Estimates** | EstimateList.razor | HIGH |
| **Estimates** | EstimateDetail.razor | MEDIUM |
| **Estimates** | EstimateEdit.razor | MEDIUM |
| **Calendar** | Calendar.razor | MEDIUM |
| **Products** | ProductList.razor | MEDIUM |
| **Products** | ProductDetail.razor | MEDIUM |
| **Products** | ProductEdit.razor | MEDIUM |
| **Assets** | AssetList.razor | MEDIUM |
| **Assets** | AssetDetail.razor | MEDIUM |
| **Assets** | AssetEdit.razor | MEDIUM |
| **Inventory** | InventoryList.razor | MEDIUM |
| **Inventory** | InventoryDetail.razor | LOW |
| **Inventory** | InventoryEdit.razor | LOW |
| **Employees** | EmployeeList.razor | MEDIUM |
| **Employees** | EmployeeDetail.razor | MEDIUM |
| **Employees** | EmployeeEdit.razor | MEDIUM |
| **Employees** | TimeLogEntry.razor | MEDIUM |
| **Employees** | AddPerformanceNoteModal.razor | LOW |
| **Companies** | CompanyList.razor | LOW |
| **Companies** | CompanyDetail.razor | LOW |
| **Companies** | CompanyEdit.razor | LOW |
| **Sites** | SiteList.razor | LOW |
| **Sites** | SiteDetail.razor | LOW |
| **Sites** | SiteEdit.razor | LOW |
| **Agreements** | AgreementList.razor | LOW |
| **Agreements** | AgreementDetail.razor | LOW |
| **Agreements** | AgreementEdit.razor | LOW |
| **Documents** | DocumentList.razor | LOW |
| **Documents** | DocumentDetail.razor | LOW |
| **Documents** | DocumentUpload.razor | LOW |
| **Material Lists** | MaterialListIndex.razor | LOW |
| **Material Lists** | MaterialListDetail.razor | LOW |
| **Material Lists** | MaterialListEdit.razor | LOW |
| **Routes** | RouteOptimization.razor | LOW |
| **Expenses** | ExpenseList.razor | MEDIUM |
| **Expenses** | ExpenseEdit.razor | MEDIUM |
| **Warranties** | ClaimsList.razor | LOW |
| **Warranties** | ClaimDetail.razor | LOW |
| **Warranties** | AddClaim.razor | LOW |
| **Notes** | QuickNotes.razor | LOW |
| **Settings** | Settings.razor | MEDIUM |

**Total Missing Pages: 47+**

---

## 3. Missing Shared Components

### 3.1 Web Shared Components (Not in MAUI)

| Component | Purpose | Required |
|-----------|---------|----------|
| DeleteConfirmationModal.razor | Delete confirmation dialog | YES |
| ExportButton.razor | CSV/Excel/PDF export | YES |
| ImportButton.razor | CSV import functionality | YES |
| PdfOptionsModal.razor | PDF generation options | NO (web-only) |
| PhoneNumberInput.razor | Formatted phone input | YES |
| SearchableDropdown.razor | Searchable select control | YES |
| ThemeToggle.razor | Dark/light mode toggle | YES |
| PwaInstallBanner.razor | PWA install prompt | NO (web-only) |

---

## 4. Missing Service Registrations

### 4.1 Services Registered in Web but Missing in MAUI

| Service | Interface | Priority |
|---------|-----------|----------|
| CompanySettingsService | - | HIGH |
| DashboardKpiService | - | HIGH |
| MaterialListService | - | MEDIUM |
| MaterialListTemplateService | - | LOW |
| DocumentService | - | MEDIUM |
| EmployeeService | - | HIGH |
| EmployeeTimeLogAutoService | IEmployeeTimeLogAutoService | HIGH |
| JobTimeClockService | IJobTimeClockService | HIGH |
| PayrollService | IPayrollService | HIGH |
| RouteOptimizationService | - | LOW |
| GoogleCalendarService | - | LOW |
| DataProtectionService | IDataProtectionService | MEDIUM |
| DropdownPresetService | - | HIGH |
| NotificationService | INotificationService | MEDIUM |
| EmailService | IEmailService | NO (web-only) |
| CsvExportService | ICsvExportService | YES |
| ExcelExportService | IExcelExportService | NO (needs different lib) |
| InvoicePdfGenerator | IInvoicePdfGenerator | MAYBE |
| EstimatePdfGenerator | IEstimatePdfGenerator | MAYBE |
| ServiceAgreementPdfGenerator | - | MAYBE |
| MaterialListPdfGenerator | - | MAYBE |
| CsvImportService | ICsvImportService | YES |

**Currently Registered in MAUI:**
- ISettingsStorage (MobileSettingsStorage)
- TradeConfigurationService
- IDbContextFactory<OneManVanDbContext>
- DatabaseInitializer

---

## 5. _Imports.razor Differences

### 5.1 Web _Imports.razor (Current)

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using OneManVan.Web
@using OneManVan.Web.Components
@using OneManVan.Web.Components.Layout
@using OneManVan.Web.Components.Shared
@using OneManVan.Shared.Models
@using OneManVan.Shared.Models.Enums
@using OneManVan.Shared.Services

@attribute [Authorize]
```

### 5.2 MAUI _Imports.razor (Current - Incomplete)

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using OneManVan.MauiBlazor
@using OneManVan.MauiBlazor.Components
@using OneManVan.MauiBlazor.Components.Layout
@using OneManVan.Shared.Models
@using OneManVan.Shared.Services
```

### 5.3 Missing Imports in MAUI

- `@using System.Net.Http.Json`
- `@using Microsoft.EntityFrameworkCore`
- `@using OneManVan.Shared.Models.Enums` (CRITICAL - used everywhere)
- `@using OneManVan.MauiBlazor.Components.Shared` (needs to be created)
- `@using OneManVan.BlazorShared.Components` (if using shared RCL)

---

## 6. NavMenu Feature Gap

### 6.1 Web NavMenu Items (23 items)

1. Dashboard
2. Customers
3. Jobs
4. Calendar
5. Routes (Route Optimization)
6. Invoices
7. Expenses
8. Estimates
9. Material Lists
10. Products
11. Assets
12. Inventory
13. Repair History (Warranties)
14. Documents
15. Employees
16. Companies
17. Sites
18. Agreements (Service Agreements)
19. Quick Notes
20. Settings
21. Account/Manage
22. Logout

### 6.2 MAUI NavMenu Items (5 items - Only!)

1. Home
2. Customers
3. Jobs
4. Invoices
5. Calendar

**Missing: 18 navigation items**

---

## 7. Database Sync Status

### 7.1 Current State

- **Local Database**: SQLite at `FileSystem.AppDataDirectory/onemanvan.db`
- **Sync Implementation**: NONE
- **Change Tracking**: Not implemented
- **Conflict Resolution**: Not implemented

### 7.2 Required for Offline-First

| Feature | Status |
|---------|--------|
| Local SQLite storage | DONE |
| LastModified timestamp on entities | PARTIAL (some entities have it) |
| SyncStatus enum (Pending/Synced/Conflict) | NOT DONE |
| Version/ETag tracking | NOT DONE |
| Delta pull API | NOT DONE |
| Batch push API | NOT DONE |
| Background sync service | NOT DONE |
| Conflict detection | NOT DONE |
| Manual merge UI | NOT DONE |

---

## 8. UI/UX Modernization Needs

### 8.1 Current MAUI UI Issues

- MainLayout uses basic sidebar (not mobile-optimized)
- No hamburger menu for mobile
- No dark mode support
- No touch-friendly styling
- No safe area inset handling
- No gesture navigation support

### 8.2 Required Mobile UX Features

| Feature | Priority |
|---------|----------|
| Touch targets >= 48dp | HIGH |
| Bottom navigation or hamburger menu | HIGH |
| Safe area insets | HIGH |
| Dark mode (AppThemeBinding) | MEDIUM |
| Pull-to-refresh | HIGH |
| Swipe gestures | MEDIUM |
| Skeleton loading states | MEDIUM |
| Empty state views | MEDIUM |
| Floating action buttons | MEDIUM |

---

## 9. BlazorShared RCL Status

### 9.1 Current State

- Project exists: `OneManVan.BlazorShared`
- Target: `net10.0` (Razor Class Library)
- References: `OneManVan.Shared`
- **Components: NONE** (empty RCL)

### 9.2 Potential Shared Components

Components that could be moved to BlazorShared for reuse:
- DeleteConfirmationModal.razor
- PhoneNumberInput.razor
- SearchableDropdown.razor
- ThemeToggle.razor
- Entity list tables (CustomerList, JobList, etc.)
- Entity detail views
- Entity edit forms

---

## 10. Recommended Action Plan

### Phase 1: Foundation (Immediate)
1. Update `_Imports.razor` with missing namespaces
2. Register missing services in `MauiProgram.cs`
3. Create `Components/Shared` folder structure

### Phase 2: Core Pages (Week 1)
1. Dashboard.razor
2. CustomerList/Detail/Edit
3. JobList/Detail/Edit
4. InvoiceList/Detail/Edit

### Phase 3: Secondary Features (Week 2)
1. Calendar
2. Estimates
3. Employees
4. Settings

### Phase 4: UX Modernization (Week 3)
1. Mobile-optimized layout
2. Dark mode support
3. Touch-friendly controls

### Phase 5: Sync Implementation (Week 4)
1. Add sync tracking fields
2. Implement sync service
3. Add background sync

---

## 11. Build Status

### Current Build Errors

The MAUI project may have pre-existing XAML namespace errors:
- `MC3074: The tag 'Application' does not exist in XML namespace`
- `MC3074: The tag 'ContentPage' does not exist in XML namespace`

**Root Cause**: MAUI workload not properly installed or outdated.

**Fix**: Run `dotnet workload install maui` and ensure Visual Studio MAUI workload is installed.

---

## Appendix: File-by-File Comparison

### Services Comparison Table

| Service | Web | MAUI | Notes |
|---------|-----|------|-------|
| IDbContextFactory | YES | YES | Both use SQLite locally |
| ISettingsStorage | WebSettingsStorage | MobileSettingsStorage | Platform-specific |
| TradeConfigurationService | YES | YES | Shared |
| CompanySettingsService | YES | NO | Need to register |
| DashboardKpiService | YES | NO | Need to register |
| EmployeeService | YES | NO | Need to register |
| JobTimeClockService | YES | NO | Need to register |
| PayrollService | YES | NO | Need to register |
| DropdownPresetService | YES | NO | Need to register |
| DocumentService | YES | NO | Need to register |
| MaterialListService | YES | NO | Need to register |

---

**Document Version:** 1.0  
**Last Updated:** 2025-02-01
