using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

public partial class QuickNotesPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private List<QuickNote> _allNotes = new();
    private bool _showPinnedOnly = false;
    private string _searchText = "";

    public bool IsRefreshing { get; set; }
    public List<QuickNote> Notes { get; set; } = new();

    public QuickNotesPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNotesAsync();
    }

    private async Task LoadNotesAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            _allNotes = await db.QuickNotes
                .Where(n => !n.IsArchived)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdatedAt)
                .AsNoTracking()
                .ToListAsync();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load notes: {ex.Message}", "OK");
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allNotes.AsEnumerable();

        if (_showPinnedOnly)
        {
            filtered = filtered.Where(n => n.IsPinned);
        }

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLower();
            filtered = filtered.Where(n =>
                (n.Title?.ToLower().Contains(search) ?? false) ||
                n.Content.ToLower().Contains(search) ||
                (n.Category?.ToLower().Contains(search) ?? false));
        }

        Notes = filtered.ToList();
        OnPropertyChanged(nameof(Notes));
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? "";
        ApplyFilters();
    }

    private void OnTogglePinnedFilter(object sender, EventArgs e)
    {
        _showPinnedOnly = !_showPinnedOnly;
        ApplyFilters();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadNotesAsync();
        IsRefreshing = false;
        OnPropertyChanged(nameof(IsRefreshing));
    }

    private async void OnNoteSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is QuickNote note)
        {
            // Clear selection
            ((CollectionView)sender).SelectedItem = null;
            
            // Show edit dialog
            await ShowNoteEditorAsync(note);
        }
    }

    private async void OnAddNoteClicked(object sender, EventArgs e)
    {
        await ShowNoteEditorAsync(null);
    }

    private async Task ShowNoteEditorAsync(QuickNote? existingNote)
    {
        var isNew = existingNote == null;
        var title = isNew ? "" : existingNote!.Title ?? "";
        var content = isNew ? "" : existingNote!.Content;
        var category = isNew ? "" : existingNote!.Category ?? "";

        // Simple prompt for quick entry - title
        var newTitle = await DisplayPromptAsync(
            isNew ? "New Note" : "Edit Note",
            "Title (optional):",
            initialValue: title,
            keyboard: Keyboard.Text);

        if (newTitle == null) return; // Cancelled

        // Content prompt
        var newContent = await DisplayPromptAsync(
            isNew ? "New Note" : "Edit Note",
            "Note content:",
            initialValue: content,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newContent)) 
        {
            if (!isNew) return; // Keep existing if editing and cancelled
            await DisplayAlert("Required", "Note content is required.", "OK");
            return;
        }

        // Category prompt (optional)
        var newCategory = await DisplayPromptAsync(
            "Category",
            "Category (optional - e.g., Customer, Material, Reminder):",
            initialValue: category,
            keyboard: Keyboard.Text);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (isNew)
            {
                var note = new QuickNote
                {
                    Title = string.IsNullOrWhiteSpace(newTitle) ? null : newTitle,
                    Content = newContent,
                    Category = string.IsNullOrWhiteSpace(newCategory) ? null : newCategory,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.QuickNotes.Add(note);
            }
            else
            {
                var noteToUpdate = await db.QuickNotes.FindAsync(existingNote!.Id);
                if (noteToUpdate != null)
                {
                    noteToUpdate.Title = string.IsNullOrWhiteSpace(newTitle) ? null : newTitle;
                    noteToUpdate.Content = newContent;
                    noteToUpdate.Category = string.IsNullOrWhiteSpace(newCategory) ? null : newCategory;
                    noteToUpdate.UpdatedAt = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync();
            await LoadNotesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save note: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteNote(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is QuickNote note)
        {
            var confirm = await DisplayAlert("Delete Note", 
                $"Delete \"{note.Title ?? "this note"}\"?", "Delete", "Cancel");
            
            if (!confirm) return;

            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var noteToDelete = await db.QuickNotes.FindAsync(note.Id);
                if (noteToDelete != null)
                {
                    db.QuickNotes.Remove(noteToDelete);
                    await db.SaveChangesAsync();
                }
                await LoadNotesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete: {ex.Message}", "OK");
            }
        }
    }

    private async void OnArchiveNote(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is QuickNote note)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var noteToArchive = await db.QuickNotes.FindAsync(note.Id);
                if (noteToArchive != null)
                {
                    noteToArchive.IsArchived = true;
                    noteToArchive.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                await LoadNotesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to archive: {ex.Message}", "OK");
            }
        }
    }

    private async void OnTogglePin(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is QuickNote note)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var noteToPin = await db.QuickNotes.FindAsync(note.Id);
                if (noteToPin != null)
                {
                    noteToPin.IsPinned = !noteToPin.IsPinned;
                    noteToPin.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                await LoadNotesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to toggle pin: {ex.Message}", "OK");
            }
        }
    }
}
