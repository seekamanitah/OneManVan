using System.Text.RegularExpressions;

namespace OneManVan.Services;

/// <summary>
/// Service for validating form data throughout the application.
/// </summary>
public static class ValidationService
{
    /// <summary>
    /// Validates an email address format.
    /// </summary>
    public static ValidationResult ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Valid(); // Email is optional

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return emailRegex.IsMatch(email)
            ? ValidationResult.Valid()
            : ValidationResult.Invalid("Invalid email format");
    }

    /// <summary>
    /// Validates a phone number format.
    /// </summary>
    public static ValidationResult ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return ValidationResult.Valid(); // Phone is optional

        // Remove common formatting characters
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        return digits.Length >= 10
            ? ValidationResult.Valid()
            : ValidationResult.Invalid("Phone number must have at least 10 digits");
    }

    /// <summary>
    /// Validates a required field is not empty.
    /// </summary>
    public static ValidationResult ValidateRequired(string? value, string fieldName)
    {
        return !string.IsNullOrWhiteSpace(value)
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} is required");
    }

    /// <summary>
    /// Validates a string has minimum length.
    /// </summary>
    public static ValidationResult ValidateMinLength(string? value, int minLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Invalid($"{fieldName} is required");

        return value.Length >= minLength
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} must be at least {minLength} characters");
    }

    /// <summary>
    /// Validates a string doesn't exceed max length.
    /// </summary>
    public static ValidationResult ValidateMaxLength(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Valid();

        return value.Length <= maxLength
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} cannot exceed {maxLength} characters");
    }

    /// <summary>
    /// Validates a number is within range.
    /// </summary>
    public static ValidationResult ValidateRange(decimal value, decimal min, decimal max, string fieldName)
    {
        return value >= min && value <= max
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} must be between {min:N0} and {max:N0}");
    }

    /// <summary>
    /// Validates a number is positive.
    /// </summary>
    public static ValidationResult ValidatePositive(decimal value, string fieldName)
    {
        return value > 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} must be greater than 0");
    }

    /// <summary>
    /// Validates a number is non-negative.
    /// </summary>
    public static ValidationResult ValidateNonNegative(decimal value, string fieldName)
    {
        return value >= 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} cannot be negative");
    }

    /// <summary>
    /// Validates a BTU rating for HVAC.
    /// </summary>
    public static ValidationResult ValidateBtuRating(int? btu)
    {
        if (!btu.HasValue)
            return ValidationResult.Valid(); // BTU is optional

        return btu.Value >= 12000 && btu.Value <= 200000
            ? ValidationResult.Valid()
            : ValidationResult.Invalid("BTU rating must be between 12,000 and 200,000");
    }

    /// <summary>
    /// Validates a SEER rating for HVAC.
    /// </summary>
    public static ValidationResult ValidateSeerRating(decimal? seer)
    {
        if (!seer.HasValue)
            return ValidationResult.Valid(); // SEER is optional

        return seer.Value >= 8 && seer.Value <= 30
            ? ValidationResult.Valid()
            : ValidationResult.Invalid("SEER rating must be between 8 and 30");
    }

    /// <summary>
    /// Validates a serial number format.
    /// </summary>
    public static ValidationResult ValidateSerial(string? serial)
    {
        if (string.IsNullOrWhiteSpace(serial))
            return ValidationResult.Invalid("Serial number is required");

        if (serial.Length < 3)
            return ValidationResult.Invalid("Serial number is too short");

        if (serial.Length > 50)
            return ValidationResult.Invalid("Serial number is too long");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Validates a date is in the past.
    /// </summary>
    public static ValidationResult ValidatePastDate(DateTime? date, string fieldName)
    {
        if (!date.HasValue)
            return ValidationResult.Valid();

        return date.Value <= DateTime.Now
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} cannot be in the future");
    }

    /// <summary>
    /// Validates a date is in the future.
    /// </summary>
    public static ValidationResult ValidateFutureDate(DateTime? date, string fieldName)
    {
        if (!date.HasValue)
            return ValidationResult.Valid();

        return date.Value >= DateTime.Today
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} cannot be in the past");
    }

    /// <summary>
    /// Validates a warranty term in years.
    /// </summary>
    public static ValidationResult ValidateWarrantyTerm(int years)
    {
        return years >= 0 && years <= 25
            ? ValidationResult.Valid()
            : ValidationResult.Invalid("Warranty term must be between 0 and 25 years");
    }

    /// <summary>
    /// Validates a currency amount.
    /// </summary>
    public static ValidationResult ValidateCurrency(decimal amount, string fieldName, decimal maxAmount = 1000000)
    {
        if (amount < 0)
            return ValidationResult.Invalid($"{fieldName} cannot be negative");

        if (amount > maxAmount)
            return ValidationResult.Invalid($"{fieldName} exceeds maximum of {maxAmount:C0}");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Validates a percentage (0-100).
    /// </summary>
    public static ValidationResult ValidatePercentage(decimal percent, string fieldName)
    {
        return percent >= 0 && percent <= 100
            ? ValidationResult.Valid()
            : ValidationResult.Invalid($"{fieldName} must be between 0% and 100%");
    }

    /// <summary>
    /// Combines multiple validation results.
    /// </summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var errors = results.Where(r => !r.IsValid).Select(r => r.ErrorMessage).ToList();
        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(string.Join("\n", errors!));
    }
}

/// <summary>
/// Result of a validation check.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Valid() => new(true);
    public static ValidationResult Invalid(string message) => new(false, message);

    public static implicit operator bool(ValidationResult result) => result.IsValid;
}

/// <summary>
/// Extension methods for validation.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates an object and shows errors via toast if invalid.
    /// </summary>
    public static bool ValidateAndNotify(this ValidationResult result)
    {
        if (!result.IsValid && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            ToastService.Warning(result.ErrorMessage);
        }
        return result.IsValid;
    }
}
