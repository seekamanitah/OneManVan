# Git Commit Message Guide

## Critical Bug Fix - Blazor Buttons Not Working

### Root Cause
- Invalid HTML in `App.razor`: Blazor error UI was wrapped in `<script>` tag instead of `<div>`
- Browser tried to parse plain text as JavaScript, causing syntax error
- This broke all JavaScript execution, preventing Blazor from initializing

### Fix Applied
- Changed `<script id="blazor-error-ui">` to `<div id="blazor-error-ui">`
- Updated close button character

### Files Changed
- `OneManVan.Web/Components/App.razor` (2 lines)

---

## New Features Added

### 1. Employee Management System
- Employee records with time tracking
- Performance notes and evaluations
- Automated time log tracking
- Invoice integration for labor hours

### 2. Document Library
- Upload and manage documents
- Categorized storage
- Linked to customers, jobs, and assets

### 3. Material Lists
- Pre-built templates for common jobs
- Custom material lists
- Integration with estimates and jobs

### 4. Company Settings
- Email configuration
- Google Calendar integration
- PDF customization settings

### 5. Progressive Web App (PWA)
- Install as app on mobile/desktop
- Offline support
- Push notifications ready

### 6. Enhanced Services
- Route optimization
- Dashboard KPIs
- Data protection/encryption
- Notification service

---

## Infrastructure Improvements

### Deployment
- Multiple deployment options (clean, image-based)
- Docker compose configurations
- Automated deployment scripts

### Database
- SQL Server migration support
- Performance indexes
- Database cleanup utilities

### Security
- Data protection service
- Production settings configuration
- Input validation improvements

---

## Documentation

### Added/Updated:
- Feature roadmaps
- Deployment guides
- Fix documentation
- Audit reports
- Quick reference guides
