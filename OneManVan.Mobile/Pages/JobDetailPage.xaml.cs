using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Job detail page with flexible workflow support for field technicians.
/// Supports skipping stages and going directly to completion/invoice.
/// Includes GPS location capture on status changes.
/// </summary>
public partial class JobDetailPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private readonly ImageCacheService _imageCache;
    private Job? _job;
    private readonly int _jobId;
    private Location? _currentLocation;
    private List<JobPhotoViewModel> _photos = [];
    private string? _selectedPhotoType;

    public JobDetailPage(int jobId)
    {
        InitializeComponent();
        _jobId = jobId;
        _db = IPlatformApplication.Current?.Services.GetRequiredService<OneManVanDbContext>()
            ?? throw new InvalidOperationException("DbContext not available");
        _imageCache = IPlatformApplication.Current?.Services.GetRequiredService<ImageCacheService>()
            ?? throw new InvalidOperationException("ImageCacheService not available");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadJobAsync();
    }

    private async Task LoadJobAsync()
    {
        try
        {
            ShowLoading("Loading job details...");

            _job = await _db.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Site)
                .Include(j => j.Asset)
                .FirstOrDefaultAsync(j => j.Id == _jobId);

            if (_job == null)
            {
                await DisplayAlertAsync("Error", "Job not found", "OK");
                await Navigation.PopAsync();
                return;
            }

            await LoadPhotosAsync();
            UpdateUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load job: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    private async Task LoadPhotosAsync()
    {
        var photosDir = Path.Combine(FileSystem.AppDataDirectory, "Photos");
        _photos.Clear();

        if (Directory.Exists(photosDir))
        {
            var photoFiles = Directory.GetFiles(photosDir, $"job_{_jobId}_*.jpg");
            
            // PERF: Preload all images in background
            _ = Task.Run(() => _imageCache.PreloadImagesAsync(photoFiles));
            
            foreach (var file in photoFiles.OrderByDescending(f => new FileInfo(f).CreationTime))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('_');
                var photoType = parts.Length > 3 ? parts[3] : "Photo";
                
                _photos.Add(new JobPhotoViewModel
                {
                    FilePath = file,
                    PhotoType = photoType,
                    CapturedAt = new FileInfo(file).CreationTime,
                    ImageSource = _imageCache.GetOrLoadImage(file) // Cached!
                });
            }
        }

        await Task.CompletedTask;
    }

    #region GPS Location Capture

    /// <summary>
    /// Captures the current GPS location and stores it with the job.
    /// </summary>
    private async Task<Location?> CaptureLocationAsync(string context = "")
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                _currentLocation = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.High,
                    Timeout = TimeSpan.FromSeconds(15)
                });

                // Log location with job
                if (_currentLocation != null && _job != null)
                {
                    var locationNote = $"[{context}] GPS: {_currentLocation.Latitude:F6}, {_currentLocation.Longitude:F6} @ {DateTime.Now:HH:mm:ss}";
                    _job.Notes = string.IsNullOrEmpty(_job.Notes) 
                        ? locationNote 
                        : $"{locationNote}\n{_job.Notes}";
                }

                return _currentLocation;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GPS Error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Gets a display-friendly location string.
    /// </summary>
    private string GetLocationString(Location? location)
    {
        if (location == null) return "Location unavailable";
        return $"{location.Latitude:F4}, {location.Longitude:F4}";
    }

    #endregion

    private void UpdateUI()
    {
        if (_job == null) return;

        JobNumberLabel.Text = $"J-{_job.Id:D4}";
        UpdateStatusBanner();

        CustomerName.Text = _job.Customer?.Name ?? "Unknown Customer";
        CustomerPhone.Text = _job.Customer?.Phone ?? "No phone";

        if (_job.Site != null)
        {
            SiteAddress.Text = _job.Site.Address;
            SiteCityState.Text = $"{_job.Site.City}, {_job.Site.State} {_job.Site.ZipCode}";
        }
        else
        {
            SiteAddress.Text = "No address on file";
            SiteCityState.Text = "";
        }

        if (_job.ScheduledDate.HasValue)
        {
            ScheduledDate.Text = _job.ScheduledDate.ToShortDate("Not scheduled");
            ScheduledTime.Text = _job.ScheduledDate.ToShortTime("Not set");
        }
        else
        {
            ScheduledDate.Text = "Not scheduled";
            ScheduledTime.Text = "";
        }

        EstimatedHours.Text = $"{_job.EstimatedHours:F1} hours";
        PriorityLabel.Text = "Normal";
        
        JobTitle.Text = _job.Title;
        JobDescription.Text = _job.Description ?? "No description provided";

        if (_job.Asset != null)
        {
            AssetFrame.IsVisible = true;
            AssetName.Text = $"{_job.Asset.Brand} {_job.Asset.Model}";
            AssetSerial.Text = $"Serial: {_job.Asset.Serial}";
            
            var specs = new List<string>();
            if (_job.Asset.TonnageX10 > 0)
                specs.Add($"{_job.Asset.TonnageX10 / 10.0:F1} Ton");
            if (_job.Asset.SeerRating > 0)
                specs.Add($"{_job.Asset.SeerRating} SEER");
            if (_job.Asset.RefrigerantType != RefrigerantType.Unknown)
                specs.Add(_job.Asset.RefrigerantType.ToString());
            
            AssetSpecs.Text = specs.Count > 0 ? string.Join(", ", specs) : "No specs";
        }
        else
        {
            AssetFrame.IsVisible = false;
        }

        UpdateTimeTracking();
        WorkPerformedEditor.Text = _job.WorkPerformed ?? "";
        UpdatePricing();
        UpdatePhotosUI();
        UpdateActionButtons();
    }

    private void UpdatePhotosUI()
    {
        if (_photos.Count > 0)
        {
            PhotosCollection.ItemsSource = _photos;
            PhotosCollection.IsVisible = true;
            PhotosPlaceholder.IsVisible = false;
        }
        else
        {
            PhotosCollection.IsVisible = false;
            PhotosPlaceholder.IsVisible = true;
        }
    }

    private void UpdateStatusBanner()
    {
        if (_job == null) return;

        var (text, subtext, color) = _job.Status switch
        {
            JobStatus.Draft => ("Draft", "Tap to change status", Color.FromArgb("#6B7280")),
            JobStatus.Scheduled => ("Scheduled", "Tap to change status", Color.FromArgb("#3B82F6")),
            JobStatus.EnRoute => ("En Route", "Tap to change status", Color.FromArgb("#8B5CF6")),
            JobStatus.InProgress => ("In Progress", "Tap to change status", Color.FromArgb("#F59E0B")),
            JobStatus.Completed => ("Completed", "Ready for invoice", Color.FromArgb("#10B981")),
            JobStatus.Closed => ("Closed", "Job invoiced", Color.FromArgb("#6366F1")),
            JobStatus.Cancelled => ("Cancelled", "Job was cancelled", Color.FromArgb("#EF4444")),
            JobStatus.OnHold => ("On Hold", "Tap to change status", Color.FromArgb("#F59E0B")),
            _ => ("Unknown", "", Color.FromArgb("#6B7280"))
        };

        StatusText.Text = text;
        StatusSubtext.Text = subtext;
        StatusBanner.BackgroundColor = color;
    }

    private void UpdateTimeTracking()
    {
        if (_job == null) return;

        var showTracking = _job.Status >= JobStatus.EnRoute || _job.StartedAt.HasValue;
        TimeTrackingFrame.IsVisible = showTracking;

        if (showTracking)
        {
            EnRouteTime.Text = _job.Status >= JobStatus.EnRoute ? "Started" : "--:--";
            ArrivedTime.Text = _job.StartedAt.ToShortTime("--:--");
            CompletedTime.Text = _job.CompletedAt.ToShortTime("--:--");

            if (_job.StartedAt.HasValue)
            {
                var endTime = _job.CompletedAt ?? DateTime.Now;
                var duration = endTime - _job.StartedAt.Value;
                TotalTime.Text = $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
            else
            {
                TotalTime.Text = "0h 0m";
            }
        }
    }

    private void UpdatePricing()
    {
        if (_job == null) return;

        LaborTotal.Text = $"${_job.LaborTotal:F2}";
        PartsTotal.Text = $"${_job.PartsTotal:F2}";
        TaxTotal.Text = $"${_job.TaxAmount:F2}";
        GrandTotal.Text = $"${_job.Total:F2}";
    }

    private void UpdateActionButtons()
    {
        if (_job == null) return;

        SignatureFrame.IsVisible = _job.Status == JobStatus.Completed;

        var (buttonText, buttonColor) = _job.Status switch
        {
            JobStatus.Draft or JobStatus.Scheduled => ("Start Job", Color.FromArgb("#1976D2")),
            JobStatus.EnRoute => ("Arrive at Site", Color.FromArgb("#7B1FA2")),
            JobStatus.InProgress => ("Complete Job", Color.FromArgb("#4CAF50")),
            JobStatus.Completed => ("Create Invoice", Color.FromArgb("#FF9800")),
            JobStatus.Closed => ("View Invoice", Color.FromArgb("#6366F1")),
            _ => ("Start Job", Color.FromArgb("#1976D2"))
        };

        PrimaryActionButton.Text = buttonText;
        PrimaryActionButton.BackgroundColor = buttonColor;
        PrimaryActionButton.IsEnabled = _job.Status != JobStatus.Cancelled;

        QuickCompleteButton.IsVisible = _job.Status < JobStatus.Completed;
        QuickInvoiceButton.IsVisible = _job.Status <= JobStatus.Completed && _job.Status != JobStatus.Closed;
    }

    #region Status Banner Tap - Flexible Status Change

    private async void OnStatusBannerTapped(object sender, EventArgs e)
    {
        if (_job == null) return;

        var options = new List<string>();
        
        if (_job.Status != JobStatus.Scheduled)
            options.Add("Scheduled");
        if (_job.Status != JobStatus.EnRoute)
            options.Add("En Route");
        if (_job.Status != JobStatus.InProgress)
            options.Add("In Progress");
        if (_job.Status != JobStatus.Completed)
            options.Add("Completed");
        if (_job.Status != JobStatus.OnHold)
            options.Add("On Hold");
        if (_job.Status != JobStatus.Cancelled)
            options.Add("Cancel Job");

        var result = await DisplayActionSheetAsync("Change Job Status", "Cancel", null, options.ToArray());
        
        if (string.IsNullOrEmpty(result) || result == "Cancel") return;

        await ChangeStatusTo(result);
    }

    private async Task ChangeStatusTo(string statusOption)
    {
        if (_job == null) return;

        try
        {
            ShowLoading("Updating status...");
            
            var newStatus = statusOption switch
            {
                "Scheduled" => JobStatus.Scheduled,
                "En Route" => JobStatus.EnRoute,
                "In Progress" => JobStatus.InProgress,
                "Completed" => JobStatus.Completed,
                "On Hold" => JobStatus.OnHold,
                "Cancel Job" => JobStatus.Cancelled,
                _ => _job.Status
            };

            if (newStatus == _job.Status) return;

            // Capture GPS location on status change
            var location = await CaptureLocationAsync($"Status: {newStatus}");

            // Handle specific status transitions
            if (newStatus == JobStatus.EnRoute)
            {
                // Capture departure location
                await CaptureLocationAsync("En Route Start");
            }
            else if (newStatus == JobStatus.InProgress && !_job.StartedAt.HasValue)
            {
                _job.StartedAt = DateTime.Now;
                // Capture arrival location
                await CaptureLocationAsync("Arrived at Site");
            }
            else if (newStatus == JobStatus.Completed && !_job.CompletedAt.HasValue)
            {
                _job.CompletedAt = DateTime.Now;
                if (!_job.StartedAt.HasValue)
                    _job.StartedAt = DateTime.Now;
                
                // Capture completion location
                await CaptureLocationAsync("Job Completed");
                
                if (_job.Total == 0)
                {
                    await PromptForQuickPricing();
                }
            }
            else if (newStatus == JobStatus.Cancelled)
            {
                var reason = await DisplayPromptAsync("Cancel Reason", "Enter cancellation reason:");
                if (string.IsNullOrEmpty(reason)) return;
                _job.Notes = $"Cancelled: {reason}\n{_job.Notes}";
            }

            _job.Status = newStatus;
            _job.WorkPerformed = WorkPerformedEditor.Text;
            _job.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            UpdateUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to update status: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    #endregion

    #region Primary Action - Context Sensitive

    private async void OnPrimaryActionClicked(object sender, EventArgs e)
    {
        if (_job == null) return;

        switch (_job.Status)
        {
            case JobStatus.Draft:
            case JobStatus.Scheduled:
                await ChangeStatusTo("In Progress");
                break;
            case JobStatus.EnRoute:
                await ChangeStatusTo("In Progress");
                break;
            case JobStatus.InProgress:
                await ChangeStatusTo("Completed");
                break;
            case JobStatus.Completed:
                await CreateInvoiceAsync();
                break;
            case JobStatus.Closed:
                await DisplayAlertAsync("Invoice", "Invoice already created for this job.", "OK");
                break;
        }
    }

    #endregion

    #region Quick Complete - Skip All Steps

    private async void OnQuickCompleteClicked(object sender, EventArgs e)
    {
        if (_job == null) return;

        var confirm = await DisplayAlertAsync("Quick Complete", 
            "Mark this job as completed immediately?\n\nThis will skip en route and in-progress stages.", 
            "Complete Now", "Cancel");
        
        if (!confirm) return;

        try
        {
            ShowLoading("Completing job...");

            // Capture GPS for quick complete
            await CaptureLocationAsync("Quick Complete");

            if (!_job.StartedAt.HasValue)
                _job.StartedAt = DateTime.Now;
            
            _job.CompletedAt = DateTime.Now;
            _job.Status = JobStatus.Completed;
            _job.WorkPerformed = WorkPerformedEditor.Text;
            _job.UpdatedAt = DateTime.Now;

            if (_job.Total == 0)
            {
                await PromptForQuickPricing();
            }

            await _db.SaveChangesAsync();
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            UpdateUI();

            await DisplayAlertAsync("Completed", "Job marked as completed!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to complete: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    #endregion

    #region Quick Invoice - Skip to Payment

    private async void OnQuickInvoiceClicked(object sender, EventArgs e)
    {
        if (_job == null) return;

        if (_job.Status < JobStatus.Completed)
        {
            var complete = await DisplayAlertAsync("Complete First?", 
                "Job is not marked as completed.\n\nComplete job and create invoice?", 
                "Yes, Complete & Invoice", "Cancel");
            
            if (!complete) return;

            // Capture GPS
            await CaptureLocationAsync("Quick Invoice");

            if (!_job.StartedAt.HasValue)
                _job.StartedAt = DateTime.Now;
            _job.CompletedAt = DateTime.Now;
            _job.Status = JobStatus.Completed;
            _job.WorkPerformed = WorkPerformedEditor.Text;
        }

        if (_job.Total == 0)
        {
            await PromptForQuickPricing();
        }

        await CreateInvoiceAsync();
    }

    private async Task PromptForQuickPricing()
    {
        if (_job == null) return;

        var totalStr = await DisplayPromptAsync("Job Total", 
            "Enter total amount for this job:", 
            "OK", "Skip", "$0.00", 
            keyboard: Keyboard.Numeric);

        if (decimal.TryParse(totalStr?.Replace("$", ""), out var total) && total > 0)
        {
            _job.LaborTotal = total * 0.7m;
            _job.PartsTotal = total * 0.2m;
            _job.TaxAmount = total * 0.1m;
            _job.Total = total;
            UpdatePricing();
        }
    }

    #endregion

    #region Set Labor Amount

    private async void OnSetLaborClicked(object sender, EventArgs e)
    {
        if (_job == null) return;

        var laborStr = await DisplayPromptAsync("Labor Amount", 
            "Enter labor charge:", 
            "OK", "Cancel", $"${_job.LaborTotal:F2}", 
            keyboard: Keyboard.Numeric);

        if (decimal.TryParse(laborStr?.Replace("$", ""), out var labor))
        {
            _job.LaborTotal = labor;
            RecalculateTotal();
            await _db.SaveChangesAsync();
            UpdatePricing();
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        }
    }

    private void RecalculateTotal()
    {
        if (_job == null) return;
        
        var subtotal = _job.LaborTotal + _job.PartsTotal;
        _job.TaxAmount = subtotal * 0.07m;
        _job.Total = subtotal + _job.TaxAmount;
    }

    #endregion

    #region More Options Menu

    private async void OnMoreOptionsClicked(object sender, EventArgs e)
    {
        if (_job == null) return;

        var options = new List<string>
        {
            "Change Status",
            "Get Signature",
            "Capture Location",
            "Email Customer",
            "Add Note",
            "Delete Job"
        };

        if (_job.Status != JobStatus.Cancelled)
            options.Add("Cancel Job");

        var result = await DisplayActionSheetAsync("More Options", "Close", null, options.ToArray());

        switch (result)
        {
            case "Change Status":
                OnStatusBannerTapped(this, EventArgs.Empty);
                break;
            case "Get Signature":
                SignatureFrame.IsVisible = true;
                await ((ScrollView)Content.GetVisualTreeDescendants().First(x => x is ScrollView))
                    .ScrollToAsync(SignatureFrame, ScrollToPosition.MakeVisible, true);
                break;
            case "Capture Location":
                var loc = await CaptureLocationAsync("Manual Capture");
                await DisplayAlertAsync("Location Captured", GetLocationString(loc), "OK");
                break;
            case "Cancel Job":
                await ChangeStatusTo("Cancel Job");
                break;
            case "Add Note":
                var note = await DisplayPromptAsync("Add Note", "Enter note:");
                if (!string.IsNullOrEmpty(note))
                {
                    _job.Notes = $"{DateTime.Now:g}: {note}\n{_job.Notes}";
                    await _db.SaveChangesAsync();
                }
                break;
        }
    }

    #endregion

    #region Create Invoice

    private async Task CreateInvoiceAsync()
    {
        if (_job == null) return;

        var confirm = await DisplayAlertAsync("Create Invoice", 
            $"Create invoice for ${_job.Total:F2}?", "Create", "Cancel");
        
        if (!confirm) return;

        try
        {
            ShowLoading("Creating invoice...");

            var taxRate = decimal.Parse(Preferences.Get("TaxRate", "7.0"));
            var subTotal = _job.LaborTotal + _job.PartsTotal;
            var taxAmount = subTotal * (taxRate / 100);
            var total = subTotal + taxAmount;

            var invoice = new Invoice
            {
                CustomerId = _job.CustomerId,
                JobId = _job.Id,
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{_job.Id}",
                Status = InvoiceStatus.Sent,
                LaborAmount = _job.LaborTotal,
                PartsAmount = _job.PartsTotal,
                SubTotal = subTotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total,
                Notes = _job.WorkPerformed,
                CreatedAt = DateTime.Now,
                DueDate = DateTime.Now.AddDays(30)
            };

            _db.Invoices.Add(invoice);

            _job.Status = JobStatus.Closed;
            _job.CustomerSignature = SignedByEntry.Text;
            _job.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            UpdateUI();

            await DisplayAlertAsync("Invoice Created", 
                $"Invoice {invoice.InvoiceNumber} created!\n\nTotal: ${invoice.Total:F2}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create invoice: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    #endregion

    #region Customer Actions

    private async void OnCallCustomerClicked(object sender, EventArgs e)
    {
        if (_job?.Customer?.Phone == null) return;

        try
        {
            PhoneDialer.Default.Open(_job.Customer.Phone);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Cannot open phone dialer: {ex.Message}", "OK");
        }
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        if (_job?.Site == null)
        {
            await DisplayAlertAsync("Error", "No address available", "OK");
            return;
        }

        try
        {
            if (_job.Site.Latitude.HasValue && _job.Site.Longitude.HasValue)
            {
                var location = new Location((double)_job.Site.Latitude.Value, (double)_job.Site.Longitude.Value);
                await Map.Default.OpenAsync(location, new MapLaunchOptions
                {
                    NavigationMode = NavigationMode.Driving
                });
            }
            else
            {
                var address = $"{_job.Site.Address}, {_job.Site.City}, {_job.Site.State} {_job.Site.ZipCode}";
                var uri = new Uri($"geo:0,0?q={Uri.EscapeDataString(address)}");
                await Launcher.Default.OpenAsync(uri);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Cannot open maps: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Photo Capture

    private void OnAddPhotoClicked(object sender, EventArgs e)
    {
        PhotoTypeGrid.IsVisible = true;
    }

    private async void OnPhotoTypeSelected(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            _selectedPhotoType = button.Text;
            PhotoTypeGrid.IsVisible = false;
            await CapturePhotoAsync(_selectedPhotoType);
        }
    }

    private async Task CapturePhotoAsync(string photoType)
    {
        var action = await DisplayActionSheetAsync($"Add {photoType} Photo", "Cancel", null, 
            "Take Photo", "Choose from Gallery");

        if (action == null || action == "Cancel") return;

        try
        {
            FileResult? photo = null;

            if (action.Contains("Take"))
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlertAsync("Error", "Camera not supported on this device", "OK");
                    return;
                }

                photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = $"{photoType} Photo"
                });
            }
            else
            {
                var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
                {
                    Title = "Select Photo"
                });
                photo = photos?.FirstOrDefault();
            }

            if (photo == null) return;

            ShowLoading("Saving photo...");

            // Capture GPS with photo
            var location = await CaptureLocationAsync($"Photo: {photoType}");

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"job_{_jobId}_{timestamp}_{photoType}.jpg";
            var filePath = Path.Combine(FileSystem.AppDataDirectory, "Photos", fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var sourceStream = await photo.OpenReadAsync();
            using var destStream = File.OpenWrite(filePath);
            await sourceStream.CopyToAsync(destStream);

            _photos.Insert(0, new JobPhotoViewModel
            {
                FilePath = filePath,
                PhotoType = photoType,
                CapturedAt = DateTime.Now,
                Latitude = location?.Latitude,
                Longitude = location?.Longitude
            });

            UpdatePhotosUI();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            await DisplayAlertAsync("Photo Saved", $"{photoType} photo has been added.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save photo: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    #endregion

    #region Parts/Materials

    private async void OnAddPartsClicked(object sender, EventArgs e)
    {
        if (_job == null) return;

        // Show options: manual entry or inventory lookup
        var choice = await DisplayActionSheetAsync("Add Parts", "Cancel", null,
            "From Inventory", "Manual Entry");

        if (choice == "From Inventory")
        {
            await AddPartFromInventoryAsync();
        }
        else if (choice == "Manual Entry")
        {
            await AddPartManuallyAsync();
        }
    }

    private async Task AddPartFromInventoryAsync()
    {
        if (_job == null) return;

        try
        {
            var lookupService = IPlatformApplication.Current?.Services.GetService<Services.InventoryLookupService>();
            if (lookupService == null)
            {
                await DisplayAlertAsync("Error", "Inventory service not available.", "OK");
                return;
            }

            // Get compatible items if asset is linked
            List<InventoryItem> items;
            if (_job.Asset != null)
            {
                items = await lookupService.GetCompatibleWithAssetAsync(_job.Asset);
            }
            else
            {
                items = await lookupService.GetFrequentlyUsedAsync(20);
            }

            if (!items.Any())
            {
                await DisplayAlertAsync("No Items", "No inventory items found. Try manual entry.", "OK");
                return;
            }

            // Search option
            var searchTerm = await DisplayPromptAsync("Search Inventory", 
                "Enter part name, SKU, or leave blank to browse:", 
                "Search", "Cancel", placeholder: "Search...");

            if (searchTerm == null) return;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                items = await lookupService.SearchAsync(searchTerm);
            }

            if (!items.Any())
            {
                await DisplayAlertAsync("Not Found", $"No items matching '{searchTerm}'. Try manual entry.", "OK");
                return;
            }

            // Show item picker
            var itemDisplayNames = items.Select(i => 
                $"{i.Name} - ${i.Price:F2} ({i.QuantityOnHand:F0} {i.Unit})").ToArray();

            var selected = await DisplayActionSheetAsync("Select Part", "Cancel", null, itemDisplayNames);

            if (string.IsNullOrEmpty(selected) || selected == "Cancel") return;

            var selectedItem = items[Array.IndexOf(itemDisplayNames, selected)];

            // Ask for quantity
            var qtyStr = await DisplayPromptAsync("Quantity",
                $"Enter quantity ({selectedItem.Unit}):",
                "Add", "Cancel", "1",
                keyboard: Keyboard.Numeric);

            if (!decimal.TryParse(qtyStr, out var quantity) || quantity <= 0) return;

            // Check stock
            if (quantity > selectedItem.QuantityOnHand)
            {
                var proceed = await DisplayAlertAsync("Low Stock",
                    $"Only {selectedItem.QuantityOnHand:F0} {selectedItem.Unit} available.\n\nContinue anyway?",
                    "Yes", "No");
                if (!proceed) return;
            }

            ShowLoading("Adding part...");

            // Calculate price
            var totalPrice = selectedItem.Price * quantity;

            // Deduct from inventory
            await lookupService.DeductInventoryAsync(selectedItem.Id, quantity, _job.Id, 
                $"{selectedItem.Name} x {quantity}");

            // Update job
            _job.PartsTotal += totalPrice;
            RecalculateTotal();
            _job.Notes = $"[PART] {selectedItem.Name} x {quantity:F0} @ ${selectedItem.Price:F2} = ${totalPrice:F2}\n{_job.Notes}";
            _job.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            UpdatePricing();

            await DisplayAlertAsync("Added",
                $"{selectedItem.Name} x {quantity:F0}\nTotal: ${totalPrice:F2}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to add part: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    private async Task AddPartManuallyAsync()
    {
        if (_job == null) return;

        var description = await DisplayPromptAsync("Add Parts", 
            "Enter part/material description:", "Add", "Cancel");
        
        if (string.IsNullOrWhiteSpace(description)) return;

        var priceStr = await DisplayPromptAsync("Part Price", 
            "Enter price:", "Add", "Cancel", "$0.00", keyboard: Keyboard.Numeric);
        
        if (!decimal.TryParse(priceStr?.Replace("$", ""), out var price)) return;

        try
        {
            ShowLoading("Adding part...");

            _job.PartsTotal += price;
            RecalculateTotal();
            _job.Notes = $"[MANUAL] {description}: ${price:F2}\n{_job.Notes}";
            _job.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            UpdatePricing();

            await DisplayAlertAsync("Added", $"{description} added: ${price:F2}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to add part: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    #endregion

    #region Signature

    private void OnSignatureChanged(object? sender, EventArgs e)
    {
        SignaturePlaceholder.IsVisible = !SignaturePadControl.HasSignature;
    }

    private void OnClearSignatureClicked(object sender, EventArgs e)
    {
        SignaturePadControl.Clear();
        SignaturePlaceholder.IsVisible = true;
        SignedByEntry.Text = "";
    }

    private async void OnAcceptSignatureClicked(object sender, EventArgs e)
    {
        if (!SignaturePadControl.HasSignature)
        {
            await DisplayAlertAsync("Required", "Please have customer sign above.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(SignedByEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter customer's printed name.", "OK");
            return;
        }

        // Capture GPS with signature
        await CaptureLocationAsync("Signature Captured");

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        
        await DisplayAlertAsync("Signature Accepted", 
            $"Signature from {SignedByEntry.Text} has been captured.", "OK");
    }

    #endregion

    #region Helpers

    private void ShowLoading(string message)
    {
        LoadingText.Text = message;
        LoadingOverlay.IsVisible = true;
    }

    private void HideLoading()
    {
        LoadingOverlay.IsVisible = false;
    }

    #endregion
}

/// <summary>
/// View model for job photos with GPS coordinates.
/// </summary>
public class JobPhotoViewModel
{
    public string FilePath { get; set; } = "";
    public string PhotoType { get; set; } = "Photo";
    public DateTime CapturedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public ImageSource? ImageSource { get; set; } // Cached image source
}
