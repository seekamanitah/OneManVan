using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

public partial class SchemaViewerPage : ContentPage
{
    private readonly OneManVanDbContext _db;

    public SchemaViewerPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSchemaDefinitionsAsync();
    }

    private async Task LoadSchemaDefinitionsAsync()
    {
        try
        {
            var definitions = await _db.SchemaDefinitions
                .Where(s => s.IsActive)
                .OrderBy(s => s.EntityType)
                .ThenBy(s => s.DisplayOrder)
                .ThenBy(s => s.DisplayLabel)
                .ToListAsync();

            // Group by entity type
            var customerFields = definitions.Where(d => d.EntityType == "Customer").ToList();
            var assetFields = definitions.Where(d => d.EntityType == "Asset").ToList();
            var jobFields = definitions.Where(d => d.EntityType == "Job").ToList();
            var estimateFields = definitions.Where(d => d.EntityType == "Estimate").ToList();
            var siteFields = definitions.Where(d => d.EntityType == "Site").ToList();
            var invoiceFields = definitions.Where(d => d.EntityType == "Invoice").ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CustomerFieldsCollection.ItemsSource = customerFields;
                CustomerFieldCount.Text = $"{customerFields.Count} field{(customerFields.Count != 1 ? "s" : "")}";

                AssetFieldsCollection.ItemsSource = assetFields;
                AssetFieldCount.Text = $"{assetFields.Count} field{(assetFields.Count != 1 ? "s" : "")}";

                JobFieldsCollection.ItemsSource = jobFields;
                JobFieldCount.Text = $"{jobFields.Count} field{(jobFields.Count != 1 ? "s" : "")}";

                EstimateFieldsCollection.ItemsSource = estimateFields;
                EstimateFieldCount.Text = $"{estimateFields.Count} field{(estimateFields.Count != 1 ? "s" : "")}";

                SiteFieldsCollection.ItemsSource = siteFields;
                SiteFieldCount.Text = $"{siteFields.Count} field{(siteFields.Count != 1 ? "s" : "")}";

                InvoiceFieldsCollection.ItemsSource = invoiceFields;
                InvoiceFieldCount.Text = $"{invoiceFields.Count} field{(invoiceFields.Count != 1 ? "s" : "")}";

                // Update sync info
                var totalFields = definitions.Count;
                if (totalFields > 0)
                {
                    var latestModified = definitions.Max(d => d.ModifiedAt ?? d.CreatedAt);
                    SyncInfoLabel.Text = $"Schema has {totalFields} custom field{(totalFields != 1 ? "s" : "")} • Last updated: {latestModified:MMM d, yyyy}";
                }
                else
                {
                    SyncInfoLabel.Text = "No custom fields defined. Create them on the desktop app.";
                }
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load schema: {ex.Message}", "OK");
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadSchemaDefinitionsAsync();
        RefreshViewControl.IsRefreshing = false;
    }
}
