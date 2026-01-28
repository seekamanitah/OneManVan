using OneManVan.Mobile.Pages;

namespace OneManVan.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for navigation
		Routing.RegisterRoute("CustomerDetail", typeof(CustomerDetailPage));
		Routing.RegisterRoute("EditCustomer", typeof(EditCustomerPage));
		Routing.RegisterRoute("CustomerPicker", typeof(CustomerPickerPage));
		Routing.RegisterRoute("AssetDetail", typeof(AssetDetailPage));
		Routing.RegisterRoute("AddAsset", typeof(AddAssetPage));
		Routing.RegisterRoute("EditAsset", typeof(EditAssetPage));
		Routing.RegisterRoute("AddSite", typeof(AddSitePage));
		Routing.RegisterRoute("AddEstimate", typeof(AddEstimatePage));
		Routing.RegisterRoute("EstimateDetail", typeof(EstimateDetailPage));
		Routing.RegisterRoute("EditEstimate", typeof(EditEstimatePage));
		Routing.RegisterRoute("JobDetail", typeof(JobDetailPage));
		Routing.RegisterRoute("AddJob", typeof(AddJobPage));
		Routing.RegisterRoute("EditJob", typeof(EditJobPage));
		Routing.RegisterRoute("InvoiceDetail", typeof(InvoiceDetailPage));
		Routing.RegisterRoute("AddInvoice", typeof(AddInvoicePage));
		Routing.RegisterRoute("EditInvoice", typeof(EditInvoicePage));
		Routing.RegisterRoute("InventoryDetail", typeof(InventoryDetailPage));
		Routing.RegisterRoute("AddInventoryItem", typeof(AddInventoryItemPage));
		Routing.RegisterRoute("EditInventoryItem", typeof(EditInventoryItemPage));
		Routing.RegisterRoute("ServiceAgreements", typeof(ServiceAgreementListPage));
		Routing.RegisterRoute("ServiceAgreementDetail", typeof(ServiceAgreementDetailPage));
		Routing.RegisterRoute("AddServiceAgreement", typeof(AddServiceAgreementPage));
		Routing.RegisterRoute("ProductDetail", typeof(ProductDetailPage));
		Routing.RegisterRoute("AddProduct", typeof(AddProductPage));
		Routing.RegisterRoute("EditProduct", typeof(EditProductPage));
		Routing.RegisterRoute("TestRunner", typeof(MobileTestRunnerPage));
		
		// Company routes
		Routing.RegisterRoute("CompanyDetail", typeof(CompanyDetailPage));
		Routing.RegisterRoute("AddCompany", typeof(AddCompanyPage));
		Routing.RegisterRoute("EditCompany", typeof(EditCompanyPage));
		
		// Schema and sync routes
		Routing.RegisterRoute("SchemaEditor", typeof(SchemaEditorPage));
		Routing.RegisterRoute("SchemaViewer", typeof(SchemaViewerPage));
		Routing.RegisterRoute("BarcodeScanner", typeof(BarcodeScannerPage));
		Routing.RegisterRoute("SyncSettings", typeof(SyncSettingsPage));
		Routing.RegisterRoute("SyncStatus", typeof(SyncStatusPage));
		Routing.RegisterRoute("SetupWizard", typeof(SetupWizardPage));
	}

	/// <summary>
	/// Handle Android hardware/gesture back button properly.
	/// This prevents the app from closing when on a sub-page.
	/// </summary>
	protected override bool OnBackButtonPressed()
	{
		// Check if we can navigate back in the current navigation stack
		if (Current?.Navigation?.NavigationStack?.Count > 1)
		{
			// There are pages in the navigation stack, go back
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await Current.Navigation.PopAsync();
			});
			return true; // Handled
		}

		// Check if we can navigate back in the modal stack
		if (Current?.Navigation?.ModalStack?.Count > 0)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await Current.Navigation.PopModalAsync();
			});
			return true; // Handled
		}

		// Check if we're not on the home/root tab - navigate to home first
		if (Current?.CurrentItem?.CurrentItem?.Route != "Home" && 
		    Current?.CurrentItem?.CurrentItem?.Route != "HomeTab")
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				if (Current != null)
				{
					await Current.GoToAsync("//Home");
				}
			});
			return true; // Handled
		}

		// On root page - let system handle (close app or show confirmation)
		// Return false to let the default behavior happen
		return base.OnBackButtonPressed();
	}
}
