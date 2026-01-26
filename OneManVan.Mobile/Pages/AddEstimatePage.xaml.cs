using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Constants;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using System.Collections.ObjectModel;

namespace OneManVan.Mobile.Pages;

public partial class AddEstimatePage
{
    private readonly OneManVanDbContext _db;
    private readonly CustomerSelectionHelper _customerHelper;
    private readonly LineItemDialogService _lineItemDialog;
    private ObservableCollection<EstimateLineViewModel> _lineItems = [];

    public AddEstimatePage(
        OneManVanDbContext db, 
        CustomerPickerService customerPicker,
        LineItemDialogService lineItemDialog)
    {
        InitializeComponent();
        _db = db;
        _customerHelper = new CustomerSelectionHelper(this, customerPicker);
        _lineItemDialog = lineItemDialog;
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        ExpiresDatePicker.Date = DateTime.Today.AddDays(BusinessDefaults.DefaultEstimateValidityDays);
        TaxRateEntry.Text = BusinessDefaults.DefaultTaxRate.ToString("F0");
        LineItemsCollection.ItemsSource = _lineItems;
        
        // Default terms
        TermsEditor.Text = BusinessDefaults.DefaultEstimateTerms;
        
        UpdateTotals();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _customerHelper.HandleQuickAddCustomerAsync(OnCustomerSelectedAsync);
    }

    private async void OnSelectCustomerTapped(object sender, TappedEventArgs e)
    {
        await _customerHelper.HandleCustomerSelectionAsync(OnCustomerSelectedAsync);
    }

    private async Task OnCustomerSelectedAsync(Customer customer)
    {
        // Use helper to update standard UI
        CustomerSelectionHelper.UpdateCustomerUI(customer, CustomerNameLabel);
    }

    private async void OnAddLineItemClicked(object sender, EventArgs e)
    {
        // Use the LineItemDialogService for consistent UX
        var input = await _lineItemDialog.GetLineItemInputAsync(this, "Line Item Type");
        
        if (input == null)
            return;

        // Map the input type to LineItemType enum
        var lineType = input.ItemType switch
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

        // Handle discount as negative
        var total = lineType == LineItemType.Discount 
            ? -Math.Abs(input.Total) 
            : input.Total;

        _lineItems.Add(new EstimateLineViewModel
        {
            Description = input.Description,
            Type = lineType,
            Quantity = input.Quantity,
            UnitPrice = input.UnitPrice,
            Total = total
        });

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        UpdateTotals();
    }

    private void OnDeleteLineItem(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is EstimateLineViewModel item)
        {
            _lineItems.Remove(item);
            UpdateTotals();
        }
    }

    private void UpdateTotals()
    {
        var subtotal = _lineItems.Sum(i => i.Total);
        SubtotalLabel.Text = $"${subtotal:N2}";

        if (!decimal.TryParse(TaxRateEntry.Text, out var taxRate))
            taxRate = 0;

        var taxAmount = subtotal * (taxRate / 100);
        TaxAmountLabel.Text = $"${taxAmount:N2}";

        var total = subtotal + taxAmount;
        TotalLabel.Text = $"${total:N2}";
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_customerHelper.SelectedCustomer == null)
        {
            await DisplayAlertAsync("Required", "Please select a customer.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter an estimate title.", "OK");
            TitleEntry.Focus();
            return;
        }

        try
        {
            var subtotal = _lineItems.Sum(i => i.Total);
            if (!decimal.TryParse(TaxRateEntry.Text, out var taxRate))
                taxRate = 0;
            var taxAmount = subtotal * (taxRate / 100);
            var total = subtotal + taxAmount;

            var estimate = new Estimate
            {
                CustomerId = _customerHelper.SelectedCustomer.Id,
                Title = TitleEntry.Text.Trim(),
                Description = DescriptionEditor.Text?.Trim(),
                Status = EstimateStatus.Draft,
                SubTotal = subtotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total,
                Notes = NotesEditor.Text?.Trim(),
                Terms = TermsEditor.Text?.Trim(),
                ExpiresAt = ExpiresDatePicker.Date,
                CreatedAt = DateTime.UtcNow
            };

            _db.Estimates.Add(estimate);
            await _db.SaveChangesAsync();

            // Add line items
            var sortOrder = 0;
            foreach (var item in _lineItems)
            {
                var line = new EstimateLine
                {
                    EstimateId = estimate.Id,
                    Description = item.Description,
                    Type = item.Type,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Total = item.Total,
                    SortOrder = sortOrder++
                };
                _db.EstimateLines.Add(line);
            }

            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", $"Estimate created for {_customerHelper.SelectedCustomer.Name}!\nTotal: ${total:N2}", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create estimate: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

/// <summary>
/// View model for estimate line items.
/// </summary>
public class EstimateLineViewModel
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public LineItemType Type { get; set; } = LineItemType.Labor;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }

    public string TypeIcon => Type switch
    {
        LineItemType.Labor => "L",
        LineItemType.Part => "P",
        LineItemType.Material => "M",
        LineItemType.Equipment => "E",
        LineItemType.Service => "S",
        LineItemType.Fee => "F",
        LineItemType.Discount => "-",
        _ => "?"
    };

    public Color TypeColor => Type switch
    {
        LineItemType.Labor => Color.FromArgb("#E3F2FD"),
        LineItemType.Part => Color.FromArgb("#FFF3E0"),
        LineItemType.Material => Color.FromArgb("#E8F5E9"),
        LineItemType.Equipment => Color.FromArgb("#F3E5F5"),
        LineItemType.Service => Color.FromArgb("#E0F7FA"),
        LineItemType.Fee => Color.FromArgb("#FBE9E7"),
        LineItemType.Discount => Color.FromArgb("#FFEBEE"),
        _ => Color.FromArgb("#F5F5F5")
    };
}
