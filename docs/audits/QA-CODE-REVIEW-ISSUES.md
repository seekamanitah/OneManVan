# Project Code Review (QA Focus) – OneManVan – June 2025

## Summary

This QA-focused review examines webpage links, navigation, buttons, responsiveness, and database integrity. **Overall risk level: MEDIUM**. The application has a well-structured navigation system with proper routing, but several issues exist including missing accessibility features, potential broken links in edge cases, missing input validation on some forms, database cascade delete concerns, and limited error handling for failed operations. The database schema is generally well-designed with proper relationships but lacks some indexes for performance optimization and has potential orphan record issues.

---

## Critical Issues

### QA-C-001: Homepage Route Returns 404
- **Severity**: CRITICAL
- **Location**: `OneManVan.Web/Components/Layout/NavMenu.razor` line 7, `App.razor`
- **Description**: The navbar brand links to `href=""` (root) but there is no page at the root route. The `Home.razor` page has `@page "/"` but may not be properly routed.
- **Evidence**: 
  ```razor
  <a class="navbar-brand" href="">OneManVan</a>
  ```
- **Impact**: Users clicking the logo get a 404 or blank page instead of landing page.

### QA-C-002: Customer Cascade Delete May Orphan Jobs
- **Severity**: CRITICAL
- **Location**: `OneManVan.Shared/Data/OneManVanDbContext.cs` lines 112-120
- **Description**: Customer cascade deletes Assets and Sites, but Jobs reference CustomerId and may become orphaned or cause foreign key constraint violations.
- **Evidence**:
  ```csharp
  entity.HasMany(e => e.Assets)
      .WithOne(a => a.Customer)
      .HasForeignKey(a => a.CustomerId)
      .OnDelete(DeleteBehavior.Cascade);
  ```
- **Impact**: Deleting a customer with active jobs could corrupt database integrity or fail silently.

---

## High Issues

### QA-H-001: Missing Loading States on Form Submissions
- **Severity**: HIGH
- **Location**: Multiple pages (CustomerEdit.razor, InvoiceEdit.razor, etc.)
- **Description**: While some pages have `isSaving` states, others lack visual feedback during form submission, leading to double-click submissions.
- **Impact**: Users may submit forms multiple times creating duplicate records.

### QA-H-002: Broken Icon References in NavMenu
- **Severity**: HIGH
- **Location**: `NavMenu.razor` lines 172, 180-181, 185
- **Description**: Non-existent Bootstrap Icon classes used.
- **Evidence**:
  ```razor
  <span class="bi bi-arrow-bar-left-nav-menu" aria-hidden="true"></span>
  <span class="bi bi-person-nav-menu" aria-hidden="true"></span>
  <span class="bi bi-person-badge-nav-menu" aria-hidden="true"></span>
  ```
- **Impact**: Missing icons for Logout, Register, and Login links.

### QA-H-003: No Confirmation Dialog for Delete Operations
- **Severity**: HIGH
- **Location**: Multiple list pages
- **Description**: Some delete operations use `confirm()` JavaScript but others lack any confirmation.
- **Impact**: Accidental data loss from misclicks.

### QA-H-004: Database Missing Indexes on Frequently Queried Columns
- **Severity**: HIGH
- **Location**: `OneManVanDbContext.cs`
- **Description**: While some indexes exist, several frequently-queried columns lack indexes.
- **Missing Indexes**:
  - `Invoices.InvoiceDate`
  - `Invoices.DueDate`
  - `Jobs.ScheduledDate`
  - `EmployeeTimeLogs.Date`
  - `Payments.PaymentDate`
- **Impact**: Slow queries as data grows, especially for date-range reports.

### QA-H-005: Invoice Status Transitions Not Validated
- **Severity**: HIGH
- **Location**: `InvoiceEdit.razor`
- **Description**: No validation prevents invalid status transitions (e.g., Draft ? Paid skipping Sent).
- **Impact**: Business logic inconsistency, reporting inaccuracies.

---

## Medium Issues

### QA-M-001: Duplicate Icon Class for Employees and Customers
- **Severity**: MEDIUM
- **Location**: `NavMenu.razor` lines 24, 113
- **Description**: Both "Customers" and "Employees" use `bi bi-people` icon class.
- **Impact**: Visual confusion, reduced UX clarity.

### QA-M-002: Missing Focus Trap in Modal Dialogs
- **Severity**: MEDIUM
- **Location**: All modal components (InvoiceEdit Line Item Modal, etc.)
- **Description**: Modals don't trap focus for keyboard navigation, allowing users to tab to elements behind the modal.
- **Impact**: Accessibility violation (WCAG 2.4.3), confusing for screen reader users.

### QA-M-003: Form Validation Only on Submit
- **Severity**: MEDIUM
- **Location**: Most form pages
- **Description**: Validation messages only appear after form submission, not on blur or input change.
- **Impact**: Poor UX - users must submit to discover errors.

### QA-M-004: No Pagination on Large Data Sets
- **Severity**: MEDIUM
- **Location**: `CustomerList.razor`, `InvoiceList.razor`, etc.
- **Description**: Lists load all records without pagination.
- **Evidence**: Most list pages use `.ToListAsync()` without `.Take()` or `.Skip()`.
- **Impact**: Performance degradation with large datasets, browser memory issues.

### QA-M-005: Missing ARIA Labels on Interactive Elements
- **Severity**: MEDIUM
- **Location**: Multiple pages
- **Description**: Many buttons and links lack `aria-label` attributes.
- **Evidence**: Action buttons like edit/delete icons have no accessible names.
- **Impact**: Screen reader users cannot understand button purposes.

### QA-M-006: Invoice Line Items Not Editable After Creation
- **Severity**: MEDIUM
- **Location**: `InvoiceEdit.razor`
- **Description**: While new line items can be added, editing existing line items may not properly update.
- **Impact**: Users may need to delete and recreate line items to make changes.

### QA-M-007: Search Not Debounced
- **Severity**: MEDIUM
- **Location**: List pages with search functionality
- **Description**: Search triggers on every keystroke (`@bind:event="oninput"`).
- **Impact**: Excessive database queries during typing, performance issues.

### QA-M-008: Dark Mode Toggle Missing from Several Pages
- **Severity**: MEDIUM
- **Location**: Various pages
- **Description**: ThemeToggle component exists but may not be accessible on all pages.
- **Impact**: Inconsistent theming experience.

---

## Low / Informational

### QA-L-001: Inconsistent Date Formatting
- **Severity**: LOW
- **Location**: Throughout the application
- **Description**: Some dates use "MMM dd, yyyy", others use "MM/dd/yyyy".
- **Impact**: Visual inconsistency.

### QA-L-002: Missing Empty State Messages
- **Severity**: LOW
- **Location**: Some list pages
- **Description**: While many pages have empty state views, some show blank content.
- **Impact**: User confusion when no data exists.

### QA-L-003: Console Logging Left in Production Code
- **Severity**: LOW
- **Location**: Various services and pages
- **Description**: `Console.WriteLine` statements in code (some removed in security fixes).
- **Impact**: Information disclosure in browser console or server logs.

### QA-L-004: Redundant Database Queries
- **Severity**: LOW
- **Location**: Pages with multiple data loads
- **Description**: Some pages make separate database calls that could be combined.
- **Impact**: Unnecessary database round trips.

### QA-L-005: Missing Tooltips on Icon-Only Buttons
- **Severity**: LOW
- **Location**: Action buttons in lists/tables
- **Description**: Icon-only buttons lack `title` attributes for hover tooltips.
- **Impact**: Users must guess icon meanings.

### QA-L-006: No Keyboard Shortcuts
- **Severity**: INFORMATIONAL
- **Location**: Entire application
- **Description**: No keyboard shortcuts for common actions (Ctrl+S to save, etc.).
- **Impact**: Reduced efficiency for power users.

### QA-L-007: Mobile Viewport Meta Tag
- **Severity**: INFORMATIONAL
- **Location**: Need to verify in `App.razor` or `_Host.cshtml`
- **Description**: Verify proper viewport meta tag exists for mobile responsiveness.

---

## Database-Specific Issues

### DB-001: Potential Orphan Records - Jobs Without Customers
- **Severity**: HIGH
- **Location**: `Job` entity relationship
- **Description**: If CustomerId FK constraint allows NULL or cascade isn't properly configured.
- **Remediation**: Audit foreign key constraints on Jobs table.

### DB-002: Missing Soft Delete on Related Entities
- **Severity**: MEDIUM
- **Location**: Various entities
- **Description**: Invoice has soft delete (IsDeleted), but line items don't cascade soft delete status.
- **Remediation**: Consider soft delete propagation or checking parent IsDeleted in queries.

### DB-003: No Audit Trail Table
- **Severity**: MEDIUM
- **Location**: Database schema
- **Description**: No audit logging of who created/modified/deleted records.
- **Remediation**: Consider adding audit table or using temporal tables.

### DB-004: Decimal Precision Inconsistencies
- **Severity**: LOW
- **Location**: Various models
- **Description**: Some monetary fields use `decimal(10,2)`, others `decimal(18,2)`.
- **Remediation**: Standardize decimal precision for monetary values.

### DB-005: Missing Default Values
- **Severity**: LOW
- **Location**: Various models
- **Description**: Some date fields default to `DateTime.Today` in C# but not in database schema.
- **Remediation**: Add database-level defaults for consistency.

---

## Navigation & Links Audit

| Page/Route | Link/Button | Status | Issue |
|------------|-------------|--------|-------|
| NavMenu | Brand Logo | ISSUE | Links to empty root, may 404 |
| NavMenu | Dashboard | OK | Correct routing |
| NavMenu | Customers | OK | Correct routing |
| NavMenu | Jobs | OK | Correct routing |
| NavMenu | Calendar | OK | Correct routing |
| NavMenu | Routes | OK | Correct routing |
| NavMenu | Invoices | OK | Correct routing |
| NavMenu | Estimates | OK | Correct routing |
| NavMenu | Material Lists | OK | Correct routing |
| NavMenu | Products | OK | Correct routing |
| NavMenu | Assets | OK | Correct routing |
| NavMenu | Inventory | OK | Correct routing |
| NavMenu | Warranty Claims | OK | Correct routing |
| NavMenu | Documents | OK | Correct routing |
| NavMenu | Employees | OK | Correct routing |
| NavMenu | Companies | OK | Correct routing |
| NavMenu | Sites | OK | Correct routing |
| NavMenu | Agreements | OK | Correct routing |
| NavMenu | Quick Notes | OK | Correct routing |
| NavMenu | Settings | OK | Correct routing |
| NavMenu | Login/Register | ISSUE | Broken icon classes |
| NavMenu | Logout | ISSUE | Broken icon class |
| Dashboard | Metric Cards | OK | Clickable, navigate correctly |
| List Pages | Edit Links | OK | Navigate to edit pages |
| List Pages | View Links | OK | Navigate to detail pages |
| Detail Pages | Back Button | OK | Navigate back to list |

---

## Responsiveness Audit

| Component | Desktop | Tablet | Mobile | Issue |
|-----------|---------|--------|--------|-------|
| NavMenu | OK | OK | Toggles correctly | None |
| Dashboard Cards | OK | 2 columns | 1 column | None |
| List Tables | OK | Scrollable | Scrollable | Consider card view |
| Forms | OK | OK | OK | None |
| Modals | OK | OK | May overflow | Test needed |
| Date Pickers | OK | OK | Touch issues | Potential |

---

## Button & Interaction Audit

| Location | Element | Type | Issue |
|----------|---------|------|-------|
| All Forms | Submit | Button | Some lack loading indicators |
| All Forms | Cancel | Button | OK |
| List Pages | Delete | Button | Some lack confirmation |
| Invoice Edit | Add Line Item | Button | OK |
| Invoice Edit | Add Labor | Button | OK |
| Customer Edit | Save | Button | Has loading state |
| Settings | Save | Button | Has loading state |
| Modals | Close (X) | Button | Some lack ARIA label |
| Dropdowns | All | Select | OK |
| Checkboxes | All | Input | OK |

---

## Steps to Remediation

### Critical Issues

**QA-C-001: Homepage Route**
- Ensure `Home.razor` or `Index.razor` exists with `@page "/"` route
- Alternatively, redirect root to `/dashboard`
- Update brand link to explicit route

**QA-C-002: Customer Cascade Delete**
- Add proper cascade or restrict behavior for Jobs FK
- Consider soft-delete approach for customers with relationships
- Add validation before hard delete checking for related records

### High Issues

**QA-H-001: Loading States**
- Add `isSaving` bool to all form components
- Disable submit button while saving
- Show spinner or "Saving..." text

**QA-H-002: Broken Icons**
- Replace with valid Bootstrap Icon classes:
  - `bi-box-arrow-left` for logout
  - `bi-person-plus` for register
  - `bi-person` for login

**QA-H-003: Delete Confirmations**
- Implement consistent `confirm()` or custom modal for all deletes
- Consider "Undo" pattern instead of confirm dialogs

**QA-H-004: Database Indexes**
- Add indexes via migration:
  ```sql
  CREATE INDEX IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);
  CREATE INDEX IX_Invoices_DueDate ON Invoices(DueDate);
  CREATE INDEX IX_Jobs_ScheduledDate ON Jobs(ScheduledDate);
  ```

**QA-H-005: Status Transitions**
- Create state machine for invoice status
- Validate transitions in service layer
- Show only valid next states in UI dropdown

### Medium Issues

**QA-M-001 through QA-M-008**
- Assign unique icons to differentiate sections
- Implement focus trap in modals (use JS interop or library)
- Add `@bind:after` or blur handlers for real-time validation
- Implement server-side pagination with skip/take
- Add `aria-label` to all interactive elements
- Ensure line item edit updates work correctly
- Add debounce to search inputs (300-500ms)
- Verify ThemeToggle availability on all pages

### Low Issues

- Standardize date formatting using a shared helper
- Add empty state messages to all list pages
- Remove or guard Console.WriteLine statements
- Combine related database queries where possible
- Add `title` attributes to icon buttons
- Consider keyboard shortcuts for common actions

---

## Testing Checklist

### Links & Navigation
- [ ] Click every NavMenu link - verify no 404s
- [ ] Click logo - verify expected behavior
- [ ] Test login/logout flows
- [ ] Test breadcrumb navigation
- [ ] Test browser back/forward buttons

### Forms & Buttons
- [ ] Submit every form with valid data
- [ ] Submit every form with invalid data
- [ ] Double-click submit buttons
- [ ] Test all delete operations
- [ ] Test all cancel operations
- [ ] Test modal open/close

### Database Operations
- [ ] Create records in all entities
- [ ] Update records in all entities
- [ ] Delete records with relationships
- [ ] Verify cascade deletes work correctly
- [ ] Check for orphan records
- [ ] Test with large datasets (1000+ records)

### Responsiveness
- [ ] Test on desktop (1920x1080)
- [ ] Test on tablet (768x1024)
- [ ] Test on mobile (375x667)
- [ ] Test modal behavior on mobile
- [ ] Test form layout on mobile

### Accessibility
- [ ] Tab through all interactive elements
- [ ] Test with screen reader
- [ ] Verify color contrast
- [ ] Test without mouse

---

I have not modified, suggested, or written any corrected code — only identified and described issues and created a report file to follow.
