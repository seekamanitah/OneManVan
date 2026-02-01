# OneManVan - Feature Roadmap & Enhancement Ideas

**Last Updated**: Auto-generated  
**Status**: Planning & Prioritization

---

## ?? High-Impact Quick Wins (1-2 weeks each)

### 1. Customer Portal ?????
**Value**: High | **Effort**: 2 weeks | **Priority**: 1

Allow customers to access their own data 24/7 without calling you.

**Features:**
- Customer login with email/password
- View invoices and estimates
- Accept/decline estimates online
- View service history for their assets
- Upload photos of issues
- Schedule service requests
- Download PDFs

**Technical Implementation:**
```
Files to create:
OneManVan.Web/Components/Pages/Portal/
  - CustomerLogin.razor
  - CustomerDashboard.razor
  - MyInvoices.razor
  - MyEstimates.razor
  - MyAssets.razor
  - ScheduleService.razor
  - MyProfile.razor
```

**Benefits:**
- Reduces phone calls and admin work
- Professional appearance
- 24/7 customer self-service
- Better customer experience

---

### 2. Payment Processing Integration ?????
**Value**: High | **Effort**: 1 week | **Priority**: 2

Accept credit card payments directly on invoices.

**Options:**
- **Stripe** (recommended - simple API)
- **Square** (already have service stub)
- **PayPal** (customer familiarity)

**Features:**
- "Pay Now" button on invoices
- Accept credit cards securely
- Automatic payment recording
- Email payment confirmations
- Recurring billing for service agreements
- Payment history

**Files to implement:**
```
OneManVan.Web/Services/Payment/
  - StripePaymentService.cs
  - IPaymentService.cs
  - PaymentResult.cs

OneManVan.Web/Components/Shared/
  - PaymentButton.razor
  - PaymentModal.razor
```

**Revenue Impact**: Faster payment = better cash flow

---

### 3. Automated Email Notifications ????
**Value**: High | **Effort**: 3 days | **Priority**: 3

You have email service - now automate it!

**Scenarios:**
- Invoice sent ? Email customer immediately
- Payment received ? Send thank you
- Estimate created ? Send for approval
- Estimate approved ? Confirmation email
- Job scheduled ? Reminder 24 hours before
- Job completed ? Request review/feedback
- Service agreement renewal ? 30 day reminder
- Asset warranty expiring ? Alert customer

**Implementation:**
```csharp
// OneManVan.Shared/Services/NotificationService.cs
public interface INotificationService
{
    Task SendInvoiceCreatedAsync(Invoice invoice);
    Task SendPaymentReceivedAsync(Payment payment);
    Task SendEstimateForApprovalAsync(Estimate estimate);
    Task SendJobReminderAsync(Job job);
    Task SendJobCompletedAsync(Job job);
    Task SendServiceAgreementRenewalAsync(ServiceAgreement agreement);
    Task SendWarrantyExpiringAsync(Asset asset);
}
```

**Triggers:**
- Invoice.Status changed to Sent
- Payment created
- Job.ScheduledDate is tomorrow
- ServiceAgreement.EndDate is 30 days away

---

### 4. Digital Signature Capture (Mobile) ????
**Value**: High | **Effort**: 1 week | **Priority**: 4

Essential for modern field service.

**Use Cases:**
- Customer sign-off on completed work
- Accept estimates in person
- Delivery confirmations
- Service agreement signatures
- Warranty paperwork

**Implementation:**
```csharp
// OneManVan.Mobile/Controls/SignaturePad.xaml
public class SignaturePad : ContentView
{
    public ImageSource SignatureImage { get; set; }
    public byte[] GetSignatureBytes();
    public void Clear();
    public string GetBase64();
}
```

**Add signature to:**
- Jobs (completion)
- Invoices (acceptance)
- Estimates (approval)
- Service Agreements (contract signing)

---

### 5. Enhanced Dashboard with Real-Time KPIs ????
**Value**: High | **Effort**: 1 week | **Priority**: 5

Make the dashboard a real business intelligence tool.

**Metrics to Add:**

**Revenue Metrics:**
- Today's revenue vs. yesterday (%)
- This month vs. last month (%)
- Outstanding receivables aging (30/60/90 days)
- Average days to payment
- Revenue by service type
- Revenue by customer

**Operational Metrics:**
- Jobs completed this week
- Jobs scheduled next 7 days
- Average job duration
- Technician utilization rate
- Customer satisfaction score
- Repeat customer rate

**Asset & Maintenance:**
- Assets under warranty expiring soon
- Assets due for maintenance
- Service agreements expiring (30/60/90 days)
- Inventory items below reorder point

**Charts:**
- Revenue trend (last 12 months)
- Job status pie chart
- Top 10 customers by revenue
- Service type breakdown

---

## ?? Mobile App Enhancements (Critical for Field Techs)

### 6. Offline-First Sync ?????
**Value**: Critical | **Effort**: 2 weeks | **Priority**: High

Mobile app must work in basements/remote areas without internet.

**Features:**
- Cache critical data locally (SQLite)
- Queue changes when offline
- Automatic sync when connection returns
- Conflict resolution strategy
- Sync status indicator
- Manual sync trigger

**Implementation:**
```csharp
// OneManVan.Mobile/Services/OfflineSyncService.cs
public class OfflineSyncService
{
    private readonly IConnectivity _connectivity;
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    
    public async Task QueueChangeAsync<T>(T entity, ChangeType type);
    public async Task SyncWhenOnlineAsync();
    public async Task<SyncStatus> GetSyncStatusAsync();
}
```

**Sync priorities:**
1. Customer data (read-only)
2. Jobs (read/write)
3. Time entries (write)
4. Photos (queued upload)

---

### 7. GPS Photo Capture Enhancement ???
**Value**: Medium | **Effort**: 3 days | **Priority**: Medium

You have PhotoCaptureService - enhance it.

**Add:**
- GPS coordinates on photos
- Automatic timestamp
- Before/after photo pairing
- Photo annotations/markup
- Automatic upload when online
- Photo categories (before, after, issue, completion)

**Use cases:**
- Document job site conditions
- Asset installation photos
- Warranty claim evidence
- Before/after comparisons

---

### 8. Route Optimization ???
**Value**: Medium | **Effort**: 1 week | **Priority**: Medium

Minimize drive time between jobs.

**Features:**
- View all scheduled jobs on map
- Optimize route order
- Calculate drive times
- Traffic-aware routing
- Multiple stops planning
- Save favorite routes

**APIs:**
- Google Maps Directions API
- Bing Maps Routes API
- MapBox Optimization API

---

## ??? Scheduling & Dispatch

### 9. Smart Scheduling with Google Calendar ????
**Value**: High | **Effort**: 1 week | **Priority**: High

You have `GoogleCalendarSettings` stub - implement it.

**Features:**
- Two-way sync with Google Calendar
- Block out personal time
- Technician availability tracking
- Automatic appointment creation
- Color-coded job types
- SMS/Email reminders

**Benefits:**
- Never double-book
- Customer sees availability
- Automatic reminders

---

### 10. Recurring Service Scheduler ????
**Value**: High | **Effort**: 1 week | **Priority**: High

Automate maintenance contracts.

**Features:**
- HVAC seasonal maintenance (spring/fall)
- Filter replacement schedules
- Annual inspections
- Automatic job creation from agreements
- Customer reminders
- Warranty expiration notices

**Implementation:**
```csharp
// Run daily as background service
public class RecurringServiceScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CreateScheduledJobsAsync();
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

---

## ?? Customer Communication

### 11. SMS/Text Messaging ?????
**Value**: Very High | **Effort**: 3 days | **Priority**: High

Email has 25% open rate. SMS has 98% open rate.

**Use Cases:**
- "Technician on the way" (with ETA)
- Appointment reminders (24 hours before)
- Payment reminders
- Job completion notifications
- Estimate ready for review
- Invoice available

**Services:**
- **Twilio** (recommended - reliable)
- Plivo
- AWS SNS

**Implementation:**
```csharp
public interface ISmsService
{
    Task SendSmsAsync(string phoneNumber, string message);
    Task SendTechnicianEnRouteAsync(Job job, int estimatedMinutes);
    Task SendAppointmentReminderAsync(Job job);
    Task SendPaymentReminderAsync(Invoice invoice);
}
```

---

### 12. Customer Review Request System ???
**Value**: Medium | **Effort**: 2 days | **Priority**: Medium

Get more online reviews automatically.

**Flow:**
1. Job marked complete
2. Wait 1 day
3. Send email/SMS: "How did we do?"
4. Happy? ? Link to Google/Yelp review
5. Unhappy? ? Internal feedback form
6. Track responses

**Benefits:**
- More online visibility
- Address issues before bad reviews
- Build reputation

---

## ?? Inventory & Parts

### 13. Parts Ordering Integration ???
**Value**: Medium | **Effort**: 1 week | **Priority**: Low

You have inventory tracking - add ordering.

**Features:**
- Reorder point alerts
- One-click ordering from suppliers
- Track orders in-transit
- Receive inventory into system
- Supplier catalog import

**Integrations:**
- Ferguson
- Johnstone Supply
- Local distributors

---

### 14. Barcode Scanning ???
**Value**: Medium | **Effort**: 2 days | **Priority**: Medium

You have `BarcodeScannerService` - wire it up fully.

**Use for:**
- Quick inventory lookup
- Asset serial number entry
- Parts usage tracking on jobs
- Stock take/audit
- Product catalog entry

---

## ?? Invoice & Estimate Improvements

### 15. Recurring Invoices ????
**Value**: High | **Effort**: 3 days | **Priority**: High

For service agreements with monthly billing.

**Features:**
- Auto-generate invoices monthly
- Email automatically
- Accept auto-payments (with Stripe)
- Track MRR (Monthly Recurring Revenue)
- Dunning management (failed payments)

---

### 16. Estimate Templates & Packages ???
**Value**: Medium | **Effort**: 3 days | **Priority**: Medium

Speed up estimate creation.

**Examples:**
```
Package: "Spring Tune-Up Special"
  - Filter replacement
  - Coil cleaning  
  - Refrigerant check
  - Thermostat calibration
  Price: $199 (save $50)

Package: "Complete System Replacement"
  - Remove old unit
  - Install new 3-ton AC
  - New thermostat
  - 5-year warranty
  Price: $5,500
```

---

## ?? Security & Compliance

### 17. Role-Based Access Control ????
**Value**: Important | **Effort**: 1 week | **Priority**: Before Production

Authentication is disabled - when enabled, add roles.

**Roles:**
- **Owner** - Full access to everything
- **Office Admin** - Invoices, customers, scheduling (no settings)
- **Technician** - Mobile app, job updates only (no pricing)
- **Customer** - Portal access only (their data)

**Implementation:**
```csharp
[Authorize(Roles = "Owner,Admin")]
public class InvoiceEdit : ComponentBase { }

[Authorize(Roles = "Owner")]  
public class Settings : ComponentBase { }

[Authorize(Roles = "Technician,Owner,Admin")]
public class JobEdit : ComponentBase { }
```

---

### 18. Audit Logging ???
**Value**: Professional | **Effort**: 3 days | **Priority**: Medium

Track who changed what and when.

**Log:**
- Invoice modifications
- Payment entries  
- Customer data changes
- Estimate approvals
- User logins
- Settings changes

**Model:**
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Action { get; set; } // "Create", "Update", "Delete"
    public string EntityType { get; set; } // "Invoice", "Customer"
    public int EntityId { get; set; }
    public string Changes { get; set; } // JSON diff
    public string IpAddress { get; set; }
}
```

---

## ?? Business Intelligence

### 19. Advanced Reports ???
**Value**: Medium | **Effort**: 1 week | **Priority**: Medium

**Reports:**
- Profit & Loss by month
- Revenue by customer
- Revenue by service type
- Job profitability analysis
- Inventory turnover
- Customer lifetime value
- Service agreement forecast
- Tax report

**Export:** PDF, Excel, CSV

---

### 20. Customer Lifetime Value Tracking ???
**Value**: Medium | **Effort**: 2 days | **Priority**: Low

Know which customers are most valuable.

**Calculate:**
- Total revenue per customer
- Average job value
- Frequency of service
- Projected annual value
- Referral count

**Use for:**
- VIP customer treatment
- Targeted marketing
- Service agreement pricing

---

## ?? Integrations

### 21. QuickBooks Integration ????
**Value**: High (for some users) | **Effort**: 2 weeks | **Priority**: Optional

**Sync:**
- Customers ? QuickBooks Customers
- Invoices ? QuickBooks Invoices
- Payments ? QuickBooks Payments
- Expenses ? QuickBooks Expenses

**API:** QuickBooks Online API v3

---

### 22. Google My Business Integration ??
**Value**: Low | **Effort**: 1 week | **Priority**: Low

Auto-post job photos to Google Business Profile.

---

## ?? UI/UX Polish

### 23. Progressive Web App (PWA) ???
**Value**: Medium | **Effort**: 1 day | **Priority**: Medium

Make web app installable.

**Benefits:**
- Install on phone home screen
- Offline capability (service worker)
- Push notifications
- App-like feel without App Store

**Add:**
```html
<!-- index.html -->
<link rel="manifest" href="manifest.json">
<meta name="theme-color" content="#0d6efd">
```

---

### 24. Mobile-Responsive Tables ??
**Value**: Medium | **Effort**: 2 days | **Priority**: Medium

Tables don't work well on phones.

**Solution:**
- Card layout on mobile (<768px)
- Table on desktop (?768px)
- Swipe gestures for actions

---

## ?? Testing & Quality

### 25. Automated Testing Suite ???
**Value**: Professional | **Effort**: Ongoing | **Priority**: Medium

You have test projects - expand them.

**Coverage:**
- Unit tests for calculations (tax, totals, pricing)
- Integration tests for database operations
- E2E tests for critical user flows
- Load testing for scalability

**Target:** 80% code coverage

---

## ?? Marketing & Growth

### 26. Referral Program ??
**Value**: Low | **Effort**: 3 days | **Priority**: Low

Encourage customer referrals.

**Features:**
- Track referral source on customers
- Referral discount/credit
- Leaderboard of top referrers
- Automated thank-you

---

## ?? Recommended Implementation Order

### Phase 1: Foundation (Month 1)
1. ? Fix SQL Server connection issues
2. Customer Portal (2 weeks)
3. Payment Processing - Stripe (1 week)
4. Automated Email Notifications (3 days)

### Phase 2: Mobile Critical (Month 2)
5. Offline-First Sync (2 weeks)
6. Digital Signatures (1 week)
7. Enhanced Photo Capture (3 days)

### Phase 3: Operations (Month 3)
8. Smart Scheduling with Google Calendar (1 week)
9. Recurring Service Scheduler (1 week)
10. SMS Notifications (3 days)
11. Enhanced Dashboard KPIs (1 week)

### Phase 4: Revenue (Month 4)
12. Recurring Invoices (3 days)
13. Customer Review Requests (2 days)
14. Estimate Templates (3 days)
15. Route Optimization (1 week)

### Phase 5: Professional (Month 5)
16. Role-Based Access Control (1 week)
17. Audit Logging (3 days)
18. Advanced Reports (1 week)

### Phase 6: Integrations (Month 6+)
19. QuickBooks (2 weeks)
20. Parts Ordering (1 week)
21. Barcode Scanning (2 days)
22. PWA Conversion (1 day)

---

## ?? Estimated Business Impact

| Feature | Monthly Revenue Impact | Time Savings |
|---------|------------------------|--------------|
| Customer Portal | +$500 (reduced calls) | 10 hrs/week |
| Payment Processing | +$2,000 (faster payments) | 5 hrs/week |
| Automated Emails | +$300 (better communication) | 3 hrs/week |
| Digital Signatures | +$0 (professional image) | 5 hrs/week |
| SMS Notifications | +$400 (fewer no-shows) | 2 hrs/week |
| Recurring Invoices | +$1,500 (service agreements) | 4 hrs/week |
| **Total Phase 1-3** | **+$4,700/month** | **29 hrs/week** |

---

## ?? Notes

- Features marked ????? should be prioritized
- Implementation times are estimates for one developer
- Some features can be implemented in parallel
- User feedback should drive priority adjustments
- Mobile features assume .NET MAUI platform

---

## ?? Status Tracking

Use this format to track progress:

```
- [ ] Customer Portal
- [ ] Payment Processing
- [x] Email Service (completed)
- [x] Enhanced Dashboard with Real-Time KPIs (completed)
- [x] Route Optimization (completed)
- [x] Smart Scheduling with Google Calendar (completed)
- [x] Database Migration Scripts (completed)
- [x] Demo Data Seeding Enhanced (completed)
- [ ] Digital Signatures
- [ ] Offline-First Sync (Mobile)
```

**Recently Implemented (This Session):**
- DashboardKpiService with comprehensive metrics (Revenue, Jobs, Customers, Receivables Aging, Estimates, Service Agreements, Inventory)
- RouteOptimizationService with nearest-neighbor algorithm and Google Maps integration
- GoogleCalendarService with ICS export and calendar URL generation
- Enhanced demo data seeding with GPS coordinates, Service Agreements, Warranty Claims, QuickNotes
- Settings page updated with Calendar & Maps configuration section
- Route Optimization page (/route-optimization) added to navigation

Update this file as features are completed!
