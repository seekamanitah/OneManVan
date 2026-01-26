using System.Windows;
using System.Windows.Controls;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for adding or editing a customer.
/// </summary>
public partial class AddEditCustomerDialog : Window
{
    private readonly Customer? _existingCustomer;
    public Customer? SavedCustomer { get; private set; }

    public AddEditCustomerDialog(Customer? customer = null)
    {
        InitializeComponent();
        _existingCustomer = customer;

        InitializeComboBoxes();

        if (customer != null)
        {
            Title = "Edit Customer";
            SaveButton.Content = "Save Changes";
            LoadCustomerData(customer);
        }
        else
        {
            Title = "Add Customer";
        }
    }

    private void InitializeComboBoxes()
    {
        // Customer Type
        CustomerTypeCombo.Items.Clear();
        foreach (CustomerType type in Enum.GetValues<CustomerType>())
        {
            CustomerTypeCombo.Items.Add(new ComboBoxItem
            {
                Content = GetCustomerTypeDisplayName(type),
                Tag = type
            });
        }
        CustomerTypeCombo.SelectedIndex = 0;

        // Preferred Contact
        PreferredContactCombo.Items.Clear();
        foreach (ContactMethod method in Enum.GetValues<ContactMethod>())
        {
            PreferredContactCombo.Items.Add(new ComboBoxItem
            {
                Content = method.ToString(),
                Tag = method
            });
        }
        PreferredContactCombo.SelectedIndex = 0;

        // Payment Terms
        PaymentTermsCombo.Items.Clear();
        foreach (PaymentTerms terms in Enum.GetValues<PaymentTerms>())
        {
            PaymentTermsCombo.Items.Add(new ComboBoxItem
            {
                Content = GetPaymentTermsDisplayName(terms),
                Tag = terms
            });
        }
        PaymentTermsCombo.SelectedIndex = 0;

        // Referral Source - common options
        ReferralSourceCombo.Items.Clear();
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Google Search" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Referral" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Yard Sign" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Facebook" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Nextdoor" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Home Advisor" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Yelp" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Repeat Customer" });
        ReferralSourceCombo.Items.Add(new ComboBoxItem { Content = "Other" });
        ReferralSourceCombo.SelectedIndex = 0;

        // Status
        StatusCombo.Items.Clear();
        foreach (CustomerStatus status in Enum.GetValues<CustomerStatus>())
        {
            StatusCombo.Items.Add(new ComboBoxItem
            {
                Content = GetStatusDisplayName(status),
                Tag = status
            });
        }
        // Default to Active for new customers
        SelectComboByTag(StatusCombo, CustomerStatus.Active);
    }

    private void LoadCustomerData(Customer customer)
    {
        // Basic Info
        NameTextBox.Text = customer.Name;
        CompanyNameTextBox.Text = customer.CompanyName;
        SelectComboByTag(CustomerTypeCombo, customer.CustomerType);

        // Contact
        EmailTextBox.Text = customer.Email;
        PhoneTextBox.Text = customer.Phone;
        SecondaryPhoneTextBox.Text = customer.SecondaryPhone;
        SelectComboByTag(PreferredContactCombo, customer.PreferredContact);

        // Load primary site address if exists
        var primarySite = customer.Sites?.FirstOrDefault(s => s.IsPrimary) ?? customer.Sites?.FirstOrDefault();
        if (primarySite != null)
        {
            AddressTextBox.Text = primarySite.Address;
            CityTextBox.Text = primarySite.City;
            StateTextBox.Text = primarySite.State;
            ZipTextBox.Text = primarySite.ZipCode;
        }

        // Billing
        SelectComboByTag(PaymentTermsCombo, customer.PaymentTerms);
        BillingEmailTextBox.Text = customer.BillingEmail;
        TaxExemptCheckBox.IsChecked = customer.TaxExempt;

        // Source
        ReferralSourceCombo.Text = customer.ReferralSource ?? "";
        ReferredByTextBox.Text = customer.ReferredBy;

        // Notes
        NotesTextBox.Text = customer.Notes;

        // Status
        SelectComboByTag(StatusCombo, customer.Status);
    }

    private void SelectComboByTag(ComboBox combo, object tag)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item && item.Tag?.Equals(tag) == true)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private T GetSelectedComboTag<T>(ComboBox combo, T defaultValue)
    {
        if (combo.SelectedItem is ComboBoxItem item && item.Tag is T value)
        {
            return value;
        }
        return defaultValue;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ValidationMessage.Text = "Customer name is required.";
            NameTextBox.Focus();
            return;
        }

        try
        {
            Customer customer;
            Site? primarySite = null;

            if (_existingCustomer != null)
            {
                customer = await App.DbContext.Customers.FindAsync(_existingCustomer.Id) ?? new Customer();
                primarySite = customer.Sites?.FirstOrDefault(s => s.IsPrimary);
            }
            else
            {
                customer = new Customer();
                // Generate customer number
                var maxNumber = App.DbContext.Customers
                    .Select(c => c.CustomerNumber)
                    .AsEnumerable()
                    .Where(n => n != null && n.StartsWith("C-"))
                    .Select(n => {
                        var parts = n!.Split('-');
                        return parts.Length >= 3 && int.TryParse(parts[2], out var num) ? num : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();
                customer.CustomerNumber = $"C-{DateTime.Now.Year}-{maxNumber + 1:D4}";
                customer.CreatedAt = DateTime.UtcNow;
            }

            // Basic Info
            customer.Name = NameTextBox.Text.Trim();
            customer.CompanyName = string.IsNullOrWhiteSpace(CompanyNameTextBox.Text) ? null : CompanyNameTextBox.Text.Trim();
            customer.CustomerType = GetSelectedComboTag(CustomerTypeCombo, CustomerType.Residential);

            // Contact
            customer.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
            customer.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
            customer.SecondaryPhone = string.IsNullOrWhiteSpace(SecondaryPhoneTextBox.Text) ? null : SecondaryPhoneTextBox.Text.Trim();
            customer.PreferredContact = GetSelectedComboTag(PreferredContactCombo, ContactMethod.Any);

            // Billing
            customer.PaymentTerms = GetSelectedComboTag(PaymentTermsCombo, PaymentTerms.DueOnReceipt);
            customer.BillingEmail = string.IsNullOrWhiteSpace(BillingEmailTextBox.Text) ? null : BillingEmailTextBox.Text.Trim();
            customer.TaxExempt = TaxExemptCheckBox.IsChecked == true;

            // Source
            customer.ReferralSource = string.IsNullOrWhiteSpace(ReferralSourceCombo.Text) ? null : ReferralSourceCombo.Text.Trim();
            customer.ReferredBy = string.IsNullOrWhiteSpace(ReferredByTextBox.Text) ? null : ReferredByTextBox.Text.Trim();

            // Notes
            customer.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

            // Status
            customer.Status = GetSelectedComboTag(StatusCombo, CustomerStatus.Active);

            if (_existingCustomer == null)
            {
                App.DbContext.Customers.Add(customer);
                await App.DbContext.SaveChangesAsync(); // Save to get customer ID
            }

            // Handle primary site/address
            if (!string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                if (primarySite == null)
                {
                    primarySite = new Site
                    {
                        CustomerId = customer.Id,
                        IsPrimary = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    App.DbContext.Sites.Add(primarySite);
                }

                primarySite.Address = AddressTextBox.Text.Trim();
                primarySite.City = string.IsNullOrWhiteSpace(CityTextBox.Text) ? null : CityTextBox.Text.Trim();
                primarySite.State = string.IsNullOrWhiteSpace(StateTextBox.Text) ? null : StateTextBox.Text.Trim();
                primarySite.ZipCode = string.IsNullOrWhiteSpace(ZipTextBox.Text) ? null : ZipTextBox.Text.Trim();
            }

            await App.DbContext.SaveChangesAsync();

            SavedCustomer = customer;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ValidationMessage.Text = $"Error saving customer: {ex.Message}";
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string GetCustomerTypeDisplayName(CustomerType type) => type switch
    {
        CustomerType.Residential => "Residential",
        CustomerType.Commercial => "Commercial",
        CustomerType.PropertyManager => "Property Manager",
        CustomerType.Government => "Government",
        CustomerType.NonProfit => "Non-Profit",
        CustomerType.NewConstruction => "New Construction",
        _ => type.ToString()
    };

    private static string GetPaymentTermsDisplayName(PaymentTerms terms) => terms switch
    {
        PaymentTerms.DueOnReceipt => "Due on Receipt",
        PaymentTerms.Net15 => "Net 15",
        PaymentTerms.Net30 => "Net 30",
        PaymentTerms.Net45 => "Net 45",
        PaymentTerms.Net60 => "Net 60",
        _ => terms.ToString()
    };

    private static string GetStatusDisplayName(CustomerStatus status) => status switch
    {
        CustomerStatus.Active => "Active",
        CustomerStatus.Inactive => "Inactive",
        CustomerStatus.Lead => "Lead",
        CustomerStatus.VIP => "VIP",
        CustomerStatus.DoNotService => "Do Not Service",
        CustomerStatus.Delinquent => "Delinquent",
        CustomerStatus.Archived => "Archived",
        _ => status.ToString()
    };
}
