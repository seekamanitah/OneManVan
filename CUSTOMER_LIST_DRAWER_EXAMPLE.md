# Customer List Page - Drawer Conversion Example

This demonstrates how to convert CustomerListPage from using navigation to AddCustomerPage into using an inline drawer.

## Before: Using Page Navigation

```xaml
<!-- CustomerListPage.xaml - OLD -->
<ContentPage>
    <Grid>
        <CollectionView x:Name="CustomerCollection"/>
        <Button Text="+ Add Customer" Clicked="OnAddCustomerClicked"/>
    </Grid>
</ContentPage>
```

```csharp
// CustomerListPage.xaml.cs - OLD
private async void OnAddCustomerClicked(object sender, EventArgs e)
{
    await Shell.Current.GoToAsync("AddCustomerPage");
}
```

## After: Using Slide-In Drawer

```xaml
<!-- CustomerListPage.xaml - NEW -->
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:OneManVan.Mobile.Controls"
             x:Class="OneManVan.Mobile.Pages.CustomerListPage"
             Title="Customers">
    
    <Grid>
        <!-- Main Content -->
        <Grid RowDefinitions="Auto,Auto,*">
            <!-- Search Bar -->
            <SearchBar Grid.Row="0" x:Name="SearchBar"/>
            
            <!-- Filters -->
            <ScrollView Grid.Row="1" Orientation="Horizontal">
                <HorizontalStackLayout>
                    <Button x:Name="AllFilter" Text="All" Clicked="OnFilterClicked"/>
                    <Button x:Name="ActiveFilter" Text="Active" Clicked="OnFilterClicked"/>
                    <Button x:Name="VIPFilter" Text="VIP" Clicked="OnFilterClicked"/>
                </HorizontalStackLayout>
            </ScrollView>
            
            <!-- Customer List -->
            <RefreshView Grid.Row="2" Refreshing="OnRefreshing">
                <CollectionView x:Name="CustomerCollection"
                                SelectionChanged="OnCustomerSelected">
                    <CollectionView.ItemTemplate>
                        <DataTemplate>
                            <Border Style="{StaticResource CardStyle}" Margin="16,4">
                                <Grid ColumnDefinitions="Auto,*,Auto" Padding="12">
                                    <Label Text="{Binding InitialIcon}" FontSize="24"/>
                                    <VerticalStackLayout Grid.Column="1" Margin="12,0">
                                        <Label Text="{Binding Name}" FontAttributes="Bold"/>
                                        <Label Text="{Binding Phone}"/>
                                    </VerticalStackLayout>
                                    <Label Grid.Column="2" Text="{Binding StatusIcon}"/>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>
            </RefreshView>
            
            <!-- FAB Button -->
            <Button Grid.Row="2"
                    Text="+"
                    FontSize="28"
                    WidthRequest="60"
                    HeightRequest="60"
                    CornerRadius="30"
                    BackgroundColor="{StaticResource Primary}"
                    TextColor="White"
                    HorizontalOptions="End"
                    VerticalOptions="End"
                    Margin="20"
                    Clicked="OnAddCustomerClicked"/>
        </Grid>

        <!-- Add/Edit Customer Drawer (overlay - last for Z-order) -->
        <controls:SlideInDrawer x:Name="CustomerDrawer"
                                Title="Add Customer"
                                SaveButtonText="Save Customer"
                                SaveClicked="OnDrawerSaveClicked"
                                CancelClicked="OnDrawerCancelClicked">
            <controls:SlideInDrawer.DrawerContent>
                <VerticalStackLayout Spacing="16">
                    <!-- Customer Type -->
                    <VerticalStackLayout Spacing="4">
                        <Label Text="Customer Type" 
                               FontSize="12" 
                               TextColor="{AppThemeBinding Light={StaticResource LightTextSecondary}, Dark={StaticResource DarkTextSecondary}}"/>
                        <Picker x:Name="CustomerTypePicker">
                            <Picker.ItemsSource>
                                <x:Array Type="{x:Type x:String}">
                                    <x:String>Residential</x:String>
                                    <x:String>Commercial</x:String>
                                    <x:String>Property Manager</x:String>
                                </x:Array>
                            </Picker.ItemsSource>
                        </Picker>
                    </VerticalStackLayout>

                    <!-- Name -->
                    <VerticalStackLayout Spacing="4">
                        <Label Text="Name *" 
                               FontSize="12" 
                               TextColor="{AppThemeBinding Light={StaticResource SectionPrimary}, Dark={StaticResource SectionPrimaryDark}}"
                               FontAttributes="Bold"/>
                        <Entry x:Name="NameEntry"
                               Placeholder="Customer name (required)"
                               FontSize="16"
                               BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                    </VerticalStackLayout>

                    <!-- Phone -->
                    <VerticalStackLayout Spacing="4">
                        <Label Text="Phone" FontSize="12"/>
                        <Entry x:Name="PhoneEntry"
                               Placeholder="(555) 123-4567"
                               Keyboard="Telephone"
                               BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                    </VerticalStackLayout>

                    <!-- Email -->
                    <VerticalStackLayout Spacing="4">
                        <Label Text="Email" FontSize="12"/>
                        <Entry x:Name="EmailEntry"
                               Placeholder="customer@email.com"
                               Keyboard="Email"
                               BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                    </VerticalStackLayout>

                    <!-- Address -->
                    <VerticalStackLayout Spacing="4">
                        <Label Text="Address" FontSize="12"/>
                        <Entry x:Name="AddressEntry"
                               Placeholder="Street address"
                               BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                    </VerticalStackLayout>

                    <!-- City, State, Zip -->
                    <Grid ColumnDefinitions="*,Auto,Auto" ColumnSpacing="8">
                        <Entry x:Name="CityEntry" Placeholder="City"/>
                        <Entry Grid.Column="1" x:Name="StateEntry" Placeholder="ST" WidthRequest="60"/>
                        <Entry Grid.Column="2" x:Name="ZipEntry" Placeholder="12345" Keyboard="Numeric" WidthRequest="80"/>
                    </Grid>

                    <!-- Notes -->
                    <VerticalStackLayout Spacing="4">
                        <Label Text="Notes" FontSize="12"/>
                        <Editor x:Name="NotesEditor"
                                Placeholder="Additional notes..."
                                HeightRequest="80"
                                BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                    </VerticalStackLayout>
                </VerticalStackLayout>
            </controls:SlideInDrawer.DrawerContent>
        </controls:SlideInDrawer>
    </Grid>
</ContentPage>
```

```csharp
// CustomerListPage.xaml.cs - NEW
using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

public partial class CustomerListPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private Customer? _editingCustomer; // Track if editing
    private CancellationTokenSource? _cts;

    public CustomerListPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        await LoadCustomersAsync(_cts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    // Open drawer for adding new customer
    private async void OnAddCustomerClicked(object sender, EventArgs e)
    {
        _editingCustomer = null; // Clear edit mode
        
        // Reset form
        CustomerTypePicker.SelectedIndex = 0;
        NameEntry.Text = "";
        PhoneEntry.Text = "";
        EmailEntry.Text = "";
        AddressEntry.Text = "";
        CityEntry.Text = "";
        StateEntry.Text = "";
        ZipEntry.Text = "";
        NotesEditor.Text = "";
        
        // Update drawer title
        CustomerDrawer.Title = "Add Customer";
        CustomerDrawer.SaveButtonText = "Save Customer";
        
        // Open drawer
        await CustomerDrawer.OpenAsync();
        
        // Focus first field
        NameEntry.Focus();
    }

    // Open drawer for editing existing customer
    private async void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Customer customer)
            return;

        // Deselect
        ((CollectionView)sender).SelectedItem = null;

        // Option 1: Navigate to detail page
        // await Shell.Current.GoToAsync($"CustomerDetailPage?customerId={customer.Id}");

        // Option 2: Show edit drawer
        _editingCustomer = customer;
        
        // Load data
        CustomerTypePicker.SelectedItem = customer.CustomerType;
        NameEntry.Text = customer.Name;
        PhoneEntry.Text = customer.Phone;
        EmailEntry.Text = customer.Email;
        AddressEntry.Text = customer.Address;
        CityEntry.Text = customer.City;
        StateEntry.Text = customer.State;
        ZipEntry.Text = customer.Zip;
        NotesEditor.Text = customer.Notes;
        
        // Update drawer title
        CustomerDrawer.Title = "Edit Customer";
        CustomerDrawer.SaveButtonText = "Update";
        
        // Open drawer
        await CustomerDrawer.OpenAsync();
    }

    // Save button clicked in drawer
    private async void OnDrawerSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(NameEntry.Text))
            {
                await DisplayAlert("Required", "Customer name is required", "OK");
                return;
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            Customer customer;
            if (_editingCustomer != null)
            {
                // Update existing
                customer = await db.Customers.FindAsync(_editingCustomer.Id);
                if (customer == null)
                {
                    await DisplayAlert("Error", "Customer not found", "OK");
                    return;
                }
            }
            else
            {
                // Create new
                customer = new Customer();
                db.Customers.Add(customer);
            }

            // Update properties
            customer.CustomerType = CustomerTypePicker.SelectedItem?.ToString() ?? "Residential";
            customer.Name = NameEntry.Text;
            customer.Phone = PhoneEntry.Text;
            customer.Email = EmailEntry.Text;
            customer.Address = AddressEntry.Text;
            customer.City = CityEntry.Text;
            customer.State = StateEntry.Text;
            customer.Zip = ZipEntry.Text;
            customer.Notes = NotesEditor.Text;

            await db.SaveChangesAsync();

            // Haptic feedback
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            // Close drawer
            await CustomerDrawer.CompleteSaveAsync();

            // Refresh list
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save customer: {ex.Message}", "OK");
        }
    }

    // Cancel button clicked in drawer
    private void OnDrawerCancelClicked(object sender, EventArgs e)
    {
        // Drawer auto-closes
        _editingCustomer = null;
    }

    private async Task LoadCustomersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            var query = db.Customers.AsQueryable();
            
            // Apply search filter if any
            var searchText = SearchBar.Text;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(c => c.Name.Contains(searchText) || 
                                        c.Phone.Contains(searchText) ||
                                        c.Email.Contains(searchText));
            }
            
            var customers = await query
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CustomerCollection.ItemsSource = customers;
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when navigating away
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlert("Error", $"Failed to load customers: {ex.Message}", "OK");
            }
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadCustomersAsync();
        ((RefreshView)sender).IsRefreshing = false;
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        // Handle filter button clicks
        await LoadCustomersAsync();
    }
}
```

## Key Changes

### 1. UI Structure
- ? Form moved from separate page into drawer content
- ? Main content and drawer in same XAML
- ? Drawer overlays content (Z-order)

### 2. Navigation
- ? OLD: `await Shell.Current.GoToAsync("AddCustomerPage")`
- ? NEW: `await CustomerDrawer.OpenAsync()`

### 3. Data Flow
- ? Form fields directly accessible in same class
- ? No need to pass data between pages
- ? Validation happens before close

### 4. User Experience
- ? Faster (no page transition)
- ? Context maintained (can see list behind drawer)
- ? Smooth animation
- ? Tap outside to dismiss

### 5. Code Organization
- ? All customer CRUD logic in one file
- ? Easier to maintain
- ? Better testability

## Benefits Demonstrated

1. **Performance**: No page creation/destruction
2. **UX**: Modern, familiar pattern
3. **Maintainability**: Less code, one file
4. **Flexibility**: Easy to switch between add/edit modes

## Next Steps

1. Test this implementation
2. Apply same pattern to other list pages
3. Remove old AddCustomerPage.xaml after testing
4. Update any navigation routes

---

*This example can be replicated for Jobs, Estimates, Invoices, Assets, etc.*
