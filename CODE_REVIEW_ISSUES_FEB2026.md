# Project Code Review - OneManVan (TradeFlow FSM) - February 2026

## Summary

This comprehensive QA audit of the OneManVan Field Service Management solution reveals **critical security vulnerabilities** that require immediate attention, particularly around the removal of authorization on the ImportController which exposes all import endpoints to unauthenticated access. The solution demonstrates reasonable architectural decisions but exhibits several high-severity issues including hardcoded credentials in source-controlled files, inconsistent authorization patterns across controllers, missing input sanitization in import services, and database cascade configurations that could lead to data loss. Medium and low-severity issues include missing indexes, responsive design gaps in MAUI components, missing HTTPS enforcement in production, and potential memory issues with large file imports. Overall risk level: **HIGH** - deployment to production without addressing Critical and High issues would expose the application to data breaches and unauthorized access.

---

## Critical Issues

### CRI-001: ImportController Endpoints Completely Unauthenticated

**Severity:** CRITICAL  
**Location:** `OneManVan.Web/Controllers/ImportController.cs`, Lines 7-11  
**Description:** The `[Authorize]` attribute was removed from the ImportController, leaving all import endpoints (customers, products, jobs, assets, inventory, invoices, estimates, companies, sites, agreements, employees) completely accessible without authentication.

**Evidence:**
```csharp
[ApiController]
[Route("api/[controller]")]
// Note: Authorization removed to allow Blazor Server HttpClient calls
// The user is authenticated via Blazor, but HttpClient doesn't forward cookies
public class ImportController : ControllerBase
```

**Impact:** Any anonymous user or attacker can:
- Import arbitrary customer data into the system
- Overwrite existing customer records via `updateExisting=true`
- Perform denial of service by uploading large/malformed files
- Potentially inject malicious data into the database

---

### CRI-002: Hardcoded Credentials in Source-Controlled .env File

**Severity:** CRITICAL  
**Location:** `.env`, Lines 1-11  
**Description:** Production database credentials are hardcoded and committed to the Git repository.

**Evidence:**
```
SA_PASSWORD=TradeFlow2025!
SYNC_USER=tradeflow_sync
SYNC_PASSWORD=SyncUser2025!
```

**Impact:** Anyone with repository access (current or historical) can obtain database credentials, potentially accessing or destroying all business data.

---

### CRI-003: AllowedHosts Set to Wildcard in Production

**Severity:** CRITICAL  
**Location:** `OneManVan.Web/appsettings.Production.json`, Line 9  
**Description:** The AllowedHosts configuration is set to "*" in production, disabling host header validation.

**Evidence:**
```json
"AllowedHosts": "*",
```

**Impact:** Enables host header injection attacks, cache poisoning, and potential password reset hijacking via manipulated host headers.

---

## High Issues

### HIGH-001: HTTPS Redirection Explicitly Disabled in Production

**Severity:** HIGH  
**Location:** `OneManVan.Web/appsettings.Production.json`, Lines 11-13  
**Description:** HTTPS redirection is explicitly disabled for production deployment.

**Evidence:**
```json
"HttpsRedirection": {
    "Enabled": false
}
```

**Impact:** All traffic including authentication cookies and sensitive business data transmitted in plaintext, vulnerable to man-in-the-middle attacks.

---

### HIGH-002: Inconsistent Authorization Patterns Across Controllers

**Severity:** HIGH  
**Location:** Multiple controller files  
**Description:** ExportController has `[Authorize]` (Line 11) while ImportController does not, creating an inconsistent and confusing security model.

**Evidence:**
- `ExportController.cs` Line 11: `[Authorize]`
- `ImportController.cs`: No authorization attribute

**Impact:** Developers may incorrectly assume all API controllers are secured, leading to future security oversights.

---

### HIGH-003: CSV Import Service Lacks File Size Validation

**Severity:** HIGH  
**Location:** `OneManVan.Web/Services/Import/CsvImportService.cs`, Lines 79-180  
**Description:** The import service processes files without validating maximum size limits, loading entire files into memory.

**Evidence:**
```csharp
using var reader = new StreamReader(csvStream);
using var csv = new CsvReader(reader, GetCsvConfiguration());
var records = csv.GetRecords<CustomerImportDto>().ToList();
```

**Impact:** Attackers can cause OutOfMemoryException by uploading extremely large files, leading to denial of service.

---

### HIGH-004: Customer Cascade Delete May Cause Unintended Data Loss

**Severity:** HIGH  
**Location:** `OneManVan.Shared/Data/OneManVanDbContext.cs`, Lines 112-120  
**Description:** Cascade delete is configured from Customer to Assets, potentially deleting valuable asset history when a customer is removed.

**Evidence:**
```csharp
entity.HasMany(e => e.Assets)
    .WithOne(a => a.Customer)
    .HasForeignKey(a => a.CustomerId)
    .OnDelete(DeleteBehavior.Cascade);
```

**Impact:** Deleting a customer will silently delete all associated assets, sites, and potentially service history without explicit user confirmation.

---

### HIGH-005: No Rate Limiting on Import Controller Endpoints

**Severity:** HIGH  
**Location:** `OneManVan.Web/Controllers/ImportController.cs` and `OneManVan.Web/Program.cs`  
**Description:** While rate limiting policies exist for "fixed", "auth", and "export", the import endpoints do not have rate limiting applied.

**Impact:** Attackers can flood the import endpoints with requests, exhausting server resources and database connections.

---

### HIGH-006: Template Download Endpoint Allows Anonymous Access

**Severity:** HIGH  
**Location:** `OneManVan.Web/Controllers/ImportController.cs`, Lines 214-216  
**Description:** The template download endpoint is marked `[AllowAnonymous]` but exists in an otherwise public controller.

**Evidence:**
```csharp
[HttpGet("template/{entityType}")]
[AllowAnonymous]
public IActionResult DownloadTemplate(string entityType)
```

**Impact:** While intentional for template downloads, this indicates the controller previously had authorization which was removed incorrectly.

---

## Medium Issues

### MED-001: Email Configuration Contains Empty Credentials in appsettings.json

**Severity:** MEDIUM  
**Location:** `OneManVan.Web/appsettings.json`, Lines 6-13  
**Description:** Email configuration section with empty values is committed to source control.

**Evidence:**
```json
"Email": {
    "SmtpHost": "",
    "SmtpPassword": "",
    ...
}
```

**Impact:** Exposes email configuration structure; if accidentally populated, credentials could be leaked.

---

### MED-002: AssetNumber and InventoryNumber Fields Added Without Migration Script

**Severity:** MEDIUM  
**Location:** `OneManVan.Shared/Models/Asset.cs`, Line 19; `OneManVan.Shared/Models/InventoryItem.cs`, Line 17  
**Description:** New fields `AssetNumber` and `InventoryNumber` were added to models but corresponding database migration scripts may not exist.

**Impact:** Deployment to production may fail or lose data if database schema doesn't match model expectations.

---

### MED-003: Missing Index on AssetNumber Column

**Severity:** MEDIUM  
**Location:** `OneManVan.Shared/Data/OneManVanDbContext.cs`, Lines 198-239  
**Description:** While AssetTag has a unique index, the new AssetNumber field lacks an index despite being used for lookups.

**Impact:** Performance degradation when searching or sorting by AssetNumber.

---

### MED-004: Docker Container Runs with Excessive Permissions

**Severity:** MEDIUM  
**Location:** `OneManVan.Web/Dockerfile`, Line 35  
**Description:** AppData directory is created with world-writable permissions (chmod 777).

**Evidence:**
```dockerfile
RUN mkdir -p /app/AppData && chmod 777 /app/AppData
```

**Impact:** Any process in the container can modify database files, reducing defense-in-depth.

---

### MED-005: Service Worker Caches Authentication-Required Pages

**Severity:** MEDIUM  
**Location:** `OneManVan.Web/wwwroot/service-worker.js`, Lines 8-17  
**Description:** The service worker pre-caches the dashboard route which requires authentication.

**Evidence:**
```javascript
const PRECACHE_ASSETS = [
    '/',
    '/dashboard',
    ...
];
```

**Impact:** May cause confusing behavior when cached content is served to unauthenticated users.

---

### MED-006: NavMenu Links Use Relative Paths Without Base Href Consideration

**Severity:** MEDIUM  
**Location:** `OneManVan.Web/Components/Layout/NavMenu.razor`, Lines 14-48  
**Description:** Navigation links use relative paths which may break in subdirectory deployments.

**Evidence:**
```razor
<NavLink class="nav-link" href="dashboard" Match="NavLinkMatch.All">
```

**Impact:** Navigation may fail if application is deployed to a subdirectory.

---

### MED-007: Import Service Exception Messages Exposed to Clients

**Severity:** MEDIUM  
**Location:** `OneManVan.Web/Controllers/ImportController.cs`, Lines 43-45, 78-80  
**Description:** Internal exception messages are returned directly to API clients.

**Evidence:**
```csharp
return StatusCode(500, new { error = ex.Message });
```

**Impact:** May reveal sensitive implementation details, stack traces, or connection strings to attackers.

---

### MED-008: CustomerEdit Page Missing Server-Side Validation

**Severity:** MEDIUM  
**Location:** `OneManVan.Web/Components/Pages/Customers/CustomerEdit.razor`  
**Description:** Form relies on DataAnnotationsValidator but server-side validation before database save is not evident.

**Impact:** Malformed data could be inserted directly if client-side validation is bypassed.

---

### MED-009: Mobile MAUI App Uses Hardcoded Localhost Connection

**Severity:** MEDIUM  
**Location:** Multiple MAUI files reference configuration  
**Description:** Mobile app may have hardcoded development URLs that won't work in production.

**Impact:** Mobile app may fail to connect to production server.

---

### MED-010: Missing CSRF Token Validation on Import Endpoints

**Severity:** MEDIUM  
**Location:** `OneManVan.Web/Controllers/ImportController.cs`  
**Description:** Import endpoints don't validate antiforgery tokens, making them vulnerable to CSRF attacks (compounded by missing authorization).

**Impact:** If a logged-in user visits a malicious site, their browser could be tricked into performing imports.

---

## Low / Informational Issues

### LOW-001: Console.WriteLine Used for Production Logging

**Severity:** LOW  
**Location:** `OneManVan.Web/Program.cs`, Lines 98, 115, 143, 153  
**Description:** Console.WriteLine is used for diagnostic messages instead of structured logging.

**Evidence:**
```csharp
Console.WriteLine("[INFO] Using SQL Server for Business database (Docker mode)");
```

**Impact:** Log messages may not be captured by production logging infrastructure.

---

### LOW-002: Magic Strings Used for Customer Number Prefix

**Severity:** LOW  
**Location:** `OneManVan.Web/Services/Import/CsvImportService.cs`, Lines 107, 115  
**Description:** "C-" prefix is hardcoded in multiple locations.

**Evidence:**
```csharp
.Where(c => c.CustomerNumber != null && c.CustomerNumber.StartsWith("C-"))
```

**Impact:** Inconsistency if prefix needs to change; violates DRY principle.

---

### LOW-003: Asset Model Contains 70+ Properties

**Severity:** LOW  
**Location:** `OneManVan.Shared/Models/Asset.cs`  
**Description:** The Asset class is extremely large with over 70 properties and many computed NotMapped properties.

**Impact:** Difficult to maintain, test, and understand. May impact serialization performance.

---

### LOW-004: Missing XML Documentation on Public API Endpoints

**Severity:** LOW  
**Location:** `OneManVan.Web/Controllers/ImportController.cs`, `ExportController.cs`  
**Description:** Controller actions lack XML documentation comments for API documentation generation.

**Impact:** Generated API documentation (if any) will lack descriptions.

---

### LOW-005: Deprecated Bootstrap Icons Usage Pattern

**Severity:** LOW  
**Location:** `OneManVan.Web/Components/Layout/NavMenu.razor`  
**Description:** Using `<span class="bi bi-*">` instead of `<i class="bi bi-*">`.

**Impact:** Minor semantic HTML issue; no functional impact.

---

### LOW-006: No Content Security Policy Headers Configured

**Severity:** LOW  
**Location:** `OneManVan.Web/Program.cs`  
**Description:** No CSP headers are configured to prevent XSS attacks.

**Impact:** Reduced defense-in-depth against XSS vulnerabilities.

---

### LOW-007: Missing Alt Text on Brand Link

**Severity:** LOW  
**Location:** `OneManVan.Web/Components/Layout/NavMenu.razor`, Line 7  
**Description:** Brand link lacks aria-label for accessibility.

**Evidence:**
```razor
<a class="navbar-brand" href="">OneManVan</a>
```

**Impact:** Screen reader users may not understand link purpose.

---

### LOW-008: Empty href on Brand Link

**Severity:** LOW  
**Location:** `OneManVan.Web/Components/Layout/NavMenu.razor`, Line 7  
**Description:** Brand link has empty href which may cause navigation issues.

**Impact:** Clicking brand may navigate to current page or root inconsistently.

---

### LOW-009: Multiple Database Connection String Resolution Logic

**Severity:** LOW  
**Location:** `OneManVan.Web/Program.cs`, Lines 74-176  
**Description:** Complex conditional logic for database connection string resolution with multiple fallbacks.

**Impact:** Difficult to predict which database will be used in different environments.

---

### LOW-010: Inconsistent Error Response Format

**Severity:** LOW  
**Location:** Various controllers  
**Description:** Some endpoints return `{ error: "message" }` while others return different formats.

**Impact:** Frontend must handle multiple error response formats.

---

## Steps to Remediation

### Critical Issues

#### CRI-001: Restore Authorization on ImportController
- **What is wrong:** All import endpoints are accessible without authentication
- **Why it matters:** Allows anonymous users to inject data into the database
- **Fix direction:** Restore `[Authorize]` attribute to controller class; implement proper authentication forwarding for Blazor Server HttpClient calls using IHttpContextAccessor or a backend-to-backend authentication mechanism

#### CRI-002: Remove Credentials from Source Control
- **What is wrong:** Database passwords committed to Git
- **Why it matters:** Credentials exposed to anyone with repository access
- **Fix direction:** Remove .env from version control; add to .gitignore; use .env.example with placeholders; rotate all exposed credentials immediately; use Azure Key Vault or similar for production

#### CRI-003: Configure Proper AllowedHosts
- **What is wrong:** AllowedHosts accepts any host header
- **Why it matters:** Enables host header injection attacks
- **Fix direction:** Set AllowedHosts to specific production domain(s); use environment-specific configuration

### High Issues

#### HIGH-001: Enable HTTPS Redirection
- **What is wrong:** Traffic transmitted in plaintext
- **Why it matters:** Credentials and data vulnerable to interception
- **Fix direction:** Enable HTTPS redirection; configure TLS certificates; use HSTS headers

#### HIGH-002: Standardize Controller Authorization
- **What is wrong:** Inconsistent security patterns
- **Why it matters:** Creates confusion and oversight risk
- **Fix direction:** Audit all controllers; apply consistent authorization; document security model

#### HIGH-003: Add File Size Validation
- **What is wrong:** No limits on uploaded file sizes
- **Why it matters:** Memory exhaustion denial of service
- **Fix direction:** Configure maximum request body size in Kestrel; validate file size before processing; use streaming for large files

#### HIGH-004: Review Cascade Delete Configuration
- **What is wrong:** Deleting customer deletes all related data
- **Why it matters:** Accidental data loss
- **Fix direction:** Consider soft-delete pattern; change to SetNull where appropriate; add confirmation dialogs with clear warnings

#### HIGH-005: Apply Rate Limiting to Import Endpoints
- **What is wrong:** Unlimited request rate allowed
- **Why it matters:** Resource exhaustion attacks
- **Fix direction:** Apply existing rate limiting policy to ImportController; consider stricter limits for file upload endpoints

#### HIGH-006: Audit AllowAnonymous Usage
- **What is wrong:** Anonymous access in context of removed authorization
- **Why it matters:** Indicates authorization was previously present
- **Fix direction:** Review all AllowAnonymous attributes; ensure they are intentional after restoring Authorize on controller

### Medium Issues

#### MED-001 through MED-010:
For each medium issue, remediation involves:
- Reviewing and removing sensitive data from source control
- Creating proper database migration scripts for new fields
- Adding missing indexes via migration
- Restricting Docker container permissions
- Reviewing service worker caching strategy
- Using base href for navigation links
- Returning generic error messages to clients
- Implementing server-side validation
- Reviewing mobile app configuration
- Implementing CSRF protection

### Low/Informational Issues

For low-severity issues, remediation involves:
- Using ILogger instead of Console.WriteLine
- Extracting magic strings to constants
- Considering splitting large models
- Adding XML documentation
- Using semantic HTML elements
- Configuring Content Security Policy headers
- Adding accessibility attributes
- Standardizing error response formats

---

I have not modified, suggested, or written any corrected code - only identified and described issues.
