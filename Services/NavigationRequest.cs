namespace OneManVan.Services;

/// <summary>
/// Static class for cross-page navigation requests.
/// Allows one page to request navigation to another with specific actions.
/// </summary>
public static class NavigationRequest
{
    /// <summary>
    /// Event raised when a navigation with action is requested.
    /// </summary>
    public static event EventHandler<NavigationRequestArgs>? Requested;

    /// <summary>
    /// Request navigation to a page with an optional action.
    /// </summary>
    /// <param name="pageName">The name of the page to navigate to.</param>
    /// <param name="action">Optional action to perform after navigation (e.g., "Add", "Edit").</param>
    /// <param name="parameter">Optional parameter for the action (e.g., entity ID).</param>
    public static void Navigate(string pageName, string? action = null, object? parameter = null)
    {
        Requested?.Invoke(null, new NavigationRequestArgs(pageName, action, parameter));
    }

    /// <summary>
    /// Pending action for pages to check after loading.
    /// </summary>
    public static PendingAction? PendingAction { get; private set; }

    /// <summary>
    /// Sets a pending action for the next page load.
    /// </summary>
    public static void SetPendingAction(string action, object? parameter = null)
    {
        PendingAction = new PendingAction(action, parameter);
    }

    /// <summary>
    /// Clears and returns the pending action.
    /// </summary>
    public static PendingAction? ConsumePendingAction()
    {
        var action = PendingAction;
        PendingAction = null;
        return action;
    }
}

/// <summary>
/// Arguments for navigation request events.
/// </summary>
public class NavigationRequestArgs : EventArgs
{
    public string PageName { get; }
    public string? Action { get; }
    public object? Parameter { get; }

    public NavigationRequestArgs(string pageName, string? action = null, object? parameter = null)
    {
        PageName = pageName;
        Action = action;
        Parameter = parameter;
    }
}

/// <summary>
/// Represents a pending action for a page to execute after loading.
/// </summary>
public class PendingAction
{
    public string Action { get; }
    public object? Parameter { get; }

    public PendingAction(string action, object? parameter = null)
    {
        Action = action;
        Parameter = parameter;
    }
}
