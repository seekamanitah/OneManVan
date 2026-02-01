using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
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

// Rate limiting configuration
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Global rate limit: 100 requests per minute per IP
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    
    // Stricter rate limit for authentication endpoints: 10 per minute
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    
    // API export rate limit: 20 per minute
    options.AddPolicy("export", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

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
var identityConnectionString = builder.Configuration.GetConnectionString("IdentityConnection") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(identityConnectionString))
{
    // Check if running in Docker (SQL Server) or local (SQLite)
    if (identityConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
    {
        // SQL Server - but only if environment variable is set (Docker mode)
        var saPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
        if (!string.IsNullOrEmpty(saPassword))
        {
            // Replace placeholder with actual password
            identityConnectionString = identityConnectionString.Replace("${SA_PASSWORD}", saPassword);
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(identityConnectionString));
        }
        else
        {
            // SQL Server connection string but no SA_PASSWORD - fall back to SQLite
            var identityDbPath = Path.Combine(builder.Environment.ContentRootPath, "AppData", "Identity.db");
            Directory.CreateDirectory(Path.GetDirectoryName(identityDbPath)!);
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={identityDbPath}"));
            Console.WriteLine("[INFO] SQL Server connection configured but SA_PASSWORD not set - using SQLite for Identity");
        }
    }
    else
    {
        // SQLite connection string (Local development)
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(identityConnectionString));
    }
}
else
{
    // No connection string - fallback to SQLite
    var identityDbPath = Path.Combine(builder.Environment.ContentRootPath, "AppData", "Identity.db");
    Directory.CreateDirectory(Path.GetDirectoryName(identityDbPath)!);
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={identityDbPath}"));
    Console.WriteLine("[INFO] No connection string found - using SQLite for Identity");
}
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Database Configuration Service
var configDirectory = Path.Combine(builder.Environment.ContentRootPath, "AppData");
Directory.CreateDirectory(configDirectory);
var dbConfigService = new DatabaseConfigService(configDirectory);
builder.Services.AddSingleton(dbConfigService);

// Shared business database (OneManVanDbContext)
// Priority: 1. Environment variable (Docker), 2. appsettings.json, 3. DatabaseConfig file, 4. SQLite fallback
var businessConnectionString = builder.Configuration.GetConnectionString("BusinessConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(businessConnectionString))
{
    // Detect if it's SQLite or SQL Server based on connection string format
    if (businessConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
    {
        // SQL Server - but only if environment variable is set (Docker mode)
        var saPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
        if (!string.IsNullOrEmpty(saPassword))
        {
            // Replace placeholder with actual password
            businessConnectionString = businessConnectionString.Replace("${SA_PASSWORD}", saPassword);
            builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
                options.UseSqlServer(businessConnectionString));
            Console.WriteLine("[INFO] Using SQL Server for Business database (Docker mode)");
        }
        else
        {
            // SQL Server connection string but no SA_PASSWORD - fall back to SQLite
            var dbConfig = dbConfigService.GetCurrentConfiguration();
            var businessDbPath = Path.Combine(builder.Environment.ContentRootPath, "AppData", dbConfig.SqliteFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(businessDbPath)!);
            builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
                options.UseSqlite($"Data Source={businessDbPath}"));
            Console.WriteLine("[INFO] SQL Server configured but SA_PASSWORD not set - using SQLite for Business database");
        }
    }
    else if (businessConnectionString.Contains("DataSource=", StringComparison.OrdinalIgnoreCase) ||
             businessConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        // SQLite connection string (Local development)
        builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
            options.UseSqlite(businessConnectionString));
    }
    else
    {
        throw new InvalidOperationException($"Unknown connection string format: {businessConnectionString}");
    }
}
else
{
    // Fallback to SQLite for local development (using DatabaseConfigService)
    var dbConfig = dbConfigService.GetCurrentConfiguration();
    var businessDbPath = Path.Combine(builder.Environment.ContentRootPath, "AppData", dbConfig.SqliteFilePath);
    Directory.CreateDirectory(Path.GetDirectoryName(businessDbPath)!);
    builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
        options.UseSqlite($"Data Source={businessDbPath}"));
}









// Register shared services
builder.Services.AddSingleton<ISettingsStorage, WebSettingsStorage>();
builder.Services.AddSingleton<TradeConfigurationService>();
builder.Services.AddScoped<CompanySettingsService>();
builder.Services.AddScoped<DashboardKpiService>();
builder.Services.AddScoped<RouteOptimizationService>();
builder.Services.AddScoped<GoogleCalendarService>();
builder.Services.AddScoped<MaterialListService>();
builder.Services.AddScoped<MaterialListTemplateService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<IEmployeeTimeLogAutoService, EmployeeTimeLogAutoService>();

// Data protection for sensitive data encryption
builder.Services.AddSingleton<IDataProtectionService, DataProtectionService>();

// Register export services
builder.Services.AddScoped<ICsvExportService, CsvExportService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();
builder.Services.AddScoped<IEstimatePdfGenerator, EstimatePdfGenerator>();
builder.Services.AddScoped<IServiceAgreementPdfGenerator, ServiceAgreementPdfGenerator>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add controllers for API endpoints
builder.Services.AddControllers();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // Account settings
    options.SignIn.RequireConfirmedAccount = false; // Can be enabled via configuration
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    
    // Strong password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
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
    
    // Optimized retry logic - shorter delays for faster startup
    var maxRetries = 5;
    var initialRetryDelay = TimeSpan.FromSeconds(2);
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Database initialization attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
            
            // Initialize both databases in parallel for faster startup
            var identityTask = Task.Run(() =>
            {
                try
                {
                    var identityDb = services.GetRequiredService<ApplicationDbContext>();
                    identityDb.Database.EnsureCreated();
                    logger.LogInformation("Identity database initialized successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Identity database initialization failed");
                    throw;
                }
            });
            
            var businessTask = Task.Run(() =>
            {
                try
                {
                    var dbFactory = services.GetRequiredService<IDbContextFactory<OneManVanDbContext>>();
                    using var businessDb = dbFactory.CreateDbContext();
                    businessDb.Database.EnsureCreated();
                    
                    // Ensure CompanySettings table exists (may be missing in older databases)
                    EnsureCompanySettingsTable(businessDb);
                    
                    logger.LogInformation("Business database initialized successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Business database initialization failed");
                    throw;
                }
            });
            
            // Wait for both with timeout
            var completed = Task.WaitAll([identityTask, businessTask], TimeSpan.FromSeconds(30));
            
            if (!completed)
            {
                throw new TimeoutException("Database initialization timed out after 30 seconds");
            }
            
            logger.LogInformation("Identity and business databases initialized");
            
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
                // Exponential backoff: 2s, 4s, 8s, 16s
                var delay = initialRetryDelay * Math.Pow(2, attempt - 1);
                logger.LogInformation("Retrying in {Delay} seconds...", delay.TotalSeconds);
                Task.Delay(delay).GetAwaiter().GetResult();
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

// LAN/Docker deployments may run HTTP-only. Only redirect when explicitly enabled.
if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(); // Serve static files

app.UseRouting(); // Enable routing

// Rate limiting middleware
app.UseRateLimiter();

// Authentication and authorization
app.UseAuthentication();
app.UseAuthorization();


// Antiforgery is REQUIRED for Blazor forms even without authentication
app.UseAntiforgery();

// Health check endpoint for Docker
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers(); // Add API controller mapping

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

// Helper method to ensure CompanySettings table exists for older databases
static void EnsureCompanySettingsTable(OneManVanDbContext db)
{
    try
    {
        // Check if CompanySettings table exists by trying to query it
        var exists = db.CompanySettings.Any();
    }
    catch (Microsoft.Data.Sqlite.SqliteException)
    {
        // Table doesn't exist, create it using raw SQL
        var sql = @"
            CREATE TABLE IF NOT EXISTS CompanySettings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CompanyName TEXT NOT NULL DEFAULT 'OneManVan',
                Tagline TEXT,
                Address TEXT,
                City TEXT,
                State TEXT,
                ZipCode TEXT,
                Email TEXT,
                Phone TEXT,
                Website TEXT,
                LogoBase64 TEXT,
                LogoFileName TEXT,
                TaxId TEXT,
                LicenseNumber TEXT,
                InsuranceNumber TEXT,
                PaymentTerms TEXT DEFAULT 'Payment due within 30 days of invoice date.',
                DocumentFooter TEXT DEFAULT 'Thank you for your business!',
                BankDetails TEXT,
                GoogleCalendarSettings TEXT,
                MapsApiKey TEXT,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );
            
            INSERT OR IGNORE INTO CompanySettings (Id, CompanyName, PaymentTerms, DocumentFooter)
            VALUES (1, 'OneManVan', 'Payment due within 30 days of invoice date.', 'Thank you for your business!');
        ";
        db.Database.ExecuteSqlRaw(sql);
    }
}


