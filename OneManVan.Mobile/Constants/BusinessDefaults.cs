namespace OneManVan.Mobile.Constants;

/// <summary>
/// Business default values used throughout the application.
/// Centralized configuration for common business rules.
/// </summary>
public static class BusinessDefaults
{
    // Estimate defaults
    public const int DefaultEstimateValidityDays = 30;
    public const string DefaultEstimateTerms = "Payment due upon completion. All work guaranteed for 1 year parts and labor.";
    
    // Invoice defaults
    public const int DefaultInvoicePaymentTermDays = 30;
    public const decimal DefaultTaxRate = 8.0m;
    public const string DefaultInvoicePaymentTerms = "Payment due within 30 days.";
    
    // Warranty defaults
    public const int DefaultWarrantyYears = 1;
    public const int DefaultPartsWarrantyYears = 5;
    public const int DefaultCompressorWarrantyYears = 10;
    
    // Service defaults
    public const int DefaultServiceIntervalMonths = 6;
    public const int DefaultFilterChangeMonths = 3;
    
    // Pricing defaults
    public const decimal DefaultHourlyLaborRate = 125.0m;
    public const decimal DefaultEmergencyServiceFee = 75.0m;
    public const decimal DefaultTripCharge = 50.0m;
    
    // Scheduling defaults
    public const int DefaultAppointmentDurationMinutes = 60;
    public const int DefaultMaintenanceReminderDays = 30;
}
