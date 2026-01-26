# Mobile App UI Cleanup & Schedule-First Redesign Plan

**Created**: 2025-12-12  
**Status**: Pending  
**Goal**: Create a schedule-first, minimal interaction mobile experience. Remove emoji/unicode issues, prioritize "where do I go next" information, ensure clean uniformity.

---

## Problem Summary

### Unicode/Emoji Issues Causing VS Crashes
- XAML files contain **corrupted Unicode sequences** appearing as `??` or `?`
- These malformed characters cause:
  - XAML Designer failures
  - IntelliSense hangs
  - Rendering exceptions (especially `U+FFFD` replacement character)

### UX Issues
- Dashboard shows KPI numbers but no schedule visibility
- JobListPage defaults to "All" filter instead of "Today"
- No quick address/navigation access for field work
- Excessive information density on some pages

---

## Phase 1: Remove All Corrupted Unicode Characters

### Files to Fix

| File | Issues Found |
|------|--------------|
| `OneManVan.Mobile/Pages/JobListPage.xaml` | `??` in filter chips, timer banner, empty view; `?` separator |
| `OneManVan.Mobile/Pages/EstimateListPage.xaml` | `??` in filter chips; `?` separator |
| `OneManVan.Mobile/Pages/InvoiceListPage.xaml` | `??` in filter chips |
| `OneManVan.Mobile/Pages/AssetListPage.xaml` | Emoji in filters and FAB |
| `OneManVan.Mobile/Pages/CustomerListPage.xaml` | Emoji badges (??????) |
| `OneManVan.Mobile/Pages/SettingsPage.xaml` | `??` throughout sections |
| `OneManVan.Mobile/MainPage.xaml` | ???????????? in KPI cards |
| `OneManVan.Mobile/AppShell.xaml` | ?? in flyout header |

### Replacement Strategy
- **Filter chips**: Use text-only labels (e.g., "Draft" not "?? Draft")
- **Status indicators**: Use colored dots/borders instead of emoji
- **Action buttons**: Use descriptive text (e.g., "Start" not "? Start")
- **KPI cards**: Remove icon labels entirely
- **Separators**: Use `·` (middle dot U+00B7) or remove entirely

---

## Phase 2: Fix JobListPage.xaml (Mobile)

### Step 2.1: Remove Corrupted Unicode
Replace all `??` and `?` with clean text:

```
Text="?? Today"      ? Text="Today"
Text="?? Scheduled"  ? Text="Scheduled"
Text="?? In Progress"? Text="In Progress"
Text="? Completed"   ? Text="Completed"
Text="??"            ? (remove or use ·)
Text="?"             ? Text="·"
Text="? Start"       ? Text="Start"
Text="? Done"        ? Text="Done"
```

### Step 2.2: Fix Stats Bar Overlap
Current: Stats bar shares `Grid.Row="3"` with RefreshView  
Fix: Add `Margin="0,0,0,60"` to RefreshView OR move stats to `Grid.Row="4"`

### Step 2.3: Default to "Today" Filter
In `JobListPage.xaml.cs`:
- Set `_activeFilter` to today filter in `OnAppearing`
- Update filter chip visual state

### Step 2.4: Add Address to Job Cards
- Include `Site.Address` or `Customer.Address` below customer name
- Make address tappable for navigation

---

## Phase 3: Fix EstimateListPage.xaml

### Changes
```
Text="?? Draft"    ? Text="Draft"
Text="?? Sent"     ? Text="Sent"
Text="? Accepted"  ? Text="Accepted"
Text="??"          ? (remove)
Text="?"           ? Text="·"
```

---

## Phase 4: Fix InvoiceListPage.xaml

### Changes
```
Text="?? Draft"    ? Text="Draft"
Text="?? Sent"     ? Text="Sent"
Text="?? Overdue"  ? Text="Overdue"
Text="? Paid"      ? Text="Paid"
Text="??"          ? (remove)
```

---

## Phase 5: Fix MainPage.xaml (Dashboard)

### Current Issues
- Shows 4 KPI cards with emoji icons
- No schedule visibility
- Quick action buttons have emoji

### Changes
Remove all emoji from labels:
```
Text="??"  ? (remove)
Text="??"  ? (remove)
Text="??"  ? (remove)
Text="??"  ? (remove)
Text="?? Search Assets" ? Text="Search Assets"
Text="??"  ? (remove)
```

---

## Phase 6: Fix Remaining Pages

### AssetListPage.xaml
```
Text="?? Gas"      ? Text="Gas"
Text="? Electric" ? Text="Electric"
Text="? Expiring" ? Text="Expiring"
Text="? R-22"     ? Text="R-22"
Text="??"          ? Text="Scan"
```

### CustomerListPage.xaml
- Remove ?? VIP crown
- Remove ?? asset icon
- Remove ?? location pin
- Use colored borders or text labels instead

### SettingsPage.xaml
- Remove all `??` characters
- Use text-only labels for all buttons

### AppShell.xaml
- Remove ?? from flyout header

---

## Phase 7: Schedule-First Dashboard Redesign

### New Dashboard Layout
1. **Date Header** — Today's date prominently displayed
2. **Quick Stats Row** — 3 compact badges: Today | This Week | Overdue
3. **"Next Up" Card** — Large card showing:
   - Scheduled time
   - Job title
   - Customer name
   - Address
   - "Navigate" button (opens maps)
   - "Start Job" button
4. **Today's Schedule List** — Compact list of remaining jobs
5. **Coming Up Section** — Next 5 scheduled jobs beyond today

### Code-Behind Changes (MainPage.xaml.cs)
- Query jobs by `ScheduledDate`
- Sort by time ascending
- Filter to active statuses
- Implement `OnNavigateClicked` with platform-specific maps integration

---

## Phase 8: Create Shared Styles

### Add to App.xaml ResourceDictionary

| Style Key | Target | Purpose |
|-----------|--------|---------|
| `CardStyle` | Border | Consistent card appearance |
| `FilterChipStyle` | Button | Uniform filter buttons |
| `FilterChipActiveStyle` | Button | Selected filter state |
| `SectionHeaderStyle` | Label | Section header text |
| `StatusDotStyle` | Border | Small colored status indicators |

---

## Implementation Checklist

### Phase 1: Unicode Cleanup
- [ ] Fix `JobListPage.xaml` corrupted unicode
- [ ] Fix `EstimateListPage.xaml` corrupted unicode
- [ ] Fix `InvoiceListPage.xaml` corrupted unicode
- [ ] Fix `MainPage.xaml` emoji characters
- [ ] Fix `AssetListPage.xaml` emoji characters
- [ ] Fix `CustomerListPage.xaml` emoji characters
- [ ] Fix `SettingsPage.xaml` emoji characters
- [ ] Fix `AppShell.xaml` emoji characters

### Phase 2: JobListPage Enhancements
- [ ] Fix stats bar overlap
- [ ] Default to "Today" filter
- [ ] Add address to job cards
- [ ] Add navigation tap gesture

### Phase 3: Dashboard Redesign
- [ ] Implement schedule-first layout
- [ ] Add "Next Up" card
- [ ] Add today's schedule list
- [ ] Add coming up section
- [ ] Implement navigate button

### Phase 4: Shared Styles
- [ ] Create CardStyle
- [ ] Create FilterChipStyle
- [ ] Create SectionHeaderStyle
- [ ] Apply styles across pages

### Phase 5: Verification
- [ ] Build succeeds without errors
- [ ] No broken characters visible
- [ ] Dashboard shows schedule on launch
- [ ] Navigation opens maps app
- [ ] All filter chips work correctly

---

## Success Criteria

| Metric | Target |
|--------|--------|
| Time to see today's schedule | < 1 second after app opens |
| Taps to start navigation | 1 tap from dashboard |
| Broken unicode characters | 0 |
| Filter default on JobListPage | "Today" |
| Information visible without scrolling | Next job + time + address + customer |

---

## Notes
- Emojis are unnecessary and cause cross-platform rendering issues
- Text-only labels are cleaner and more professional
- Color-coded status indicators communicate state without emoji
- Schedule visibility is the #1 priority for field work
