using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace OneManVan.Web.Tests;

/// <summary>
/// Automated end-to-end tests for OneManVan Web application.
/// Uses Playwright for browser automation.
/// </summary>
[TestFixture]
public class WebAppAutomatedTests : PageTest
{
    private const string BaseUrl = "https://localhost:5001";
    private const string AdminEmail = "admin@onemanvan.com";
    private const string AdminPassword = "Admin123!";

    [SetUp]
    public async Task Setup()
    {
        // Navigate to app and login before each test
        await Page.GotoAsync(BaseUrl);
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        // Check if already logged in
        if (await Page.Locator("text=Dashboard").IsVisibleAsync())
        {
            return; // Already logged in
        }

        // Fill login form
        await Page.FillAsync("input[type='email']", AdminEmail);
        await Page.FillAsync("input[type='password']", AdminPassword);
        await Page.ClickAsync("button[type='submit']");

        // Wait for dashboard to load
        await Page.WaitForSelectorAsync("text=Dashboard", new() { Timeout = 5000 });
    }

    #region Authentication Tests

    [Test]
    public async Task Test_01_Login_WithValidCredentials_Success()
    {
        // Already logged in via Setup
        await Expect(Page.Locator("text=Dashboard")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Test_02_Dashboard_Loads_Successfully()
    {
        await Page.ClickAsync("text=Dashboard");
        await Expect(Page).ToHaveTitleAsync(new Regex("Dashboard"));
        
        // Check for dashboard elements
        await Expect(Page.Locator("h1:has-text('Dashboard')")).ToBeVisibleAsync();
    }

    #endregion

    #region Customer CRUD Tests

    [Test]
    public async Task Test_03_Customers_Create_Read_Update_Delete()
    {
        // Navigate to Customers
        await Page.ClickAsync("text=Customers");
        await Expect(Page.Locator("h1:has-text('Customers')")).ToBeVisibleAsync();

        // CREATE
        await Page.ClickAsync("button:has-text('Add Customer')");
        await Page.FillAsync("input[name='Name']", "Test Customer Automated");
        await Page.FillAsync("input[name='Email']", "automated@test.com");
        await Page.FillAsync("input[name='Phone']", "5551234567");
        await Page.SelectOptionAsync("select[name='CustomerType']", "Residential");
        await Page.ClickAsync("button:has-text('Save')");

        // READ - Verify customer appears in list
        await Expect(Page.Locator("text=Test Customer Automated")).ToBeVisibleAsync();

        // UPDATE
        await Page.ClickAsync("text=Test Customer Automated");
        await Page.ClickAsync("button:has-text('Edit')");
        await Page.FillAsync("input[name='Name']", "Test Customer Updated");
        await Page.ClickAsync("button:has-text('Save')");
        await Expect(Page.Locator("text=Test Customer Updated")).ToBeVisibleAsync();

        // DELETE
        await Page.ClickAsync("text=Test Customer Updated");
        await Page.ClickAsync("button:has-text('Delete')");
        await Page.ClickAsync("button:has-text('Confirm')"); // Confirm deletion
        
        // Verify deletion
        await Expect(Page.Locator("text=Test Customer Updated")).Not.ToBeVisibleAsync();
    }

    #endregion

    #region Company CRUD Tests

    [Test]
    public async Task Test_04_Companies_Create_Read_Update_Delete()
    {
        // Navigate to Companies
        await Page.ClickAsync("text=Companies");
        await Expect(Page.Locator("h1:has-text('Companies')")).ToBeVisibleAsync();

        // CREATE
        await Page.ClickAsync("button:has-text('Add Company')");
        await Page.FillAsync("input[name='CompanyName']", "Test Company Automated");
        await Page.FillAsync("input[name='Email']", "company@automated.com");
        await Page.SelectOptionAsync("select[name='CompanyType']", "Contractor");
        await Page.ClickAsync("button:has-text('Save')");

        // READ
        await Expect(Page.Locator("text=Test Company Automated")).ToBeVisibleAsync();

        // UPDATE
        await Page.ClickAsync("text=Test Company Automated");
        await Page.ClickAsync("button:has-text('Edit')");
        await Page.FillAsync("input[name='CompanyName']", "Test Company Updated");
        await Page.ClickAsync("button:has-text('Save')");
        await Expect(Page.Locator("text=Test Company Updated")).ToBeVisibleAsync();

        // DELETE
        await Page.ClickAsync("text=Test Company Updated");
        await Page.ClickAsync("button:has-text('Delete')");
        await Page.ClickAsync("button:has-text('Confirm')");
        await Expect(Page.Locator("text=Test Company Updated")).Not.ToBeVisibleAsync();
    }

    #endregion

    #region Product CRUD Tests

    [Test]
    public async Task Test_05_Products_Create_Read_Update_Delete()
    {
        // Navigate to Products
        await Page.ClickAsync("text=Products");
        await Expect(Page.Locator("h1:has-text('Products')")).ToBeVisibleAsync();

        // CREATE
        await Page.ClickAsync("button:has-text('Add Product')");
        await Page.FillAsync("input[name='Manufacturer']", "Test Manufacturer");
        await Page.FillAsync("input[name='ModelNumber']", "TEST-001");
        await Page.FillAsync("input[name='ProductName']", "Test Product Automated");
        await Page.ClickAsync("button:has-text('Save')");

        // READ
        await Expect(Page.Locator("text=Test Product Automated")).ToBeVisibleAsync();

        // UPDATE
        await Page.ClickAsync("text=Test Product Automated");
        await Page.ClickAsync("button:has-text('Edit')");
        await Page.FillAsync("input[name='ProductName']", "Test Product Updated");
        await Page.ClickAsync("button:has-text('Save')");
        await Expect(Page.Locator("text=Test Product Updated")).ToBeVisibleAsync();

        // DELETE
        await Page.ClickAsync("text=Test Product Updated");
        await Page.ClickAsync("button:has-text('Delete')");
        await Page.ClickAsync("button:has-text('Confirm')");
        await Expect(Page.Locator("text=Test Product Updated")).Not.ToBeVisibleAsync();
    }

    #endregion

    #region Inventory CRUD Tests

    [Test]
    public async Task Test_06_Inventory_Create_Read_Update_Delete()
    {
        // Navigate to Inventory
        await Page.ClickAsync("text=Inventory");
        await Expect(Page.Locator("h1:has-text('Inventory')")).ToBeVisibleAsync();

        // CREATE
        await Page.ClickAsync("button:has-text('Add Item')");
        await Page.FillAsync("input[name='Sku']", "TEST-SKU-001");
        await Page.FillAsync("input[name='Name']", "Test Inventory Item");
        await Page.FillAsync("input[name='QuantityOnHand']", "10");
        await Page.FillAsync("input[name='Cost']", "50.00");
        await Page.FillAsync("input[name='Price']", "75.00");
        await Page.ClickAsync("button:has-text('Save')");

        // READ
        await Expect(Page.Locator("text=Test Inventory Item")).ToBeVisibleAsync();

        // UPDATE
        await Page.ClickAsync("text=Test Inventory Item");
        await Page.ClickAsync("button:has-text('Edit')");
        await Page.FillAsync("input[name='QuantityOnHand']", "15");
        await Page.ClickAsync("button:has-text('Save')");

        // DELETE
        await Page.ClickAsync("text=Test Inventory Item");
        await Page.ClickAsync("button:has-text('Delete')");
        await Page.ClickAsync("button:has-text('Confirm')");
        await Expect(Page.Locator("text=Test Inventory Item")).Not.ToBeVisibleAsync();
    }

    #endregion

    #region Navigation Tests

    [Test]
    public async Task Test_07_Navigation_All_Pages_Load()
    {
        var pages = new[]
        {
            "Dashboard",
            "Customers",
            "Assets",
            "Products",
            "Inventory",
            "Estimates",
            "Jobs",
            "Invoices",
            "Companies",
            "Sites",
            "Service Agreements",
            "Settings"
        };

        foreach (var pageName in pages)
        {
            await Page.ClickAsync($"text={pageName}");
            
            // Wait for page to load
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Verify no errors
            var consoleErrors = new List<string>();
            Page.Console += (_, msg) =>
            {
                if (msg.Type == "error")
                {
                    consoleErrors.Add(msg.Text);
                }
            };

            Assert.That(consoleErrors, Is.Empty, $"Console errors on {pageName}: {string.Join(", ", consoleErrors)}");
        }
    }

    #endregion

    #region Search and Filter Tests

    [Test]
    public async Task Test_08_Customers_Search_Works()
    {
        // Create test customer first
        await Page.ClickAsync("text=Customers");
        await Page.ClickAsync("button:has-text('Add Customer')");
        await Page.FillAsync("input[name='Name']", "Searchable Customer");
        await Page.FillAsync("input[name='Email']", "searchable@test.com");
        await Page.ClickAsync("button:has-text('Save')");

        // Test search
        await Page.FillAsync("input[placeholder*='Search']", "Searchable");
        await Expect(Page.Locator("text=Searchable Customer")).ToBeVisibleAsync();

        // Clear search
        await Page.FillAsync("input[placeholder*='Search']", "");
        
        // Cleanup
        await Page.ClickAsync("text=Searchable Customer");
        await Page.ClickAsync("button:has-text('Delete')");
        await Page.ClickAsync("button:has-text('Confirm')");
    }

    #endregion

    #region Form Validation Tests

    [Test]
    public async Task Test_09_Customer_Form_Validation_Required_Fields()
    {
        await Page.ClickAsync("text=Customers");
        await Page.ClickAsync("button:has-text('Add Customer')");

        // Try to save without filling required fields
        await Page.ClickAsync("button:has-text('Save')");

        // Should show validation errors
        await Expect(Page.Locator("text=required")).ToBeVisibleAsync();
    }

    #endregion

    #region Relationship Tests

    [Test]
    public async Task Test_10_Asset_Requires_Customer()
    {
        // Create customer first
        await Page.ClickAsync("text=Customers");
        await Page.ClickAsync("button:has-text('Add Customer')");
        await Page.FillAsync("input[name='Name']", "Customer For Asset");
        await Page.FillAsync("input[name='Email']", "asset@test.com");
        await Page.ClickAsync("button:has-text('Save')");

        // Create asset linked to customer
        await Page.ClickAsync("text=Assets");
        await Page.ClickAsync("button:has-text('Add Asset')");
        await Page.SelectOptionAsync("select[name='CustomerId']", new[] { "Customer For Asset" });
        await Page.FillAsync("input[name='Serial']", "ASSET-TEST-001");
        await Page.FillAsync("input[name='Brand']", "Test Brand");
        await Page.FillAsync("input[name='Model']", "Test Model");
        await Page.ClickAsync("button:has-text('Save')");

        // Verify asset was created
        await Expect(Page.Locator("text=ASSET-TEST-001")).ToBeVisibleAsync();

        // Cleanup
        await Page.ClickAsync("text=ASSET-TEST-001");
        await Page.ClickAsync("button:has-text('Delete')");
        await Page.ClickAsync("button:has-text('Confirm')");

        await Page.ClickAsync("text=Customers");
        await Page.ClickAsync("text=Customer For Asset");
        await Page.ClickAsync("button:has-text('Delete')");
        await Page.ClickAsync("button:has-text('Confirm')");
    }

    #endregion
}
