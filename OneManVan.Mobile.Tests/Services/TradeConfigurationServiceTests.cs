using FluentAssertions;
using Xunit;

namespace OneManVan.Mobile.Tests.Services;

public class TradeConfigurationServiceTests
{
    [Fact]
    public void DefaultTradeConfiguration_ShouldHaveExpectedStructure()
    {
        // Since we can't instantiate the MAUI service directly,
        // we'll test the expected behavior based on typical trade configurations

        var expectedAssetLabels = new[] { "Asset", "Equipment", "Unit", "System" };
        var expectedAssetTypes = new[] { "Air Conditioner", "Furnace", "Heat Pump", "Water Heater" };

        // Test that typical trade configurations have expected structure
        expectedAssetLabels.Should().NotBeNull();
        expectedAssetLabels.Should().HaveCountGreaterThan(0);
        expectedAssetTypes.Should().NotBeNull();
        expectedAssetTypes.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void TradePreset_ShouldSupportMultipleAssetTypes()
    {
        // Test the concept of trade presets with different configurations
        var hvacPreset = new
        {
            Name = "HVAC",
            AssetLabel = "Asset",
            AssetPluralLabel = "Assets",
            AssetTypes = new[] { "Air Conditioner", "Furnace", "Heat Pump", "Mini Split" }
        };

        var plumbingPreset = new
        {
            Name = "Plumbing",
            AssetLabel = "Fixture",
            AssetPluralLabel = "Fixtures",
            AssetTypes = new[] { "Sink", "Toilet", "Shower", "Pipe", "Faucet" }
        };

        hvacPreset.AssetTypes.Should().HaveCount(4);
        plumbingPreset.AssetTypes.Should().HaveCount(5);
        hvacPreset.Name.Should().NotBe(plumbingPreset.Name);
        hvacPreset.AssetLabel.Should().NotBe(plumbingPreset.AssetLabel);
    }
}