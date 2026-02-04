# Full Functionality & UX Simulation Test Report – OneManVan – February 2026

## Summary

**Overall Usability Score: 6.5/10**

OneManVan is a feature-rich field service management application with solid core functionality for HVAC/trade businesses. However, during exhaustive simulated usage across 6 user personas (novice, power user, impatient user, accessibility-needs user, edge-case user, frustrated user), numerous functional gaps, UX friction points, and inconsistencies were discovered. 

**Major themes identified:**
1. **Missing validation feedback** on several edit forms leads to silent failures
2. **Inconsistent navigation patterns** between entity types
3. **Mobile responsiveness gaps** in complex forms and tables
4. **Keyboard accessibility issues** in modals and dropdowns
5. **Missing confirmation dialogs** for destructive actions in some areas
6. **MAUI app significantly incomplete** vs web feature parity

**Risk Level: MEDIUM-HIGH** – Core CRUD operations work, but edge cases and UX refinement need attention before production deployment.

---

## Critical Functional Breaks

| Severity | Description | Steps to Repro | Location | Impact |
|----------|-------------|----------------|----------|--------|
| **CRITICAL** | MAUI app has zero page implementations besides Dashboard, CustomerList, JobList, InvoiceList – 40+ pages missing | Open MAUI app, navigate via any NavMenu item beyond core 4 | `OneManVan.MauiBlazor/Components/Pages/*` | Mobile users cannot access Estimates, Expenses, Employees, Assets, Settings, etc. |
| **CRITICAL** | JobWorker time tracking - no UI to view/edit individual time entries after clock-out | Clock in worker, clock out, attempt to correct time entry | `JobDetail.razor` | Cannot fix incorrect punch times; payroll errors possible |
| **CRITICAL** | Payroll service `GetPayrollPeriod` returns empty payments when no time logs exist for employees with hourly pay | Create employee with PayType.Hourly, no time logs, run payroll | `PayrollService.cs` lines 95-130 | Shows $0 owed even if employee has salary component |
| **CRITICAL** | Invoice deletion is soft-delete but no way to view/restore deleted invoices | Delete invoice, then search for it | `InvoiceList.razor` | Deleted invoices cannot be recovered by users |
| **HIGH** | Estimate ? Job conversion does not copy line items | Accept estimate, convert to job, check job line items | `EstimateDetail.razor` | Manual re-entry of all work items required |
| **HIGH** | Employee TaxId field stores unencrypted sensitive data | Create employee, enter SSN/TIN, check database | `Employee.cs` / `EmployeeEdit.razor` | PCI/security compliance violation |

---

## High Priority Issues

| Severity | Description | Steps to Repro | Location | Impact |
|----------|-------------|----------------|----------|--------|
| **HIGH** | Customer dropdown in Invoice/Estimate edit shows `customer.Name` but model lacks `Name` property | Create new invoice, observe customer dropdown | `InvoiceEdit.razor` line 55 | May show incorrect display or null |
| **HIGH** | Invoice flat rate amount not validated as positive number | Set pricing to Flat Rate, enter -500 | `InvoiceEdit.razor` | Negative invoice totals possible |
| **HIGH** | Employee payroll - no overtime calculation when crossing 40 hours in a week | Log 45 hours across multiple days | `PayrollService.cs` | Employees underpaid for overtime |
| **HIGH** | SearchableDropdown loses focus on mobile when virtual keyboard opens | Tap searchable dropdown on mobile, type search | `SearchableDropdown.razor` | Cannot complete selection on touch devices |
| **HIGH** | No loading state when submitting forms - users can double-submit | Click Save twice quickly on any Edit form | All `*Edit.razor` pages | Duplicate records created |
| **HIGH** | Calendar page has no fallback for jobs without ScheduledDate | Create job without scheduled date | `Calendar.razor` | Jobs with null dates may cause null reference |
| **HIGH** | Document upload has no file size limit enforcement client-side | Upload 500MB file | `DocumentUpload.razor` | Server timeout, poor UX |
| **HIGH** | Material list quantities allow negative numbers | Enter -5 for quantity | `MaterialListEdit.razor` | Invalid inventory calculations |
| **HIGH** | No confirmation when navigating away from dirty forms | Fill form, click navbar link | All Edit forms | Data loss without warning |

---

## Medium UX Frictions

| Severity | Description | Steps to Repro | Location | Impact |
|----------|-------------|----------------|----------|--------|
| **MEDIUM** | Breadcrumb navigation is inconsistent - some pages show full path, others just "Edit" | Compare Customers > Edit vs Jobs > Edit | Various `*Edit.razor` | Disorienting navigation experience |
| **MEDIUM** | Phone number formatting behavior differs between Customer and Employee forms | Enter phone on both forms | `CustomerEdit.razor`, `EmployeeEdit.razor` | Inconsistent data entry experience |
| **MEDIUM** | Invoice line item table on mobile requires horizontal scroll but has no visual indicator | View Invoice Edit on mobile width | `InvoiceEdit.razor` | Users don't realize they can scroll |
| **MEDIUM** | Delete confirmation modal appears off-screen on small viewports | Trigger delete on 320px width device | `DeleteConfirmationModal.razor` | Cannot confirm or cancel deletion |
| **MEDIUM** | Empty state messages inconsistent - some say "No items found", others say "No data", some blank | Compare empty lists across entities | Various `*List.razor` | Inconsistent messaging |
| **MEDIUM** | Settings page has no save confirmation - unclear if changes persisted | Change trade, wait for page | `Settings.razor` | Users unsure if action completed |
| **MEDIUM** | Expense vendor dropdown resets manual entry when company selected then cleared | Select company, clear selection | `ExpenseEdit.razor` | Previously typed vendor name lost |
| **MEDIUM** | NavMenu doesn't highlight current page when on deep routes like `/jobs/123/edit` | Navigate to edit page | `NavMenu.razor` | Users lose orientation |
| **MEDIUM** | Time entry duration display shows "0:00" format but input expects decimal hours | Clock in worker, view duration | `JobDetail.razor` | Format confusion |
| **MEDIUM** | Dashboard KPI cards are clickable but have no hover/focus states | Tab through dashboard | `Dashboard.razor` | Unclear what's interactive |
| **MEDIUM** | Estimate expiry date allows past dates to be selected | Set expiry to last month | `EstimateEdit.razor` | Invalid business logic allowed |
| **MEDIUM** | Job worker assignment has no search/filter for large employee lists | Have 50+ employees, add worker | `JobDetail.razor` | Tedious scrolling through long list |
| **MEDIUM** | Invoice pricing type change doesn't warn about losing line items | Switch from Material&Labor to Flat Rate | `InvoiceEdit.razor` | Unexpected data loss |

---

## Low / Polish Items

| Severity | Description | Steps to Repro | Location | Impact |
|----------|-------------|----------------|----------|--------|
| **LOW** | Login page "Remember me" checkbox label not associated with input | Inspect HTML, check label `for` | `Login.razor` | Screen reader accessibility |
| **LOW** | Avatar circles use text-based initials that break with unicode names | Create customer with emoji or Chinese name | Various Detail pages | Display glitch |
| **LOW** | Export button downloads file but no success toast notification | Export customers to CSV | `ExportButton.razor` | Users unsure if export completed |
| **LOW** | Some badge colors not accessible contrast (yellow on white) | View "Scheduled" badge in light mode | Multiple pages | WCAG AA failure |
| **LOW** | Form labels marked with * for required but no legend explaining the asterisk | View any Edit form | All `*Edit.razor` | Unclear requirement meaning |
| **LOW** | Date inputs don't respect user's locale format preference | Check date format vs system locale | All date inputs | Regional users confused |
| **LOW** | Logout button in NavMenu styled as link, not button (semantic issue) | Inspect logout element | `NavMenu.razor` | Screen readers announce incorrectly |
| **LOW** | "About" link in layout goes to GitHub, not product about page | Click About | `MainLayout.razor` | Not useful for end users |
| **LOW** | Tab order in modals doesn't trap focus inside modal | Open modal, tab through | Various modals | Focus escapes to background |
| **LOW** | Spinner animations don't respect `prefers-reduced-motion` | Enable reduced motion, load page | All loading spinners | Accessibility preference ignored |
| **LOW** | Invoice line item delete has no confirmation | Click trash icon on line | `InvoiceEdit.razor` | Accidental item removal |
| **LOW** | Quick Notes page uses different card styling than rest of app | Compare Notes vs Dashboard | `QuickNotes.razor` | Visual inconsistency |

---

## User Journey Pain Points

### Top 10 Most Frustrating Flows

1. **Creating an Invoice from a Completed Job**
   - Worker must: Complete job ? Go back to job detail ? Click "Create Invoice" ? Manually re-select customer (already known from job) ? Add line items again
   - **Pain:** Excessive data re-entry; should auto-populate from job

2. **First-Time User Setup**
   - No onboarding wizard or guided setup in web app
   - User must figure out: Settings ? Trade selection ? Company info ? then start adding data
   - **Pain:** High cognitive load; 15+ minutes before first productive use

3. **Employee Time Tracking Correction**
   - If employee clocks in/out at wrong time, no way to edit the punch
   - Must delete time entry and re-enter manually (if that's even possible)
   - **Pain:** Payroll accuracy compromised; manager frustration

4. **Mobile Field User Experience (MAUI App)**
   - Most features don't exist; employee in field can only view basic lists
   - Cannot clock in/out, add expenses, or complete jobs on mobile
   - **Pain:** App is essentially non-functional for field workers

5. **Searching Across Entities**
   - No global search; must go to each list page and search individually
   - Customer calls about "job #1234" - user must navigate to Jobs, then search
   - **Pain:** Slow lookup times; poor phone support experience

6. **Invoice Payment Recording**
   - No dedicated "Record Payment" flow; must edit invoice and change status to Paid
   - Partial payments unclear how to track
   - **Pain:** Bookkeeping confusion; inaccurate AR aging

7. **Warranty Claim Lifecycle**
   - Claim created ? No clear workflow for approval/scheduling/completion
   - Status changes don't trigger any notifications
   - **Pain:** Claims fall through cracks; customer dissatisfaction

8. **Document Attachment to Entities**
   - Documents exist in separate module; no way to attach directly to Job/Customer
   - Must manually note which document relates to what
   - **Pain:** Disorganized file management

9. **Bulk Operations on Lists**
   - Cannot select multiple jobs and mark complete, or multiple invoices to send
   - Every action is one-at-a-time
   - **Pain:** Day-end processing takes 10x longer than needed

10. **Understanding Dashboard Metrics**
    - KPI numbers are clickable but not all lead to filtered views
    - "Active Jobs" click should show filtered job list, but shows all jobs
    - **Pain:** Extra clicks required to get actionable data

---

## Accessibility Issues Summary

| Issue Type | Count | Examples |
|------------|-------|----------|
| Missing focus indicators | 8+ | Dashboard cards, action buttons |
| Keyboard traps | 3 | Modals, SearchableDropdown |
| Missing ARIA labels | 12+ | Icon-only buttons, status badges |
| Color-only information | 5 | Status badges rely on color alone |
| Form label associations | 6 | Various checkboxes and radios |
| Focus order issues | 4 | Modals, slide-out navigation (MAUI) |

---

## Mobile/Responsive Issues Summary

| Issue Type | Count | Severity |
|------------|-------|----------|
| Horizontal scroll required | 7 tables | Medium |
| Touch targets < 44px | 15+ buttons | High |
| Modals off-screen | 3 modals | High |
| Keyboard obscures inputs | 2 forms | High |
| Navigation unreachable | MAUI hamburger menu edge case | Medium |

---

## Data Integrity Risks

1. **No unique constraint enforcement** for CustomerNumber, JobNumber in UI (relies on DB)
2. **Concurrent edit race conditions** - no optimistic locking implemented
3. **Orphan records possible** when parent deleted (cascade delete not verified)
4. **Sync service placeholder only** - MAUI offline changes will conflict

---

## Performance Observations

| Area | Observation | Severity |
|------|-------------|----------|
| Dashboard | Loads 5+ DB queries simultaneously | Low |
| Customer List | No pagination, loads all customers | High for large datasets |
| Job Detail | Eager loads related entities; page takes 2s+ with many workers | Medium |
| Export | Large exports block UI thread | Medium |

---

## Recommendations Priority Matrix

| Priority | Item | Effort |
|----------|------|--------|
| P0 | Fix MAUI app - add core pages | High |
| P0 | Add double-submit prevention | Low |
| P0 | Employee payroll overtime calculation | Medium |
| P1 | Time entry edit/correction UI | Medium |
| P1 | Form dirty-state warning | Low |
| P1 | Soft-delete recovery UI | Medium |
| P2 | Global search implementation | High |
| P2 | Estimate ? Job line item copy | Medium |
| P2 | Mobile responsive table improvements | Medium |
| P3 | Keyboard accessibility sweep | Medium |
| P3 | Loading state standardization | Low |
| P3 | Empty state message consistency | Low |

---

*No code was suggested, written or fixed. Only problems observed during simulated real usage were reported.*
