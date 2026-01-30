using Microsoft.AspNetCore.Identity;
using OneManVan.Web.Data;

namespace OneManVan.Web.Services;

/// <summary>
/// Seeds the database with default admin user for development.
/// </summary>
public static class AdminAccountSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Check if admin already exists
        var adminEmail = "admin@onemanvan.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        
        if (existingAdmin == null)
        {
            // Create admin user
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true // Auto-confirm for dev
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            
            if (result.Succeeded)
            {
                Console.WriteLine($"? Default admin account created: {adminEmail} / Admin123!");
            }
            else
            {
                Console.WriteLine($"?? Failed to create admin account:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error.Description}");
                }
            }
        }
        else
        {
            Console.WriteLine($"?? Admin account already exists: {adminEmail}");
        }
    }
}
