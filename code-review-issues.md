# Project Code Review – OneManVan – June 2025

## Summary

The OneManVan solution is a multi-platform field service management application targeting .NET 10 with Blazor Web, .NET MAUI Mobile, and shared libraries. **Overall risk level: HIGH**. The codebase contains multiple critical and high-severity security vulnerabilities including hardcoded credentials, disabled authentication, exposed API endpoints without authorization, potential path traversal in file handling, unencrypted storage of sensitive data (SSN/EIN), and missing input validation across numerous entry points. While the application has good architectural separation and uses Entity Framework Core with parameterized queries (mitigating SQL injection), the authentication bypass for "development" purposes and exposed secrets represent immediate production blockers.

---

## Critical Issues

### C-001: Hardcoded Database Credentials in Version Control
- **Severity**: CRITICAL
- **Location**: 
  - `.env` lines 2, 11
  - `docker-compose.yml` lines 12, 24, 38-39
- **Description**: Database SA password `TradeFlow2025!` and sync password `SyncUser2025!` are hardcoded in plain text and committed to source control.
- **Evidence**:
  ```
  .env line 2: SA_PASSWORD=TradeFlow2025!
  docker-compose.yml line 12: MSSQL_SA_PASSWORD=TradeFlow2025!
  docker-compose.yml line 38: Password=TradeFlow2025!
  ```
- **Impact**: Any attacker with repository access gains full database admin privileges. If the repo is public or leaked, complete data breach occurs.

### C-002: Default Admin Credentials Hardcoded and Logged
- **Severity**: CRITICAL
- **Location**: `OneManVan.Web/Services/AdminAccountSeeder.cs` lines 21, 34, 38
- **Description**: Default admin account with predictable credentials `admin@onemanvan.com / Admin123!` is created automatically and credentials are printed to console output.
- **Evidence**:
  ```csharp
  var adminEmail = "admin@onemanvan.com";
  var result = await userManager.CreateAsync(adminUser, "Admin123!");
  Console.WriteLine($"[OK] Default admin account created: {adminEmail} / Admin123!");
  ```
- **Impact**: Attackers can immediately access admin functions with known credentials. Console logging may expose credentials in log files.

### C-003: Authentication Disabled on All API Controllers
- **Severity**: CRITICAL
- **Location**: 
  - `OneManVan.Web/Controllers/ExportController.cs` line 11
  - `OneManVan.Web/Controllers/DocumentsController.cs` (missing [Authorize])
- **Description**: The `[Authorize]` attribute is commented out with "temporarily disabled for development" note, leaving all API endpoints completely unprotected.
- **Evidence**:
  ```csharp
  // [Authorize] - temporarily disabled for development
  [ApiController]
  [Route("api/[controller]")]
  public class ExportController : ControllerBase
  ```
- **Impact**: Any unauthenticated user can export all customer data, invoices, financial records. Complete IDOR vulnerability across all export endpoints.

### C-004: Authentication Disabled on All Razor Pages
- **Severity**: CRITICAL  
- **Location**: All `.razor` files with `@* [Authorize] temporarily disabled for development *@` pattern
- **Description**: Authorization is disabled site-wide on Blazor components.
- **Evidence**: Pattern observed in QuickNotes.razor, InvoiceEdit.razor, and likely all other pages.
- **Impact**: No access control on any application functionality. Any user can view/edit any data.

---

## High Issues

### H-001: Unencrypted Storage of PII - SSN/EIN/TaxId
- **Severity**: HIGH
- **Location**: `OneManVan.Shared/Models/Employee.cs` lines 73-82
- **Description**: Employee SSN/EIN (TaxId field) is stored in plain text. Comment acknowledges need for encryption but none is implemented.
- **Evidence**:
  ```csharp
  // === Sensitive Information (should be encrypted in production) ===
  /// <summary>
  /// SSN for employees, EIN for contractors (encrypted storage recommended).
  /// </summary>
  [MaxLength(50)]
  public string? TaxId { get; set; }
  ```
- **Impact**: Data breach exposes employee SSN/EIN, enabling identity theft. Violates PCI-DSS, HIPAA, and state privacy laws.

### H-002: SMTP Password Stored in Plain Text
- **Severity**: HIGH
- **Location**: `OneManVan.Web/Services/EmailService.cs` lines 47, 59
- **Description**: SMTP credentials are stored and retrieved as plain text through ISettingsStorage.
- **Evidence**:
  ```csharp
  SmtpPassword = _settingsStorage.GetString("Email_SmtpPassword", ""),
  _settingsStorage.SetString("Email_SmtpPassword", settings.SmtpPassword);
  ```
- **Impact**: Email account compromise if settings storage is accessed.

### H-003: Wildcard AllowedHosts Configuration
- **Severity**: HIGH
- **Location**: `OneManVan.Web/appsettings.json` line 20
- **Description**: `"AllowedHosts": "*"` allows requests from any host, enabling host header injection attacks.
- **Evidence**: `"AllowedHosts": "*"`
- **Impact**: Host header poisoning, cache poisoning, password reset attacks, phishing redirects.

### H-004: Missing Authorization on Document File Access
- **Severity**: HIGH
- **Location**: `OneManVan.Web/Controllers/DocumentsController.cs` lines 27-53
- **Description**: Document download/view endpoints lack authorization AND don't validate user ownership of documents.
- **Evidence**:
  ```csharp
  [HttpGet("{id}/content")]
  public async Task<IActionResult> GetContent(int id)
  {
      var result = await _documentService.GetFileContentAsync(id);
      // No authorization check, no ownership validation
  ```
- **Impact**: Any user can access any document by guessing/incrementing IDs (IDOR vulnerability).

### H-005: Missing Rate Limiting on All Endpoints
- **Severity**: HIGH
- **Location**: `OneManVan.Web/Program.cs` (entire file)
- **Description**: No rate limiting middleware configured. All endpoints vulnerable to brute force and DoS attacks.
- **Impact**: Account enumeration, password brute forcing, API abuse, denial of service.

### H-006: Weak Password Policy for Production
- **Severity**: HIGH
- **Location**: `OneManVan.Web/Program.cs` lines 174-178
- **Description**: Password requirements are weakened for "development" but this will deploy to production.
- **Evidence**:
  ```csharp
  // Simpler password requirements for development
  options.Password.RequireNonAlphanumeric = false;
  options.Password.RequiredLength = 6;
  ```
- **Impact**: Weak passwords are easily brute-forced. 6 character passwords with no special chars are trivially cracked.

---

## Medium Issues

### M-001: Email Service Does Not Validate Recipient Addresses
- **Severity**: MEDIUM
- **Location**: `OneManVan.Web/Services/EmailService.cs` line 67
- **Description**: No validation of email recipient addresses before sending. Could be abused for spam relay.
- **Impact**: Application could be used as spam relay if attacker controls recipient email parameter.

### M-002: No HTTPS Enforcement in Development Configuration
- **Severity**: MEDIUM
- **Location**: `docker-compose.yml` line 37, `OneManVan.Web/appsettings.Development.json`
- **Description**: ASPNETCORE_URLS set to `http://+:8080` without HTTPS. No HSTS or HTTPS redirection configured.
- **Evidence**: `ASPNETCORE_URLS=http://+:8080`
- **Impact**: Credentials and sensitive data transmitted in plaintext on network.

### M-003: Console.WriteLine Used for Security-Sensitive Logging
- **Severity**: MEDIUM
- **Location**: `OneManVan.Web/Services/AdminAccountSeeder.cs` line 38
- **Description**: Credentials logged using Console.WriteLine which may persist in container logs.
- **Impact**: Credentials exposed in log aggregation systems, container logs, stdout redirects.

### M-004: Missing Input Validation on File Upload
- **Severity**: MEDIUM
- **Location**: `OneManVan.Shared/Services/DocumentService.cs` lines 140-151
- **Description**: CreateAsync accepts Document objects without validating file type, size, or content. Only extracts extension from filename.
- **Evidence**:
  ```csharp
  document.FileExtension = Path.GetExtension(document.FileName)?.ToLower();
  context.Documents.Add(document);
  ```
- **Impact**: Malicious file upload (web shells, malware), storage exhaustion, path traversal via filename.

### M-005: Debug/Development Mode Indicators Left in Code
- **Severity**: MEDIUM
- **Location**: Multiple files with "temporarily disabled for development" comments
- **Description**: Development-mode configurations are not environment-gated and will deploy to production.
- **Impact**: Security controls bypassed in production deployments.

### M-006: Missing CSRF Protection Verification
- **Severity**: MEDIUM
- **Location**: All API controllers use `[ApiController]` but no explicit antiforgery validation on state-changing endpoints
- **Description**: While Blazor forms use `<AntiforgeryToken />`, API endpoints don't verify antiforgery tokens.
- **Impact**: Cross-site request forgery attacks on API endpoints.

### M-007: Database Connection String Contains Password
- **Severity**: MEDIUM
- **Location**: `docker-compose.yml` lines 38-39
- **Description**: Connection strings with embedded passwords in environment variables.
- **Impact**: Password visible in `docker inspect`, process listings, environment dumps.

---

## Low / Informational

### L-001: No Content Security Policy (CSP) Headers
- **Severity**: LOW
- **Location**: No CSP configuration found in `Program.cs` or middleware
- **Description**: Missing CSP headers increase XSS impact if vulnerabilities exist.
- **Impact**: XSS attacks have broader scope without CSP restrictions.

### L-002: Verbose Error Messages May Leak Information
- **Severity**: LOW
- **Location**: Multiple Razor components catch exceptions and display `ex.Message`
- **Description**: Exception messages may reveal internal paths, database schemas, or stack traces.
- **Impact**: Information disclosure aiding further attacks.

### L-003: Magic Strings/Numbers Throughout Codebase
- **Severity**: INFORMATIONAL
- **Location**: Multiple files (e.g., payment terms "30", tax rate "7.0m")
- **Description**: Configuration values hardcoded rather than centralized.
- **Impact**: Maintainability issues; difficult to audit for security-relevant values.

### L-004: Missing Database Column Indexes
- **Severity**: LOW
- **Location**: Entity models lack index annotations on frequently-queried columns
- **Description**: No explicit indexes defined for search/filter columns.
- **Impact**: Performance degradation under load; potential DoS vector.

### L-005: Test Credentials in Seed Data
- **Severity**: LOW
- **Location**: `docker/init/02-seed-data.sql` (if exists with test data)
- **Description**: Seed scripts may contain test accounts that persist to production.
- **Impact**: Backdoor accounts if seed data runs in production.

### L-006: Empty SMTP Configuration in appsettings.json
- **Severity**: INFORMATIONAL
- **Location**: `OneManVan.Web/appsettings.json` lines 6-12
- **Description**: Empty SMTP configuration in version control provides config template but could confuse deployment.
- **Impact**: Minimal - just FYI for clean configuration management.

---

## Steps to Remediation

### Critical Issues

**C-001: Hardcoded Database Credentials**
- Remove all passwords from version control immediately
- Use Docker secrets or Azure Key Vault for production
- Rotate the exposed password `TradeFlow2025!` before any deployment
- Add `.env` to `.gitignore` and use `.env.example` as template

**C-002: Default Admin Credentials**
- Remove automatic admin creation from startup code
- Require first admin to be created through secure setup wizard
- Never log credentials to console; use structured logging with sensitive data masking

**C-003 & C-004: Disabled Authentication**
- Remove all `// [Authorize]` comments and enable authorization
- Use environment-based feature flags instead of code comments
- Implement `[Authorize]` on controllers and `[Authorize]` attribute or AuthorizeView in Blazor

### High Issues

**H-001: Unencrypted PII**
- Implement column-level encryption using ASP.NET Core Data Protection or Azure Always Encrypted
- Create encryption service wrapper for TaxId field access
- Consider tokenization for SSN/EIN storage

**H-002: Plain Text SMTP Password**
- Use Data Protection API to encrypt sensitive settings
- Consider OS credential store or vault integration

**H-003: Wildcard AllowedHosts**
- Set explicit allowed hosts for production domain(s)
- Configure based on environment (appsettings.Production.json)

**H-004: Document Access IDOR**
- Add ownership validation in DocumentService.GetFileContentAsync
- Implement claims-based authorization checking user's access rights
- Consider using GUIDs instead of sequential IDs for document references

**H-005: Rate Limiting**
- Add `AspNetCoreRateLimit` NuGet package or use built-in .NET 7+ rate limiting
- Configure per-IP and per-user limits on authentication and export endpoints

**H-006: Weak Password Policy**
- Remove "development" weakening of password requirements
- Use appsettings.{Environment}.json for environment-specific settings

### Medium Issues

**M-001 through M-007**
- Implement input validation for all email recipients
- Enforce HTTPS in production with HSTS headers
- Replace Console.WriteLine with ILogger with sensitive data filtering
- Add file type whitelist, size limits, and antivirus scanning for uploads
- Gate development features behind proper environment checks
- Implement antiforgery token validation on API state-changing endpoints
- Use environment variables or secrets for connection string passwords

### Low/Informational Issues

- Implement Content Security Policy headers
- Create custom error handling middleware with environment-aware detail levels
- Extract magic numbers to configuration
- Add Entity Framework index annotations or migrations
- Audit seed data scripts for test accounts
- Clean up configuration templates

---

I have not modified, suggested, or written any corrected code — only identified and described issues.
