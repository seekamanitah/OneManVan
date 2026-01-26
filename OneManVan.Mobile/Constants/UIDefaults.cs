namespace OneManVan.Mobile.Constants;

/// <summary>
/// UI-related constants for consistent user experience.
/// </summary>
public static class UIDefaults
{
    // Delays and timeouts
    public const int SearchDebounceMilliseconds = 300;
    public const int LoadingTimeoutSeconds = 30;
    public const int ToastDurationMilliseconds = 3000;
    
    // Pagination
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    
    // Validation
    public const int MaxPhoneNumberLength = 20;
    public const int MaxEmailLength = 100;
    public const int MaxNotesLength = 2000;
    public const int MaxDescriptionLength = 500;
    
    // Image handling
    public const int MaxPhotoSizeKB = 5120; // 5MB
    public const int ThumbnailWidth = 200;
    public const int ThumbnailHeight = 200;
    public const int PhotoCompressionQuality = 85;
    
    // Currency formatting
    public const string CurrencyFormat = "C2";
    public const string PercentageFormat = "P0";
}
