using System.Windows;
using System.Windows.Input;

namespace OneManVan.Services;

/// <summary>
/// Service for managing keyboard shortcuts throughout the application.
/// </summary>
public class KeyboardShortcutService
{
    private readonly Window _mainWindow;

    public event EventHandler? NewCustomerRequested;
    public event EventHandler? NewAssetRequested;
    public event EventHandler? NewEstimateRequested;
    public event EventHandler? NewJobRequested;
    public event EventHandler? BackupRequested;
    public event EventHandler? SearchRequested;
    public event EventHandler? RefreshRequested;
    public event EventHandler? SettingsRequested;

    public KeyboardShortcutService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// Registers all keyboard shortcuts with the main window.
    /// </summary>
    public void RegisterShortcuts()
    {
        // Ctrl+N - New Customer
        _mainWindow.InputBindings.Add(new KeyBinding(
            new RelayCommand(() => NewCustomerRequested?.Invoke(this, EventArgs.Empty)),
            Key.N, ModifierKeys.Control));

        // Ctrl+Shift+N - New Asset
        _mainWindow.InputBindings.Add(new KeyBinding(
            new RelayCommand(() => NewAssetRequested?.Invoke(this, EventArgs.Empty)),
            Key.N, ModifierKeys.Control | ModifierKeys.Shift));

        // Ctrl+E - New Estimate
        _mainWindow.InputBindings.Add(new KeyBinding(
            new RelayCommand(() => NewEstimateRequested?.Invoke(this, EventArgs.Empty)),
            Key.E, ModifierKeys.Control));

        // Ctrl+J - New Job
        _mainWindow.InputBindings.Add(new KeyBinding(
            new RelayCommand(() => NewJobRequested?.Invoke(this, EventArgs.Empty)),
            Key.J, ModifierKeys.Control));

        // Ctrl+B - Backup
        _mainWindow.InputBindings.Add(new KeyBinding(
            new RelayCommand(() => BackupRequested?.Invoke(this, EventArgs.Empty)),
            Key.B, ModifierKeys.Control));

        // Ctrl+F - Search (global)
        _mainWindow.InputBindings.Add(new KeyBinding(
            new RelayCommand(() => SearchRequested?.Invoke(this, EventArgs.Empty)),
            Key.F, ModifierKeys.Control));

        // F5 - Refresh
        _mainWindow.InputBindings.Add(new KeyBinding(
            new RelayCommand(() => RefreshRequested?.Invoke(this, EventArgs.Empty)),
            Key.F5, ModifierKeys.None));

        // Ctrl+, - Settings
        _mainWindow.InputBindings.Add(new KeyBinding(
            new RelayCommand(() => SettingsRequested?.Invoke(this, EventArgs.Empty)),
            Key.OemComma, ModifierKeys.Control));
    }

    /// <summary>
    /// Gets a formatted string of all available shortcuts for display.
    /// </summary>
    public static IEnumerable<ShortcutInfo> GetShortcutList()
    {
        return
        [
            new("New Customer", "Ctrl+N"),
            new("New Asset", "Ctrl+Shift+N"),
            new("New Estimate", "Ctrl+E"),
            new("New Job", "Ctrl+J"),
            new("Export Backup", "Ctrl+B"),
            new("Search", "Ctrl+F"),
            new("Refresh", "F5"),
            new("Settings", "Ctrl+,")
        ];
    }
}

/// <summary>
/// Information about a keyboard shortcut.
/// </summary>
public record ShortcutInfo(string Action, string Keys);

/// <summary>
/// Simple relay command for keyboard bindings.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}
