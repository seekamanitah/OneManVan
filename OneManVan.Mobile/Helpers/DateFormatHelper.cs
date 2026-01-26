namespace OneManVan.Mobile.Helpers;

/// <summary>
/// Extension methods for consistent date formatting across the application.
/// Provides standardized formats for display in UI.
/// </summary>
public static class DateFormatHelper
{
    /// <summary>
    /// Format date as short format: "Jan 15, 2026"
    /// </summary>
    public static string ToShortDate(this DateTime? date, string defaultText = "Not set") 
        => date?.ToString("MMM d, yyyy") ?? defaultText;
    
    /// <summary>
    /// Format date as short format: "Jan 15, 2026"
    /// </summary>
    public static string ToShortDate(this DateTime date) 
        => date.ToString("MMM d, yyyy");
    
    /// <summary>
    /// Format date as long format: "January 15, 2026"
    /// </summary>
    public static string ToLongDate(this DateTime date) 
        => date.ToString("MMMM d, yyyy");
    
    /// <summary>
    /// Format date as long format: "January 15, 2026"
    /// </summary>
    public static string ToLongDate(this DateTime? date, string defaultText = "Not set") 
        => date?.ToString("MMMM d, yyyy") ?? defaultText;
    
    /// <summary>
    /// Format date with time: "Jan 15, 2026 2:30 PM"
    /// </summary>
    public static string ToShortDateTime(this DateTime? date, string defaultText = "Not set") 
        => date?.ToString("MMM d, yyyy h:mm tt") ?? defaultText;
    
    /// <summary>
    /// Format date with time: "Jan 15, 2026 2:30 PM"
    /// </summary>
    public static string ToShortDateTime(this DateTime date) 
        => date.ToString("MMM d, yyyy h:mm tt");
    
    /// <summary>
    /// Format time only: "2:30 PM"
    /// </summary>
    public static string ToShortTime(this DateTime? date, string defaultText = "Not set") 
        => date?.ToString("h:mm tt") ?? defaultText;
    
    /// <summary>
    /// Format time only: "2:30 PM"
    /// </summary>
    public static string ToShortTime(this DateTime date) 
        => date.ToString("h:mm tt");
    
    /// <summary>
    /// Format day of week: "Monday", "Tuesday", etc.
    /// </summary>
    public static string ToDayOfWeek(this DateTime date) 
        => date.ToString("dddd");
    
    /// <summary>
    /// Format day of week: "Monday", "Tuesday", etc.
    /// </summary>
    public static string ToDayOfWeek(this DateTime? date, string defaultText = "Not set") 
        => date?.ToString("dddd") ?? defaultText;
}
