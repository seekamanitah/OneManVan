using System.Windows.Controls;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Controls;

public partial class CustomerFormContent : UserControl
{
    public CustomerFormContent()
    {
        InitializeComponent();
        CustomerTypeCombo.SelectedIndex = 0;
    }

    public Customer GetCustomer()
    {
        return new Customer
        {
            Name = NameTextBox.Text,
            CompanyName = CompanyNameTextBox.Text,
            Email = EmailTextBox.Text,
            Phone = PhoneTextBox.Text,
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
        NameTextBox.Text = customer.Name;
        CompanyNameTextBox.Text = customer.CompanyName;
        EmailTextBox.Text = customer.Email;
        PhoneTextBox.Text = customer.Phone;
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
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            return false;
        }
        return true;
    }
}
