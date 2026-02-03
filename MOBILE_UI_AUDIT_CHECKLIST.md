# Mobile UI Audit Checklist

## Purpose
This document tracks the mobile UI audit for the OneManVan Blazor web application to ensure optimal usability on mobile devices (phones and tablets).

## Audit Date: [To be completed during testing]

---

## Critical Mobile Elements

### Navigation
- [ ] **Hamburger Menu**: Sidebar collapses on mobile, menu accessible via hamburger
- [ ] **Touch Targets**: All buttons/links minimum 44x44px touch targets
- [ ] **Breadcrumbs**: Visible and usable on small screens
- [ ] **Back Navigation**: Clear back buttons or browser back works correctly

### Forms & Input
- [ ] **Input Fields**: Large enough for finger tapping
- [ ] **Keyboard Types**: Correct keyboard shown (number, email, phone, etc.)
- [ ] **Labels**: Labels visible and associated with fields
- [ ] **Error Messages**: Clear, visible error messages on validation failure
- [ ] **Phone Input**: Auto-formatting works correctly on mobile
- [ ] **Date Pickers**: Touch-friendly date selection
- [ ] **Dropdowns**: Easy to use with touch (no tiny targets)

### Tables & Lists
- [ ] **Responsive Tables**: Tables scroll horizontally or reflow on mobile
- [ ] **List Views**: Adequate spacing between tap targets
- [ ] **Pagination**: Easy to navigate between pages
- [ ] **Action Buttons**: Row actions accessible without horizontal scroll

### Modals & Dialogs
- [ ] **Modal Size**: Modals fit on mobile screens
- [ ] **Close Buttons**: Easy to close modals
- [ ] **Scrolling**: Long content scrolls within modal

---

## Page-by-Page Audit

### Dashboard (/dashboard)
- [ ] KPI cards stack on mobile
- [ ] Quick actions accessible
- [ ] Charts resize appropriately
- [ ] Recent items list usable

### Customer Pages
- [ ] Customer List - search works, table responsive
- [ ] Customer Detail - all sections visible
- [ ] Customer Edit - form inputs accessible
- [ ] Phone/Mobile fields auto-format

### Job Pages
- [ ] Job List - status badges visible
- [ ] Job Detail - customer info, notes visible
- [ ] Job Edit - scheduling date picker works
- [ ] Status dropdown accessible

### Invoice Pages
- [ ] Invoice List - amounts visible
- [ ] Invoice Detail - line items readable
- [ ] Invoice Edit - add line items works
- [ ] PDF export triggers download

### Asset Pages
- [ ] Asset List - equipment types visible
- [ ] Asset Detail - HVAC specs readable
- [ ] Asset Edit - all HVAC fields accessible
  - [ ] EquipmentType dropdown
  - [ ] FuelType dropdown
  - [ ] UnitConfig dropdown
  - [ ] Tonnage input
  - [ ] SEER2 input
  - [ ] Install date picker

### Site Pages
- [ ] Site List - addresses visible
- [ ] Site Detail - map link works
- [ ] Site Edit - address fields accessible

### Company Pages
- [ ] Company List - types visible
- [ ] Company Edit - all fields accessible

### Expense Pages (NEW)
- [ ] Expense List - amounts, categories visible
- [ ] Expense Edit - category dropdown works
- [ ] Amount input shows number keyboard
- [ ] Date picker accessible

### Quick Notes (/notes)
- [ ] Note creation works
- [ ] Entity linking dropdowns accessible
- [ ] Note cards stack properly
- [ ] Pin/unpin accessible

### Calendar (/calendar)
- [ ] Month view usable
- [ ] Day selection works
- [ ] Job details accessible from calendar

### Service Agreements
- [ ] Agreement List - dates visible
- [ ] Agreement Detail - terms readable
- [ ] Agreement Edit - date pickers work

### Settings (/settings)
- [ ] All tabs accessible
- [ ] Form inputs work
- [ ] Save buttons visible

---

## PWA Features
- [ ] **Install Prompt**: PWA install banner shows on mobile
- [ ] **Offline Page**: Offline fallback shows when disconnected
- [ ] **App Icon**: Correct icon on home screen
- [ ] **Splash Screen**: Loading screen displays correctly
- [ ] **Theme Color**: Address bar color matches app theme

---

## Export Features
- [ ] CSV downloads trigger correctly on mobile
- [ ] Excel downloads trigger correctly
- [ ] PDF generation works

---

## Authentication
- [ ] Login page usable on mobile
- [ ] Register page usable on mobile
- [ ] Logout accessible from menu

---

## Known Issues to Address

### Priority 1 (Critical)
| Issue | Page | Status |
|-------|------|--------|
| [Example: Table too wide] | [Invoices] | [ ] Fixed |

### Priority 2 (High)
| Issue | Page | Status |
|-------|------|--------|
| | | |

### Priority 3 (Medium)
| Issue | Page | Status |
|-------|------|--------|
| | | |

---

## Testing Devices

### Tested On:
- [ ] iPhone (Safari)
- [ ] Android Phone (Chrome)
- [ ] iPad (Safari)
- [ ] Android Tablet (Chrome)

### Screen Sizes:
- [ ] 320px width (small phone)
- [ ] 375px width (iPhone SE/mini)
- [ ] 414px width (iPhone Plus/Pro Max)
- [ ] 768px width (tablet portrait)
- [ ] 1024px width (tablet landscape)

---

## CSS Breakpoints Used

```css
/* Extra small devices (phones, less than 576px) */
@media (max-width: 575.98px) { }

/* Small devices (landscape phones, 576px and up) */
@media (min-width: 576px) and (max-width: 767.98px) { }

/* Medium devices (tablets, 768px and up) */
@media (min-width: 768px) and (max-width: 991.98px) { }

/* Large devices (desktops, 992px and up) */
@media (min-width: 992px) { }
```

---

## Recommendations

### Quick Wins
1. Ensure all buttons have minimum touch target size (44x44px)
2. Add viewport meta tag optimization
3. Stack form columns on mobile

### Future Enhancements
1. Consider bottom navigation for primary actions
2. Add swipe gestures for common actions
3. Implement pull-to-refresh on list pages

---

## Sign-Off

| Role | Name | Date | Approved |
|------|------|------|----------|
| Developer | | | [ ] |
| Beta Tester | | | [ ] |
| Owner | | | [ ] |
