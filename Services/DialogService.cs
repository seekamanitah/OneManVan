using System.Windows;

namespace OneManVan.Services;

/// <summary>
/// Service for displaying consistent dialog boxes throughout the application.
/// </summary>
public static class DialogService
{
    /// <summary>
    /// Shows a confirmation dialog for destructive actions.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Main message</param>
    /// <param name="detail">Optional detail text</param>
    /// <returns>True if user confirmed, false otherwise</returns>
    public static bool ConfirmDelete(string title, string message, string? detail = null)
    {
        var fullMessage = detail != null ? $"{message}\n\n{detail}" : message;

        var result = MessageBox.Show(
            fullMessage,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Shows a confirmation dialog with a warning.
    /// </summary>
    public static bool ConfirmAction(string title, string message)
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Shows an information dialog.
    /// </summary>
    public static void ShowInfo(string title, string message)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Shows a success dialog.
    /// </summary>
    public static void ShowSuccess(string title, string message)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    public static void ShowError(string title, string message)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    /// <summary>
    /// Shows a warning dialog.
    /// </summary>
    public static void ShowWarning(string title, string message)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    /// <summary>
    /// Shows a dialog with a choice between save, don't save, and cancel.
    /// </summary>
    /// <returns>True for save, false for don't save, null for cancel</returns>
    public static bool? ShowSaveChangesDialog(string itemName)
    {
        var result = MessageBox.Show(
            $"Do you want to save changes to {itemName}?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => true,
            MessageBoxResult.No => false,
            _ => null
        };
    }

    /// <summary>
    /// Shows a dangerous action confirmation requiring double confirmation.
    /// </summary>
    public static bool ConfirmDangerousAction(string title, string message, string consequences)
    {
        var firstResult = MessageBox.Show(
            $"{message}\n\n{consequences}",
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (firstResult != MessageBoxResult.Yes)
            return false;

        var secondResult = MessageBox.Show(
            "Are you absolutely sure? This cannot be undone.",
            $"Confirm {title}",
            MessageBoxButton.YesNo,
            MessageBoxImage.Stop,
            MessageBoxResult.No);

        return secondResult == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Shows an input dialog for simple text entry.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="prompt">Prompt text</param>
    /// <param name="defaultValue">Default value</param>
    /// <returns>The entered text, or null if cancelled</returns>
    public static string? ShowInputDialog(string title, string prompt, string defaultValue = "")
    {
        // For now, use a simple message box approach
        // In a full implementation, this would be a custom dialog window
        var result = Microsoft.VisualBasic.Interaction.InputBox(prompt, title, defaultValue);
        return string.IsNullOrEmpty(result) ? null : result;
    }
}
