using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Constants;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using System.Collections.ObjectModel;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(EstimateId), "id")]
public partial class EditEstimatePage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Estimate? _estimate;
    private int _estimateId;
    private ObservableCollection<EstimateLineViewModel> _lineItems = [];
    private List<int> _deletedLineIds = [];

    public int EstimateId
    {
        get => _estimateId;
        set
        {
            _estimateId = value;
            if (_estimateId > 0)
            {
                LoadEstimateAsync();
            }
        }
    }

    public EditEstimatePage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
        LineItemsCollection.ItemsSource = _lineItems;
    }

    private async void LoadEstimateAsync()
    {
        try
        {
            _db.ChangeTracker.Clear();
            
            _estimate = await _db.Estimates
                .Include(e => e.Customer)
                .Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.Id == _estimateId);

            if (_estimate == null)
            {
                await DisplayAlertAsync("Error", "Estimate not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            PopulateForm();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load estimate: {ex.Message}", "OK");
        }
    }

    private void PopulateForm()
    {
        if (_estimate == null) return;

        // Status banner
        UpdateStatusBanner();

        // Customer
        CustomerNameLabel.Text = _estimate.Customer?.Name ?? "Unknown";

        // Details
        TitleEntry.Text = _estimate.Title;
        DescriptionEditor.Text = _estimate.Description;
        ExpiresDatePicker.Date = _estimate.ExpiresAt ?? DateTime.Today.AddDays(30);
        TaxRateEntry.Text = _estimate.TaxRate.ToString("F2");
        TaxIncludedSwitch.IsToggled = _estimate.TaxIncluded;
        NotesEditor.Text = _estimate.Notes;
        TermsEditor.Text = _estimate.Terms;

        // Line items
        _lineItems.Clear();
        foreach (var line in _estimate.Lines.OrderBy(l => l.SortOrder))
        {
            _lineItems.Add(new EstimateLineViewModel
            {
                Id = line.Id,
                Description = line.Description,
                Type = line.Type,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Total = line.Total
            });
        }

        // Enable/disable editing based on status
        var canEdit = _estimate.CanEdit;
        TitleEntry.IsEnabled = canEdit;
        DescriptionEditor.IsEnabled = canEdit;
        TaxRateEntry.IsEnabled = canEdit;
        AddLineButton.IsVisible = canEdit;
        DeleteButton.IsVisible = canEdit;

        // Quick actions
        SendButton.IsVisible = _estimate.Status == EstimateStatus.Draft;
        ConvertButton.IsVisible = _estimate.Status == EstimateStatus.Accepted;

        UpdateTotals();
    }

    private void UpdateStatusBanner()
    {
        if (_estimate == null) return;

        var (icon, text, subtext, color) = _estimate.Status switch
        {
            EstimateStatus.Draft => ("", "Draft", "Tap to change status", Color.FromArgb("#6B7280")),
            EstimateStatus.Sent => ("", "Sent", "Waiting for response", Color.FromArgb("#1976D2")),
            EstimateStatus.Accepted => ("", "Accepted", "Ready to convert to job", Color.FromArgb("#4CAF50")),
            EstimateStatus.Declined => ("", "Declined", "Customer declined", Color.FromArgb("#F44336")),
            EstimateStatus.Expired => ("", "Expired", "Past expiration date", Color.FromArgb("#FF9800")),
            EstimateStatus.Converted => ("", "Converted", "Converted to job", Color.FromArgb("#9C27B0")),
            _ => ("", "Unknown", "", Color.FromArgb("#6B7280"))
        };

        StatusIcon.Text = icon;
        StatusText.Text = text;
        StatusSubtext.Text = subtext;
        StatusBanner.BackgroundColor = color;
    }

    private async void OnStatusTapped(object sender, TappedEventArgs e)
    {
        if (_estimate == null) return;

        var options = new List<string>();
        
        if (_estimate.Status != EstimateStatus.Draft)
            options.Add("Draft");
        if (_estimate.Status != EstimateStatus.Sent)
            options.Add("Sent");
        if (_estimate.Status != EstimateStatus.Accepted)
            options.Add("Accepted");
        if (_estimate.Status != EstimateStatus.Declined)
            options.Add("Declined");

        var result = await DisplayActionSheetAsync("Change Status", "Cancel", null, options.ToArray());
        
        if (string.IsNullOrEmpty(result) || result == "Cancel") return;

        _estimate.Status = result switch
        {
            "Draft" => EstimateStatus.Draft,
            "Sent" => EstimateStatus.Sent,
            "Accepted" => EstimateStatus.Accepted,
            "Declined" => EstimateStatus.Declined,
            _ => _estimate.Status
        };

        if (result == "Sent" && !_estimate.SentAt.HasValue)
            _estimate.SentAt = DateTime.UtcNow;
        
        if (result == "Accepted" && !_estimate.AcceptedAt.HasValue)
            _estimate.AcceptedAt = DateTime.UtcNow;

        UpdateStatusBanner();
        PopulateForm(); // Refresh UI based on new status
    }

    private async void OnAddLineItemClicked(object sender, EventArgs e)
    {
        var type = await DisplayActionSheetAsync("Line Item Type", "Cancel", null, LineItemTypes.All);
        
        if (type == "Cancel" || type == null) return;

        var description = await DisplayPromptAsync($"Add {type}", "Description:");
        if (string.IsNullOrWhiteSpace(description)) return;

        var qtyStr = await DisplayPromptAsync("Quantity", "Enter quantity:", "Next", "Cancel", "1", keyboard: Keyboard.Numeric);
        if (!decimal.TryParse(qtyStr, out var qty)) qty = 1;

        var priceStr = await DisplayPromptAsync("Unit Price", "Enter unit price:", "Add", "Cancel", "0.00", keyboard: Keyboard.Numeric);
        if (!decimal.TryParse(priceStr, out var price)) price = 0;

        var lineType = type switch
        {
            "Labor" => LineItemType.Labor,
            "Part" => LineItemType.Part,
            "Material" => LineItemType.Material,
            "Equipment" => LineItemType.Equipment,
            "Service" => LineItemType.Service,
            "Fee" => LineItemType.Fee,
            "Discount" => LineItemType.Discount,
            _ => LineItemType.Labor
        };

        var total = lineType == LineItemType.Discount ? -Math.Abs(qty * price) : qty * price;

        _lineItems.Add(new EstimateLineViewModel
        {
            Id = 0, // New item
            Description = description.Trim(),
            Type = lineType,
            Quantity = qty,
            UnitPrice = price,
            Total = total
        });

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        UpdateTotals();
    }

    private async void OnLineItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is EstimateLineViewModel item)
        {
            if (_estimate?.CanEdit != true) return;

            // Edit the line item
            var description = await DisplayPromptAsync("Edit Description", "Description:", initialValue: item.Description);
            if (description == null) return;

            var qtyStr = await DisplayPromptAsync("Quantity", "Enter quantity:", "Next", "Cancel", item.Quantity.ToString("F0"), keyboard: Keyboard.Numeric);
            if (!decimal.TryParse(qtyStr, out var qty)) return;

            var priceStr = await DisplayPromptAsync("Unit Price", "Enter unit price:", "Save", "Cancel", item.UnitPrice.ToString("F2"), keyboard: Keyboard.Numeric);
            if (!decimal.TryParse(priceStr, out var price)) return;

            item.Description = description.Trim();
            item.Quantity = qty;
            item.UnitPrice = price;
            item.Total = item.Type == LineItemType.Discount ? -Math.Abs(qty * price) : qty * price;

            // Refresh collection
            var index = _lineItems.IndexOf(item);
            if (index >= 0)
            {
                _lineItems[index] = item;
            }

            UpdateTotals();
        }
    }

    private void OnDeleteLineItem(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is EstimateLineViewModel item)
        {
            if (item.Id > 0)
            {
                _deletedLineIds.Add(item.Id);
            }
            _lineItems.Remove(item);
            UpdateTotals();
        }
    }

    private void OnTaxRateChanged(object sender, TextChangedEventArgs e)
    {
        UpdateTotals();
    }

    private void OnTaxIncludedToggled(object sender, ToggledEventArgs e)
    {
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        var subtotal = _lineItems.Sum(i => i.Total);
        SubtotalLabel.Text = $"${subtotal:N2}";

        if (!decimal.TryParse(TaxRateEntry.Text, out var taxRate))
            taxRate = 0;

        var taxIncluded = TaxIncludedSwitch.IsToggled;
        var taxAmount = taxIncluded ? 0 : subtotal * (taxRate / 100);
        TaxAmountLabel.Text = taxIncluded ? "Included" : $"${taxAmount:N2}";

        var total = subtotal + taxAmount;
        TotalLabel.Text = $"${total:N2}";
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (_estimate == null) return;

        var confirm = await DisplayAlertAsync("Send Estimate", "Mark this estimate as sent to customer?", "Send", "Cancel");
        if (!confirm) return;

        _estimate.Status = EstimateStatus.Sent;
        _estimate.SentAt = DateTime.UtcNow;
        
        await SaveChangesAsync();
        PopulateForm();

        await DisplayAlertAsync("Sent", "Estimate marked as sent.", "OK");
    }

    private async void OnConvertToJobClicked(object sender, EventArgs e)
    {
        if (_estimate == null) return;

        var existingJob = await _db.Jobs.FirstOrDefaultAsync(j => j.EstimateId == _estimate.Id);
        if (existingJob != null)
        {
            await DisplayAlertAsync("Already Converted", "This estimate has already been converted to a job.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync("Convert to Job", $"Create a job from this estimate?", "Convert", "Cancel");
        if (!confirm) return;

        try
        {
            var job = new Job
            {
                EstimateId = _estimate.Id,
                CustomerId = _estimate.CustomerId,
                SiteId = _estimate.SiteId,
                AssetId = _estimate.AssetId,
                Title = _estimate.Title,
                Description = _estimate.Description,
                Status = JobStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _estimate.Status = EstimateStatus.Converted;

            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", "Job created! View it in the Jobs tab.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to convert: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_estimate == null) return;

        if (_estimate.Status == EstimateStatus.Accepted || _estimate.Status == EstimateStatus.Converted)
        {
            await DisplayAlertAsync("Cannot Delete", "Accepted or converted estimates cannot be deleted.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync("Delete Estimate", $"Delete '{_estimate.Title}'?", "Delete", "Cancel");
        if (!confirm) return;

        try
        {
            // Remove line items first
            var lines = await _db.EstimateLines.Where(l => l.EstimateId == _estimate.Id).ToListAsync();
            _db.EstimateLines.RemoveRange(lines);
            
            _db.Estimates.Remove(_estimate);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to delete: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_estimate == null) return;

        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter an estimate title.", "OK");
            TitleEntry.Focus();
            return;
        }

        await this.SaveWithFeedbackAsync(async () =>
        {
            await SaveChangesAsync();
        }, 
        successMessage: "Estimate updated.");
    }

    private async Task SaveChangesAsync()
    {
        if (_estimate == null) return;

        try
        {
            // Update estimate
            _estimate.Title = TitleEntry.Text.Trim();
            _estimate.Description = DescriptionEditor.Text?.Trim();
            _estimate.ExpiresAt = ExpiresDatePicker.Date;
            _estimate.Notes = NotesEditor.Text?.Trim();
            _estimate.Terms = TermsEditor.Text?.Trim();

            if (decimal.TryParse(TaxRateEntry.Text, out var taxRate))
                _estimate.TaxRate = taxRate;

            _estimate.TaxIncluded = TaxIncludedSwitch.IsToggled;

            // Delete removed lines
            foreach (var lineId in _deletedLineIds)
            {
                var line = await _db.EstimateLines.FindAsync(lineId);
                if (line != null)
                    _db.EstimateLines.Remove(line);
            }

            // Update/add line items
            var sortOrder = 0;
            foreach (var item in _lineItems)
            {
                if (item.Id > 0)
                {
                    // Update existing
                    var line = await _db.EstimateLines.FindAsync(item.Id);
                    if (line != null)
                    {
                        line.Description = item.Description;
                        line.Type = item.Type;
                        line.Quantity = item.Quantity;
                        line.UnitPrice = item.UnitPrice;
                        line.Total = item.Total;
                        line.SortOrder = sortOrder++;
                    }
                }
                else
                {
                    // Add new
                    var line = new EstimateLine
                    {
                        EstimateId = _estimate.Id,
                        Description = item.Description,
                        Type = item.Type,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Total = item.Total,
                        SortOrder = sortOrder++
                    };
                    _db.EstimateLines.Add(line);
                }
            }

            // Recalculate totals
            _estimate.SubTotal = _lineItems.Sum(i => i.Total);
            _estimate.TaxAmount = _estimate.SubTotal * (_estimate.TaxRate / 100);
            _estimate.Total = _estimate.SubTotal + _estimate.TaxAmount;

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

// EstimateLineViewModel is defined in AddEstimatePage.xaml.cs
