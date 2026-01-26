namespace OneManVan.Mobile.Extensions;

/// <summary>
/// Extension methods for ContentPage to reduce boilerplate code
/// </summary>
public static class PageExtensions
{
    /// <summary>
    /// Execute a save operation with standard success feedback and error handling
    /// </summary>
    /// <param name="page">The page performing the save</param>
    /// <param name="saveAction">The async save action to execute</param>
    /// <param name="successMessage">Message to show on success</param>
    /// <param name="navigateBack">Whether to navigate back after success (default: true)</param>
    /// <returns>True if save succeeded, false otherwise</returns>
    public static async Task<bool> SaveWithFeedbackAsync(
        this ContentPage page,
        Func<Task> saveAction,
        string successMessage = "Saved successfully",
        bool navigateBack = true)
    {
        try
        {
            await saveAction();
            
            // Haptic feedback on success
            try 
            { 
                HapticFeedback.Default.Perform(HapticFeedbackType.Click); 
            } 
            catch 
            { 
                // Haptic may not be supported on all platforms
            }
            
            await page.DisplayAlert("Success", successMessage, "OK");
            
            if (navigateBack)
            {
                await Shell.Current.GoToAsync("..");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save error: {ex}");
            await page.DisplayAlert(
                "Save Failed", 
                "Unable to save. Please try again.", 
                "OK");
            return false;
        }
    }

    /// <summary>
    /// Execute a save operation with custom success and error handling
    /// </summary>
    public static async Task<bool> SaveWithCustomFeedbackAsync(
        this ContentPage page,
        Func<Task> saveAction,
        string successTitle,
        string successMessage,
        string errorTitle = "Error",
        string errorMessage = "Operation failed. Please try again.",
        bool navigateBack = true,
        bool useHaptic = true)
    {
        try
        {
            await saveAction();
            
            if (useHaptic)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
            
            await page.DisplayAlert(successTitle, successMessage, "OK");
            
            if (navigateBack)
            {
                await Shell.Current.GoToAsync("..");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Operation error: {ex}");
            await page.DisplayAlert(errorTitle, errorMessage, "OK");
            return false;
        }
    }

    /// <summary>
    /// Show a simple loading overlay during an async operation
    /// </summary>
    public static async Task<T> WithLoadingAsync<T>(
        this ContentPage page,
        Func<Task<T>> operation,
        ActivityIndicator loadingIndicator)
    {
        using (new Helpers.LoadingScope(loadingIndicator))
        {
            return await operation();
        }
    }

    /// <summary>
    /// Show a simple loading overlay during an async operation (void return)
    /// </summary>
    public static async Task WithLoadingAsync(
        this ContentPage page,
        Func<Task> operation,
        ActivityIndicator loadingIndicator)
    {
        using (new Helpers.LoadingScope(loadingIndicator))
        {
            await operation();
        }
    }
}
