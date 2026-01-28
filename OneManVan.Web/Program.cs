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

// Shared business database (OneManVanDbContext)
var businessDbPath = Path.Combine(builder.Environment.ContentRootPath, "AppData", "onemanvan.db");
Directory.CreateDirectory(Path.GetDirectoryName(businessDbPath)!);
builder.Services.AddDbContextFactory<OneManVanDbContext>(options =>
    options.UseSqlite($"Data Source={businessDbPath}"));

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
    
    try
    {
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
        
        // Seed default admin account (Development only)
        if (app.Environment.IsDevelopment())
        {
            AdminAccountSeeder.SeedAdminUserAsync(services).GetAwaiter().GetResult();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing databases");
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

app.UseAntiforgery();

app.MapControllers(); // Add API controller mapping

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
