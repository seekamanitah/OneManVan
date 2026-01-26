# Quick Reference: Desktop Job Management

## ?? How to Use

### Add New Job (2 ways)

#### From Calendar
1. Click "Schedule Job" button (top right)
2. Fill in customer, site, asset, details
3. Set scheduled date/time
4. Click "Create Job"
5. Job appears on calendar

#### From Kanban Board
1. Click "Add Job" button (top right)
2. Fill in job details
3. Job appears in appropriate status column based on Status field

### Edit Existing Job (2 ways)

#### From Calendar
1. **Double-click** any job card on the calendar
2. Dialog opens with job data pre-filled
3. Modify as needed
4. Click "Save Changes"

#### From Kanban Board
1. **Double-click** any job card
2. Dialog opens with job data
3. Edit fields
4. Click "Save Changes"

### Reschedule Job
- **Drag & drop** job to different date on Calendar
- Job's ScheduledDate updates automatically
- Draft jobs automatically become Scheduled

### Change Job Status
- **Drag & drop** job between columns on Kanban board
- Status updates automatically with timestamps
- EnRoute, InProgress, Completed get automatic timestamps

## ?? Dialog Fields

### Required (*)
- **Title**: Job description
- **Customer**: Select from dropdown
- **Job Type**: ServiceCall, Repair, Maintenance, etc.
- **Priority**: Low, Normal, High, Urgent, Emergency
- **Status**: Draft, Scheduled, EnRoute, etc.

### Optional
- **Site/Location**: Filtered by customer
- **Asset/Equipment**: Filtered by site or customer
- **Job Number**: Auto-generated (read-only)
- **Description**: Detailed notes
- **Scheduled Date**: When to perform work
- **Start Time**: Arrival window start
- **Duration (minutes)**: Estimated work time (auto-calculates end time)

### Cascading Filters
1. Select **Customer** ? Sites dropdown populates
2. Select **Site** (optional) ? Assets dropdown shows site-specific equipment
3. Leave **Site** blank ? Assets dropdown shows all customer equipment

## ?? Visual Indicators

### Calendar
- **Today**: Blue highlighted date
- **Past dates**: Dimmed
- **Weekends**: Light gray background
- **Jobs**: Colored by status
  - Draft: Gray
  - Scheduled: Blue
  - EnRoute: Orange
  - InProgress: Green
  - Completed: Purple
  - OnHold: Red

### Kanban
- **Priority badges**: 
  - ?? Low
  - ?? Normal
  - ?? High
  - ?? Urgent
  - ?? Emergency
- **Overdue dates**: Red text
- **Job type**: Small badge at bottom

## ?? Keyboard Shortcuts

### Navigation
- **Scroll**: Mouse wheel in navigation pane
- **Tab**: Move between form fields
- **Enter**: Save dialog (when focused on button)
- **Esc**: Cancel dialog

### Calendar
- **Today button**: Jump to current month
- **< >**: Navigate months

### Kanban
- **Filters**: Date range, Priority, Search
- **Search**: Real-time as you type

## ?? Sync with Mobile

### What Syncs Automatically
- ? New jobs created on Desktop ? visible on Mobile
- ? Job edits on Desktop ? updated on Mobile
- ? Status changes via drag & drop ? synced
- ? Date changes via drag & drop ? synced
- ? Customer/Site/Asset relationships ? maintained

### Mobile-Only Features (view-only on Desktop)
- GPS location tracking
- Photo capture (Before/After/Problem/Solution)
- Digital signature
- Time tracking (EnRoute/Arrived/Completed timestamps)

### Desktop Advantages
- Faster data entry with keyboard
- Drag & drop scheduling and status changes
- Week/Month calendar visualization
- Multi-job bulk operations (Kanban filters)

## ?? Troubleshooting

### Dialog doesn't open
- Check that database connection is working
- Verify at least one active customer exists
- Check console for error messages

### Sites/Assets not loading
- Ensure customer has sites/assets created
- Check database relationships (CustomerId, SiteId)
- Sites must have CustomerId
- Assets must have CustomerId or SiteId

### Job not appearing after save
- Check dialog closed with "Create Job" (not Cancel)
- Verify job's ScheduledDate is in current calendar month
- For Kanban, check correct status column
- Try clicking Refresh button

### Duration not calculating end time
- Ensure Start Time is valid format (e.g., 9:00 AM)
- Duration must be numeric (minutes)
- End time shows automatically when both fields valid

## ?? Tips & Best Practices

### Data Entry
1. Create customers before jobs
2. Add sites for property-based tracking
3. Register assets for equipment history
4. Use meaningful job titles (customer can see them)
5. Set realistic durations for scheduling

### Scheduling
- Use Calendar for date-based scheduling
- Use Kanban for status-based workflow
- Drag unscheduled jobs from side panel to calendar dates
- Filter Kanban by date range to see week/month ahead

### Status Workflow
- Draft ? Scheduled ? EnRoute ? InProgress ? Completed
- OnHold for delayed/waiting jobs
- Cancelled for jobs that won't happen
- Closed after invoicing/payment

### Priority Usage
- **Emergency**: Same-day, after-hours
- **Urgent**: Next available slot
- **High**: Within 1-2 days
- **Normal**: Standard scheduling
- **Low**: Non-critical, flexible

## ?? Support

### Build Issues
- Run `dotnet build` to check for errors
- Check `DESKTOP_FIXES_COMPLETE_SUCCESS.md` for troubleshooting

### Model Changes
- Database migrations may be needed
- Check `OneManVan.Shared\Models\` for entity definitions

### Feature Requests
- Week view calendar (planned)
- Job templates (planned)
- Recurring jobs (future)
- Photo viewer (future)

---

**Version**: 1.0  
**Last Updated**: Current Session  
**Status**: ? Fully Functional
