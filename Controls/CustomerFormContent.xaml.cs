using System.Windows;
using System.Windows.Controls;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Shared.Utilities;

namespace OneManVan.Controls;

public partial class CustomerFormContent : UserControl
{
    public CustomerFormContent()
    {
        InitializeComponent();
        CustomerTypeCombo.SelectedIndex = 0;
        
        // Enable/disable Navigate button based on address
        HomeAddressTextBox.TextChanged += (s, e) =>
        {
            NavigateButton.IsEnabled = !string.IsNullOrWhiteSpace(HomeAddressTextBox.Text);
        };
    }

    public Customer GetCustomer()
    {
        // Construct full name from FirstName and LastName
        var fullName = $"{FirstNameTextBox.Text?.Trim()} {LastNameTextBox.Text?.Trim()}".Trim();
        
        return new Customer
        {
            FirstName = FirstNameTextBox.Text?.Trim(),
            LastName = LastNameTextBox.Text?.Trim(),
            Name = fullName, // For backward compatibility
            CompanyName = CompanyNameTextBox.Text,
            Email = EmailTextBox.Text,
            Phone = PhoneNumberFormatter.Unformat(PhoneTextBox.Text),
            Mobile = PhoneNumberFormatter.Unformat(MobileTextBox.Text),
            Website = WebsiteTextBox.Text,
            HomeAddress = HomeAddressTextBox.Text?.Trim(),
            CustomerType = CustomerTypeCombo.SelectedIndex switch
            {
                1 => CustomerType.Commercial,
                2 => CustomerType.PropertyManager,
                3 => CustomerType.Government,
                _ => CustomerType.Residential
            },
            Notes = NotesTextBox.Text,
            Status = CustomerStatus.Active,
            CustomerNumber = null // Will be auto-generated
        };
    }

    public void LoadCustomer(Customer customer)
    {
        // Load split names or full name
        FirstNameTextBox.Text = customer.FirstName ?? string.Empty;
        LastNameTextBox.Text = customer.LastName ?? string.Empty;
        
        // If FirstName/LastName are empty, try to split the Name field
        if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) && !string.IsNullOrWhiteSpace(customer.Name))
        {
            var nameParts = customer.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length >= 1)
                FirstNameTextBox.Text = nameParts[0];
            if (nameParts.Length >= 2)
                LastNameTextBox.Text = nameParts[1];
        }
        
        CompanyNameTextBox.Text = customer.CompanyName;
        EmailTextBox.Text = customer.Email;
        PhoneTextBox.Text = PhoneNumberFormatter.Format(customer.Phone);
        MobileTextBox.Text = PhoneNumberFormatter.Format(customer.Mobile);
        WebsiteTextBox.Text = customer.Website;
        HomeAddressTextBox.Text = customer.HomeAddress;
        NotesTextBox.Text = customer.Notes;
        
        CustomerTypeCombo.SelectedIndex = customer.CustomerType switch
        {
            CustomerType.Commercial => 1,
            CustomerType.PropertyManager => 2,
            CustomerType.Government => 3,
            _ => 0
        };
    }

    public bool Validate()
    {
        // Require either FirstName or LastName
        if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) && 
            string.IsNullOrWhiteSpace(LastNameTextBox.Text))
        {
            return false;
        }
        return true;
    }

    private void OnNavigateToAddressClick(object sender, RoutedEventArgs e)
    {
        var address = HomeAddressTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            MessageBox.Show("Please enter an address first.", "No Address", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            // Encode address for URL
            var encodedAddress = Uri.EscapeDataString(address);
            var mapsUrl = $"https://www.google.com/maps/search/?api=1&query={encodedAddress}";
            
            // Open in default browser
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = mapsUrl,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open maps: {ex.Message}", "Navigation Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        PhoneTextBox.Text = PhoneNumberFormatter.Format(PhoneTextBox.Text);
    }

    private void MobileTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        MobileTextBox.Text = PhoneNumberFormatter.Format(MobileTextBox.Text);
    }
}
