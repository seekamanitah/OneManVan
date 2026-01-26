using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for adding or editing service agreements.
/// </summary>
public partial class AddEditServiceAgreementDialog : Window
{
    private readonly OneManVanDbContext _dbContext;
    private readonly bool _isEdit;

    public ServiceAgreement? Agreement { get; private set; }

    public AddEditServiceAgreementDialog(OneManVanDbContext dbContext, ServiceAgreement? existing = null)
    {
        InitializeComponent();
        _dbContext = dbContext;
        _isEdit = existing != null;
        Agreement = existing ?? new ServiceAgreement();

        Title = _isEdit ? "Edit Service Agreement" : "New Service Agreement";
        
        LoadCustomers();
        
        if (_isEdit)
        {
            PopulateFields();
        }
        else
        {
            // Set defaults for new agreement
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddYears(1);
            AnnualPriceTextBox.Text = "299";
            CalculateMonthlyPrice();
        }
    }

    private async void LoadCustomers()
    {
        var customers = await _dbContext.Customers
            .Where(c => c.Status != CustomerStatus.DoNotService && c.Status != CustomerStatus.Archived)
            .OrderBy(c => c.Name)
            .ToListAsync();

        CustomerComboBox.ItemsSource = customers;

        if (_isEdit && Agreement != null)
        {
            CustomerComboBox.SelectedValue = Agreement.CustomerId;
            await LoadSitesForCustomer(Agreement.CustomerId);
            SiteComboBox.SelectedValue = Agreement.SiteId;
        }

        CustomerComboBox.SelectionChanged += CustomerComboBox_SelectionChanged;
    }

    private async void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomerComboBox.SelectedValue is int customerId)
        {
            await LoadSitesForCustomer(customerId);
        }
    }

    private async Task LoadSitesForCustomer(int customerId)
    {
        var sites = await _dbContext.Sites
            .Where(s => s.CustomerId == customerId)
            .OrderBy(s => s.IsPrimary ? 0 : 1)
            .ThenBy(s => s.Address)
            .ToListAsync();

        // Add "All Sites" option
        var allSitesOption = new Site { Id = 0, Address = "(All Sites)" };
        var sitesList = new List<Site> { allSitesOption };
        sitesList.AddRange(sites);

        SiteComboBox.ItemsSource = sitesList;
        SiteComboBox.SelectedIndex = 0;
    }

    private void PopulateFields()
    {
        if (Agreement == null) return;

        NameTextBox.Text = Agreement.Name;
        DescriptionTextBox.Text = Agreement.Description;
        
        // Type
        foreach (ComboBoxItem item in TypeComboBox.Items)
        {
            if (item.Tag?.ToString() == Agreement.Type.ToString())
            {
                TypeComboBox.SelectedItem = item;
                break;
            }
        }

        // Dates
        StartDatePicker.SelectedDate = Agreement.StartDate;
        EndDatePicker.SelectedDate = Agreement.EndDate;
        AutoRenewCheckBox.IsChecked = Agreement.AutoRenew;

        // Pricing
        AnnualPriceTextBox.Text = Agreement.AnnualPrice.ToString("F2");
        MonthlyPriceTextBox.Text = Agreement.MonthlyPrice.ToString("F2");
        RepairDiscountTextBox.Text = Agreement.RepairDiscountPercent.ToString("F0");
        RepairDiscountSlider.Value = (double)Agreement.RepairDiscountPercent;

        // Billing frequency
        foreach (ComboBoxItem item in BillingFrequencyComboBox.Items)
        {
            if (item.Tag?.ToString() == Agreement.BillingFrequency.ToString())
            {
                BillingFrequencyComboBox.SelectedItem = item;
                break;
            }
        }

        // Visits
        foreach (ComboBoxItem item in VisitsPerYearComboBox.Items)
        {
            if (item.Tag?.ToString() == Agreement.IncludedVisitsPerYear.ToString())
            {
                VisitsPerYearComboBox.SelectedItem = item;
                break;
            }
        }
        VisitsUsedTextBox.Text = Agreement.VisitsUsed.ToString();

        // Services
        AcTuneUpCheckBox.IsChecked = Agreement.IncludesAcTuneUp;
        HeatingTuneUpCheckBox.IsChecked = Agreement.IncludesHeatingTuneUp;
        FilterReplacementCheckBox.IsChecked = Agreement.IncludesFilterReplacement;
        RefrigerantTopOffCheckBox.IsChecked = Agreement.IncludesRefrigerantTopOff;
        MaxRefrigerantLbsTextBox.Text = Agreement.MaxRefrigerantLbsIncluded.ToString();
        PartsCoverageCheckBox.IsChecked = Agreement.IncludesLimitedPartsCoverage;
        MaxPartsCoverageTextBox.Text = Agreement.MaxPartsCoverageAmount.ToString("F0");

        // Benefits
        PriorityServiceCheckBox.IsChecked = Agreement.PriorityService;
        WaiveTripChargeCheckBox.IsChecked = Agreement.WaiveTripCharge;

        // Scheduling
        if (Agreement.PreferredSpringMonth.HasValue)
        {
            foreach (ComboBoxItem item in SpringMonthComboBox.Items)
            {
                if (item.Tag?.ToString() == Agreement.PreferredSpringMonth.ToString())
                {
                    SpringMonthComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        if (Agreement.PreferredFallMonth.HasValue)
        {
            foreach (ComboBoxItem item in FallMonthComboBox.Items)
            {
                if (item.Tag?.ToString() == Agreement.PreferredFallMonth.ToString())
                {
                    FallMonthComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        // Notes
        TermsTextBox.Text = Agreement.Terms;
        InternalNotesTextBox.Text = Agreement.InternalNotes;

        // Status
        foreach (ComboBoxItem item in StatusComboBox.Items)
        {
            if (item.Tag?.ToString() == Agreement.Status.ToString())
            {
                StatusComboBox.SelectedItem = item;
                break;
            }
        }

        // Update visibility
        RefrigerantLbsPanel.Visibility = RefrigerantTopOffCheckBox.IsChecked == true 
            ? Visibility.Visible : Visibility.Collapsed;
        PartsCoveragePanel.Visibility = PartsCoverageCheckBox.IsChecked == true 
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AnnualPriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        CalculateMonthlyPrice();
    }

    private void BillingFrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CalculateMonthlyPrice();
    }

    private void CalculateMonthlyPrice()
    {
        // Guard against calls before controls are initialized
        if (AnnualPriceTextBox == null || MonthlyPriceTextBox == null || BillingFrequencyComboBox == null)
            return;

        if (!decimal.TryParse(AnnualPriceTextBox.Text, out var annualPrice))
        {
            MonthlyPriceTextBox.Text = "0.00";
            return;
        }

        var frequency = (BillingFrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Annual";
        var monthlyPrice = frequency switch
        {
            "Monthly" => annualPrice / 12,
            "Quarterly" => annualPrice / 4,
            "SemiAnnual" => annualPrice / 2,
            _ => annualPrice
        };

        MonthlyPriceTextBox.Text = monthlyPrice.ToString("F2");
    }

    private void RepairDiscountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Guard against calls before controls are initialized
        if (RepairDiscountTextBox == null)
            return;
            
        RepairDiscountTextBox.Text = ((int)e.NewValue).ToString();
    }

    private void RefrigerantTopOffCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Guard against calls before controls are initialized
        if (RefrigerantLbsPanel == null || RefrigerantTopOffCheckBox == null)
            return;
            
        RefrigerantLbsPanel.Visibility = RefrigerantTopOffCheckBox.IsChecked == true 
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PartsCoverageCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Guard against calls before controls are initialized
        if (PartsCoveragePanel == null || PartsCoverageCheckBox == null)
            return;
            
        PartsCoveragePanel.Visibility = PartsCoverageCheckBox.IsChecked == true 
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Please enter an agreement name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        if (CustomerComboBox.SelectedValue == null)
        {
            MessageBox.Show("Please select a customer.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select start and end dates.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (EndDatePicker.SelectedDate <= StartDatePicker.SelectedDate)
        {
            MessageBox.Show("End date must be after start date.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Populate agreement
        Agreement!.Name = NameTextBox.Text.Trim();
        Agreement.Description = DescriptionTextBox.Text?.Trim();
        Agreement.CustomerId = (int)CustomerComboBox.SelectedValue;
        
        var siteId = SiteComboBox.SelectedValue as int?;
        Agreement.SiteId = siteId == 0 ? null : siteId;

        // Type
        var typeTag = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Standard";
        Agreement.Type = Enum.TryParse<AgreementType>(typeTag, out var type) ? type : AgreementType.Standard;

        // Dates
        Agreement.StartDate = StartDatePicker.SelectedDate!.Value;
        Agreement.EndDate = EndDatePicker.SelectedDate!.Value;
        Agreement.AutoRenew = AutoRenewCheckBox.IsChecked == true;

        // Pricing
        decimal.TryParse(AnnualPriceTextBox.Text, out var annualPrice);
        Agreement.AnnualPrice = annualPrice;
        decimal.TryParse(MonthlyPriceTextBox.Text, out var monthlyPrice);
        Agreement.MonthlyPrice = monthlyPrice;
        decimal.TryParse(RepairDiscountTextBox.Text, out var discount);
        Agreement.RepairDiscountPercent = discount;

        // Billing frequency
        var billingTag = (BillingFrequencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Annual";
        Agreement.BillingFrequency = Enum.TryParse<BillingFrequency>(billingTag, out var billing) 
            ? billing : BillingFrequency.Annual;

        // Visits
        var visitsTag = (VisitsPerYearComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "2";
        Agreement.IncludedVisitsPerYear = int.TryParse(visitsTag, out var visits) ? visits : 2;
        int.TryParse(VisitsUsedTextBox.Text, out var visitsUsed);
        Agreement.VisitsUsed = visitsUsed;

        // Services
        Agreement.IncludesAcTuneUp = AcTuneUpCheckBox.IsChecked == true;
        Agreement.IncludesHeatingTuneUp = HeatingTuneUpCheckBox.IsChecked == true;
        Agreement.IncludesFilterReplacement = FilterReplacementCheckBox.IsChecked == true;
        Agreement.IncludesRefrigerantTopOff = RefrigerantTopOffCheckBox.IsChecked == true;
        int.TryParse(MaxRefrigerantLbsTextBox.Text, out var maxRefrigerant);
        Agreement.MaxRefrigerantLbsIncluded = maxRefrigerant;
        Agreement.IncludesLimitedPartsCoverage = PartsCoverageCheckBox.IsChecked == true;
        decimal.TryParse(MaxPartsCoverageTextBox.Text, out var maxCoverage);
        Agreement.MaxPartsCoverageAmount = maxCoverage;

        // Benefits
        Agreement.PriorityService = PriorityServiceCheckBox.IsChecked == true;
        Agreement.WaiveTripCharge = WaiveTripChargeCheckBox.IsChecked == true;

        // Scheduling
        var springTag = (SpringMonthComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        Agreement.PreferredSpringMonth = int.TryParse(springTag, out var spring) ? spring : 4;
        var fallTag = (FallMonthComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        Agreement.PreferredFallMonth = int.TryParse(fallTag, out var fall) ? fall : 10;

        // Calculate next maintenance
        Agreement.NextMaintenanceDue = Agreement.CalculateNextMaintenanceDue();

        // Notes
        Agreement.Terms = TermsTextBox.Text?.Trim();
        Agreement.InternalNotes = InternalNotesTextBox.Text?.Trim();

        // Status
        var statusTag = (StatusComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Draft";
        Agreement.Status = Enum.TryParse<AgreementStatus>(statusTag, out var status) 
            ? status : AgreementStatus.Draft;

        // Timestamps
        if (_isEdit)
        {
            Agreement.UpdatedAt = DateTime.Now;
        }
        else
        {
            Agreement.CreatedAt = DateTime.Now;
        }

        if (Agreement.Status == AgreementStatus.Active && !Agreement.ActivatedAt.HasValue)
        {
            Agreement.ActivatedAt = DateTime.Now;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
