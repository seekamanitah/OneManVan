# OneManVan Mobile UI Modernization Roadmap

**Goal:** Rebuild mobile UI to match Web UI visual style while keeping all existing backend logic.  
**Approach:** One page at a time, incremental improvement.  
**Estimated Total Time:** 3-4 weeks (part-time)

---

## ?? Current State Assessment

### What's Working Well
- ? Database layer (OneManVan.Shared)
- ? Services and business logic
- ? SQL Server remote connectivity
- ? Most existing pages function correctly
- ? Navigation structure

### What Needs Improvement
- ?? Visual design doesn't match Web UI
- ?? No tablet/landscape optimization
- ?? Performance issues on some pages (Calendar, Sites, Warranties)
- ?? Inconsistent styling across pages
- ?? No responsive layouts

### Pages Currently Disabled (Performance Issues)
- ? CalendarPage
- ? SiteListPage
- ? WarrantyClaimsListPage

---

## ?? Design Principles

### 1. Match Web UI Style
- Same color palette (#3b82f6 primary blue)
- Card-based layouts
- Consistent spacing (16px standard padding)
- Similar typography hierarchy

### 2. Mobile-First, Tablet-Aware
- Portrait: Standard mobile layout
- Landscape/Tablet: Consider master-detail or 2-column layouts
- Use `OnIdiom` for device-specific adjustments

### 3. Performance First
- Load data on background threads
- Use pagination (never load ALL data)
- Virtualized lists (CollectionView)
- Show loading states

### 4. Consistency
- Reusable components
- Shared styles/colors
- Standard patterns for list?detail navigation

---

## ?? Page-by-Page Modernization Plan

### Phase 1: Foundation & Core Pages (Week 1)

#### 1.1 Design System Update ? DONE
- [x] Update Colors.xaml with Web UI palette
- [x] Create MainPageModern.xaml as proof-of-concept
- [ ] Create shared component library

#### 1.2 Dashboard (MainPage) ? IN PROGRESS
**Current:** Basic list with some metrics
**Target:** Card grid with key metrics, recent activity, quick actions

**Files:**
- `MainPageModern.xaml` ? Created
- `MainPageModern.xaml.cs` ? Created

**Features:**
- [x] Key metrics grid (Revenue, Jobs, Customers, Invoices)
- [x] Recent activity feed
- [x] Quick action buttons
- [ ] Pull-to-refresh
- [ ] Tablet landscape layout

#### 1.3 Customer List Page
**Priority:** HIGH (most used page)
**Current:** Basic list with search
**Target:** Modern card list with avatars, quick actions

**Tasks:**
- [ ] Create CustomerListPageModern.xaml
- [ ] Card-based list items
- [ ] Avatar/initials display
- [ ] Swipe actions (call, email, navigate)
- [ ] Search with filters
- [ ] Tablet: Master-detail layout

#### 1.4 Customer Detail Page
**Priority:** HIGH
**Current:** Basic form layout
**Target:** Card sections matching Web UI

**Tasks:**
- [ ] Create CustomerDetailPageModern.xaml
- [ ] Contact info card
- [ ] Sites card (expandable)
- [ ] Assets card (expandable)
- [ ] Recent jobs card
- [ ] Quick action FAB

---

### Phase 2: Job & Estimate Pages (Week 2)

#### 2.1 Job List Page
**Priority:** HIGH
**Current:** Basic list
**Target:** Card list with status badges, filters

**Tasks:**
- [ ] Create JobListPageModern.xaml
- [ ] Status badge colors
- [ ] Date/time display
- [ ] Filter by status, date
- [ ] Swipe actions (start, complete)

#### 2.2 Job Detail Page
**Priority:** HIGH
**Current:** Form layout
**Target:** Card sections with timeline

**Tasks:**
- [ ] Job info card
- [ ] Customer/site card
- [ ] Timeline/status stepper
- [ ] Parts/materials card
- [ ] Signature capture
- [ ] Photo gallery

#### 2.3 Estimate List Page
**Priority:** MEDIUM
**Current:** Basic list
**Target:** Card list with totals, status

**Tasks:**
- [ ] Card-based items
- [ ] Total amount prominent
- [ ] Status badges
- [ ] Quick convert to job

#### 2.4 Estimate Detail Page
**Priority:** MEDIUM
**Current:** Form layout
**Target:** Line items table, customer info

**Tasks:**
- [ ] Customer info header
- [ ] Line items list
- [ ] Totals summary
- [ ] Actions (send, convert)

---

### Phase 3: Invoice & Inventory Pages (Week 3)

#### 3.1 Invoice List Page
**Priority:** MEDIUM
**Current:** Basic list
**Target:** Card list with payment status

**Tasks:**
- [ ] Card with amount/status
- [ ] Overdue highlighting
- [ ] Filter by status
- [ ] Quick payment entry

#### 3.2 Invoice Detail Page
**Priority:** MEDIUM
**Current:** Form layout
**Target:** Professional invoice view

**Tasks:**
- [ ] Invoice header
- [ ] Line items
- [ ] Payment history
- [ ] Send/print actions

#### 3.3 Inventory List Page
**Priority:** LOW
**Current:** Basic list
**Target:** Card grid with stock levels

**Tasks:**
- [ ] Stock level indicators
- [ ] Low stock warnings
- [ ] Search by SKU/name

#### 3.4 Product List Page
**Priority:** LOW
**Current:** Basic list
**Target:** Card grid with images

**Tasks:**
- [ ] Product cards
- [ ] Price display
- [ ] Quick add to job

---

### Phase 4: Assets & Advanced Pages (Week 4)

#### 4.1 Asset List Page
**Priority:** MEDIUM
**Current:** Basic list
**Target:** Card list with equipment info

**Tasks:**
- [ ] Equipment type icons
- [ ] Warranty status indicator
- [ ] Last service date
- [ ] QR/barcode scanner integration

#### 4.2 Asset Detail Page
**Priority:** MEDIUM
**Current:** Form layout
**Target:** Equipment profile card

**Tasks:**
- [ ] Equipment photo
- [ ] Specs card
- [ ] Service history
- [ ] Warranty info

#### 4.3 Quick Notes Page (NEW)
**Priority:** HIGH
**Current:** Not implemented
**Target:** Match Web UI Quick Notes

**Tasks:**
- [ ] Create QuickNotesPage.xaml
- [ ] Note cards with colors
- [ ] Pin/archive functionality
- [ ] Category filters
- [ ] Search

#### 4.4 Settings Page
**Priority:** LOW (already functional)
**Current:** Working with database config
**Target:** Minor polish

**Tasks:**
- [ ] Section headers
- [ ] Toggle styles
- [ ] Visual polish

---

### Phase 5: Performance-Optimized Pages

#### 5.1 Calendar Page (Rebuild)
**Priority:** LOW
**Current:** Disabled (performance issues)
**Target:** Month view with job indicators

**Tasks:**
- [ ] Async data loading
- [ ] Month navigation
- [ ] Day selection
- [ ] Job list for selected day
- [ ] Performance testing

#### 5.2 Site List Page (Rebuild)
**Priority:** LOW
**Current:** Disabled (performance issues)
**Target:** Location cards with map

**Tasks:**
- [ ] Async data loading
- [ ] Address cards
- [ ] Map integration
- [ ] Navigation button

#### 5.3 Warranty Claims Page (Rebuild)
**Priority:** LOW
**Current:** Disabled (performance issues)
**Target:** Claim cards with status

**Tasks:**
- [ ] Async data loading
- [ ] Status badges
- [ ] Asset info
- [ ] Claim timeline

---

## ?? Shared Components to Create

### ModernListItem.xaml
Reusable card for list items with:
- Title, subtitle, meta
- Status badge
- Avatar/icon
- Swipe actions

### MetricCard.xaml
Dashboard metric display:
- Icon, value, label
- Trend indicator
- Tap action

### StatusBadge.xaml
Colored status indicator:
- Status text
- Auto-color based on status

### EmptyStateView.xaml ? EXISTS
Already have this component.

### SearchHeader.xaml
Consistent search/filter UI:
- Search input
- Filter chips
- Count display

### FormSection.xaml
Consistent form grouping:
- Section title
- Card container
- Fields

---

## ?? Responsive Layout Patterns

### Pattern 1: List Page (Phone)
```
????????????????????
?    Header        ?
????????????????????
?  Search/Filter   ?
????????????????????
? ???????????????? ?
? ?   Card 1     ? ?
? ???????????????? ?
? ???????????????? ?
? ?   Card 2     ? ?
? ???????????????? ?
?       ...        ?
????????????????????
```

### Pattern 2: List Page (Tablet Landscape)
```
???????????????????????????????????????
?              Header                 ?
???????????????????????????????????????
? Search/Filter ?                     ?
?????????????????                     ?
?  ???????????  ?   Detail View       ?
?  ? Card 1  ?  ?   (Selected Item)   ?
?  ???????????  ?                     ?
?  ???????????  ?                     ?
?  ? Card 2* ??????????????????????????
?  ???????????  ?                     ?
?      ...      ?                     ?
???????????????????????????????????????
```

### Pattern 3: Detail Page (Phone)
```
????????????????????
?    Nav Header    ?
????????????????????
? ???????????????? ?
? ?  Info Card   ? ?
? ???????????????? ?
? ???????????????? ?
? ? Details Card ? ?
? ???????????????? ?
? ???????????????? ?
? ? Related Card ? ?
? ???????????????? ?
????????????????????
?  Action Buttons  ?
????????????????????
```

---

## ?? Color Reference (From Colors.xaml)

| Color | Hex | Usage |
|-------|-----|-------|
| Primary | #3b82f6 | Main brand, links, buttons |
| PrimaryDark | #2563eb | Hover, pressed states |
| Success | #10b981 | Completed, active, good |
| Warning | #f59e0b | Pending, attention needed |
| Error | #ef4444 | Errors, cancelled, overdue |
| Info | #06b6d4 | Informational |
| Gray600 | #4b5563 | Secondary text |
| Gray400 | #9ca3af | Placeholder text |

---

## ? Completion Checklist

### Phase 1 (Foundation)
- [x] Colors.xaml updated
- [x] MainPageModern created
- [ ] CustomerListPageModern created
- [ ] CustomerDetailPageModern created
- [ ] Build/test on phone
- [ ] Build/test on tablet

### Phase 2 (Jobs/Estimates)
- [ ] JobListPageModern created
- [ ] JobDetailPageModern created
- [ ] EstimateListPageModern created
- [ ] EstimateDetailPageModern created
- [ ] Build/test

### Phase 3 (Invoices/Inventory)
- [ ] InvoiceListPageModern created
- [ ] InvoiceDetailPageModern created
- [ ] InventoryListPageModern created
- [ ] ProductListPageModern created
- [ ] Build/test

### Phase 4 (Assets/Notes)
- [ ] AssetListPageModern created
- [ ] AssetDetailPageModern created
- [ ] QuickNotesPage created
- [ ] SettingsPage polished
- [ ] Build/test

### Phase 5 (Advanced/Rebuild)
- [ ] CalendarPage rebuilt
- [ ] SiteListPage rebuilt
- [ ] WarrantyClaimsPage rebuilt
- [ ] Full regression test
- [ ] Production release

---

## ?? Getting Started

### Next Immediate Steps:
1. **Test MainPageModern** on phone and tablet
2. **Create CustomerListPageModern** following the same pattern
3. **Create shared components** (MetricCard, StatusBadge)
4. **Iterate** based on feedback

### Commands:
```powershell
# Build APK
dotnet build OneManVan.Mobile\OneManVan.Mobile.csproj -f net10.0-android -c Debug

# Install on device
adb install -r "OneManVan.Mobile\bin\Debug\net10.0-android\com.onemanvan.fsm-Signed.apk"

# View logs for debugging
adb logcat | Select-String "onemanvan"
```

---

## ?? Notes

### Keep These Files Unchanged:
- All code in `OneManVan.Shared/`
- All services in `OneManVan.Mobile/Services/`
- Database contexts and models
- MauiProgram.cs (DI setup)

### Create New Files (Don't Modify Old Ones Yet):
- Create `*Modern.xaml` versions of pages
- Test new versions before replacing old ones
- Use AppShell.xaml to switch between versions

### Rollback Safety:
```powershell
# Revert to original dashboard
git checkout -- OneManVan.Mobile/AppShell.xaml
```

---

*Last Updated: 2025-01-28*
*Version: 1.0*
