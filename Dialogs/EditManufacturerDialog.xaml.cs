using System;
using System.Windows;
using OneManVan.Shared.Models;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for editing manufacturer registration details.
/// </summary>
public partial class EditManufacturerDialog : Window
{
    private readonly ManufacturerRegistration _manufacturer;

    public EditManufacturerDialog(ManufacturerRegistration manufacturer)
    {
        InitializeComponent();
        _manufacturer = manufacturer;
        LoadManufacturer();
    }

    private void LoadManufacturer()
    {
        BrandNameTextBox.Text = _manufacturer.BrandName;
        RegistrationUrlTextBox.Text = _manufacturer.RegistrationUrl;
        ManufacturerWebsiteTextBox.Text = _manufacturer.ManufacturerWebsite;
        SupportPhoneTextBox.Text = _manufacturer.SupportPhone;
        SupportEmailTextBox.Text = _manufacturer.SupportEmail;
        RegistrationNotesTextBox.Text = _manufacturer.RegistrationNotes;
        DisplayOrderTextBox.Text = _manufacturer.DisplayOrder.ToString();
        IsActiveCheckBox.IsChecked = _manufacturer.IsActive;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!Validate())
            return;

        // Update manufacturer
        _manufacturer.BrandName = BrandNameTextBox.Text.Trim();
        _manufacturer.RegistrationUrl = string.IsNullOrWhiteSpace(RegistrationUrlTextBox.Text) 
            ? null 
            : RegistrationUrlTextBox.Text.Trim();
        _manufacturer.ManufacturerWebsite = string.IsNullOrWhiteSpace(ManufacturerWebsiteTextBox.Text) 
            ? null 
            : ManufacturerWebsiteTextBox.Text.Trim();
        _manufacturer.SupportPhone = string.IsNullOrWhiteSpace(SupportPhoneTextBox.Text) 
            ? null 
            : SupportPhoneTextBox.Text.Trim();
        _manufacturer.SupportEmail = string.IsNullOrWhiteSpace(SupportEmailTextBox.Text) 
            ? null 
            : SupportEmailTextBox.Text.Trim();
        _manufacturer.RegistrationNotes = string.IsNullOrWhiteSpace(RegistrationNotesTextBox.Text) 
            ? null 
            : RegistrationNotesTextBox.Text.Trim();
        _manufacturer.DisplayOrder = int.TryParse(DisplayOrderTextBox.Text, out var order) ? order : 0;
        _manufacturer.IsActive = IsActiveCheckBox.IsChecked == true;
        _manufacturer.UpdatedAt = DateTime.UtcNow;

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(BrandNameTextBox.Text))
        {
            MessageBox.Show("Brand Name is required", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            BrandNameTextBox.Focus();
            return false;
        }

        return true;
    }
}
