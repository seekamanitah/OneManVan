using System;
using System.Threading.Tasks;
using System.Windows;
using OneManVan.Controls;

namespace OneManVan.Services;

/// <summary>
/// Service to manage the global drawer panel.
/// Provides a centralized way to open/close drawers from any page.
/// </summary>
public class DrawerService
{
    private static DrawerService? _instance;
    private DrawerPanel? _globalDrawer;

    public static DrawerService Instance => _instance ??= new DrawerService();

    private DrawerService() { }

    /// <summary>
    /// Initializes the drawer service with the global drawer from MainShell
    /// </summary>
    public void Initialize(DrawerPanel drawerPanel)
    {
        _globalDrawer = drawerPanel;
    }

    /// <summary>
    /// Opens the drawer with the specified content
    /// </summary>
    /// <param name="title">Drawer header title</param>
    /// <param name="content">The UIElement to display in the drawer</param>
    /// <param name="saveButtonText">Text for the save button (default: "Save")</param>
    /// <param name="onSave">Action to execute when save is clicked</param>
    /// <param name="onCancel">Optional action to execute when cancel is clicked</param>
    public async Task OpenDrawerAsync(
        string title,
        UIElement content,
        string saveButtonText = "Save",
        Action? onSave = null,
        Action? onCancel = null)
    {
        if (_globalDrawer == null)
        {
            MessageBox.Show("Drawer service not initialized!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Set up the drawer
        _globalDrawer.Title = title;
        _globalDrawer.SaveButtonText = saveButtonText;
        _globalDrawer.Content = content;

        // Remove existing event handlers
        _globalDrawer.SaveClicked -= OnDrawerSaveClicked;
        _globalDrawer.CancelClicked -= OnDrawerCancelClicked;

        // Store actions for event handlers
        _currentSaveAction = onSave;
        _currentCancelAction = onCancel;

        // Attach new event handlers
        _globalDrawer.SaveClicked += OnDrawerSaveClicked;
        _globalDrawer.CancelClicked += OnDrawerCancelClicked;

        // Open the drawer
        await _globalDrawer.OpenAsync();
    }

    /// <summary>
    /// Closes the drawer after successful save
    /// </summary>
    public async Task CompleteDrawerAsync()
    {
        if (_globalDrawer != null)
        {
            await _globalDrawer.CompleteSaveAsync();
        }
    }

    /// <summary>
    /// Closes the drawer without saving
    /// </summary>
    public async Task CloseDrawerAsync()
    {
        if (_globalDrawer != null)
        {
            await _globalDrawer.CloseAsync(false);
        }
    }

    /// <summary>
    /// Returns the current global drawer instance (for advanced scenarios)
    /// </summary>
    public DrawerPanel? GetDrawer() => _globalDrawer;

    private Action? _currentSaveAction;
    private Action? _currentCancelAction;

    private void OnDrawerSaveClicked(object? sender, EventArgs e)
    {
        _currentSaveAction?.Invoke();
    }

    private void OnDrawerCancelClicked(object? sender, EventArgs e)
    {
        _currentCancelAction?.Invoke();
    }
}
