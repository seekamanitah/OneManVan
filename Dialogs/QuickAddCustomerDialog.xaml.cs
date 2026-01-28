using System.Windows;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Shared.Utilities;

namespace OneManVan.Dialogs;

public partial class QuickAddCustomerDialog : Window
{
    private readonly OneManVanDbContext _context;
    
    public Customer? CreatedCustomer { get; private set; }

    public QuickAddCustomerDialog()
    {
        InitializeComponent();
        _context = App.DbContext;
        
        // Focus first name on load
        Loaded += (s, e) => FirstNameTextBox.Focus();
    }

    private void OnPhoneTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Format phone number as user types
        var textBox = PhoneTextBox;
        var cursorPosition = textBox.SelectionStart;
        var originalLength = textBox.Text?.Length ?? 0;
        
        var formatted = PhoneNumberFormatter.FormatAsTyping(textBox.Text);
        if (formatted != textBox.Text)
        {
            textBox.TextChanged -= OnPhoneTextChanged; // Prevent recursion
            textBox.Text = formatted;
            textBox.TextChanged += OnPhoneTextChanged;
            
            // Adjust cursor position
            var lengthDiff = formatted.Length - originalLength;
            textBox.SelectionStart = Math.Max(0, cursorPosition + lengthDiff);
        }
    }

    private void PhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        PhoneTextBox.Text = PhoneNumberFormatter.Format(PhoneTextBox.Text);
    }

    private async void OnCreateClick(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) && 
            string.IsNullOrWhiteSpace(LastNameTextBox.Text))
        {
            MessageBox.Show("Please enter at least a first or last name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            FirstNameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
        {
            MessageBox.Show("Please enter a phone number.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            PhoneTextBox.Focus();
            return;
        }

        if (!PhoneNumberFormatter.IsValid(PhoneTextBox.Text))
        {
            MessageBox.Show("Please enter a valid phone number (10 digits).", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            PhoneTextBox.Focus();
            return;
        }

        // Disable button during save
        CreateButton.IsEnabled = false;
        CreateButton.Content = "Creating...";

        try
        {
            // Construct full name
            var fullName = $"{FirstNameTextBox.Text?.Trim()} {LastNameTextBox.Text?.Trim()}".Trim();
            
            // Create customer
            var customer = new Customer
            {
                FirstName = FirstNameTextBox.Text?.Trim(),
                LastName = LastNameTextBox.Text?.Trim(),
                Name = fullName,
                Phone = PhoneNumberFormatter.Unformat(PhoneTextBox.Text),
                Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim(),
                HomeAddress = string.IsNullOrWhiteSpace(HomeAddressTextBox.Text) ? null : HomeAddressTextBox.Text.Trim(),
                CompanyName = string.IsNullOrWhiteSpace(CompanyNameTextBox.Text) ? null : CompanyNameTextBox.Text.Trim(),
                CustomerType = CustomerTypeCombo.SelectedIndex switch
                {
                    1 => CustomerType.Commercial,
                    2 => CustomerType.PropertyManager,
                    3 => CustomerType.Government,
                    _ => CustomerType.Residential
                },
                Status = CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            // Generate customer number
            var maxNumber = await _context.Customers
                .Where(c => c.CustomerNumber != null && c.CustomerNumber.StartsWith("C-"))
                .Select(c => c.CustomerNumber)
                .ToListAsync();

            var nextNumber = 1;
            if (maxNumber.Any())
            {
                nextNumber = maxNumber
                    .Select(n => int.TryParse(n?.Substring(2), out int num) ? num : 0)
                    .DefaultIfEmpty(0)
                    .Max() + 1;
            }
            customer.CustomerNumber = $"C-{nextNumber:D4}";

            // Save
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            CreatedCustomer = customer;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create customer: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Re-enable button
            CreateButton.IsEnabled = true;
            CreateButton.Content = "Create Customer";
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _context?.Dispose();
    }
}
