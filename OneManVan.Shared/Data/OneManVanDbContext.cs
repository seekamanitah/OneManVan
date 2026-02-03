using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;

namespace OneManVan.Shared.Data;

/// <summary>
/// Entity Framework Core database context for OneManVan FSM.
/// Supports SQLite (local) and SQL Server (remote Docker) modes.
/// </summary>
public class OneManVanDbContext : DbContext
{
    public OneManVanDbContext(DbContextOptions<OneManVanDbContext> options)
        : base(options)
    {
    }

    // Core Entities
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<CustomField> CustomFields => Set<CustomField>();
    public DbSet<SchemaDefinition> SchemaDefinitions => Set<SchemaDefinition>();
    
    // Custom Field System
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<CustomFieldChoice> CustomFieldChoices => Set<CustomFieldChoice>();

    
    // Multi-ownership relationships
    public DbSet<AssetOwner> AssetOwners => Set<AssetOwner>();
    public DbSet<CustomerCompanyRole> CustomerCompanyRoles => Set<CustomerCompanyRole>();
    
    // Estimates & Inventory
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<EstimateLine> EstimateLines => Set<EstimateLine>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();
    
    // Jobs & Time
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<JobPart> JobParts => Set<JobPart>();
    
    // Invoicing
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    
    // Products
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductDocument> ProductDocuments => Set<ProductDocument>();
    
    // Manufacturer Registration URLs (configurable)
    public DbSet<ManufacturerRegistration> ManufacturerRegistrations => Set<ManufacturerRegistration>();
    
    // Warranty Claims
    public DbSet<WarrantyClaim> WarrantyClaims => Set<WarrantyClaim>();
    
    // Service History & Photos
    public DbSet<ServiceHistory> ServiceHistory => Set<ServiceHistory>();
    public DbSet<JobPhoto> JobPhotos => Set<JobPhoto>();
    public DbSet<AssetPhoto> AssetPhotos => Set<AssetPhoto>();
    public DbSet<SitePhoto> SitePhotos => Set<SitePhoto>();
    
    // Communication & Documents
    public DbSet<CommunicationLog> CommunicationLogs => Set<CommunicationLog>();
    public DbSet<CustomerDocument> CustomerDocuments => Set<CustomerDocument>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    
    // Document Library (service manuals, technical guides, business templates)
    public DbSet<Document> Documents => Set<Document>();
    
    // Employees & Contractors
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeTimeLog> EmployeeTimeLogs => Set<EmployeeTimeLog>();
    public DbSet<EmployeePerformanceNote> EmployeePerformanceNotes => Set<EmployeePerformanceNote>();
    public DbSet<EmployeePayment> EmployeePayments => Set<EmployeePayment>();
    
    // Business Expenses
    public DbSet<Expense> Expenses => Set<Expense>();
    
    // Service Agreements
    public DbSet<ServiceAgreement> ServiceAgreements => Set<ServiceAgreement>();
    
    // Quick Notes (standalone notes/reminders)
    public DbSet<QuickNote> QuickNotes => Set<QuickNote>();
    
    // Company Settings (singleton - branding, contact info for documents)
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    
    // Material Lists (HVAC job bidding and material tracking)
    public DbSet<MaterialList> MaterialLists => Set<MaterialList>();
    public DbSet<MaterialListSystem> MaterialListSystems => Set<MaterialListSystem>();
    public DbSet<MaterialListItem> MaterialListItems => Set<MaterialListItem>();
    public DbSet<MaterialListTemplate> MaterialListTemplates => Set<MaterialListTemplate>();
    public DbSet<MaterialListTemplateItem> MaterialListTemplateItems => Set<MaterialListTemplateItem>();

    // Configurable Dropdown Presets (user-editable dropdown options)
    public DbSet<DropdownPreset> DropdownPresets => Set<DropdownPreset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =====================
        // Customer configuration
        // =====================
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerNumber).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CustomerType);

            entity.HasMany(e => e.Sites)
                .WithOne(s => s.Customer)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Assets)
                .WithOne(a => a.Customer)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore computed properties
            entity.Ignore(e => e.DisplayName);
            entity.Ignore(e => e.CustomerTypeDisplay);
            entity.Ignore(e => e.StatusDisplay);
            entity.Ignore(e => e.TagList);
            entity.Ignore(e => e.IsVip);
            entity.Ignore(e => e.HasOutstandingBalance);
            entity.Ignore(e => e.PreferredServiceWindowDisplay);
        });

        // =====================
        // ServiceAgreement configuration
        // =====================
        modelBuilder.Entity<ServiceAgreement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AgreementNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.EndDate);
            entity.HasIndex(e => e.NextMaintenanceDue);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore computed properties
            entity.Ignore(e => e.IsActive);
            entity.Ignore(e => e.IsExpiringSoon);
            entity.Ignore(e => e.IsExpired);
            entity.Ignore(e => e.DaysUntilExpiration);
            entity.Ignore(e => e.VisitsRemaining);
            entity.Ignore(e => e.StatusDisplay);
            entity.Ignore(e => e.TypeDisplay);
            entity.Ignore(e => e.CoveredAssetIdList);
        });

        // =====================
        // Site configuration
        // =====================
        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SiteNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => new { e.CustomerId, e.IsPrimary });
            entity.HasIndex(e => e.PropertyType);
            entity.HasIndex(e => e.ServiceZone);

            entity.HasMany(e => e.Assets)
                .WithOne(a => a.Site)
                .HasForeignKey(a => a.SiteId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore computed properties
            entity.Ignore(e => e.FullAddress);
            entity.Ignore(e => e.ShortAddress);
            entity.Ignore(e => e.HasGeoLocation);
            entity.Ignore(e => e.PropertyTypeDisplay);
            entity.Ignore(e => e.GoogleMapsUrl);
            entity.Ignore(e => e.AccessWarnings);
            entity.Ignore(e => e.HasAccessCodes);
        });

        // =====================
        // Asset configuration
        // =====================
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AssetNumber);
            entity.HasIndex(e => e.AssetTag).IsUnique();
            entity.HasIndex(e => e.Serial);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.FuelType);
            entity.HasIndex(e => e.EquipmentType);
            entity.HasIndex(e => e.RefrigerantType);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.ReplacedByAsset)
                .WithMany()
                .HasForeignKey(e => e.ReplacedByAssetId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflict in SQL Server

            entity.HasMany(e => e.CustomFields)
                .WithOne(cf => cf.Asset)
                .HasForeignKey(cf => cf.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore all computed properties
            entity.Ignore(e => e.WarrantyEndDate);
            entity.Ignore(e => e.PartsWarrantyEnd);
            entity.Ignore(e => e.LaborWarrantyEnd);
            entity.Ignore(e => e.CompressorWarrantyEnd);
            entity.Ignore(e => e.IsWarrantyExpired);
            entity.Ignore(e => e.IsWarrantyExpiringSoon);
            entity.Ignore(e => e.DaysUntilWarrantyExpires);
            entity.Ignore(e => e.Tonnage);
            entity.Ignore(e => e.AgeYears);
            entity.Ignore(e => e.DisplayName);
            entity.Ignore(e => e.EquipmentTypeDisplay);
            entity.Ignore(e => e.RefrigerantTypeDisplay);
            entity.Ignore(e => e.IsLegacyRefrigerant);
            entity.Ignore(e => e.ConditionDisplay);
            entity.Ignore(e => e.IsFilterDue);
            entity.Ignore(e => e.IsServiceDue);
            entity.Ignore(e => e.CapacitySummary);
            entity.Ignore(e => e.EfficiencySummary);
            entity.Ignore(e => e.HasValidLocation);
            entity.Ignore(e => e.LocationDescription);
        });

        // =====================
        // CustomField configuration
        // =====================
        modelBuilder.Entity<CustomField>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => new { e.EntityType, e.Key });
        });

        // =====================
        // SchemaDefinition configuration
        // =====================
        modelBuilder.Entity<SchemaDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.FieldName }).IsUnique();
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.IsActive);

            entity.Ignore(e => e.EnumOptionsList);
            entity.Ignore(e => e.FieldTypeDisplay);
        });

        // =====================
        // Estimate configuration
        // =====================
        modelBuilder.Entity<Estimate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Asset)
                .WithMany()
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Lines)
                .WithOne(l => l.Estimate)
                .HasForeignKey(l => l.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.StatusDisplay);
            entity.Ignore(e => e.CanEdit);
            entity.Ignore(e => e.IsValid);
        });

        // =====================
        // EstimateLine configuration
        // =====================
        modelBuilder.Entity<EstimateLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EstimateId);
            entity.HasIndex(e => e.InventoryItemId);

            entity.HasOne(e => e.InventoryItem)
                .WithMany(i => i.EstimateLines)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Ignore(e => e.TypeDisplay);
        });

        // =====================
        // InventoryItem configuration
        // =====================
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InventoryNumber);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Sku);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);

            entity.HasMany(e => e.Logs)
                .WithOne(l => l.InventoryItem)
                .HasForeignKey(l => l.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.IsLowStock);
            entity.Ignore(e => e.IsOutOfStock);
            entity.Ignore(e => e.ProfitMargin);
        });

        // =====================
        // InventoryLog configuration
        // =====================
        modelBuilder.Entity<InventoryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InventoryItemId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ChangeType);

            entity.Ignore(e => e.ChangeTypeDisplay);
            entity.Ignore(e => e.ChangeDisplay);
        });

        // =====================
        // Job configuration
        // =====================
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.JobType);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.ScheduledDate);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Estimate)
                .WithMany()
                .HasForeignKey(e => e.EstimateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Asset)
                .WithMany()
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.FollowUpJob)
                .WithMany()
                .HasForeignKey(e => e.FollowUpJobId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflict in SQL Server

            entity.HasOne(e => e.FollowUpFromJob)
                .WithMany()
                .HasForeignKey(e => e.FollowUpFromJobId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflict in SQL Server

            entity.HasMany(e => e.TimeEntries)
                .WithOne(t => t.Job)
                .HasForeignKey(t => t.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Invoices)
                .WithOne(i => i.Job)
                .HasForeignKey(i => i.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore computed properties
            entity.Ignore(e => e.ActualHours);
            entity.Ignore(e => e.StatusDisplay);
            entity.Ignore(e => e.JobTypeDisplay);
            entity.Ignore(e => e.PriorityDisplay);
            entity.Ignore(e => e.IsEmergency);
            entity.Ignore(e => e.CanStart);
            entity.Ignore(e => e.CanComplete);
            entity.Ignore(e => e.CanEdit);
            entity.Ignore(e => e.CanInvoice);
            entity.Ignore(e => e.HasSignature);
            entity.Ignore(e => e.ArrivalWindowDisplay);
            entity.Ignore(e => e.Duration);
            entity.Ignore(e => e.TagList);
            entity.Ignore(e => e.IsScheduledToday);
            entity.Ignore(e => e.IsOverdue);
        });

        // =====================
        // TimeEntry configuration
        // =====================
        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.StartTime);

            entity.Ignore(e => e.Duration);
            entity.Ignore(e => e.DurationHours);
            entity.Ignore(e => e.Amount);
            entity.Ignore(e => e.IsActive);
            entity.Ignore(e => e.DurationDisplay);
            entity.Ignore(e => e.TypeDisplay);
        });

        // =====================
        // JobPart configuration
        // =====================
        modelBuilder.Entity<JobPart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.InventoryItemId);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Ignore(e => e.Profit);
        });

        // =====================
        // Invoice configuration
        // =====================
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Estimate)
                .WithMany()
                .HasForeignKey(e => e.EstimateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.BalanceDue);
            entity.Ignore(e => e.IsPaid);
            entity.Ignore(e => e.IsOverdue);
            entity.Ignore(e => e.StatusDisplay);
        });

        // =====================
        // Payment configuration
        // =====================
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.PaymentDate);
            entity.HasIndex(e => e.TransactionId);

            entity.Ignore(e => e.MethodDisplay);
            entity.Ignore(e => e.Description);
        });

        // =====================
        // Product configuration
        // =====================
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductNumber).IsUnique();
            entity.HasIndex(e => e.Manufacturer);
            entity.HasIndex(e => e.ModelNumber);
            entity.HasIndex(e => new { e.Manufacturer, e.ModelNumber }).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.EquipmentType);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsDiscontinued);

            entity.HasOne(e => e.ReplacementProduct)
                .WithMany()
                .HasForeignKey(e => e.ReplacementProductId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflict in SQL Server

            entity.HasMany(e => e.Documents)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore computed properties
            entity.Ignore(e => e.Tonnage);
            entity.Ignore(e => e.DisplayName);
            entity.Ignore(e => e.FullDisplayName);
            entity.Ignore(e => e.CategoryDisplay);
            entity.Ignore(e => e.CapacityDisplay);
            entity.Ignore(e => e.EfficiencyDisplay);
            entity.Ignore(e => e.RefrigerantDisplay);
            entity.Ignore(e => e.ElectricalDisplay);
            entity.Ignore(e => e.DimensionsDisplay);
            entity.Ignore(e => e.WarrantyDisplay);
            entity.Ignore(e => e.ProfitMargin);
            entity.Ignore(e => e.TagList);
        });

        // =====================
        // ProductDocument configuration
        // =====================
        modelBuilder.Entity<ProductDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.DocumentType);

            // Ignore computed properties
            entity.Ignore(e => e.FileSizeDisplay);
            entity.Ignore(e => e.DocumentTypeDisplay);
            entity.Ignore(e => e.IsPdf);
        });

        // =====================
        // ServiceHistory configuration
        // =====================
        modelBuilder.Entity<ServiceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AssetId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.ServiceDate);

            entity.HasOne(e => e.Asset)
                .WithMany()
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Ignore(e => e.ServiceDateDisplay);
        });

        // =====================
        // Photo configurations
        // =====================
        modelBuilder.Entity<JobPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Type);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.TypeDisplay);
        });

        modelBuilder.Entity<AssetPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AssetId);

            entity.HasOne(e => e.Asset)
                .WithMany()
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SitePhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SiteId);

            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================
        // Communication configurations
        // =====================
        modelBuilder.Entity<CommunicationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Type);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Ignore(e => e.TypeDisplay);
            entity.Ignore(e => e.DirectionDisplay);
        });

        modelBuilder.Entity<CustomerDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.FileSizeDisplay);
        });

        modelBuilder.Entity<CustomerNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.IsPinned);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =====================
        // Company configuration
        // =====================
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CompanyType);
            entity.HasIndex(e => e.IsActive);

            entity.HasMany(e => e.Customers)
                .WithOne(c => c.Company)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Assets)
                .WithOne(a => a.Company)
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =====================
        // AssetOwner configuration
        // =====================
        modelBuilder.Entity<AssetOwner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AssetId, e.OwnerId, e.OwnerType });
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Asset)
                .WithMany(a => a.Owners)
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================
        // CustomerCompanyRole configuration
        // =====================
        modelBuilder.Entity<CustomerCompanyRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CustomerId, e.CompanyId });
            entity.HasIndex(e => e.IsPrimaryContact);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.CompanyRoles)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Company)
                .WithMany(c => c.CustomerRoles)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================
        // CustomFieldDefinition configuration
        // =====================
        modelBuilder.Entity<CustomFieldDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.FieldName }).IsUnique();
            entity.HasIndex(e => e.DisplayOrder);
            entity.HasIndex(e => e.IsVisible);

            entity.HasMany(e => e.Choices)
                .WithOne(c => c.FieldDefinition)
                .HasForeignKey(c => c.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================
        // CustomFieldChoice configuration
        // =====================
        modelBuilder.Entity<CustomFieldChoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FieldDefinitionId, e.DisplayOrder });
            entity.HasIndex(e => e.IsActive);
        });

        // =====================
        // WarrantyClaim configuration
        // =====================
        modelBuilder.Entity<WarrantyClaim>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AssetId);
            entity.HasIndex(e => e.ClaimNumber);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ClaimDate);
            entity.HasIndex(e => e.IsCoveredByWarranty);

            entity.HasOne(e => e.Asset)
                .WithMany()
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =====================
        // MaterialList configuration
        // =====================
        modelBuilder.Entity<MaterialList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ListNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Site)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Estimate)
                .WithMany()
                .HasForeignKey(e => e.EstimateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Systems)
                .WithOne(s => s.MaterialList)
                .HasForeignKey(s => s.MaterialListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.MaterialList)
                .HasForeignKey(i => i.MaterialListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.IsFinalized);
            entity.Ignore(e => e.HasMultipleSystems);
            entity.Ignore(e => e.TotalItemCount);
            entity.Ignore(e => e.StatusDisplay);
        });

        // =====================
        // MaterialListSystem configuration
        // =====================
        modelBuilder.Entity<MaterialListSystem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MaterialListId);
            entity.HasIndex(e => e.SortOrder);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.MaterialListSystem)
                .HasForeignKey(i => i.MaterialListSystemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Ignore(e => e.SystemTypeDisplay);
            entity.Ignore(e => e.DuctSystemDisplay);
            entity.Ignore(e => e.TotalCost);
            entity.Ignore(e => e.ItemCount);
        });

        // =====================
        // MaterialListItem configuration
        // =====================
        modelBuilder.Entity<MaterialListItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MaterialListId);
            entity.HasIndex(e => e.MaterialListSystemId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.SortOrder);

            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Ignore(e => e.TotalCost);
            entity.Ignore(e => e.CategoryDisplay);
            entity.Ignore(e => e.DisplayName);
            entity.Ignore(e => e.QuantityDisplay);
        });

        // =====================
        // MaterialListTemplate configuration
        // =====================
        modelBuilder.Entity<MaterialListTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.SystemType);
            entity.HasIndex(e => e.IsBuiltIn);
            entity.HasIndex(e => e.IsActive);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.Template)
                .HasForeignKey(i => i.MaterialListTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.ItemCount);
            entity.Ignore(e => e.SystemTypeDisplay);
        });

        // =====================
        // MaterialListTemplateItem configuration
        // =====================
        modelBuilder.Entity<MaterialListTemplateItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MaterialListTemplateId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.SortOrder);

            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Ignore(e => e.DisplayName);
        });
    }
}


