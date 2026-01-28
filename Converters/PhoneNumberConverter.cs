using System.Globalization;
using System.Windows.Data;

namespace OneManVan.Converters;

/// <summary>
/// Converts phone numbers to xxx-xxx-xxxx format for display.
/// Removes formatting when converting back for storage.
/// </summary>
public class PhoneNumberConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        var phoneNumber = value.ToString();
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        return FormatPhoneNumber(phoneNumber);
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        var phoneNumber = value.ToString();
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        return UnformatPhoneNumber(phoneNumber);
    }

    /// <summary>
    /// Formats phone number to xxx-xxx-xxxx pattern.
    /// </summary>
    public static string? FormatPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Remove all non-digit characters
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Handle different lengths
        return digits.Length switch
        {
            10 => $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}",
            11 when digits[0] == '1' => $"{digits.Substring(1, 3)}-{digits.Substring(4, 3)}-{digits.Substring(7, 4)}", // US with country code
            7 => $"{digits.Substring(0, 3)}-{digits.Substring(3, 4)}", // Local number
            _ => phoneNumber // Return original if not standard length
        };
    }

    /// <summary>
    /// Removes formatting from phone number for storage.
    /// </summary>
    public static string? UnformatPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Keep only digits
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Validates if phone number has correct format or can be formatted.
    /// </summary>
    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return digits.Length is 7 or 10 or 11;
    }
}
