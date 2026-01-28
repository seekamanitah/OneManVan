using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVan.Mobile.Converters;
using OneManVan.Mobile.Pages;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Services;
using OneManVan.Shared.Models;
using ZXing.Net.Maui.Controls;

namespace OneManVan.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader() // Add ZXing barcode reader
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Load database configuration
        var configDir = FileSystem.AppDataDirectory;
        var dbConfigService = new DatabaseConfigService(configDir);
        var dbConfig = dbConfigService.LoadConfiguration();

        // Configure database based on configuration
        // Note: Mobile currently supports SQLite only. SQL Server support requires additional packages.
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, 
            dbConfig.Type == DatabaseType.SQLite ? dbConfig.SqliteFilePath : "onemanvan.db");
        
        builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));
        
        // Also register DbContext for backward compatibility
        builder.Services.AddDbContext<OneManVanDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Register core services (Singleton - stateless or app-wide state)
        builder.Services.AddSingleton<ISettingsStorage, MobileSettingsStorage>();
        builder.Services.AddSingleton<IBackupService, MobileBackupService>();
        builder.Services.AddSingleton<MobileBackupService>();
        builder.Services.AddSingleton<OneManVan.Shared.Services.TradeConfigurationService>();
        builder.Services.AddSingleton<OfflineSyncService>();
        builder.Services.AddSingleton<AdvancedSyncService>();
        builder.Services.AddSingleton<SyncConflictResolver>();
        builder.Services.AddSingleton<IBarcodeScannerService, BarcodeScannerService>();
        builder.Services.AddSingleton<OfflinePhotoQueueService>();
        builder.Services.AddSingleton<ImageCacheService>();
        
        // Register services that need DbContext (Scoped - tied to request/page lifetime)
        builder.Services.AddScoped<PhotoCaptureService>();
        builder.Services.AddScoped<InventoryLookupService>();
        builder.Services.AddScoped<Shared.Services.CsvExportImportService>(); // Using shared service
        builder.Services.AddScoped<QuickAddCustomerService>();
        builder.Services.AddScoped<CustomerPickerService>();
        builder.Services.AddScoped<LineItemDialogService>();
        builder.Services.AddScoped<DashboardDataService>(); // Dashboard metrics

        // Register main pages (Transient - new instance each time)
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CustomerListPage>();
        builder.Services.AddTransient<CustomerDetailPage>();
        builder.Services.AddTransient<EditCustomerPage>();
        builder.Services.AddTransient<CustomerPickerPage>();
        builder.Services.AddTransient<AssetListPage>();
        builder.Services.AddTransient<AssetDetailPage>();
        builder.Services.AddTransient<AddAssetPage>();
        builder.Services.AddTransient<EditAssetPage>();
        builder.Services.AddTransient<AddSitePage>();
        builder.Services.AddTransient<EstimateListPage>();
        builder.Services.AddTransient<EstimateDetailPage>();
        builder.Services.AddTransient<AddEstimatePage>();
        builder.Services.AddTransient<EditEstimatePage>();
        builder.Services.AddTransient<JobListPage>();
        builder.Services.AddTransient<JobDetailPage>();
        builder.Services.AddTransient<AddJobPage>();
        builder.Services.AddTransient<EditJobPage>();
        builder.Services.AddTransient<InvoiceListPage>();
        builder.Services.AddTransient<InvoiceDetailPage>();
        builder.Services.AddTransient<AddInvoicePage>();
        builder.Services.AddTransient<EditInvoicePage>();
        builder.Services.AddTransient<InventoryListPage>();
        builder.Services.AddTransient<InventoryDetailPage>();
        builder.Services.AddTransient<AddInventoryItemPage>();
        builder.Services.AddTransient<EditInventoryItemPage>();
        builder.Services.AddTransient<ServiceAgreementListPage>();
        builder.Services.AddTransient<ServiceAgreementDetailPage>();
        builder.Services.AddTransient<AddServiceAgreementPage>();
        builder.Services.AddTransient<ProductListPage>();
        builder.Services.AddTransient<ProductDetailPage>();
        builder.Services.AddTransient<AddProductPage>();
        builder.Services.AddTransient<EditProductPage>();
        builder.Services.AddTransient<CompanyListPage>();
        builder.Services.AddTransient<CompanyDetailPage>();
        builder.Services.AddTransient<AddCompanyPage>();
        builder.Services.AddTransient<EditCompanyPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<SetupWizardPage>();
        builder.Services.AddTransient<SchemaEditorPage>();
        builder.Services.AddTransient<SchemaViewerPage>();
        builder.Services.AddTransient<BarcodeScannerPage>();
        builder.Services.AddTransient<SyncSettingsPage>();
        builder.Services.AddTransient<SyncStatusPage>();
        builder.Services.AddTransient<MobileTestRunnerPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Ensure database is created using factory
        using var scope = app.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OneManVanDbContext>>();
        using var db = dbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        return app;
    }
}
