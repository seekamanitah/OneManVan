namespace OneManVan.MauiBlazor;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Initialize database
        InitializeDatabaseAsync().ConfigureAwait(false);

        MainPage = new MainPage();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var dbInitializer = Handler.MauiContext!.Services
                .GetRequiredService<DatabaseInitializer>();
            
            await dbInitializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
        }
    }
}
