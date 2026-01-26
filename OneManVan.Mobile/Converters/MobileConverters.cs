using System.Globalization;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Converters;

/// <summary>
/// Converts a name string to initials (e.g., "John Smith" -> "JS").
/// </summary>
public class InitialsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string name && !string.IsNullOrWhiteSpace(name))
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2)
                return parts[0][..2].ToUpper();
            if (parts.Length == 1)
                return parts[0][0].ToString().ToUpper();
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts a string to boolean (true if not null/empty).
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value?.ToString());

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts null to boolean (true if not null).
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value != null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

/// <summary>
/// Converts FuelType to an icon emoji.
/// </summary>
public class FuelTypeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            FuelType.NaturalGas => "Gas",
            FuelType.Propane => "Propane",
            FuelType.Electric => "Electric",
            FuelType.Oil => "Oil",
            FuelType.DualFuel => "Dual",
            _ => "Other"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts FuelType to a background color.
/// </summary>
public class FuelTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            FuelType.NaturalGas => Color.FromArgb("#FF5722"),  // Deep Orange
            FuelType.Propane => Color.FromArgb("#FF9800"),     // Orange
            FuelType.Electric => Color.FromArgb("#2196F3"),   // Blue
            FuelType.Oil => Color.FromArgb("#795548"),        // Brown
            FuelType.DualFuel => Color.FromArgb("#9C27B0"),   // Purple
            _ => Color.FromArgb("#9E9E9E")                    // Grey
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts warranty status to badge color.
/// </summary>
public class WarrantyBadgeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Expects IsWarrantyExpired boolean
        if (value is bool isExpired && isExpired)
            return Color.FromArgb("#F44336"); // Red

        // Check if expiring soon (this would need the full asset, so we use a simpler approach)
        return Color.FromArgb("#4CAF50"); // Green by default
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts days until warranty expires to display text.
/// </summary>
public class WarrantyDaysConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int days)
        {
            if (days < 0) return "Expired";
            if (days == 0) return "Today";
            if (days <= 30) return $"{days}d";
            if (days <= 365) return $"{days / 30}mo";
            return $"{days / 365}yr";
        }
        return "N/A";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts EstimateStatus to a color.
/// </summary>
public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EstimateStatus.Draft => Color.FromArgb("#9E9E9E"),      // Grey
            EstimateStatus.Sent => Color.FromArgb("#2196F3"),       // Blue
            EstimateStatus.Accepted => Color.FromArgb("#4CAF50"),   // Green
            EstimateStatus.Declined => Color.FromArgb("#F44336"),   // Red
            EstimateStatus.Expired => Color.FromArgb("#FF9800"),    // Orange
            EstimateStatus.Converted => Color.FromArgb("#673AB7"), // Purple
            _ => Color.FromArgb("#9E9E9E")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts JobStatus to a color.
/// </summary>
public class JobStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            JobStatus.Draft => Color.FromArgb("#9E9E9E"),          // Grey
            JobStatus.Scheduled => Color.FromArgb("#2196F3"),     // Blue
            JobStatus.EnRoute => Color.FromArgb("#00BCD4"),       // Cyan
            JobStatus.InProgress => Color.FromArgb("#FF9800"),    // Orange
            JobStatus.Completed => Color.FromArgb("#4CAF50"),     // Green
            JobStatus.Closed => Color.FromArgb("#673AB7"),        // Purple
            JobStatus.Cancelled => Color.FromArgb("#F44336"),     // Red
            JobStatus.OnHold => Color.FromArgb("#FFC107"),        // Amber
            _ => Color.FromArgb("#9E9E9E")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts InvoiceStatus to a color.
/// </summary>
public class InvoiceStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            InvoiceStatus.Draft => Color.FromArgb("#9E9E9E"),        // Grey
            InvoiceStatus.Sent => Color.FromArgb("#2196F3"),         // Blue
            InvoiceStatus.PartiallyPaid => Color.FromArgb("#FF9800"),// Orange
            InvoiceStatus.Paid => Color.FromArgb("#4CAF50"),         // Green
            InvoiceStatus.Overdue => Color.FromArgb("#F44336"),      // Red
            InvoiceStatus.Cancelled => Color.FromArgb("#795548"),    // Brown
            InvoiceStatus.Refunded => Color.FromArgb("#9C27B0"),     // Purple
            _ => Color.FromArgb("#9E9E9E")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts PaymentMethod to display string.
/// </summary>
public class PaymentMethodConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            PaymentMethod.Cash => "Cash",
            PaymentMethod.Check => "Check",
            PaymentMethod.CreditCard => "Credit Card",
            PaymentMethod.DebitCard => "Debit Card",
            PaymentMethod.BankTransfer => "Bank Transfer",
            PaymentMethod.Digital => "Digital",
            PaymentMethod.Financing => "Financing",
            _ => "Other"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts InventoryCategory to display string with icon.
/// </summary>
public class InventoryCategoryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            InventoryCategory.Filters => "Filters",
            InventoryCategory.Coils => "Coils",
            InventoryCategory.Refrigerants => "Refrigerants",
            InventoryCategory.Motors => "Motors",
            InventoryCategory.Capacitors => "Capacitors",
            InventoryCategory.Thermostats => "Thermostats",
            InventoryCategory.Ductwork => "Ductwork",
            InventoryCategory.Electrical => "Electrical",
            InventoryCategory.Tools => "Tools",
            InventoryCategory.Consumables => "Consumables",
            InventoryCategory.Contactors => "Contactors",
            InventoryCategory.Fittings => "Fittings",
            _ => "General"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CustomerStatus to a background color.
/// </summary>
public class CustomerStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CustomerStatus.Active => Color.FromArgb("#4CAF50"),      // Green
            CustomerStatus.Inactive => Color.FromArgb("#9E9E9E"),    // Grey
            CustomerStatus.Lead => Color.FromArgb("#2196F3"),        // Blue
            CustomerStatus.VIP => Color.FromArgb("#FFD700"),         // Gold
            CustomerStatus.DoNotService => Color.FromArgb("#F44336"),// Red
            CustomerStatus.Delinquent => Color.FromArgb("#FF9800"),  // Orange
            CustomerStatus.Archived => Color.FromArgb("#795548"),    // Brown
            _ => Color.FromArgb("#9E9E9E")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CustomerStatus to display text with icon.
/// </summary>
public class CustomerStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CustomerStatus.Active => "Active",
            CustomerStatus.Inactive => "Inactive",
            CustomerStatus.Lead => "Lead",
            CustomerStatus.VIP => "VIP",
            CustomerStatus.DoNotService => "DNS",
            CustomerStatus.Delinquent => "Delinquent",
            CustomerStatus.Archived => "Archived",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CustomerType to a background color.
/// </summary>
public class CustomerTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CustomerType.Residential => Color.FromArgb("#E3F2FD"),    // Light Blue
            CustomerType.Commercial => Color.FromArgb("#FFF3E0"),     // Light Orange
            CustomerType.PropertyManager => Color.FromArgb("#E8F5E9"),// Light Green
            CustomerType.Government => Color.FromArgb("#F3E5F5"),     // Light Purple
            CustomerType.NonProfit => Color.FromArgb("#E0F7FA"),      // Light Cyan
            CustomerType.NewConstruction => Color.FromArgb("#FBE9E7"),// Light Deep Orange
            _ => Color.FromArgb("#F5F5F5")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CustomerType to a text color.
/// </summary>
public class CustomerTypeTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CustomerType.Residential => Color.FromArgb("#1976D2"),    // Blue
            CustomerType.Commercial => Color.FromArgb("#FF9800"),     // Orange
            CustomerType.PropertyManager => Color.FromArgb("#388E3C"),// Green
            CustomerType.Government => Color.FromArgb("#7B1FA2"),     // Purple
            CustomerType.NonProfit => Color.FromArgb("#00796B"),      // Teal
            CustomerType.NewConstruction => Color.FromArgb("#E64A19"),// Deep Orange
            _ => Color.FromArgb("#757575")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CustomerType to display text with icon.
/// </summary>
public class CustomerTypeTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CustomerType.Residential => "Residential",
            CustomerType.Commercial => "Commercial",
            CustomerType.PropertyManager => "Property Mgr",
            CustomerType.Government => "Government",
            CustomerType.NonProfit => "Non-Profit",
            CustomerType.NewConstruction => "New Construction",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts RefrigerantType to a display string.
/// </summary>
public class RefrigerantTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            RefrigerantType.R22 => "R-22 (Legacy)",
            RefrigerantType.R410A => "R-410A",
            RefrigerantType.R407C => "R-407C",
            RefrigerantType.R134a => "R-134a",
            RefrigerantType.R32 => "R-32",
            RefrigerantType.R454B => "R-454B",
            RefrigerantType.R452B => "R-452B",
            RefrigerantType.R290 => "R-290 (Propane)",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts RefrigerantType to a background color (highlighting legacy refrigerants).
/// </summary>
public class RefrigerantColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            RefrigerantType.R22 => Color.FromArgb("#FFCDD2"),    // Light Red - legacy
            RefrigerantType.Unknown => Color.FromArgb("#E0E0E0"),// Grey
            _ => Color.FromArgb("#E8F5E9")                       // Light Green - modern
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts EquipmentType to a display icon.
/// </summary>
public class EquipmentTypeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EquipmentType.GasFurnace => "Gas Furnace",
            EquipmentType.OilFurnace => "Oil Furnace",
            EquipmentType.ElectricFurnace => "Electric Furnace",
            EquipmentType.Boiler => "Boiler",
            EquipmentType.AirConditioner => "AC",
            EquipmentType.HeatPump => "Heat Pump",
            EquipmentType.MiniSplit => "Mini Split",
            EquipmentType.DuctlessMiniSplit => "Ductless",
            EquipmentType.PackagedUnit => "Packaged",
            EquipmentType.RooftopUnit => "Rooftop",
            EquipmentType.Coil => "Coil",
            EquipmentType.Condenser => "Condenser",
            EquipmentType.AirHandler => "Air Handler",
            EquipmentType.Humidifier => "Humidifier",
            EquipmentType.Dehumidifier => "Dehumidifier",
            EquipmentType.AirPurifier => "Purifier",
            EquipmentType.UVLight => "UV Light",
            EquipmentType.Thermostat => "Thermostat",
            _ => "Other"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts EquipmentType to background color.
/// </summary>
public class EquipmentTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EquipmentType.GasFurnace or EquipmentType.OilFurnace => Color.FromArgb("#FF5722"),
            EquipmentType.ElectricFurnace => Color.FromArgb("#FF9800"),
            EquipmentType.Boiler => Color.FromArgb("#E91E63"),
            EquipmentType.AirConditioner => Color.FromArgb("#2196F3"),
            EquipmentType.HeatPump => Color.FromArgb("#9C27B0"),
            EquipmentType.MiniSplit or EquipmentType.DuctlessMiniSplit => Color.FromArgb("#00BCD4"),
            EquipmentType.PackagedUnit or EquipmentType.RooftopUnit => Color.FromArgb("#607D8B"),
            EquipmentType.Coil or EquipmentType.Condenser or EquipmentType.AirHandler => Color.FromArgb("#3F51B5"),
            EquipmentType.Humidifier or EquipmentType.Dehumidifier => Color.FromArgb("#009688"),
            EquipmentType.AirPurifier or EquipmentType.UVLight => Color.FromArgb("#8BC34A"),
            EquipmentType.Thermostat => Color.FromArgb("#795548"),
            _ => Color.FromArgb("#9E9E9E")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean VIP status to visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts decimal balance to formatted currency string.
/// </summary>
public class CurrencyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d.ToString("C2");
        return "$0.00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
