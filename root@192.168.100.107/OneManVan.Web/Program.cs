using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneManVan.Web.Components;
using OneManVan.Web.Components.Account;
using OneManVan.Web.Data;
using OneManVan.Web.Services;
using OneManVan.Web.Services.Export;
using OneManVan.Web.Services.Pdf;
using OneManVan.Shared.Data;
using OneManVan.Shared.Services;
using OneManVan.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Identity database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Database Configuration Service
var configDirectory = Path.Combine(builder.Environment.ContentRootPath, "AppData");
Directory.CreateDirectory(configDirectory);
var dbConfigService = new DatabaseConfigService(configDirectory);
builder.Services.AddSingleton(dbConfigService);

// Shared business database (OneManVanDbContext) - using DatabaseConfigService
var dbConfig = dbConfigService.GetCurrentConfiguration();
var businessConnectionString = builder.Configuration.GetConnectionString("BusinessConnection");

// Priority: 1. appsettings.json, 2. DatabaseConfig file, 3. SQLite fallback
if (!string.IsNullOrEmpty(businessConnectionString))
{
    // Detect if it's SQLite or SQL Server based on connection string format
    if (businessConnectionString.Contains("DataSource=", StringComparison.OrdinalIgnoreCase) ||
        businessConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) && 
        businessConnectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
    {
        // SQLite connection string detected
        builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
            options.UseSqlite(businessConnectionString));
    }
    else
    {
        // SQL Server connection string
        builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
            options.UseSqlServer(businessConnectionString));
    }
}
else if (dbConfig.Type == DatabaseType.SqlServer && !string.IsNullOrEmpty(dbConfig.ServerAddress))
{
    // Use SQL Server configuration from DatabaseConfigService
    var configConnectionString = dbConfig.GetConnectionString();
    builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
        options.UseSqlServer(configConnectionString));
}
else
{
    // Fallback to SQLite for local development
    var businessDbPath = Path.Combine(builder.Environment.ContentRootPath, "AppData", dbConfig.SqliteFilePath);
    Directory.CreateDirectory(Path.GetDirectoryName(businessDbPath)!);
    builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
        options.UseSqlite($"Data Source={businessDbPath}"));
}




// Register shared services
builder.Services.AddSingleton<ISettingsStorage, WebSettingsStorage>();
builder.Services.AddSingleton<TradeConfigurationService>();

// Register export services
builder.Services.AddScoped<ICsvExportService, CsvExportService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();

// Add controllers for API endpoints
builder.Services.AddControllers();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Disabled for development
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    // Simpler password requirements for development
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Initialize databases
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    // Retry logic for SQL Server connection (important for Docker startup)
    var maxRetries = 10;
    var retryDelay = TimeSpan.FromSeconds(5);
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Database initialization attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
            
            // Ensure Identity database is created
            var identityDb = services.GetRequiredService<ApplicationDbContext>();
            identityDb.Database.EnsureCreated();
            logger.LogInformation("Identity database initialized");
            
            // Ensure business database is created
            var dbFactory = services.GetRequiredService<IDbContextFactory<OneManVanDbContext>>();
            using (var businessDb = dbFactory.CreateDbContext())
            {
                businessDb.Database.EnsureCreated();
                logger.LogInformation("Business database initialized with all tables");
            }
            
            // Seed default admin account (for all environments to ensure first-time login works)
            AdminAccountSeeder.SeedAdminUserAsync(services).GetAwaiter().GetResult();
            
            logger.LogInformation("Database initialization completed successfully");
            break; // Success - exit retry loop
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization attempt {Attempt} failed", attempt);
            
            if (attempt == maxRetries)
            {
                logger.LogError(ex, "All database initialization attempts failed. Application may not function correctly.");
            }
            else
            {
                logger.LogInformation("Retrying in {Delay} seconds...", retryDelay.TotalSeconds);
                Task.Delay(retryDelay).GetAwaiter().GetResult();
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles(); // Serve static files

app.UseRouting(); // Enable routing

// === AUTHENTICATION TEMPORARILY DISABLED FOR DEVELOPMENT ===
// Uncomment these when ready to add login:
// app.UseAuthentication();
// app.UseAuthorization();

// Antiforgery is REQUIRED for Blazor forms even without authentication
app.UseAntiforgery();

// Health check endpoint for Docker
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers(); // Add API controller mapping

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
// Commented out for now - no login needed
// app.MapAdditionalIdentityEndpoints();

app.Run();

