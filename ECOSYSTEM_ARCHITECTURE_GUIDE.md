# ?? OneManVan Multi-Platform Ecosystem Architecture Guide

**Date:** 2025-01-23  
**Version:** 1.0  
**Status:** ?? COMPREHENSIVE PLANNING DOCUMENT  
**Goal:** Docker-deployable server + Web UI + Desktop/Mobile full sync ecosystem  

---

## ?? Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State vs. Future State](#current-state-vs-future-state)
3. [Architecture Overview](#architecture-overview)
4. [Project Structure](#project-structure)
5. [Implementation Phases](#implementation-phases)
6. [Technical Deep Dive](#technical-deep-dive)
7. [Docker Configuration](#docker-configuration)
8. [Security Architecture](#security-architecture)
9. [Sync System Design](#sync-system-design)
10. [API Specification](#api-specification)
11. [Web UI Design](#web-ui-design)
12. [Desktop Updates](#desktop-updates)
13. [Mobile Updates](#mobile-updates)
14. [Deployment Guide](#deployment-guide)
15. [Testing Strategy](#testing-strategy)
16. [Performance Considerations](#performance-considerations)
17. [Timeline & Resources](#timeline--resources)
18. [Decision Matrix](#decision-matrix)
19. [Risk Assessment](#risk-assessment)
20. [Next Steps](#next-steps)

---

## ?? Executive Summary

### Vision

Transform the current Desktop WPF + Mobile MAUI solution into a fully synchronized, cloud-ready ecosystem with:

- **Central API Server** running in Docker
- **Web-based UI** accessible from any browser
- **Desktop WPF** app with online/offline sync
- **Mobile MAUI** app with background sync
- **PostgreSQL** database as source of truth
- **Real-time sync** across all platforms

### Business Value

? **Accessibility** - Access from any device, anywhere  
? **Scalability** - Docker makes scaling easy  
? **Reliability** - Central database with backups  
? **Flexibility** - Work offline, sync when online  
? **Cost-effective** - Self-hosted on your server  
? **Modern** - Cloud-native architecture  

### High-Level Timeline

- **MVP (Basic Sync):** 4-6 weeks
- **Full Implementation:** 11-17 weeks (3-4 months)
- **Production Deployment:** +1 week

---

## ?? Current State vs. Future State

### Current Architecture

```
???????????????????         ???????????????????
?  Desktop WPF    ?         ?  Mobile MAUI    ?
?                 ?         ?                 ?
?  ?????????????  ?         ?  ?????????????  ?
?  ?  SQLite   ?  ?         ?  ?  SQLite   ?  ?
?  ?  Local DB ?  ?         ?  ?  Local DB ?  ?
?  ?????????????  ?         ?  ?????????????  ?
?                 ?         ?                 ?
?  Independent    ?         ?  Independent    ?
?  No Sync        ?         ?  No Sync        ?
???????????????????         ???????????????????

? No data synchronization
? No web access
? No centralized database
? No multi-device support
```

### Future Architecture

```
                    ????????????????????????????????????
                    ?      Docker Host (Server)        ?
                    ?                                  ?
                    ?  ??????????????????????????????  ?
                    ?  ?   ASP.NET Core Web API     ?  ?
                    ?  ?   (Port 5000/5001)         ?  ?
                    ?  ??????????????????????????????  ?
                    ?             ?                    ?
                    ?  ??????????????????????????????  ?
                    ?  ?   PostgreSQL Database      ?  ?
                    ?  ?   (Central Source)         ?  ?
                    ?  ??????????????????????????????  ?
                    ?                                  ?
                    ?  ??????????????????????????????  ?
                    ?  ?   Blazor Server Web UI     ?  ?
                    ?  ?   (Port 8080)              ?  ?
                    ?  ??????????????????????????????  ?
                    ?                                  ?
                    ?  ??????????????????????????????  ?
                    ?  ?   SignalR Hub              ?  ?
                    ?  ?   (Real-time sync)         ?  ?
                    ?  ??????????????????????????????  ?
                    ????????????????????????????????????
                               ?
                               ? REST API + SignalR
                               ?
         ?????????????????????????????????????????????
         ?                     ?                     ?
    ????????????          ????????????         ????????????
    ? Desktop  ?          ?  Mobile  ?         ?   Web    ?
    ?   WPF    ?          ?   MAUI   ?         ? Browser  ?
    ?          ?          ?          ?         ?          ?
    ? SQLite   ?          ? SQLite   ?         ?   N/A    ?
    ? (Cache)  ?          ? (Cache)  ?         ? (Server) ?
    ????????????          ????????????         ????????????

? Real-time synchronization
? Web access from anywhere
? Central PostgreSQL database
? Multi-device support
? Offline capability (Desktop/Mobile)
? Docker containerization
```

---

## ??? Architecture Overview

### System Components

```
??????????????????????????????????????????????????????????????????????
?                        OneManVan Ecosystem                         ?
??????????????????????????????????????????????????????????????????????
?                                                                    ?
?  ???????????????????????????????????????????????????????????????? ?
?  ?                     Presentation Layer                        ? ?
?  ???????????????????????????????????????????????????????????????? ?
?  ?                                                              ? ?
?  ?  ????????????    ????????????    ????????????????????????  ? ?
?  ?  ? Desktop  ?    ?  Mobile  ?    ?    Web Browser       ?  ? ?
?  ?  ?   WPF    ?    ?   MAUI   ?    ?   (Blazor Server)    ?  ? ?
?  ?  ? Windows  ?    ? iOS/Droid?    ?   Any Platform       ?  ? ?
?  ?  ????????????    ????????????    ????????????????????????  ? ?
?  ?       ?               ?                     ?              ? ?
?  ?????????????????????????????????????????????????????????????? ?
?          ?               ?                     ?                ?
?  ?????????????????????????????????????????????????????????????? ?
?  ?       ?               ?   API Layer         ?              ? ?
?  ?       ?               ?                     ?              ? ?
?  ?       ???????????????????????????????????????              ? ?
?  ?                           ?                                ? ?
?  ?                    ???????????????                         ? ?
?  ?                    ?             ?                         ? ?
?  ?                    ?  ASP.NET    ?                         ? ?
?  ?                    ?  Core API   ?                         ? ?
?  ?                    ?             ?                         ? ?
?  ?                    ?  • REST     ?                         ? ?
?  ?                    ?  • SignalR  ?                         ? ?
?  ?                    ?  • JWT Auth ?                         ? ?
?  ?                    ???????????????                         ? ?
?  ?????????????????????????????????????????????????????????????? ?
?                              ?                                  ?
?  ?????????????????????????????????????????????????????????????? ?
?  ?                     Data Layer                             ? ?
?  ?                              ?                             ? ?
?  ?                    ?????????????????????                  ? ?
?  ?                    ?                   ?                  ? ?
?  ?                    ?   PostgreSQL      ?                  ? ?
?  ?                    ?   Database        ?                  ? ?
?  ?                    ?                   ?                  ? ?
?  ?                    ?   • Customers     ?                  ? ?
?  ?                    ?   • Assets        ?                  ? ?
?  ?                    ?   • Jobs          ?                  ? ?
?  ?                    ?   • Invoices      ?                  ? ?
?  ?                    ?   • etc.          ?                  ? ?
?  ?                    ?????????????????????                  ? ?
?  ?????????????????????????????????????????????????????????????? ?
?                                                                  ?
????????????????????????????????????????????????????????????????????
```

---

## ?? Project Structure

### Proposed Solution Layout

```
OneManVan.sln
??? OneManVan.Shared/                    ? EXISTS
?   ??? Models/                          # Domain models
?   ?   ??? Customer.cs
?   ?   ??? Asset.cs
?   ?   ??? Job.cs
?   ?   ??? Invoice.cs
?   ?   ??? ...
?   ??? Models/Enums/                    # Enumerations
?   ??? Data/                            # DbContext
?   ?   ??? OneManVanDbContext.cs
?   ??? DTOs/                            # Data transfer objects
?       ??? CustomerDto.cs
?       ??? AssetDto.cs
?       ??? ...
?
??? OneManVan.Desktop/                   ? EXISTS (Renamed from OneManVan)
?   ??? Pages/                           # WPF pages
?   ??? Services/                        # Desktop-specific services
?   ??? ViewModels/                      # MVVM view models
?   ??? App.xaml
?
??? OneManVan.Mobile/                    ? EXISTS
?   ??? Pages/                           # MAUI pages
?   ??? Services/                        # Mobile-specific services
?   ??? Controls/                        # Custom controls
?   ??? App.xaml
?
??? OneManVan.Api/                       ?? NEW - Central API Server
?   ??? Controllers/
?   ?   ??? AuthController.cs           # Authentication
?   ?   ??? CustomersController.cs      # Customer CRUD
?   ?   ??? AssetsController.cs         # Asset CRUD
?   ?   ??? JobsController.cs           # Job CRUD
?   ?   ??? InvoicesController.cs       # Invoice CRUD
?   ?   ??? EstimatesController.cs      # Estimate CRUD
?   ?   ??? InventoryController.cs      # Inventory CRUD
?   ?   ??? ProductsController.cs       # Products CRUD
?   ?   ??? ServiceAgreementsController.cs
?   ?   ??? SitesController.cs
?   ?   ??? SyncController.cs           # Sync operations
?   ?
?   ??? Hubs/
?   ?   ??? SyncHub.cs                  # SignalR real-time hub
?   ?
?   ??? Services/
?   ?   ??? AuthService.cs              # Authentication logic
?   ?   ??? TokenService.cs             # JWT token management
?   ?   ??? SyncService.cs              # Sync coordination
?   ?   ??? ConflictResolver.cs         # Handle sync conflicts
?   ?
?   ??? Middleware/
?   ?   ??? ErrorHandlingMiddleware.cs
?   ?   ??? RateLimitingMiddleware.cs
?   ?
?   ??? Data/
?   ?   ??? ApiDbContext.cs             # API-specific DbContext config
?   ?
?   ??? Program.cs                      # API startup
?   ??? appsettings.json                # Configuration
?   ??? appsettings.Production.json
?   ??? Dockerfile                      # Container definition
?
??? OneManVan.Web/                       ?? NEW - Blazor Web UI
?   ??? Pages/
?   ?   ??? Index.razor                 # Dashboard
?   ?   ??? Login.razor                 # Login page
?   ?   ??? Customers/
?   ?   ?   ??? CustomerList.razor
?   ?   ?   ??? CustomerDetail.razor
?   ?   ?   ??? CustomerEdit.razor
?   ?   ??? Assets/
?   ?   ?   ??? AssetList.razor
?   ?   ?   ??? AssetDetail.razor
?   ?   ?   ??? AssetEdit.razor
?   ?   ??? Jobs/
?   ?   ?   ??? JobList.razor
?   ?   ?   ??? JobCalendar.razor
?   ?   ?   ??? JobKanban.razor
?   ?   ?   ??? JobDetail.razor
?   ?   ??? Invoices/
?   ?   ?   ??? InvoiceList.razor
?   ?   ?   ??? InvoiceDetail.razor
?   ?   ?   ??? InvoicePdf.razor
?   ?   ??? Estimates/
?   ?   ??? Inventory/
?   ?   ??? Products/
?   ?   ??? ServiceAgreements/
?   ?   ??? Settings/
?   ?       ??? SettingsPage.razor
?   ?
?   ??? Components/
?   ?   ??? Layout/
?   ?   ?   ??? MainLayout.razor
?   ?   ?   ??? NavMenu.razor
?   ?   ?   ??? TopBar.razor
?   ?   ??? Forms/
?   ?   ?   ??? CustomerForm.razor
?   ?   ?   ??? AssetForm.razor
?   ?   ?   ??? JobForm.razor
?   ?   ??? Shared/
?   ?   ?   ??? LoadingIndicator.razor
?   ?   ?   ??? DataGrid.razor
?   ?   ?   ??? SearchBox.razor
?   ?   ?   ??? ConfirmDialog.razor
?   ?   ??? Charts/
?   ?       ??? RevenueChart.razor
?   ?       ??? JobStatusChart.razor
?   ?
?   ??? Services/
?   ?   ??? ApiClient.cs                # API communication
?   ?   ??? AuthService.cs              # Auth state management
?   ?   ??? SignalRService.cs           # Real-time updates
?   ?
?   ??? wwwroot/
?   ?   ??? css/
?   ?   ?   ??? app.css
?   ?   ??? js/
?   ?   ?   ??? site.js
?   ?   ??? favicon.ico
?   ?
?   ??? Program.cs                      # Web app startup
?   ??? appsettings.json
?   ??? Dockerfile                      # Container definition
?
??? OneManVan.Sync/                      ?? NEW - Sync Library
?   ??? Interfaces/
?   ?   ??? ISyncable.cs                # Syncable entity marker
?   ?   ??? ISyncService.cs             # Sync service interface
?   ?   ??? IConflictResolver.cs        # Conflict resolution
?   ?
?   ??? Models/
?   ?   ??? SyncMetadata.cs             # Sync tracking data
?   ?   ??? SyncChange.cs               # Change record
?   ?   ??? SyncResult.cs               # Sync operation result
?   ?   ??? SyncConflict.cs             # Conflict details
?   ?   ??? DeviceInfo.cs               # Device identification
?   ?
?   ??? Services/
?   ?   ??? SyncEngine.cs               # Core sync logic
?   ?   ??? ChangeTracker.cs            # Track local changes
?   ?   ??? ConflictResolver.cs         # Resolve conflicts
?   ?   ??? OfflineQueue.cs             # Queue for offline changes
?   ?   ??? MergeEngine.cs              # Merge strategies
?   ?
?   ??? Strategies/
?   ?   ??? LastWriteWinsStrategy.cs
?   ?   ??? ServerWinsStrategy.cs
?   ?   ??? ClientWinsStrategy.cs
?   ?   ??? ManualMergeStrategy.cs
?   ?
?   ??? Extensions/
?       ??? SyncableExtensions.cs
?       ??? EntityExtensions.cs
?
??? OneManVan.Api.Tests/                 ?? NEW - API Tests
?   ??? Controllers/
?   ??? Integration/
?   ??? Unit/
?
??? OneManVan.Web.Tests/                 ?? NEW - Web Tests
?   ??? bUnit/
?
??? docker-compose.yml                   ?? NEW - Multi-container orchestration
??? docker-compose.override.yml          ?? Development overrides
??? docker-compose.prod.yml              ?? Production config
??? nginx.conf                           ?? Reverse proxy config
??? .dockerignore                        ?? Docker ignore patterns
??? README.md                            ?? Updated documentation
```

---

## ?? Implementation Phases

### Phase 1: API Foundation (2-3 weeks)

**Goal:** Create a fully functional REST API with PostgreSQL backend

#### Week 1: Project Setup & Basic CRUD

**Tasks:**
- [ ] Create `OneManVan.Api` project
  ```bash
  dotnet new webapi -n OneManVan.Api
  dotnet sln add OneManVan.Api
  ```
- [ ] Install required packages:
  ```bash
  dotnet add OneManVan.Api package Npgsql.EntityFrameworkCore.PostgreSQL
  dotnet add OneManVan.Api package Microsoft.EntityFrameworkCore.Design
  dotnet add OneManVan.Api package Swashbuckle.AspNetCore
  dotnet add OneManVan.Api package Serilog.AspNetCore
  ```
- [ ] Configure PostgreSQL connection in `appsettings.json`
- [ ] Update `OneManVan.Shared/Data/OneManVanDbContext.cs` to support PostgreSQL
- [ ] Create database migration:
  ```bash
  dotnet ef migrations add InitialCreate --project OneManVan.Api
  ```
- [ ] Create `CustomersController` with full CRUD
- [ ] Create `AssetsController` with full CRUD
- [ ] Test endpoints with Postman/Swagger

#### Week 2: Complete All Entity Controllers

**Tasks:**
- [ ] Create `JobsController`
- [ ] Create `InvoicesController`
- [ ] Create `EstimatesController`
- [ ] Create `InventoryController`
- [ ] Create `ProductsController`
- [ ] Create `ServiceAgreementsController`
- [ ] Create `SitesController`
- [ ] Add filtering, sorting, pagination to all endpoints
- [ ] Add validation using FluentValidation
- [ ] Configure Swagger/OpenAPI documentation

#### Week 3: Docker & Testing

**Tasks:**
- [ ] Create `Dockerfile` for API
- [ ] Create `docker-compose.yml` with PostgreSQL
- [ ] Test API in Docker container
- [ ] Set up Serilog logging
- [ ] Add health check endpoint
- [ ] Create API documentation
- [ ] Write unit tests for controllers

**Deliverables:**
- ? Working REST API with all CRUD operations
- ? PostgreSQL database running in Docker
- ? Swagger documentation
- ? Docker container for API
- ? Basic docker-compose setup

---

### Phase 2: Authentication & Security (1-2 weeks)

**Goal:** Implement secure multi-device authentication

#### Week 1: JWT Authentication

**Tasks:**
- [ ] Install authentication packages:
  ```bash
  dotnet add OneManVan.Api package Microsoft.AspNetCore.Authentication.JwtBearer
  dotnet add OneManVan.Api package Microsoft.AspNetCore.Identity.EntityFrameworkCore
  ```
- [ ] Add `User` and `Device` tables to database
- [ ] Create `AuthController` with:
  - `POST /api/auth/register` - User registration
  - `POST /api/auth/login` - Login with credentials
  - `POST /api/auth/refresh` - Refresh access token
  - `POST /api/auth/logout` - Logout
  - `POST /api/auth/device` - Register device
- [ ] Implement JWT token generation with claims
- [ ] Add refresh token storage and rotation
- [ ] Configure authentication middleware
- [ ] Add `[Authorize]` attribute to all controllers

#### Week 2: Security Hardening

**Tasks:**
- [ ] Implement rate limiting
- [ ] Add CORS policy configuration
- [ ] Set up HTTPS with certificates
- [ ] Add request validation middleware
- [ ] Implement password hashing (bcrypt/Argon2)
- [ ] Add audit logging for sensitive operations
- [ ] Configure API keys for service-to-service calls
- [ ] Add security headers (HSTS, CSP, etc.)

**Deliverables:**
- ? JWT authentication working
- ? User registration and login
- ? Device token management
- ? Secure HTTPS endpoints
- ? Rate limiting in place

---

### Phase 3: Sync System (2-3 weeks)

**Goal:** Enable real-time bidirectional sync between all clients

#### Week 1: Sync Foundation

**Tasks:**
- [ ] Create `OneManVan.Sync` project
- [ ] Install SignalR packages:
  ```bash
  dotnet add OneManVan.Api package Microsoft.AspNetCore.SignalR
  ```
- [ ] Add sync metadata to all entities:
  ```csharp
  public interface ISyncable
  {
      Guid SyncId { get; set; }
      DateTime ModifiedAt { get; set; }
      string ModifiedBy { get; set; }
      int SyncVersion { get; set; }
      bool IsDeleted { get; set; }
  }
  ```
- [ ] Update all models to implement `ISyncable`
- [ ] Create database migration for sync columns
- [ ] Create `SyncMetadata` table for tracking
- [ ] Create `ChangeLog` table for changes

#### Week 2: Sync Logic

**Tasks:**
- [ ] Create `SyncHub` (SignalR hub)
- [ ] Create `SyncController` with:
  - `GET /api/sync/changes` - Get changes since timestamp
  - `POST /api/sync/push` - Push local changes
  - `POST /api/sync/pull` - Pull remote changes
  - `POST /api/sync/resolve` - Resolve conflicts
- [ ] Implement `ChangeTracker` service
- [ ] Implement conflict detection
- [ ] Implement conflict resolution strategies:
  - Last-write-wins
  - Server-wins
  - Client-wins
  - Manual merge
- [ ] Add SignalR notification on entity changes

#### Week 3: Testing & Refinement

**Tasks:**
- [ ] Create sync test scenarios
- [ ] Test Desktop ? Server sync
- [ ] Test Mobile ? Server sync
- [ ] Test Desktop ? Mobile via Server
- [ ] Test conflict resolution
- [ ] Test offline queue
- [ ] Performance testing with large datasets
- [ ] Optimize sync payload size

**Deliverables:**
- ? Sync library with core logic
- ? SignalR hub for real-time updates
- ? Sync API endpoints
- ? Conflict resolution working
- ? Change tracking in place

---

### Phase 4: Web UI (Blazor) (3-4 weeks)

**Goal:** Build a modern web interface accessible from any browser

#### Week 1: Project Setup & Layout

**Tasks:**
- [ ] Create `OneManVan.Web` project:
  ```bash
  dotnet new blazorserver -n OneManVan.Web
  dotnet sln add OneManVan.Web
  ```
- [ ] Install required packages:
  ```bash
  dotnet add OneManVan.Web package Microsoft.AspNetCore.SignalR.Client
  ```
- [ ] Reference `OneManVan.Shared` project
- [ ] Create `ApiClient` service for API calls
- [ ] Design main layout (responsive)
- [ ] Create navigation menu
- [ ] Create login page
- [ ] Implement authentication state management

#### Week 2: Core Pages - Customers & Assets

**Tasks:**
- [ ] Create `CustomerList.razor` page
- [ ] Create `CustomerDetail.razor` page
- [ ] Create `CustomerEdit.razor` page with validation
- [ ] Create `CustomerForm` component (reusable)
- [ ] Create `AssetList.razor` page
- [ ] Create `AssetDetail.razor` page
- [ ] Create `AssetEdit.razor` page
- [ ] Create `AssetForm` component
- [ ] Add search and filter functionality
- [ ] Add pagination

#### Week 3: Jobs, Invoices & More

**Tasks:**
- [ ] Create `JobList.razor` with filters
- [ ] Create `JobCalendar.razor` (calendar view)
- [ ] Create `JobKanban.razor` (kanban board)
- [ ] Create `JobDetail.razor` page
- [ ] Create `InvoiceList.razor` page
- [ ] Create `InvoiceDetail.razor` page
- [ ] Create PDF preview component
- [ ] Create `EstimateList.razor` and detail pages
- [ ] Create `InventoryList.razor` and management pages

#### Week 4: Dashboard & Polish

**Tasks:**
- [ ] Create dashboard with KPIs:
  - Revenue charts
  - Job status breakdown
  - Upcoming jobs
  - Overdue invoices
- [ ] Create settings page
- [ ] Add real-time updates via SignalR
- [ ] Implement error handling and user feedback
- [ ] Add loading states
- [ ] Responsive design testing
- [ ] Cross-browser testing
- [ ] Create `Dockerfile` for Web

**Deliverables:**
- ? Working Blazor web application
- ? All main pages implemented
- ? Authentication and authorization
- ? Real-time updates
- ? Responsive design
- ? Docker container

---

### Phase 5: Desktop Client Updates (1-2 weeks)

**Goal:** Connect Desktop WPF app to central API with sync

#### Week 1: API Integration

**Tasks:**
- [ ] Create `ApiClient` service in Desktop
- [ ] Install HTTP client packages:
  ```bash
  dotnet add OneManVan.Desktop package Microsoft.AspNetCore.SignalR.Client
  ```
- [ ] Implement token storage (secure)
- [ ] Create login dialog
- [ ] Update all ViewModels to use API when online
- [ ] Maintain SQLite for offline mode
- [ ] Add connection status indicator in UI
- [ ] Implement automatic reconnection

#### Week 2: Sync Integration

**Tasks:**
- [ ] Reference `OneManVan.Sync` library
- [ ] Implement `ChangeTracker` for local changes
- [ ] Create offline queue for changes
- [ ] Implement background sync service
- [ ] Add manual sync trigger button
- [ ] Show sync status in UI
- [ ] Handle online/offline transitions
- [ ] Test full sync cycle

**Deliverables:**
- ? Desktop connects to API
- ? Offline mode with local SQLite
- ? Automatic sync when online
- ? Connection status UI
- ? Conflict resolution UI

---

### Phase 6: Mobile Client Updates (1-2 weeks)

**Goal:** Connect Mobile MAUI app to central API with background sync

#### Week 1: API Integration

**Tasks:**
- [ ] Create `ApiClient` service in Mobile
- [ ] Use existing `Preferences` for token storage
- [ ] Create login page (if not exists)
- [ ] Update all pages to use API when online
- [ ] Maintain SQLite for offline mode
- [ ] Add connection status indicator
- [ ] Handle poor network conditions (retry logic)

#### Week 2: Background Sync

**Tasks:**
- [ ] Reference `OneManVan.Sync` library
- [ ] Implement background sync service
- [ ] Configure background tasks (iOS/Android)
- [ ] Add push notifications for sync events
- [ ] Test sync in background
- [ ] Test with airplane mode
- [ ] Optimize battery usage
- [ ] Test on real devices

**Deliverables:**
- ? Mobile connects to API
- ? Background sync working
- ? Offline mode functional
- ? Push notifications
- ? Battery-optimized

---

### Phase 7: Production Deployment (1 week)

**Goal:** Deploy to local server with full production setup

#### Day 1-2: Server Setup

**Tasks:**
- [ ] Install Docker on local server
- [ ] Configure firewall rules
- [ ] Set up DNS or hosts file
- [ ] Generate SSL certificates (Let's Encrypt or self-signed)
- [ ] Create production `docker-compose.prod.yml`
- [ ] Configure environment variables

#### Day 3-4: Deployment

**Tasks:**
- [ ] Deploy containers with docker-compose
- [ ] Configure nginx reverse proxy
- [ ] Set up SSL termination
- [ ] Test all endpoints
- [ ] Configure automatic backups
- [ ] Set up log aggregation
- [ ] Configure monitoring (optional)

#### Day 5-7: Testing & Documentation

**Tasks:**
- [ ] End-to-end testing
- [ ] Performance testing
- [ ] Security audit
- [ ] Create deployment documentation
- [ ] Create user guide
- [ ] Train users
- [ ] Monitor for issues

**Deliverables:**
- ? Running on production server
- ? Accessible via HTTPS
- ? Backups configured
- ? Monitoring in place
- ? Documentation complete

---

## ?? Technical Deep Dive

### Database Strategy

#### Server Database (PostgreSQL)

**Why PostgreSQL?**
- ? Robust and reliable
- ? Excellent JSON support (for custom fields)
- ? Full ACID compliance
- ? Great performance
- ? Docker-ready with official images
- ? Open source, no licensing costs

**Configuration:**
```csharp
// OneManVan.Api/Program.cs
builder.Services.AddDbContext<OneManVanDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    ));
```

**Connection String:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Database=onemanvan;Username=onemanvan;Password=${DB_PASSWORD}"
  }
}
```

#### Client Database (SQLite)

**Why Keep SQLite on Clients?**
- ? Offline capability
- ? Fast local queries
- ? Small footprint
- ? No network latency
- ? Already working

**Sync Strategy:**
- SQLite acts as local cache
- Changes tracked in local `ChangeLog` table
- Sync pushes changes to server
- Server sends back consolidated changes
- Client applies remote changes to SQLite

---

### API Design Principles

#### RESTful Conventions

```
GET    /api/customers              # List all customers
GET    /api/customers/{id}         # Get specific customer
POST   /api/customers              # Create new customer
PUT    /api/customers/{id}         # Update customer
DELETE /api/customers/{id}         # Delete customer
PATCH  /api/customers/{id}         # Partial update

# Query parameters for filtering/paging
GET    /api/customers?search=john&page=1&pageSize=20&sortBy=name
```

#### Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly OneManVanDbContext _context;
    private readonly ILogger<CustomersController> _logger;
    private readonly IHubContext<SyncHub> _hubContext;

    public CustomersController(
        OneManVanDbContext context,
        ILogger<CustomersController> logger,
        IHubContext<SyncHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Get all customers with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetCustomers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? modifiedSince = null)
    {
        var query = _context.Customers
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => 
                c.Name.Contains(search) || 
                c.CompanyName.Contains(search) ||
                c.Email.Contains(search));
        }

        if (modifiedSince.HasValue)
        {
            query = query.Where(c => c.ModifiedAt > modifiedSince.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.ModifiedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.ToDto())
            .ToListAsync();

        return Ok(new PagedResult<CustomerDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get a specific customer by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Sites)
            .Include(c => c.Assets)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (customer == null)
            return NotFound();

        return Ok(customer.ToDto());
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var customer = dto.ToEntity();
        customer.SyncId = Guid.NewGuid();
        customer.ModifiedAt = DateTime.UtcNow;
        customer.ModifiedBy = User.Identity?.Name ?? "api";
        customer.SyncVersion = 1;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Notify all connected clients via SignalR
        await _hubContext.Clients.All.SendAsync(
            "EntityChanged",
            "Customer",
            customer.SyncId,
            "Created");

        _logger.LogInformation(
            "Customer {CustomerId} created by {User}",
            customer.Id,
            customer.ModifiedBy);

        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = customer.Id },
            customer.ToDto());
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(
        int id,
        UpdateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null || customer.IsDeleted)
            return NotFound();

        // Optimistic concurrency check
        if (dto.SyncVersion != customer.SyncVersion)
        {
            return Conflict(new
            {
                Message = "Customer has been modified by another user",
                CurrentVersion = customer.SyncVersion,
                YourVersion = dto.SyncVersion
            });
        }

        // Update properties
        customer.Name = dto.Name;
        customer.CompanyName = dto.CompanyName;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        // ... other properties

        customer.ModifiedAt = DateTime.UtcNow;
        customer.ModifiedBy = User.Identity?.Name ?? "api";
        customer.SyncVersion++;

        await _context.SaveChangesAsync();

        // Notify clients
        await _hubContext.Clients.All.SendAsync(
            "EntityChanged",
            "Customer",
            customer.SyncId,
            "Updated");

        return Ok(customer.ToDto());
    }

    /// <summary>
    /// Delete a customer (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null || customer.IsDeleted)
            return NotFound();

        // Soft delete
        customer.IsDeleted = true;
        customer.ModifiedAt = DateTime.UtcNow;
        customer.ModifiedBy = User.Identity?.Name ?? "api";
        customer.SyncVersion++;

        await _context.SaveChangesAsync();

        // Notify clients
        await _hubContext.Clients.All.SendAsync(
            "EntityChanged",
            "Customer",
            customer.SyncId,
            "Deleted");

        return NoContent();
    }
}
```

---

### DTOs (Data Transfer Objects)

```csharp
// OneManVan.Shared/DTOs/CustomerDto.cs
public record CustomerDto
{
    public int Id { get; init; }
    public Guid SyncId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string CustomerType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public int SyncVersion { get; init; }
    public int SiteCount { get; init; }
    public int AssetCount { get; init; }
}

public record CreateCustomerDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? CompanyName { get; init; }

    [EmailAddress]
    public string? Email { get; init; }

    [Phone]
    public string? Phone { get; init; }

    public CustomerType CustomerType { get; init; }
}

public record UpdateCustomerDto : CreateCustomerDto
{
    [Required]
    public int SyncVersion { get; init; }
}

// Extension methods for mapping
public static class CustomerMappingExtensions
{
    public static CustomerDto ToDto(this Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            SyncId = customer.SyncId,
            Name = customer.Name,
            CompanyName = customer.CompanyName,
            Email = customer.Email,
            Phone = customer.Phone,
            CustomerType = customer.CustomerType.ToString(),
            Status = customer.Status.ToString(),
            CreatedAt = customer.CreatedAt,
            ModifiedAt = customer.ModifiedAt,
            SyncVersion = customer.SyncVersion,
            SiteCount = customer.Sites?.Count ?? 0,
            AssetCount = customer.Assets?.Count ?? 0
        };
    }

    public static Customer ToEntity(this CreateCustomerDto dto)
    {
        return new Customer
        {
            Name = dto.Name,
            CompanyName = dto.CompanyName,
            Email = dto.Email,
            Phone = dto.Phone,
            CustomerType = dto.CustomerType,
            Status = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

---

### Authentication System

#### JWT Token Structure

```csharp
// OneManVan.Api/Services/TokenService.cs
public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResult GenerateTokens(User user, Device device)
    {
        var accessToken = GenerateAccessToken(user, device);
        var refreshToken = GenerateRefreshToken();

        return new TokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900, // 15 minutes
            TokenType = "Bearer"
        };
    }

    private string GenerateAccessToken(User user, Device device)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("device_id", device.Id.ToString()),
            new Claim("device_name", device.Name)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
```

#### Authentication Flow

```
????????????                                  ????????????
?  Client  ?                                  ?   API    ?
????????????                                  ????????????
     ?                                             ?
     ?  1. POST /api/auth/login                    ?
     ?     { username, password, deviceInfo }      ?
     ???????????????????????????????????????????????
     ?                                             ?
     ?                                          2. Validate
     ?                                             Credentials
     ?                                             ?
     ?  3. { accessToken, refreshToken }           ?
     ???????????????????????????????????????????????
     ?                                             ?
     ?  4. Store tokens securely                   ?
     ?     (Desktop: Windows Credential Manager)   ?
     ?     (Mobile: Secure Storage)                ?
     ?                                             ?
     ?  5. API requests with Bearer token          ?
     ?     Authorization: Bearer {accessToken}     ?
     ???????????????????????????????????????????????
     ?                                             ?
     ?  6. Validate token                          ?
     ?                                             ?
     ?  7. Response                                ?
     ???????????????????????????????????????????????
     ?                                             ?
     ?  ... (token expires after 15 min) ...      ?
     ?                                             ?
     ?  8. POST /api/auth/refresh                  ?
     ?     { refreshToken }                        ?
     ???????????????????????????????????????????????
     ?                                             ?
     ?                                          9. Validate
     ?                                             Refresh Token
     ?                                             ?
     ?  10. { accessToken, refreshToken }          ?
     ???????????????????????????????????????????????
     ?                                             ?
     ?  11. Update stored tokens                   ?
     ?                                             ?
```

---

### Sync System Architecture

#### Sync Metadata

```csharp
// OneManVan.Shared/Models/ISyncable.cs
public interface ISyncable
{
    /// <summary>
    /// Global unique identifier for sync across devices
    /// </summary>
    Guid SyncId { get; set; }

    /// <summary>
    /// Last modification timestamp (UTC)
    /// </summary>
    DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Who/what made the last modification
    /// </summary>
    string ModifiedBy { get; set; }

    /// <summary>
    /// Version number for optimistic concurrency
    /// </summary>
    int SyncVersion { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    bool IsDeleted { get; set; }
}

// Update all entities
public class Customer : ISyncable
{
    public int Id { get; set; } // Local database ID
    
    // Sync properties
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string ModifiedBy { get; set; } = "local";
    public int SyncVersion { get; set; } = 1;
    public bool IsDeleted { get; set; } = false;
    
    // Regular properties
    public string Name { get; set; } = string.Empty;
    // ... other properties
}
```

#### Change Tracking

```csharp
// OneManVan.Sync/Models/SyncChange.cs
public class SyncChange
{
    public int Id { get; set; }
    public Guid EntitySyncId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // Created, Updated, Deleted
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? EntityJson { get; set; } // Full entity as JSON
    public bool IsSynced { get; set; }
    public DateTime? SyncedAt { get; set; }
}

// OneManVan.Sync/Services/ChangeTracker.cs
public class ChangeTracker
{
    private readonly DbContext _context;

    public async Task TrackChange<T>(T entity, string changeType) where T : ISyncable
    {
        var change = new SyncChange
        {
            EntitySyncId = entity.SyncId,
            EntityType = typeof(T).Name,
            ChangeType = changeType,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = GetCurrentDevice(),
            EntityJson = JsonSerializer.Serialize(entity),
            IsSynced = false
        };

        await _context.Set<SyncChange>().AddAsync(change);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SyncChange>> GetPendingChangesAsync()
    {
        return await _context.Set<SyncChange>()
            .Where(c => !c.IsSynced)
            .OrderBy(c => c.ChangedAt)
            .ToListAsync();
    }

    public async Task MarkAsSyncedAsync(List<int> changeIds)
    {
        var changes = await _context.Set<SyncChange>()
            .Where(c => changeIds.Contains(c.Id))
            .ToListAsync();

        foreach (var change in changes)
        {
            change.IsSynced = true;
            change.SyncedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private string GetCurrentDevice()
    {
        // Return device identifier
        return $"{Environment.MachineName}_{Guid.NewGuid()}";
    }
}
```

#### Sync Flow

```
???????????????????????????????????????????????????????????????????????
?                          Sync Process                                ?
???????????????????????????????????????????????????????????????????????
?                                                                      ?
?  CLIENT (Desktop/Mobile)                SERVER (API)                 ?
?  ??????????????????????                ??????????????????????       ?
?  ? 1. Get Pending     ?                ?                    ?       ?
?  ?    Changes         ?                ?                    ?       ?
?  ?    (ChangeLog)     ?                ?                    ?       ?
?  ??????????????????????                ?                    ?       ?
?           ?                            ?                    ?       ?
?           ?                            ?                    ?       ?
?  ??????????????????????                ?                    ?       ?
?  ? 2. POST /api/sync/ ?                ?                    ?       ?
?  ?    push            ??????????????????                    ?       ?
?  ?    { changes[] }   ?                ?                    ?       ?
?  ??????????????????????                ?                    ?       ?
?                                        ?                    ?       ?
?                                 ???????????????????         ?       ?
?                                 ? 3. Validate     ?         ?       ?
?                                 ?    Changes      ?         ?       ?
?                                 ???????????????????         ?       ?
?                                        ?                    ?       ?
?                                 ???????????????????         ?       ?
?                                 ? 4. Detect       ?         ?       ?
?                                 ?    Conflicts    ?         ?       ?
?                                 ???????????????????         ?       ?
?                                        ?                    ?       ?
?                            ?????????????????????????        ?       ?
?                            ?                       ?        ?       ?
?                     ???????????????      ????????????????  ?       ?
?                     ? No Conflict ?      ?  Conflict!   ?  ?       ?
?                     ???????????????      ????????????????  ?       ?
?                            ?                      ?        ?       ?
?                     ???????????????      ????????????????  ?       ?
?                     ? 5. Apply to ?      ? 5. Resolve   ?  ?       ?
?                     ?    Database ?      ?    (Strategy)?  ?       ?
?                     ???????????????      ????????????????  ?       ?
?                            ?                      ?        ?       ?
?                            ????????????????????????        ?       ?
?                                       ?                    ?       ?
?  ??????????????????????        ???????????????????         ?       ?
?  ? 6. { success,      ?        ? 6. Notify       ?         ?       ?
?  ?     conflicts[],   ??????????    SignalR Hub  ?         ?       ?
?  ?     serverChanges }?        ???????????????????         ?       ?
?  ??????????????????????                                    ?       ?
?           ?                                                ?       ?
?  ??????????????????????                                    ?       ?
?  ? 7. Apply Server    ?                                    ?       ?
?  ?    Changes to      ?                                    ?       ?
?  ?    Local SQLite    ?                                    ?       ?
?  ??????????????????????                                    ?       ?
?           ?                                                ?       ?
?  ??????????????????????                                    ?       ?
?  ? 8. Update Last     ?                                    ?       ?
?  ?    Sync Timestamp  ?                                    ?       ?
?  ??????????????????????                                    ?       ?
?                                                            ?       ?
???????????????????????????????????????????????????????????????????????
```

#### Conflict Resolution

```csharp
// OneManVan.Sync/Services/ConflictResolver.cs
public class ConflictResolver
{
    public enum ResolutionStrategy
    {
        LastWriteWins,
        ServerWins,
        ClientWins,
        ManualMerge
    }

    public SyncResult ResolveConflict<T>(
        T localVersion,
        T serverVersion,
        ResolutionStrategy strategy) where T : ISyncable
    {
        switch (strategy)
        {
            case ResolutionStrategy.LastWriteWins:
                return ResolveByTimestamp(localVersion, serverVersion);

            case ResolutionStrategy.ServerWins:
                return new SyncResult
                {
                    Success = true,
                    ResolvedEntity = serverVersion,
                    Message = "Server version accepted"
                };

            case ResolutionStrategy.ClientWins:
                return new SyncResult
                {
                    Success = true,
                    ResolvedEntity = localVersion,
                    Message = "Client version accepted"
                };

            case ResolutionStrategy.ManualMerge:
                return new SyncResult
                {
                    Success = false,
                    RequiresManualMerge = true,
                    ConflictDetails = new ConflictDetails
                    {
                        LocalVersion = localVersion,
                        ServerVersion = serverVersion
                    }
                };

            default:
                throw new ArgumentException("Unknown strategy", nameof(strategy));
        }
    }

    private SyncResult ResolveByTimestamp<T>(T local, T server) where T : ISyncable
    {
        var winner = local.ModifiedAt > server.ModifiedAt ? local : server;
        return new SyncResult
        {
            Success = true,
            ResolvedEntity = winner,
            Message = $"Last write wins: {(winner.Equals(local) ? "client" : "server")}"
        };
    }
}
```

---

### SignalR Real-Time Hub

```csharp
// OneManVan.Api/Hubs/SyncHub.cs
[Authorize]
public class SyncHub : Hub
{
    private readonly ILogger<SyncHub> _logger;

    public SyncHub(ILogger<SyncHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var deviceId = Context.User?.FindFirst("device_id")?.Value;

        _logger.LogInformation(
            "User {UserId} connected from device {DeviceId}",
            userId,
            deviceId);

        // Join user-specific group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        _logger.LogInformation(
            "User {UserId} disconnected",
            userId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client notifies server of a change
    /// </summary>
    public async Task NotifyChange(string entityType, Guid syncId, string changeType)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var deviceId = Context.User?.FindFirst("device_id")?.Value;

        // Notify all OTHER devices of the same user
        await Clients.GroupExcept($"user_{userId}", Context.ConnectionId)
            .SendAsync("EntityChanged", entityType, syncId, changeType);

        _logger.LogInformation(
            "Change notification: {EntityType} {SyncId} {ChangeType} by device {DeviceId}",
            entityType,
            syncId,
            changeType,
            deviceId);
    }

    /// <summary>
    /// Trigger sync for all connected clients
    /// </summary>
    public async Task TriggerSync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Clients.Group($"user_{userId}").SendAsync("SyncRequested");
    }
}
```

#### Client-Side SignalR Connection

```csharp
// Desktop/Mobile - SignalR connection
public class SignalRService
{
    private HubConnection? _connection;
    private readonly string _hubUrl;

    public event EventHandler<EntityChangedEventArgs>? EntityChanged;
    public event EventHandler? SyncRequested;

    public SignalRService(string apiBaseUrl)
    {
        _hubUrl = $"{apiBaseUrl}/hubs/sync";
    }

    public async Task ConnectAsync(string accessToken)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        // Subscribe to events
        _connection.On<string, Guid, string>("EntityChanged", (entityType, syncId, changeType) =>
        {
            EntityChanged?.Invoke(this, new EntityChangedEventArgs
            {
                EntityType = entityType,
                SyncId = syncId,
                ChangeType = changeType
            });
        });

        _connection.On("SyncRequested", () =>
        {
            SyncRequested?.Invoke(this, EventArgs.Empty);
        });

        await _connection.StartAsync();
    }

    public async Task NotifyChangeAsync(string entityType, Guid syncId, string changeType)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("NotifyChange", entityType, syncId, changeType);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
```

---

## ?? Docker Configuration

### Dockerfile for API

```dockerfile
# OneManVan.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["OneManVan.Api/OneManVan.Api.csproj", "OneManVan.Api/"]
COPY ["OneManVan.Shared/OneManVan.Shared.csproj", "OneManVan.Shared/"]
COPY ["OneManVan.Sync/OneManVan.Sync.csproj", "OneManVan.Sync/"]

# Restore dependencies
RUN dotnet restore "OneManVan.Api/OneManVan.Api.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/OneManVan.Api"
RUN dotnet build "OneManVan.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OneManVan.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OneManVan.Api.dll"]
```

### Dockerfile for Web

```dockerfile
# OneManVan.Web/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["OneManVan.Web/OneManVan.Web.csproj", "OneManVan.Web/"]
COPY ["OneManVan.Shared/OneManVan.Shared.csproj", "OneManVan.Shared/"]

# Restore dependencies
RUN dotnet restore "OneManVan.Web/OneManVan.Web.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/OneManVan.Web"
RUN dotnet build "OneManVan.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OneManVan.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OneManVan.Web.dll"]
```

### docker-compose.yml

```yaml
# docker-compose.yml
version: '3.8'

services:
  # PostgreSQL Database
  db:
    image: postgres:16-alpine
    container_name: onemanvan-db
    environment:
      POSTGRES_USER: onemanvan
      POSTGRES_PASSWORD: ${DB_PASSWORD:-changeme}
      POSTGRES_DB: onemanvan
      POSTGRES_INITDB_ARGS: "--encoding=UTF8 --lc-collate=en_US.UTF-8 --lc-ctype=en_US.UTF-8"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init-db.sql
    ports:
      - "5432:5432"
    networks:
      - onemanvan-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U onemanvan"]
      interval: 10s
      timeout: 5s
      retries: 5

  # ASP.NET Core API
  api:
    build:
      context: .
      dockerfile: OneManVan.Api/Dockerfile
    container_name: onemanvan-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ConnectionStrings__DefaultConnection=Host=db;Database=onemanvan;Username=onemanvan;Password=${DB_PASSWORD:-changeme}
      - Jwt__Secret=${JWT_SECRET:-your-super-secret-jwt-key-change-in-production}
      - Jwt__Issuer=OneManVanApi
      - Jwt__Audience=OneManVanClients
      - Jwt__ExpirationMinutes=15
      - Logging__LogLevel__Default=Information
      - Logging__LogLevel__Microsoft.AspNetCore=Warning
    ports:
      - "5000:80"
      - "5001:443"
    depends_on:
      db:
        condition: service_healthy
    networks:
      - onemanvan-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  # Blazor Web UI
  web:
    build:
      context: .
      dockerfile: OneManVan.Web/Dockerfile
    container_name: onemanvan-web
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - ApiSettings__BaseUrl=http://api:80
      - ApiSettings__HubUrl=http://api:80/hubs/sync
    ports:
      - "8080:80"
    depends_on:
      - api
    networks:
      - onemanvan-network
    restart: unless-stopped

  # Nginx Reverse Proxy (optional but recommended)
  nginx:
    image: nginx:alpine
    container_name: onemanvan-proxy
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/certs:/etc/nginx/certs:ro
      - ./nginx/html:/usr/share/nginx/html:ro
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - api
      - web
    networks:
      - onemanvan-network
    restart: unless-stopped

  # Redis Cache (optional - for session state)
  redis:
    image: redis:alpine
    container_name: onemanvan-redis
    ports:
      - "6379:6379"
    networks:
      - onemanvan-network
    restart: unless-stopped

volumes:
  postgres_data:
    driver: local

networks:
  onemanvan-network:
    driver: bridge
```

### Environment Variables (.env)

```bash
# .env
# IMPORTANT: Never commit this file to source control!

# Database
DB_PASSWORD=your-secure-database-password-here

# JWT
JWT_SECRET=your-super-secret-jwt-signing-key-at-least-32-characters-long

# Optional
POSTGRES_PORT=5432
API_PORT=5000
API_HTTPS_PORT=5001
WEB_PORT=8080
```

### .dockerignore

```
# .dockerignore
**/bin/
**/obj/
**/out/
**/.vs/
**/.vscode/
**/.idea/
**/*.user
**/*.suo
**/node_modules/
**/.git/
**/.gitignore
**/.dockerignore
**/docker-compose*.yml
**/Dockerfile*
**/*.md
**/tests/
```

### nginx Configuration

```nginx
# nginx/nginx.conf
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:80;
    }

    upstream web {
        server web:80;
    }

    # Redirect HTTP to HTTPS
    server {
        listen 80;
        server_name onemanvan.local;
        return 301 https://$server_name$request_uri;
    }

    # HTTPS Server
    server {
        listen 443 ssl http2;
        server_name onemanvan.local;

        ssl_certificate /etc/nginx/certs/onemanvan.crt;
        ssl_certificate_key /etc/nginx/certs/onemanvan.key;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        # Web UI
        location / {
            proxy_pass http://web;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;
        }

        # API
        location /api/ {
            proxy_pass http://api;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # SignalR Hub
        location /hubs/ {
            proxy_pass http://api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_cache_bypass $http_upgrade;
        }
    }
}
```

---

## ?? Security Architecture

### Authentication & Authorization

#### User Roles

```csharp
public enum UserRole
{
    Owner,        // Full access
    Manager,      // Most access, no settings
    Technician,   // Field work, limited admin
    ReadOnly      // View only
}
```

#### API Security Checklist

- [x] **HTTPS Only** - All endpoints require HTTPS
- [x] **JWT Authentication** - Stateless token-based auth
- [x] **Refresh Tokens** - Short-lived access tokens
- [x] **Rate Limiting** - Prevent abuse
- [x] **CORS Policy** - Restrict origins
- [x] **Input Validation** - FluentValidation on all DTOs
- [x] **SQL Injection Prevention** - EF Core parameterized queries
- [x] **XSS Prevention** - Input sanitization
- [x] **CSRF Protection** - Anti-forgery tokens
- [x] **Password Hashing** - Argon2 or bcrypt
- [x] **Audit Logging** - Track sensitive operations
- [x] **API Versioning** - Backwards compatibility
- [x] **Health Checks** - Monitor service status

### Secure Token Storage

#### Desktop (Windows)

```csharp
// Use Windows Credential Manager
public class SecureTokenStorage
{
    private const string TargetName = "OneManVan_API_Token";

    public void SaveToken(string accessToken, string refreshToken)
    {
        using var cred = new Credential
        {
            Target = TargetName,
            Username = "access_token",
            Password = accessToken,
            Type = CredentialType.Generic,
            PersistenceType = PersistenceType.LocalComputer
        };
        cred.Save();

        using var refreshCred = new Credential
        {
            Target = $"{TargetName}_Refresh",
            Username = "refresh_token",
            Password = refreshToken,
            Type = CredentialType.Generic,
            PersistenceType = PersistenceType.LocalComputer
        };
        refreshCred.Save();
    }

    public (string? AccessToken, string? RefreshToken) LoadTokens()
    {
        var accessToken = Credential.Load(TargetName)?.Password;
        var refreshToken = Credential.Load($"{TargetName}_Refresh")?.Password;
        return (accessToken, refreshToken);
    }
}
```

#### Mobile (MAUI)

```csharp
// Use MAUI Secure Storage
public class SecureTokenStorage
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
    }

    public async Task<(string? AccessToken, string? RefreshToken)> LoadTokensAsync()
    {
        var accessToken = await SecureStorage.GetAsync(AccessTokenKey);
        var refreshToken = await SecureStorage.GetAsync(RefreshTokenKey);
        return (accessToken, refreshToken);
    }

    public void ClearTokens()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshToken);
    }
}
```

---

## ?? Performance Considerations

### API Performance

**Best Practices:**
- ? Use pagination (default 50 items per page)
- ? Implement response caching where appropriate
- ? Use async/await throughout
- ? Enable response compression (gzip/brotli)
- ? Use connection pooling for database
- ? Implement query result caching (Redis)
- ? Use projections (DTOs) to reduce payload size
- ? Add database indexes on frequently queried columns

**Example - Cached Response:**

```csharp
[HttpGet]
[ResponseCache(Duration = 60)] // Cache for 60 seconds
public async Task<ActionResult<List<CustomerDto>>> GetCustomers()
{
    // ...
}
```

### Database Optimization

**Indexes to Add:**

```sql
-- Customer indexes
CREATE INDEX idx_customer_syncid ON customers(sync_id);
CREATE INDEX idx_customer_modified ON customers(modified_at);
CREATE INDEX idx_customer_deleted ON customers(is_deleted);
CREATE INDEX idx_customer_name ON customers(name);
CREATE INDEX idx_customer_email ON customers(email);

-- Asset indexes
CREATE INDEX idx_asset_syncid ON assets(sync_id);
CREATE INDEX idx_asset_modified ON assets(modified_at);
CREATE INDEX idx_asset_customer ON assets(customer_id);
CREATE INDEX idx_asset_serial ON assets(serial);

-- Job indexes
CREATE INDEX idx_job_syncid ON jobs(sync_id);
CREATE INDEX idx_job_modified ON jobs(modified_at);
CREATE INDEX idx_job_customer ON jobs(customer_id);
CREATE INDEX idx_job_scheduled ON jobs(scheduled_start);
CREATE INDEX idx_job_status ON jobs(status);

-- Invoice indexes
CREATE INDEX idx_invoice_syncid ON invoices(sync_id);
CREATE INDEX idx_invoice_modified ON invoices(modified_at);
CREATE INDEX idx_invoice_customer ON invoices(customer_id);
CREATE INDEX idx_invoice_status ON invoices(status);
CREATE INDEX idx_invoice_date ON invoices(invoice_date);
```

### SignalR Performance

**Configuration:**

```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.StreamBufferCapacity = 10;
})
.AddMessagePackProtocol(); // More efficient than JSON
```

---

## ?? Timeline & Resources

### Detailed Timeline

| Phase | Tasks | Duration | Effort (Hours) | Complexity |
|-------|-------|----------|----------------|------------|
| **Phase 1: API Foundation** | Project setup, Controllers, Docker | 2-3 weeks | 60-80 hours | Medium |
| **Phase 2: Authentication** | JWT, Security, Tokens | 1-2 weeks | 30-40 hours | Medium |
| **Phase 3: Sync System** | Change tracking, Conflicts, SignalR | 2-3 weeks | 60-80 hours | High |
| **Phase 4: Web UI** | Blazor pages, Components, Layout | 3-4 weeks | 80-100 hours | Medium-High |
| **Phase 5: Desktop Updates** | API client, Sync integration | 1-2 weeks | 30-40 hours | Medium |
| **Phase 6: Mobile Updates** | API client, Background sync | 1-2 weeks | 30-40 hours | Medium |
| **Phase 7: Deployment** | Production setup, Testing | 1 week | 20-30 hours | Medium |
| **Total** | | **11-17 weeks** | **310-410 hours** | |

### Parallel Work Opportunities

- Phase 4 (Web UI) can start after Phase 1 completes
- Phase 5 & 6 (Client updates) can run in parallel
- Documentation can be written throughout

### Resource Requirements

**Developer Time:**
- 1 developer full-time: ~3-4 months
- 2 developers part-time: ~2-3 months
- 1 developer part-time: ~5-6 months

**Infrastructure:**
- Local server (NAS, old PC, or VPS)
- Docker installed
- ~2GB RAM for containers
- ~10GB disk space for database + backups

**Skills Needed:**
- ? C# / .NET (already have)
- ? Entity Framework Core (already have)
- ?? ASP.NET Core Web API (easy to learn)
- ?? Blazor (similar to WPF/MAUI)
- ?? Docker basics (tutorials available)
- ?? PostgreSQL (similar to SQLite)

---

## ?? Decision Matrix

### ? YOUR KEY DECISIONS (Confirmed)

#### 1. Hosting Location: **Local Server/NAS** ?

**Decision:** Docker will run on local server/NAS for now

| Aspect | Your Choice |
|--------|-------------|
| **Hosting** | Local Server/NAS |
| **Network** | Local network primary |
| **External Access** | Future: VPN/Wireguard/Cloudflare Tunnel |
| **Cost** | $0/month (self-hosted) |
| **Control** | Full control |

**Implementation Notes:**
- Docker on your existing server/NAS
- No cloud costs
- Full data ownership
- Easy local access from Desktop/Mobile/Web

**Implementation Notes:**
- Docker on your existing server/NAS
- No cloud costs
- Full data ownership
- Easy local access from Desktop/Mobile/Web

#### 2. Database Choice: **PostgreSQL** ?

**Decision:** PostgreSQL for production reliability

| Aspect | Your Choice |
|--------|-------------|
| **Database** | PostgreSQL 16 |
| **Docker Image** | postgres:16-alpine |
| **Storage** | Local NAS volumes |
| **Backups** | Local + optional cloud backup |

**Why PostgreSQL:**
- ? Production-grade reliability
- ? Excellent JSON support (for custom fields)
- ? ACID compliance
- ? Great performance
- ? Docker-ready official images
- ? Open source, no licensing

#### 3. Network Access: **Local + Future External** ?

**Decision:** Start local-only, add external access later

| Phase | Access Method | Status |
|-------|--------------|--------|
| **Phase 1 (Now)** | Local network only | ? Implement First |
| **Phase 2 (Future)** | VPN (Wireguard) | ?? Optional Enhancement |
| **Phase 3 (Future)** | Cloudflare Tunnel | ?? Optional Enhancement |

**Local Network Setup:**
```
Desktop/Mobile/Web ? Router ? Local Server ? Docker Containers

Access URLs:
- Web: https://onemanvan.local:8080
- API: https://onemanvan.local:5001/api
```

**Future External Access Options:**

**Option A: Wireguard VPN** (Recommended)
```
Mobile (Away) ? Wireguard VPN ? Home Network ? Server
```
- ? Secure encrypted tunnel
- ? Full network access
- ? Free and open source
- ? Works on all platforms
- ?? Requires router configuration

**Option B: Cloudflare Tunnel** (Easiest)
```
Mobile (Away) ? Cloudflare ? Tunnel ? Server (No port forwarding!)
```
- ? No port forwarding needed
- ? Free tier available
- ? Built-in SSL
- ? DDoS protection
- ?? Requires Cloudflare account

**Option C: Tailscale** (Alternative)
```
Mobile (Away) ? Tailscale Mesh ? Server
```
- ? Zero-config VPN
- ? Free for personal use
- ? Cross-platform
- ? Very easy setup

#### 4. Domain Setup: **Local Domain + Future Real Domain** ?

**Decision:** Start with `.local` domain, optionally add real domain later

| Phase | Domain | Status |
|-------|--------|--------|
| **Phase 1 (Now)** | `onemanvan.local` | ? Implement First |
| **Phase 2 (Future)** | `onemanvan.yourdomain.com` | ?? Optional |

**Local Domain Setup:**

**Windows (Desktop):**
```
# Edit C:\Windows\System32\drivers\etc\hosts
192.168.1.100    onemanvan.local
```

**iOS/Android (Mobile):**
- Use mDNS/Bonjour (automatically discovers `.local`)
- Or manually set DNS in WiFi settings

**macOS:**
- Automatically discovers `.local` via Bonjour

**Future Real Domain Options:**
1. **Subdomain** of existing domain
   - `hvac.yourdomain.com`
   - Requires DNS A record

2. **New domain** 
   - `onemanvan.com` (example)
   - ~$10/year

3. **Free domain**
   - DuckDNS, No-IP, etc.
   - Free dynamic DNS

#### 5. User Management: **Single User ? Multi-User** ?

**Decision:** Start with simple auth, expand to multi-user later

| Phase | User Setup | Status |
|-------|-----------|--------|
| **Phase 1 (Now)** | Single user (you) | ? Implement First |
| **Phase 2 (Future)** | Multi-user with roles | ?? Add When Needed |

**Phase 1 Implementation (Simple):**
```csharp
// Simple single-user authentication
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Email { get; set; }
}

// No roles, no complex permissions
// Just: Logged In = Full Access
```

**Phase 2 Implementation (Multi-User):**
```csharp
// Add when you hire employees
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Email { get; set; }
    public UserRole Role { get; set; }  // NEW
}

public enum UserRole
{
    Owner,       // Full access (you)
    Manager,     // Most access, no settings
    Technician,  // Field work only
    ReadOnly     // View only
}
```

---

### ?? Implementation Priorities Based on Decisions

#### High Priority (Implement First)

1. **API with PostgreSQL** ?
   - Docker on local server/NAS
   - PostgreSQL container
   - All CRUD endpoints
   - Simple JWT auth (single user)

2. **Local Network Access** ?
   - Self-signed SSL certificate
   - `.local` domain via mDNS
   - Desktop/Mobile connect locally

3. **Blazor Web UI** ?
   - Full CRUD interface
   - Works in any browser
   - Local access only

4. **Sync System** ?
   - Desktop ? Server
   - Mobile ? Server
   - Real-time via SignalR

#### Medium Priority (Add When Ready)

5. **VPN Access** ??
   - Wireguard setup
   - Mobile access from anywhere
   - Encrypted tunnel

6. **Multi-User** ??
   - When you hire help
   - Role-based access
   - User management UI

#### Low Priority (Optional)

7. **Real Domain** ??
   - If you want external access
   - Cloudflare Tunnel (easiest)
   - Or traditional DNS + port forward

8. **Advanced Features** ??
   - OAuth/SSO
   - Two-factor auth
   - Advanced permissions

---

### ?? Updated Implementation Plan

#### Phase 1: API Foundation (2-3 weeks) ? Start Here

**Focus:** Get API running on your local server

**Tasks:**
- [ ] Install Docker on server/NAS
- [ ] Create `OneManVan.Api` project
- [ ] Configure PostgreSQL (Docker)
- [ ] Create all CRUD controllers
- [ ] Add simple JWT auth (single user)
- [ ] Test locally with `https://localhost:5001`

**Deliverable:** Working API accessible on local network

---

#### Phase 2: Local Network Setup (3-5 days) ? Then This

**Focus:** Make API accessible from Desktop/Mobile on local network

**Tasks:**
- [ ] Generate self-signed SSL certificate
- [ ] Configure nginx reverse proxy
- [ ] Set up `.local` domain (mDNS)
- [ ] Update docker-compose for production
- [ ] Test from Desktop: `https://onemanvan.local:8080`
- [ ] Test from Mobile: `https://onemanvan.local:8080`

**Deliverable:** API + Web accessible via `.local` domain

---

#### Phase 3: Web UI (3-4 weeks)

**Focus:** Blazor interface for browser access

**Tasks:**
- [ ] Create `OneManVan.Web` project
- [ ] Build all CRUD pages
- [ ] Add authentication
- [ ] Style and polish
- [ ] Deploy to Docker

**Deliverable:** Full web interface on local network

---

#### Phase 4: Sync System (2-3 weeks)

**Focus:** Real-time sync between all clients

**Tasks:**
- [ ] Add sync metadata to models
- [ ] Implement SignalR hub
- [ ] Create sync endpoints
- [ ] Update Desktop with sync
- [ ] Update Mobile with sync
- [ ] Test full sync cycle

**Deliverable:** Desktop ? Server ? Mobile sync working

---

#### Phase 5: External Access (1 week) ?? Future

**Option A: Wireguard VPN**
- [ ] Install Wireguard on server
- [ ] Configure Wireguard on router
- [ ] Install Wireguard on mobile
- [ ] Test remote access

**Option B: Cloudflare Tunnel**
- [ ] Create Cloudflare account
- [ ] Install cloudflared on server
- [ ] Configure tunnel
- [ ] Access via `onemanvan.yourdomain.com`

---

#### Phase 6: Multi-User (1-2 weeks) ?? Future

**When you hire employees:**
- [ ] Add `UserRole` enum
- [ ] Update JWT with role claims
- [ ] Add `[Authorize(Roles = "Owner")]` attributes
- [ ] Create user management UI
- [ ] Test permissions

---

### ?? Quick Start Guide (Your Setup)

#### Step 1: Prepare Server (Day 1)

**On your local server/NAS:**

```bash
# Check Docker is installed
docker --version

# If not installed:
# - Synology: Install Docker from Package Center
# - QNAP: Install Container Station
# - Ubuntu/Debian: apt install docker.io docker-compose
# - Windows Server: Install Docker Desktop

# Create project directory
mkdir -p /volume1/docker/onemanvan
cd /volume1/docker/onemanvan
```

#### Step 2: Deploy Basic Setup (Day 1)

**Create initial docker-compose.yml:**

```yaml
version: '3.8'

services:
  db:
    image: postgres:16-alpine
    container_name: onemanvan-db
    environment:
      POSTGRES_USER: onemanvan
      POSTGRES_PASSWORD: your_secure_password_here  # CHANGE THIS!
      POSTGRES_DB: onemanvan
    volumes:
      - ./data/postgres:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    restart: unless-stopped

  # Add API and Web containers later
```

**Start database:**
```bash
docker-compose up -d
docker-compose logs -f db
```

#### Step 3: Test Local Access (Day 1)

**Add to hosts file on Desktop:**
```
# Windows: C:\Windows\System32\drivers\etc\hosts
# Add this line (replace with your server IP):
192.168.1.100    onemanvan.local
```

**Test connectivity:**
```bash
ping onemanvan.local
```

#### Step 4: Generate SSL Certificate (Day 2)

**Self-signed for local network:**

```bash
# On your server
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /path/to/onemanvan.key \
  -out /path/to/onemanvan.crt \
  -subj "/CN=onemanvan.local"
```

**Trust certificate on Desktop:**
- Windows: Double-click .crt ? Install ? Trusted Root
- iOS: Email .crt ? Open ? Install
- Android: Settings ? Security ? Install certificate

#### Step 5: Deploy API (Week 2-3)

After creating the API project:

```bash
# Build and deploy
docker-compose -f docker-compose.prod.yml up -d --build

# Check logs
docker-compose logs -f api
```

**Test API:**
```bash
curl https://onemanvan.local:5001/api/health
```

#### Step 6: Access from Mobile (Week 3)

**On same WiFi as server:**
1. Mobile automatically discovers `.local` domain
2. Open browser: `https://onemanvan.local:8080`
3. Accept self-signed certificate warning
4. Login and use!

---

### ?? Security for Local Network

#### Current Security (Phase 1)

**What you have:**
- ? HTTPS (self-signed cert)
- ? JWT authentication
- ? Password hashing
- ? Local network only (firewall)
- ? No external exposure

**This is GOOD ENOUGH for local use!**

#### Future Security (Phase 5 - External Access)

**When adding VPN/Cloudflare:**
- ? Add rate limiting
- ? Add fail2ban or similar
- ? Use strong passwords
- ? Enable 2FA (optional)
- ? Regular backups
- ? Monitor access logs

---

### ?? Backup Strategy (Local Server)

#### Automated Backups

**Database backup script:**

```bash
#!/bin/bash
# /volume1/docker/onemanvan/backup.sh

BACKUP_DIR="/volume1/backups/onemanvan"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup
docker exec onemanvan-db pg_dump -U onemanvan onemanvan > \
  "${BACKUP_DIR}/onemanvan_${DATE}.sql"

# Keep last 30 days
find ${BACKUP_DIR} -name "onemanvan_*.sql" -mtime +30 -delete

# Optional: Upload to cloud
# rclone copy ${BACKUP_DIR} remote:backups/onemanvan
```

**Cron job:**
```bash
# Run daily at 2 AM
0 2 * * * /volume1/docker/onemanvan/backup.sh
```

#### Restore Process

```bash
# Stop containers
docker-compose down

# Restore database
cat onemanvan_20250123_020000.sql | \
  docker exec -i onemanvan-db psql -U onemanvan onemanvan

# Restart
docker-compose up -d
```

---

### ?? Network Configuration

#### Local Network Topology

```
???????????????????????????????????????????????????????
?             Your Local Network (192.168.1.0/24)     ?
???????????????????????????????????????????????????????
?                                                     ?
?  Desktop (192.168.1.50)                             ?
?  ?? WPF App                                         ?
?     ?? API Client ? onemanvan.local:5001           ?
?                                                     ?
?  Mobile (192.168.1.51) - WiFi                       ?
?  ?? MAUI App                                        ?
?     ?? API Client ? onemanvan.local:5001           ?
?                                                     ?
?  Laptop (192.168.1.52) - WiFi                       ?
?  ?? Web Browser                                     ?
?     ?? https://onemanvan.local:8080                ?
?                                                     ?
?  Server/NAS (192.168.1.100) ? Your Docker Host     ?
?  ?? Docker Containers:                              ?
?     ?? PostgreSQL (5432)                            ?
?     ?? API (5001)                                   ?
?     ?? Web UI (8080)                                ?
?     ?? nginx (443) ? Routes to API/Web             ?
?                                                     ?
?  Router (192.168.1.1)                               ?
?  ?? DHCP, DNS, Firewall                            ?
?                                                     ?
???????????????????????????????????????????????????????

Future: Add Wireguard/Cloudflare for external access
```

#### Firewall Rules (Router)

**Current (Local Only):**
- ? Allow all internal (192.168.1.0/24) ? Server
- ? Block all external ? Server

**Future (With VPN):**
- ? Allow Wireguard port (51820) ? Server
- ? Allow VPN subnet ? Server
- ? Still block direct external ? Server

---

## ?? Decision Matrix

### Key Decisions Before Starting

---

## ?? Risk Assessment

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Sync conflicts** | Medium | High | Implement robust conflict resolution |
| **Data loss during sync** | Low | Critical | Backup before sync, transaction rollback |
| **Performance issues** | Medium | Medium | Load testing, optimization, caching |
| **Docker complexity** | Medium | Medium | Use docker-compose, good documentation |
| **Certificate issues** | Low | Medium | Self-signed cert acceptable for local |
| **Migration complexity** | Medium | High | Phased rollout, keep Desktop standalone |

### Business Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Timeline overrun** | Medium | Medium | Start with MVP, iterate |
| **Feature creep** | High | Medium | Stick to defined phases |
| **User adoption** | Low | High | Training, gradual rollout |
| **Maintenance burden** | Medium | Medium | Good documentation, automated backups |

---

## ?? Next Steps

### Immediate Actions (This Week)

1. **Make Key Decisions:**
   - [ ] Choose hosting location (server/NAS)
   - [ ] Decide on MVP scope (4-6 weeks) or full (3-4 months)
   - [ ] Confirm database choice (PostgreSQL recommended)

2. **Prepare Environment:**
   - [ ] Install Docker on target server
   - [ ] Test PostgreSQL container
   - [ ] Set up development environment

3. **Start Phase 1:**
   - [ ] Create OneManVan.Api project
   - [ ] Configure PostgreSQL connection
   - [ ] Create first controller (Customers)
   - [ ] Test with Postman

### Week 2 Actions

4. **Continue API Development:**
   - [ ] Add all entity controllers
   - [ ] Set up Swagger documentation
   - [ ] Create Dockerfile
   - [ ] Test in Docker

### Week 3-4 Actions

5. **Add Authentication:**
   - [ ] Implement JWT
   - [ ] Create login endpoint
   - [ ] Secure all controllers
   - [ ] Test token flow

### Month 2-3 Actions

6. **Build Sync & Web UI:**
   - [ ] Implement sync system
   - [ ] Create Blazor pages
   - [ ] Connect Desktop
   - [ ] Connect Mobile

### When Ready

7. **Deploy to Production:**
   - [ ] Deploy with docker-compose
   - [ ] Configure SSL
   - [ ] Test all flows
   - [ ] Train users

---

## ?? Learning Resources

### Essential Reading

**ASP.NET Core API:**
- [Official Tutorial](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api)
- [REST API Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design)

**JWT Authentication:**
- [JWT Authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt)

**Blazor:**
- [Blazor Tutorial](https://dotnet.microsoft.com/learn/aspnet/blazor-tutorial/intro)
- [Blazor Server vs WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)

**Docker:**
- [Docker for .NET](https://docs.docker.com/samples/dotnetcore/)
- [docker-compose Tutorial](https://docs.docker.com/compose/gettingstarted/)

**SignalR:**
- [SignalR Introduction](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [SignalR with Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/tutorials/signalr-blazor)

**PostgreSQL:**
- [PostgreSQL with EF Core](https://www.npgsql.org/efcore/)

### Video Tutorials

- [Building REST APIs with ASP.NET Core](https://www.youtube.com/results?search_query=asp.net+core+web+api+tutorial)
- [Blazor Full Course](https://www.youtube.com/results?search_query=blazor+full+course)
- [Docker for Beginners](https://www.youtube.com/results?search_query=docker+tutorial+for+beginners)

---

## ? Readiness Checklist

Before starting implementation:

### Prerequisites
- [ ] Decision matrix completed
- [ ] Hosting location confirmed
- [ ] Docker installed and tested
- [ ] Git repository ready
- [ ] Development environment set up

### Phase 1 Ready
- [ ] OneManVan.Shared project working
- [ ] PostgreSQL test container running
- [ ] Postman or similar API testing tool ready
- [ ] Time allocated (2-3 weeks)

### Knowledge Gaps
- [ ] ASP.NET Core basics understood
- [ ] Docker basics understood
- [ ] JWT auth concepts understood
- [ ] Time set aside for learning

---

## ?? Summary

### What You're Building

A modern, cloud-ready ecosystem with:
- ? Central API server (ASP.NET Core)
- ? Web UI (Blazor Server)
- ? Desktop app with sync (WPF)
- ? Mobile app with sync (MAUI)
- ? PostgreSQL database
- ? Docker deployment
- ? Real-time updates (SignalR)

### Timeline

- **MVP:** 4-6 weeks (basic functionality)
- **Full System:** 11-17 weeks (complete ecosystem)
- **Effort:** 310-410 hours development time

### Investment

- **Learning:** Docker, ASP.NET Core API, Blazor basics
- **Infrastructure:** Server/NAS (already have?)
- **Time:** 3-4 months part-time or 2-3 months full-time

### Payoff

- ? Access from any device
- ? Work offline, sync online
- ? Scalable architecture
- ? Modern web interface
- ? Self-hosted (your data, your control)

---

## ?? Support & Questions

**When you're ready to start**, I can help with:

1. **Setting up the API project** - Create controllers, configure database
2. **Docker configuration** - Write Dockerfiles and docker-compose
3. **Implementing sync** - Build the sync engine and conflict resolution
4. **Building Blazor pages** - Create the web UI
5. **Connecting clients** - Update Desktop and Mobile to use API

**Just say:** "Let's start Phase 1" and we'll begin building!

---

**Document Version:** 1.0  
**Created:** 2025-01-23  
**Status:** ? **COMPLETE & READY TO USE**  
**Next:** Make decisions and start Phase 1!  

?? **LET'S BUILD THE FUTURE OF ONEMANVAN!** ??
