namespace OneManVan.Shared.Utilities;

/// <summary>
/// Utility class for phone number formatting and validation.
/// Provides consistent phone number handling across Desktop and Mobile apps.
/// </summary>
public static class PhoneNumberFormatter
{
    /// <summary>
    /// Formats phone number to xxx-xxx-xxxx pattern.
    /// </summary>
    /// <param name="phoneNumber">Raw phone number</param>
    /// <returns>Formatted phone number or original if can't format</returns>
    public static string? Format(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Remove all non-digit characters
        var digits = ExtractDigits(phoneNumber);

        // Handle different lengths
        return digits.Length switch
        {
            10 => $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}",
            11 when digits[0] == '1' => $"{digits.Substring(1, 3)}-{digits.Substring(4, 3)}-{digits.Substring(7, 4)}", // US with country code
            7 => $"{digits.Substring(0, 3)}-{digits.Substring(4, 4)}", // Local number
            _ => phoneNumber // Return original if not standard length
        };
    }

    /// <summary>
    /// Removes all formatting from phone number, leaving only digits.
    /// Use this before saving to database.
    /// </summary>
    /// <param name="phoneNumber">Formatted phone number</param>
    /// <returns>Digits only</returns>
    public static string? Unformat(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        return ExtractDigits(phoneNumber);
    }

    /// <summary>
    /// Validates if phone number has correct format or can be formatted.
    /// </summary>
    /// <param name="phoneNumber">Phone number to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var digits = ExtractDigits(phoneNumber);
        return digits.Length is 7 or 10 or 11;
    }

    /// <summary>
    /// Formats phone number as user types (for real-time input formatting).
    /// </summary>
    /// <param name="input">Current input text</param>
    /// <returns>Formatted text</returns>
    public static string FormatAsTyping(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var digits = ExtractDigits(input);
        var length = digits.Length;

        if (length == 0)
            return string.Empty;

        // Format progressively as user types
        if (length <= 3)
            return digits;
        if (length <= 6)
            return $"{digits.Substring(0, 3)}-{digits.Substring(3)}";
        if (length <= 10)
            return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6)}";
        
        // Max 10 digits
        return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
    }

    /// <summary>
    /// Extracts only digits from phone number string.
    /// </summary>
    private static string ExtractDigits(string phoneNumber)
    {
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Gets display format for phone number (with country code if available).
    /// </summary>
    /// <param name="phoneNumber">Raw phone number</param>
    /// <param name="includeCountryCode">Whether to include +1 prefix</param>
    /// <returns>Display formatted phone number</returns>
    public static string? GetDisplayFormat(string? phoneNumber, bool includeCountryCode = false)
    {
        var formatted = Format(phoneNumber);
        
        if (formatted == null)
            return null;

        return includeCountryCode ? $"+1 {formatted}" : formatted;
    }

    /// <summary>
    /// Converts phone number to clickable tel: link format.
    /// </summary>
    /// <param name="phoneNumber">Phone number</param>
    /// <returns>tel: link or null</returns>
    public static string? ToTelLink(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var digits = ExtractDigits(phoneNumber);
        return digits.Length > 0 ? $"tel:{digits}" : null;
    }

    /// <summary>
    /// Converts phone number to SMS link format.
    /// </summary>
    /// <param name="phoneNumber">Phone number</param>
    /// <returns>sms: link or null</returns>
    public static string? ToSmsLink(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var digits = ExtractDigits(phoneNumber);
        return digits.Length > 0 ? $"sms:{digits}" : null;
    }
}
