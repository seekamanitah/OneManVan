using Microsoft.Maui.Controls;
using OneManVan.Shared.Utilities;

namespace OneManVan.Mobile.Behaviors;

/// <summary>
/// Behavior that formats phone numbers as xxx-xxx-xxxx while user types.
/// Attach to Entry controls for phone number input.
/// </summary>
public class PhoneNumberFormattingBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.TextChanged += OnTextChanged;
        entry.Keyboard = Keyboard.Telephone;
        entry.MaxLength = 12; // xxx-xxx-xxxx
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        base.OnDetachingFrom(entry);
        entry.TextChanged -= OnTextChanged;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        // Prevent recursion
        if (e.NewTextValue == e.OldTextValue)
            return;

        var cursorPosition = entry.CursorPosition;
        var originalLength = e.OldTextValue?.Length ?? 0;
        
        // Format the text
        var formatted = PhoneNumberFormatter.FormatAsTyping(e.NewTextValue);
        
        // Only update if changed
        if (formatted != e.NewTextValue)
        {
            entry.TextChanged -= OnTextChanged; // Prevent recursion
            entry.Text = formatted;
            entry.TextChanged += OnTextChanged;
            
            // Adjust cursor position to account for added dashes
            var lengthDiff = formatted.Length - originalLength;
            entry.CursorPosition = Math.Max(0, cursorPosition + lengthDiff);
        }
    }
}
