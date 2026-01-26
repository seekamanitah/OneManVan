# ?? **Naming Conventions - OneManVan Mobile**

**Version:** 1.0  
**Last Updated:** January 2026  
**Status:** Official Team Standard

---

## **?? Purpose**

This document defines the naming conventions for the OneManVan Mobile (.NET MAUI) project. Following these standards ensures consistency, readability, and maintainability across the codebase.

---

## **1. Field Naming**

### **Private Fields**
**Pattern:** `_camelCase` with underscore prefix

```csharp
// ? Correct:
private readonly OneManVanDbContext _db;
private Customer? _customer;
private List<Job> _jobs = [];

// ? Incorrect:
private OneManVanDbContext dbContext;        // Missing underscore
private OneManVanDbContext _dbContext;       // Use shorter '_db'
private Customer _selectedCustomer;          // Be specific when needed
```

### **Standard Field Names**
- Database context: `_db` (not `_dbContext` or `_context`)
- Services: `_customerPicker`, `_lineItemDialog`, etc.
- Collections: Plural form (`_jobs`, `_customers`, `_lineItems`)

---

## **2. Method Naming**

### **Async Methods**
**Pattern:** `VerbNounAsync()`

Always include `Async` suffix for async methods.

```csharp
// ? Correct:
private async Task LoadCustomerAsync()
private async Task SaveChangesAsync()
private async Task<Customer?> GetCustomerByIdAsync(int id)

// ? Incorrect:
private async Task LoadCustomer()           // Missing 'Async'
private async Task Load()                   // Too vague
private async Task GetCustomer(int id)      // Missing 'Async'
```

### **Load Methods**

#### **List Pages** - Use Plural
```csharp
// ? List pages load multiple items:
private async Task LoadCustomersAsync()
private async Task LoadJobsAsync()
private async Task LoadEstimatesAsync(CancellationToken ct)
```

#### **Detail/Edit Pages** - Use Singular
```csharp
// ? Detail/Edit pages load single item:
private async Task LoadCustomerAsync()
private async Task LoadJobAsync()
private async Task LoadInvoiceAsync()
```

#### **Complex Pages** - Be Descriptive
```csharp
// ? Multiple entities:
private async Task LoadPageDataAsync()
private async Task LoadCustomerAndSitesAsync()
```

---

## **3. Event Handler Naming**

### **Pattern:** `On{Control}{Event}`

Always prefix with `On` and use PascalCase.

```csharp
// ? Correct:
private async void OnSaveClicked(object sender, EventArgs e)
private async void OnCancelClicked(object sender, EventArgs e)
private void OnCustomerTapped(object sender, TappedEventArgs e)
private void OnSearchTextChanged(object sender, TextChangedEventArgs e)

// ? Incorrect:
private void SaveClicked(object sender, EventArgs e)      // Missing 'On'
private void HandleSave(object sender, EventArgs e)       // Wrong prefix
private void Save_Clicked(object sender, EventArgs e)     // Wrong format
private void btnSave_Click(object sender, EventArgs e)    // WinForms style
```

### **Common Event Patterns**
- Button: `OnSaveClicked`, `OnDeleteClicked`, `OnCancelClicked`
- Tap: `OnItemTapped`, `OnCustomerTapped`
- Text: `OnSearchTextChanged`, `OnNameEntryCompleted`
- Selection: `OnCustomerSelected`, `OnPickerSelectedIndexChanged`

---

## **4. Property Naming**

### **Public Properties**
**Pattern:** `PascalCase`

```csharp
// ? Correct:
public int CustomerId { get; set; }
public string Title { get; set; }
public DateTime? ScheduledDate { get; set; }

// ? Incorrect:
public int customerID { get; set; }        // camelCase not allowed
public string title { get; set; }          // camelCase not allowed
```

### **Boolean Properties**
**Pattern:** `Is`, `Has`, `Can` prefix

```csharp
// ? Correct:
public bool IsActive { get; set; }
public bool HasItems { get; set; }
public bool CanEdit { get; set; }
public bool IsLoading { get; private set; }

// ? Incorrect:
public bool Active { get; set; }           // Missing 'Is'
public bool Editing { get; set; }          // Use 'CanEdit' or 'IsEditing'
```

---

## **5. Parameter Naming**

### **Pattern:** `camelCase`

```csharp
// ? Correct:
public AddJobPage(OneManVanDbContext db, CustomerPickerService customerPicker)
public async Task<Customer?> FindCustomerAsync(int customerId)
private void UpdateTotal(decimal subtotal, decimal taxRate)

// ? Incorrect:
public AddJobPage(OneManVanDbContext DB)                   // PascalCase
public async Task<Customer?> FindCustomerAsync(int ID)     // PascalCase
```

---

## **6. Collection Naming**

### **Pattern:** Plural Nouns

```csharp
// ? Correct:
private List<Customer> _customers = [];
private ObservableCollection<Job> _jobs = [];
private IEnumerable<Estimate> _estimates;

// ? Incorrect:
private List<Customer> _customerList = [];      // Redundant 'List'
private List<Job> _jobArray = [];               // Wrong type in name
```

---

## **7. Constant Naming**

### **Pattern:** `PascalCase` or `ALL_CAPS`

```csharp
// ? Correct (Class Constants):
public static class BusinessDefaults
{
    public const int DefaultEstimateValidityDays = 30;
    public const decimal DefaultTaxRate = 8.0m;
}

// ? Correct (Local Constants):
private const int MaxRetries = 3;
private const string DefaultStatus = "Active";

// ? Incorrect:
private const int max_retries = 3;             // Use PascalCase
private const string DEFAULT_STATUS = "Active"; // Acceptable but prefer PascalCase
```

---

## **8. XAML Control Naming**

### **Pattern:** `{PurposePascalCase}{ControlType}`

```csharp
// ? Correct:
<Entry x:Name="NameEntry" />
<Button x:Name="SaveButton" />
<Label x:Name="CustomerNameLabel" />
<ActivityIndicator x:Name="LoadingIndicator" />
<DatePicker x:Name="ScheduledDatePicker" />

// ? Incorrect:
<Entry x:Name="txtName" />               // Old VB/WinForms style
<Button x:Name="btnSave" />              // Abbreviations
<Label x:Name="lblCustomerName" />       // Abbreviations
```

---

## **9. Namespace Naming**

### **Pattern:** `CompanyName.ProjectName.FeatureArea`

```csharp
// ? Correct:
namespace OneManVan.Mobile.Pages;
namespace OneManVan.Mobile.Services;
namespace OneManVan.Mobile.Helpers;
namespace OneManVan.Mobile.Extensions;

// ? Incorrect:
namespace OneManVan.Mobile.Pages.Customer;      // Too deep
namespace Mobile.Pages;                         // Missing company
```

---

## **10. Class Naming**

### **Pattern:** `PascalCase` with Descriptive Suffix

```csharp
// ? Correct:
public class AddJobPage : ContentPage
public class CustomerPickerService
public class LineItemDialogService
public class LoadingScope : IDisposable
public static class PageExtensions

// ? Incorrect:
public class JobAdd : ContentPage              // Wrong order
public class CustomerPicker                    // Add 'Service' if it's a service
public class Loading                           // Too vague
```

### **Common Suffixes**
- Pages: `Page` (AddJobPage, CustomerListPage)
- Services: `Service` (CustomerPickerService)
- Helpers: `Helper` (CustomerSelectionHelper)
- Extensions: `Extensions` (PageExtensions)
- ViewModels: `ViewModel` (EstimateLineViewModel)

---

## **11. Interface Naming**

### **Pattern:** `I` prefix + `PascalCase`

```csharp
// ? Correct:
public interface ICustomerService
public interface IRepository<T>
public interface ILoadingIndicator

// ? Incorrect:
public interface CustomerService         // Missing 'I'
public interface InterfaceCustomer       // Don't use 'Interface'
```

---

## **12. Enum Naming**

### **Pattern:** Singular noun, PascalCase values

```csharp
// ? Correct:
public enum JobStatus
{
    Draft,
    Scheduled,
    InProgress,
    Completed
}

public enum CustomerType
{
    Residential,
    Commercial
}

// ? Incorrect:
public enum JobStatuses { }              // Plural
public enum jobStatus { }                // camelCase
public enum JobStatus
{
    draft,                               // camelCase values
    IN_PROGRESS                          // ALL_CAPS
}
```

---

## **?? Quick Reference**

| Item | Pattern | Example |
|------|---------|---------|
| Private Field | `_camelCase` | `_db`, `_customer` |
| Public Property | `PascalCase` | `CustomerId`, `Title` |
| Method | `VerbNounAsync()` | `LoadCustomerAsync()` |
| Event Handler | `On{Control}{Event}` | `OnSaveClicked` |
| Parameter | `camelCase` | `customerId`, `db` |
| Local Variable | `camelCase` | `customer`, `total` |
| Constant | `PascalCase` | `MaxRetries`, `DefaultTaxRate` |
| Class | `PascalCase{Suffix}` | `AddJobPage`, `CustomerService` |
| Interface | `IPascalCase` | `ICustomerService` |
| Enum | `PascalCase` singular | `JobStatus`, `CustomerType` |

---

## **? Validation Checklist**

Before committing code, verify:

- [ ] All private fields start with `_`
- [ ] All async methods end with `Async`
- [ ] Event handlers start with `On`
- [ ] Boolean properties use `Is`/`Has`/`Can`
- [ ] Collections use plural names
- [ ] Methods have clear, descriptive names
- [ ] No abbreviations (use full names)
- [ ] Consistent casing throughout

---

## **?? Benefits of Following These Conventions**

1. **Readability** - Code is self-documenting
2. **Consistency** - Easy to navigate codebase
3. **Maintainability** - Changes are predictable
4. **Onboarding** - New developers learn faster
5. **Code Reviews** - Easier to spot issues
6. **Tooling** - Better IDE support

---

**Last Updated:** January 2026  
**Version:** 1.0  
**Status:** ? Official Standard
