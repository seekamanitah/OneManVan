using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneManVan.Web.Data;

namespace OneManVan.Web.Services;

/// <summary>
/// Seeds the database with admin user from environment configuration.
/// In production, admin accounts should be created through a secure setup wizard.
/// </summary>
public static class AdminAccountSeeder
{
    private static bool _hasRun = false;
    
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        // Skip if already run this session (optimization)
        if (_hasRun) return;
        
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetService<ILogger<Program>>();
        
        // Get admin credentials from configuration (environment variables or appsettings)
        var adminEmail = configuration["AdminUser:Email"];
        var adminPassword = configuration["AdminUser:Password"];
        
        // Skip seeding if no admin credentials configured
        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            logger?.LogInformation("Admin user seeding skipped - no credentials configured. Set AdminUser:Email and AdminUser:Password in environment or configuration.");
            _hasRun = true;
            return;
        }
        
        // Check if admin already exists
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        
        if (existingAdmin == null)
        {
            // Create admin user
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            
            if (result.Succeeded)
            {
                logger?.LogInformation("Admin account created successfully.");
            }
            else
            {
                logger?.LogWarning("Failed to create admin account: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        
        _hasRun = true;
    }
}
