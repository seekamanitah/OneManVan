# Calendar Loading States - Quick Reference

## Loading State Flow

```
User Action ? Loading State ? Data Fetch ? Result State
```

### 1. **Initial Page Load**
```
Navigate to Calendar
    ?
[LoadingOverlay visible]
[CalendarGrid hidden]
    ?
Load month + unscheduled jobs
    ?
SUCCESS: Show calendar
ERROR: Show error panel with retry
```

### 2. **Refresh Data**
```
Click Refresh Button
    ?
[Button disabled: "Refreshing..."]
[Existing calendar still visible]
    ?
Reload data
    ?
SUCCESS: Update calendar, restore button
ERROR: Toast notification, restore button
```

### 3. **Change Month/Week**
```
Click Prev/Next or Switch View
    ?
[No loading indicator - instant UI update]
    ?
Load new date range
    ?
Populate calendar with jobs
```

## UI States

### Loading Overlay
```xaml
<Border Background="#80000000" (semi-transparent)
    <Border Background="SurfaceBrush" (modal card)
        <StackPanel>
            [Spinner: 48x48 circle]
            "Loading calendar..." (16px, SemiBold)
            "Please wait..." (12px, Subtext)
```

### Error State
```xaml
<StackPanel in CalendarGrid>
    [? Icon: 48px]
    [Error Message: 14px]
    [Retry Button: Primary color]
```

### Refresh Button States
| State | Content | IsEnabled | Opacity |
|-------|---------|-----------|---------|
| Ready | "Refresh" | true | 1.0 |
| Loading | "Refreshing..." | false | 0.6 |

## Code Patterns

### Check if initialized
```csharp
if (LoadingOverlay == null || CalendarGrid == null)
    return; // Not ready yet
```

### Progressive loading
```csharp
if (_isInitialLoad)
    // Full overlay
else
    // Inline indicator
```

### Safe property access
```csharp
if (RefreshButton != null)
{
    RefreshButton.IsEnabled = true;
    RefreshButton.Content = "Refresh";
}
```

## SQL Query Pattern

### ? Correct (In-memory sorting)
```csharp
var jobs = await _dbContext.Jobs
    .Include(j => j.Customer)
    .Where(j => j.ScheduledDate.HasValue && ...)
    .ToListAsync(); // Fetch first

jobs = jobs
    .OrderBy(j => j.ScheduledDate) // Then sort
    .ThenBy(j => j.ArrivalWindowStart)
    .ToList();
```

### ? Wrong (SQL translation error)
```csharp
var jobs = await _dbContext.Jobs
    .Include(j => j.Customer)
    .Where(j => j.ScheduledDate >= date) // May fail
    .OrderBy(j => j.ArrivalWindowStart) // Can't translate TimeSpan
    .ToListAsync();
```

## Error Recovery

```csharp
try
{
    // Show loading
    LoadingOverlay.Visibility = Visible;
    
    // Load data
    var jobs = await LoadJobs();
    
    // Show results
    CalendarGrid.Visibility = Visible;
}
catch (Exception ex)
{
    ToastService.Error(ex.Message);
    
    if (_isInitialLoad)
        ShowErrorState("message");
}
finally
{
    // Always cleanup
    LoadingOverlay.Visibility = Collapsed;
    RestoreButtons();
}
```

## Testing Checklist

- [ ] First load shows overlay
- [ ] Refresh disables button
- [ ] Month/week switching works
- [ ] Error shows retry button
- [ ] Retry button reloads data
- [ ] No emojis in UI
- [ ] Loading states are smooth
- [ ] Null reference exceptions handled
