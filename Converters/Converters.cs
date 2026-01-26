using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Converters;

/// <summary>
/// Converts a string to visibility - Visible when null/empty, Collapsed otherwise.
/// Used for placeholder text in search boxes.
/// </summary>
public class StringEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = value as string;
        return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a string to visibility - Collapsed when null/empty, Visible otherwise.
/// Inverse of StringEmptyToVisibilityConverter.
/// </summary>
public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = value as string;
        return !string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Safely gets the first character of a string, or "?" if empty/null.
/// </summary>
public class FirstCharConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = value as string;
        if (string.IsNullOrEmpty(str))
            return "?";
        return str[0].ToString().ToUpper();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean to visibility with optional inversion.
/// Pass "inverse" or "Inverse" as parameter to invert.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        var inverse = parameter?.ToString()?.Equals("inverse", StringComparison.OrdinalIgnoreCase) == true;

        if (inverse)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = (Visibility)value;
        var inverse = parameter?.ToString()?.Equals("inverse", StringComparison.OrdinalIgnoreCase) == true;
        var result = visibility == Visibility.Visible;

        return inverse ? !result : result;
    }
}

/// <summary>
/// Converts null to visibility - Visible when not null, Collapsed when null.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isNotNull = value != null;
        var inverse = parameter?.ToString()?.Equals("inverse", StringComparison.OrdinalIgnoreCase) == true;

        if (inverse)
            isNotNull = !isNotNull;

        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a number to visibility - Visible when greater than 0, Collapsed otherwise.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var count = System.Convert.ToInt32(value);
        var inverse = parameter?.ToString()?.Equals("inverse", StringComparison.OrdinalIgnoreCase) == true;
        var hasItems = count > 0;

        if (inverse)
            hasItems = !hasItems;

        return hasItems ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts enum values to display strings.
/// </summary>
public class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        var enumValue = value.ToString() ?? string.Empty;

        // Add spaces before capital letters (camelCase to Title Case)
        return System.Text.RegularExpressions.Regex.Replace(enumValue, "(\\B[A-Z])", " $1");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts decimal currency values to formatted strings.
/// </summary>
public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d.ToString("C2", culture);
        if (value is double dbl)
            return dbl.ToString("C2", culture);
        return "$0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && decimal.TryParse(s, NumberStyles.Currency, culture, out var result))
            return result;
        return 0m;
    }
}

/// <summary>
/// Converts DateTime to relative time string (e.g., "2 hours ago", "Yesterday").
/// </summary>
public class RelativeTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime dt)
            return string.Empty;

        var now = DateTime.Now;
        var diff = now - dt;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 2)
            return "Yesterday";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} days ago";
        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)} weeks ago";
        if (diff.TotalDays < 365)
            return $"{(int)(diff.TotalDays / 30)} months ago";

        return dt.ToString("MMM d, yyyy", culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts CustomerStatus to a background brush color.
/// </summary>
public class CustomerStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CustomerStatus status)
        {
            return status switch
            {
                CustomerStatus.Active => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
                CustomerStatus.Inactive => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Grey
                CustomerStatus.Lead => new SolidColorBrush(Color.FromRgb(33, 150, 243)),     // Blue
                CustomerStatus.VIP => new SolidColorBrush(Color.FromRgb(255, 215, 0)),       // Gold
                CustomerStatus.DoNotService => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                CustomerStatus.Delinquent => new SolidColorBrush(Color.FromRgb(255, 152, 0)),   // Orange
                CustomerStatus.Archived => new SolidColorBrush(Color.FromRgb(121, 85, 72)),     // Brown
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
            };
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CustomerType to a background brush color.
/// </summary>
public class CustomerTypeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CustomerType type)
        {
            return type switch
            {
                CustomerType.Residential => new SolidColorBrush(Color.FromRgb(227, 242, 253)),    // Light Blue
                CustomerType.Commercial => new SolidColorBrush(Color.FromRgb(255, 243, 224)),     // Light Orange
                CustomerType.PropertyManager => new SolidColorBrush(Color.FromRgb(232, 245, 233)), // Light Green
                CustomerType.Government => new SolidColorBrush(Color.FromRgb(243, 229, 245)),     // Light Purple
                CustomerType.NonProfit => new SolidColorBrush(Color.FromRgb(224, 247, 250)),      // Light Cyan
                CustomerType.NewConstruction => new SolidColorBrush(Color.FromRgb(251, 233, 231)), // Light Deep Orange
                _ => new SolidColorBrush(Color.FromRgb(245, 245, 245))
            };
        }
        return new SolidColorBrush(Color.FromRgb(245, 245, 245));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts RefrigerantType to a background brush color.
/// </summary>
public class RefrigerantColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is RefrigerantType type)
        {
            return type switch
            {
                RefrigerantType.R22 => new SolidColorBrush(Color.FromRgb(255, 205, 210)),    // Light Red - legacy
                RefrigerantType.Unknown => new SolidColorBrush(Color.FromRgb(224, 224, 224)), // Grey
                _ => new SolidColorBrush(Color.FromRgb(232, 245, 233))                        // Light Green - modern
            };
        }
        return new SolidColorBrush(Color.FromRgb(224, 224, 224));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts EquipmentType to a background brush color.
/// </summary>
public class EquipmentTypeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EquipmentType type)
        {
            return type switch
            {
                EquipmentType.GasFurnace or EquipmentType.OilFurnace => new SolidColorBrush(Color.FromRgb(255, 87, 34)),
                EquipmentType.ElectricFurnace => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                EquipmentType.Boiler => new SolidColorBrush(Color.FromRgb(233, 30, 99)),
                EquipmentType.AirConditioner => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                EquipmentType.HeatPump => new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                EquipmentType.MiniSplit or EquipmentType.DuctlessMiniSplit => new SolidColorBrush(Color.FromRgb(0, 188, 212)),
                EquipmentType.PackagedUnit or EquipmentType.RooftopUnit => new SolidColorBrush(Color.FromRgb(96, 125, 139)),
                EquipmentType.Coil or EquipmentType.Condenser or EquipmentType.AirHandler => new SolidColorBrush(Color.FromRgb(63, 81, 181)),
                EquipmentType.Humidifier or EquipmentType.Dehumidifier => new SolidColorBrush(Color.FromRgb(0, 150, 136)),
                EquipmentType.AirPurifier or EquipmentType.UVLight => new SolidColorBrush(Color.FromRgb(139, 195, 74)),
                EquipmentType.Thermostat => new SolidColorBrush(Color.FromRgb(121, 85, 72)),
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
            };
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts warranty expiry status to a background brush color.
/// </summary>
public class WarrantyStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int daysUntilExpiry)
        {
            if (daysUntilExpiry < 0)
                return new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Red - expired
            if (daysUntilExpiry <= 90)
                return new SolidColorBrush(Color.FromRgb(255, 152, 0));  // Orange - expiring soon
            return new SolidColorBrush(Color.FromRgb(76, 175, 80));      // Green - valid
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));        // Grey - unknown
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts JobStatus to a color brush for visual indicators.
/// </summary>
public class JobStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return status switch
            {
                JobStatus.Draft => new SolidColorBrush(Color.FromRgb(158, 158, 158)),      // Grey
                JobStatus.Scheduled => new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Blue
                JobStatus.InProgress => new SolidColorBrush(Color.FromRgb(255, 152, 0)),   // Orange
                JobStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
                JobStatus.Cancelled => new SolidColorBrush(Color.FromRgb(244, 67, 54)),    // Red
                JobStatus.OnHold => new SolidColorBrush(Color.FromRgb(255, 193, 7)),       // Amber
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))                     // Grey
            };
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}


