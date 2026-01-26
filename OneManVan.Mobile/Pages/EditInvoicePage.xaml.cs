using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(InvoiceId), "id")]
public partial class EditInvoicePage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Invoice? _invoice;
    
    public int InvoiceId { get; set; }

    public EditInvoicePage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInvoiceAsync();
    }

    private async Task LoadInvoiceAsync()
    {
        try
        {
            using (new LoadingScope(LoadingIndicator))
            {
                _invoice = await _db.Invoices
                    .Include(i => i.Customer)
                    .FirstOrDefaultAsync(i => i.Id == InvoiceId);

                if (_invoice == null)
                {
                    await DisplayAlertAsync("Error", "Invoice not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                InvoiceDatePicker.Date = _invoice.InvoiceDate;
                DueDatePicker.Date = _invoice.DueDate;
                LaborAmountEntry.Text = _invoice.LaborAmount.ToString("N2");
                PartsAmountEntry.Text = _invoice.PartsAmount.ToString("N2");
                TaxRateEntry.Text = _invoice.TaxRate.ToString("N1");
                NotesEditor.Text = _invoice.Notes;
                StatusPicker.SelectedIndex = (int)_invoice.Status;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditInvoice load error: {ex}");
            await DisplayAlertAsync("Unable to Load", 
                "Failed to load invoice data. Please try again.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate data is loaded
        if (_invoice == null)
        {
            await DisplayAlertAsync("Cannot Save", 
                "Invoice data not loaded. Please go back and try again.", "OK");
            return;
        }

        // Use SaveWithFeedbackAsync for clean save pattern
        await this.SaveWithFeedbackAsync(async () =>
        {
            _invoice.InvoiceDate = InvoiceDatePicker.Date ?? DateTime.Today;
            _invoice.DueDate = DueDatePicker.Date ?? DateTime.Today.AddDays(30);
            _invoice.LaborAmount = decimal.Parse(LaborAmountEntry.Text ?? "0");
            _invoice.PartsAmount = decimal.Parse(PartsAmountEntry.Text ?? "0");
            _invoice.TaxRate = decimal.Parse(TaxRateEntry.Text ?? "7.0");
            _invoice.Notes = NotesEditor.Text?.Trim();
            _invoice.Status = (InvoiceStatus)StatusPicker.SelectedIndex;

            _invoice.RecalculateTotals();
            await _db.SaveChangesAsync();
        }, 
        successMessage: "Invoice updated successfully");
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
