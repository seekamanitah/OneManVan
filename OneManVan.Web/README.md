# OneManVan.Web - TradeFlow Web Application

## Overview

A modern web-based user interface for the TradeFlow (OneManVan) field service management application. Built with ASP.NET Core Blazor Server, it provides full access to all features through any web browser.

## Features

### Core Functionality
- **Dashboard** - Real-time overview of key metrics, today's jobs, recent invoices, and low stock alerts
- **Customer Management** - Complete customer database with search and filtering
- **Job Management** - Job scheduling, tracking, and status management
- **Asset Tracking** - HVAC equipment tracking with service history
- **Product Catalog** - Equipment specifications and compatibility information
- **Inventory Management** - Parts and materials tracking with stock alerts
- **Invoicing** - Invoice generation with payment tracking
- **Estimates** - Create and manage customer quotes
- **Calendar** - Day/week/month views for job scheduling

### Technical Features
- Fully integrated with shared OneManVan.Shared library
- Real-time server-side rendering with Blazor Server
- Responsive Bootstrap UI design
- SQLite database (compatible with SQL Server)
- Automatic database initialization
- Search and filtering capabilities
- Status badges and visual indicators

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- A web browser (Chrome, Firefox, Edge, Safari)

### Running the Application

1. Navigate to the web project directory:
   ```bash
   cd OneManVan.Web
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser and navigate to:
   - HTTPS: https://localhost:5001
   - HTTP: http://localhost:5000

### Configuration

The application can be configured through `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=onemanvan.db"
  }
}
```

To use SQL Server instead of SQLite, update the connection string and change the database provider in `Program.cs`.

## Database

The application shares the same database schema as the desktop and mobile applications through the `OneManVanDbContext` from OneManVan.Shared. The database is automatically created on first run using Entity Framework Core.

### Shared Models
All data models are defined in the OneManVan.Shared project:
- Customer, Company, Site
- Job, TimeEntry, JobPart
- Invoice, Payment, Estimate
- Asset, Product, InventoryItem
- ServiceAgreement
- Custom field definitions

## Architecture

```
OneManVan.Web
├── Components
│   ├── Layout          # Navigation and main layout
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── Pages           # Feature pages
│       ├── Home.razor          # Dashboard
│       ├── Customers.razor     # Customer list
│       ├── Jobs.razor          # Job management
│       ├── Invoices.razor      # Invoice list
│       ├── Estimates.razor     # Estimate list
│       ├── Assets.razor        # Asset tracking
│       ├── Products.razor      # Product catalog
│       ├── Inventory.razor     # Inventory management
│       └── Calendar.razor      # Job calendar
├── wwwroot             # Static files and CSS
├── Program.cs          # Application startup
└── appsettings.json    # Configuration
```

## Integration with Desktop and Mobile Apps

OneManVan.Web is part of the TradeFlow ecosystem:

1. **Shared Library**: All three applications (Desktop WPF, Mobile MAUI, Web Blazor) reference the same OneManVan.Shared project
2. **Common Database**: All applications can work with the same database file
3. **Consistent Models**: Business logic and data models are identical across platforms
4. **Service Layer**: Validation and business rules are centralized in the shared library

## Future Enhancements

Planned features for future releases:
- [ ] Entity detail/edit pages with full CRUD operations
- [ ] Dark mode support
- [ ] Authentication and multi-user support
- [ ] Custom field schema editor
- [ ] Export/import functionality (CSV, Excel)
- [ ] PDF generation for invoices and estimates
- [ ] Advanced search and filtering
- [ ] Real-time updates with SignalR
- [ ] Mobile-responsive improvements
- [ ] Reporting and analytics
- [ ] Email integration
- [ ] Payment gateway integration

## Development

### Building the Project
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Adding to Solution
The project is already added to the OneManVan.sln solution file.

## Technology Stack

- **Framework**: .NET 10.0
- **UI Framework**: Blazor Server
- **CSS Framework**: Bootstrap 5
- **ORM**: Entity Framework Core 9.0
- **Database**: SQLite (configurable for SQL Server)
- **Icons**: Bootstrap Icons

## Support

For issues, questions, or contributions, please refer to the main OneManVan repository.

## License

Same license as the main OneManVan project.
