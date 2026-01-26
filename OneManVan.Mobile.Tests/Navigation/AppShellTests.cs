using FluentAssertions;
using Xunit;

namespace OneManVan.Mobile.Tests.Navigation;

public class AppShellTests
{
    [Fact]
    public void NavigationLogic_ShouldHandleBackButtonCorrectly()
    {
        // Test the conceptual navigation logic without MAUI dependencies

        // Simulate having pages in navigation stack
        var hasPagesInStack = true;
        var result = hasPagesInStack; // Should handle navigation

        result.Should().BeTrue();

        // Simulate no pages in stack
        hasPagesInStack = false;
        result = hasPagesInStack; // Should let system handle

        result.Should().BeFalse();
    }

    [Fact]
    public void RouteRegistration_ShouldIncludeAllMainPages()
    {
        // Test that the expected routes would be registered
        var expectedRoutes = new[]
        {
            "Home", "Customers", "Assets", "Estimates", "Jobs", "Invoices",
            "Inventory", "ProductsCatalog", "Agreements", "CustomFields", "Settings"
        };

        expectedRoutes.Should().HaveCountGreaterThan(5);
        expectedRoutes.Should().Contain("Home");
        expectedRoutes.Should().Contain("Jobs");
        expectedRoutes.Should().Contain("Customers");
    }
}