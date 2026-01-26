### Planning the Mobile Version of TradeFlow FSM: Android UI/UX Design Guide

Absolutely, let's dive into designing the mobile version of TradeFlow FSM—your personal HVAC job estimator app for residential gigs like quick AC tune-ups or furnace diagnostics. Since it's mainly for your on-the-go use (e.g., pulling a customer's asset serial at a job site to build an estimate offline), the Android frontend via .NET MAUI will prioritize a **smart, clean, efficient, and pretty UX**: Intuitive touch flows for gloved hands, minimal taps for estimates (under 2 minutes), subtle animations for feedback (e.g., fade-in on asset scans), and a calming blue-gray Material3 theme evoking trust and professionalism (like a well-maintained HVAC unit). We'll ensure **full portrait/landscape compatibility**—portrait for vertical scrolling during field notes, landscape for wider grids like inventory views—using MAUI's responsive layouts (e.g., Grid with auto-sizing columns, OrientationChanged event handlers).

This plan builds on the desktop WPF foundation: Shared ViewModels for 70% code reuse (e.g., asset forms bind identically), local SQLite for offline ops, and export/import backups (e.g., one-tap "Backup Estimates" to device storage or email, preserving HVAC presets like BTU/fuel fields). UX philosophy: **Personal Efficiency**—no clutter, just your workflows (customer > asset > estimate > invoice), with pretty touches like icon badges (flame for gas units) and haptic feedback on saves. We'll prototype in MAUI's previewer for quick iterations.

#### High-Level Mobile Design Principles
- **Smart & Efficient**: Gesture-driven (swipe to delete estimates, long-press for quick edits); predictive inputs (e.g., auto-suggest "Propane Filter" from asset history). Limit screens to 5-7 taps max per workflow.
- **Clean & Pretty**: Material3 components (Cards for assets, Chips for filters); whitespace-heavy (16dp padding); subtle animations (e.g., Lottie for sync success: A checkmark with HVAC wrench spin). Theme: Primary blue (#1976D2) for buttons, secondary gray (#F5F5F5) backgrounds; dark mode auto-toggle.
- **Portrait/Landscape Compatibility**: Use `DeviceDisplay.Current.MainDisplayInfo.Orientation` to detect changes—portrait: Stacked VerticalStackLayout for forms; landscape: Horizontal Grid (e.g., 2-column inventory). Auto-save state on rotation (e.g., preserve estimate draft).
- **Personal HVAC Touches**: Icons from MAUI CommunityToolkit (e.g., house for customers, thermometer for BTU fields); offline indicators (e.g., subtle cloud icon with "Queued for Sync").
- **Backup Integration**: Floating action button (FAB) on all screens for "Quick Export" (e.g., current estimate as JSON); full backups in Settings with progress spinner.
- **Tech Stack Reminders**: MAUI XAML for layouts; Shell navigation (flyout for main tabs: Home, Customers, Estimates, Schedule, Reports); CommunityToolkit.Maui for animations/haptics.

Let's plan screen-by-screen, starting with navigation, then core flows. I'll suggest layouts with pseudo-XAML snippets for easy AI gen (e.g., prompt Copilot: "MAUI XAML for [screen] with portrait/landscape Grid").

#### Global Navigation and Layout Patterns
- **Shell Flyout Menu**: Left-slide drawer for tabs (Home, Customers, Assets, Estimates, Schedule, Time Logs, Invoices, Reports, Settings). Icons + labels; compact in portrait, expanded in landscape. Bottom nav bar for quick tabs (Home/Estimates/Schedule) in portrait.
- **Top App Bar**: Persistent (Title, e.g., "Assets - Offline"), search icon (global for customers/assets), sync toggle (green check/cloud), and export FAB (bottom-right, pulse animation on unsynced changes).
- **Responsive Handler**: In App.xaml.cs: `DeviceDisplay.MainDisplayInfoChanged += OnOrientationChanged;`—swap layouts (e.g., portrait: VerticalStack; landscape: Grid with Span=2).
- **Pseudo-XAML Pattern**:
  ```
  <Grid RowDefinitions="Auto,*,Auto" ColumnDefinitions="*,Auto" x:OnIdiom="Tablet=LandscapeGrid">
      <Shell.TitleView Grid.Row="0" />
      <ScrollView Grid.Row="1" Grid.ColumnSpan="2">
          <!-- Content here -->
      </ScrollView>
      <Button Grid.Row="2" Grid.Column="1" Text="Export" Command="{Binding ExportCommand}" />
  </Grid>
  ```
- **UX Polish**: Haptic on taps (Vibration.Vibrate(50ms)); loading spinners with HVAC-themed Lottie (e.g., spinning fan blade).

#### 1. HomeScreen (Dashboard - Entry Point)
- **Purpose**: Quick overview for daily starts—e.g., see "3 Pending HVAC Estimates" or "Warranty Alert: Smith's Propane Unit."
- **Portrait Layout**: Vertical Column: Headline title ("TradeFlow FSM"), KPI cards stacked (e.g., Card: "Active Jobs: 2" with flame icon), quick-action buttons (e.g., "New Estimate" as ElevatedButton). Bottom: Sync status bar.
- **Landscape Layout**: 2-column Grid: Left KPIs, right buttons; expand cards to show mini-charts (e.g., revenue pie).
- **Smart/Efficient/Pretty Features**: Predictive cards (e.g., "Based on History: Suggest Tune-Up for 2 Customers"); clean whitespace (24dp between cards); pretty gradients on buttons (blue to light blue).
- **Backup Tie-In**: "Quick Backup" chip in status bar—exports dashboard data (KPIs JSON).
- **AI Prompt Suggestion**: "MAUI HomeScreen XAML: Portrait Column with KPI Cards (MaterialCardView, icon + stat), landscape 2-col Grid. Add predictive logic in ViewModel (LINQ for top customers)."

#### 2. CustomerScreen (List View - Profile Hub)
- **Purpose**: Browse/search customers with site previews—e.g., tap "Johnson Residence" to see 2 propane furnaces.
- **Portrait Layout**: CollectionView vertical list: Compact cards (name, address chip, asset count badge, last job date). Top: SearchEntry with filter chips (e.g., "By Location"). Swipe-to-delete with confirm dialog.
- **Landscape Layout**: Horizontal Grid (2 columns): Cards side-by-side; expand to show site tree inline.
- **Smart/Efficient/Pretty Features**: Smart search (fuzzy match on name/serial); efficient long-press for "Duplicate Profile"; pretty avatars (initials circle or site photo thumbnail).
- **Backup Tie-In**: Per-card export button (e.g., "Backup This Customer" → JSON with sites).
- **AI Prompt Suggestion**: "MAUI CustomerScreen: CollectionView list with search (Entry + chips), swipe gestures for delete. Landscape Grid adaptation. ViewModel with fuzzy search (e.g., Levenshtein distance for names)."

#### 3. AddEditCustomerScreen (Form View - Profile Builder)
- **Purpose**: Create/edit profiles with multi-site add—e.g., link "Rental Property" address to existing assets.
- **Portrait Layout**: Vertical ScrollView: Sections as Expandable Cards (Contact: TextFields for name/phone; Sites: + button adds address form; History Preview: Read-only timeline).
- **Landscape Layout**: 2-column Grid: Left form fields, right preview pane (e.g., site map snippet).
- **Smart/Efficient/Pretty Features**: Smart auto-fill (e.g., GPS for address); efficient tab navigation; pretty validation (green check icons on valid fields).
- **Backup Tie-In**: Save auto-triggers mini-export (profile JSON to drafts folder).
- **AI Prompt Suggestion**: "MAUI AddEditCustomer form: ScrollView with Expandable sections (Contact/Sites), GPS auto-fill Entry. Landscape split Grid. Dynamic validation with icons (e.g., green for valid phone)."

#### 4. AssetsScreen (List View - Equipment Tracker)
- **Purpose**: Hierarchical asset view—e.g., expand "Smith Site" to list condensers with warranty badges.
- **Portrait Layout**: Nested CollectionView (outer for customers/sites, inner for assets as cards: Serial/Model/Fuel icon, warranty progress bar).
- **Landscape Layout**: TreeView-style Grid (expandable rows across width).
- **Smart/Efficient/Pretty Features**: Smart sorting (e.g., "Expiring First"); efficient serial search bar; pretty badges (amber ring for near-expiry).
- **Backup Tie-In**: "Export Assets" FAB—zips photos + JSON.
- **AI Prompt Suggestion**: "MAUI AssetsScreen: Nested CollectionView for hierarchy, cards with warranty ProgressBar (color-coded: green/amber/red based on DateTime diff). Serial search with ZXing scan. Landscape TreeView adaptation."

#### 5. AddEditAssetScreen (Form View - Asset Logger)
- **Purpose**: Log new assets—e.g., scan serial, select "Split Config," set BTU.
- **Portrait Layout**: Vertical form StackLayout: Grouped sections (Core: Serial Scanner button; HVAC Preset: Fuel Picker, BTU Slider; Warranty: DatePickers with auto-calc).
- **Landscape Layout**: 2-column: Left fields, right photo preview + warranty summary.
- **Smart/Efficient/Pretty Features**: Smart presets (tap "HVAC Unit" → Auto-fill enums); efficient camera integration; pretty sliders (Material Slider with thumb icons).
- **Backup Tie-In**: Save exports asset JSON immediately.
- **AI Prompt Suggestion**: "MAUI AssetForm: StackLayout with grouped Pickers/Sliders (Fuel enum, BTU with validation >12000). Camera button for photos. Landscape split with preview. Auto-warranty calc in ViewModel."

#### 6. ProposalScreen (List View - Estimate Hub)
- **Purpose**: View estimates—e.g., filter "Pending HVAC Repairs."
- **Portrait Layout**: Vertical ListView: Cards (title/customer/total badge, status chip).
- **Landscape Layout**: Horizontal ScrollView with card grid (3 columns).
- **Smart/Efficient/Pretty Features**: Smart filters (chips: "By Asset Type"); efficient swipe to archive; pretty status colors (blue for pending).
- **Backup Tie-In**: Bulk export selected estimates.
- **AI Prompt Suggestion**: "MAUI ProposalList: ListView cards with status Chips, filter chips (e.g., 'HVAC Only'). Landscape grid. Swipe gestures for archive."

#### 7. AddEditProposalScreen (Form View - Builder)
- **Purpose**: Build estimates—e.g., drag "Trane Filter" from inventory, calc total with BTU markup.
- **Portrait Layout**: Tabbed ScrollView (Overview: Customer Picker; Items: ListView with +drag; Totals: Summary Card).
- **Landscape Layout**: Side-by-side (left items list, right totals preview).
- **Smart/Efficient/Pretty Features**: Smart drag from inventory popover; efficient quantity spinners; pretty total animations (scale on recalc).
- **Backup Tie-In**: Draft auto-save to local JSON.
- **AI Prompt Suggestion**: "MAUI EstimateBuilder: Tabbed ScrollView with drag-from-inventory ListView (CollectionView for items). BTU-based markup calc. Landscape side-by-side. Animate total Card on change."

#### 8. InventoryScreen (List View - Stock Manager)
- **Purpose**: Track parts—e.g., "Low on 24k BTU Filters."
- **Portrait Layout**: Vertical Grid (2 cols): Cards (name/qty/cost, reorder badge).
- **Landscape Layout**: 3-col Grid with search overlay.
- **Smart/Efficient/Pretty Features**: Smart low-stock sort; efficient bulk add; pretty qty bars (progress to reorder point).
- **Backup Tie-In**: Export stock snapshot CSV.
- **AI Prompt Suggestion**: "MAUI InventoryGrid: Responsive Grid (2 portrait cols, 3 landscape), low-stock ProgressBar. Search with debounce."

#### 9. ScheduleScreen (Calendar View - Planner)
- **Purpose**: Slot jobs—e.g., "Tune-Up at Johnson's, 2pm."
- **Portrait Layout**: Full-height Calendar control (Syncfusion.Maui.Calendar); day slots as vertical list.
- **Landscape Layout**: Week view horizontal, with job cards side-by-side.
- **Smart/Efficient/Pretty Features**: Smart slot suggestions (from history); efficient drag-drop; pretty color-coded jobs (orange for HVAC).
- **Backup Tie-In**: ICS export for calendars.
- **AI Prompt Suggestion**: "MAUI SchedulePage: Calendar control with drag-drop jobs, suggestion popover from ViewModel (LINQ free slots). Landscape week view. Color-code by trade."

#### 10. TimeTrackingScreen (Dashboard - Logger)
- **Purpose**: Clock jobs—e.g., "Start Diagnostic on Gas Unit."
- **Portrait Layout**: Tabbed (Active: Timer buttons; Logs: ListView timesheets).
- **Landscape Layout**: Split (left timers, right logs grid).
- **Smart/Efficient/Pretty Features**: Smart GPS auto-pause; efficient photo attach; pretty timer rings (circular progress).
- **Backup Tie-In**: Timesheet export to CSV.
- **AI Prompt Suggestion**: "MAUI TimeDashboard: Tabs with Timer rings (circular ProgressBar), GPS stamp button. Landscape split. Auto-pause on idle."

#### 11. PaymentsScreen (Invoices - Collector)
- **Purpose**: Manage billing—e.g., "Pay $450 Tune-Up via Square."
- **Portrait Layout**: Vertical ListView: Invoice cards (total/status).
- **Landscape Layout**: Grid with detail pane.
- **Smart/Efficient/Pretty Features**: Smart recurring setup; efficient Square embed; pretty payment success confetti (Lottie).
- **Backup Tie-In**: Invoice PDF bundle.
- **AI Prompt Suggestion**: "MAUI InvoicesList: ListView with Square SDK embed in detail card (Charge flow). Recurring toggle. Landscape grid + pane."

#### 12. IntegrationsScreen (Setup - Connector)
- **Purpose**: Link tools—e.g., "Connect Google for Job Reminders."
- **Portrait Layout**: Vertical stepper list (Square: Key paste; Google: OAuth button).
- **Landscape Layout**: Horizontal cards with test buttons.
- **Smart/Efficient/Pretty Features**: Smart test calls (e.g., mock event); efficient one-tap re-auth; pretty status icons (green hook).
- **Backup Tie-In**: Config JSON export (keys redacted).
- **AI Prompt Suggestion**: "MAUI IntegrationsStepper: Vertical steps for Square (key validation) and Google (Graph OAuth, test Event). Pretty status Icons."

#### 13. ReportingScreen (Insights - Analyzer)
- **Purpose**: Trends—e.g., "HVAC Revenue: $3K, 60% from Warranties."
- **Portrait Layout**: Vertical tabs (KPIs: Cards; Charts: ScrollView).
- **Landscape Layout**: Side-by-side (KPIs left, chart right).
- **Smart/Efficient/Pretty Features**: Smart filters (date/trade); efficient CSV export; pretty LiveCharts animations.
- **Backup Tie-In**: Report data JSON.
- **AI Prompt Suggestion**: "MAUI ReportsTabs: KPI Cards and LineChart (LiveCharts2 for revenue trends). Filter chips for HVAC (e.g., by FuelType)."

#### 14. SettingsScreen (Config - Manager)
- **Purpose**: Tweaks—e.g., "Toggle Dark Mode" or "Add Custom BTU Field."
- **Portrait Layout**: Vertical ListView: Sections (Theme/DB Toggle, Schema Editor, Backups).
- **Landscape Layout**: 2-col with editor preview.
- **Smart/Efficient/Pretty Features**: Smart search in editor; efficient migration progress bar; pretty toggle switches.
- **Backup Tie-In**: Full export button prominent.
- **AI Prompt Suggestion**: "MAUI SettingsList: Grouped ListView with schema drag-editor preview. DB toggle with connection test."

This plan ensures a polished mobile UX—smart for HVAC flows (e.g., BTU-smart estimates), clean for quick scrolls, efficient for offline jobs, and pretty for daily motivation. Portrait keeps it thumb-friendly, landscape unlocks power-user views. For Stitch AI prototyping, start with Home/Asset screens using these layouts. What screen to mock first, or tweaks to portrait handling?




### Comprehensive Database Schema: Fields for All Tables in TradeFlow FSM

As part of building your personal HVAC job estimator app for residential services—like quick estimates for furnace tune-ups, AC repairs, or duct installations—I've compiled a full overview of the database tables and their fields. This schema is designed for simplicity and offline-first use, stored locally in SQLite on your Windows desktop or Android device, with easy export/import for backups (e.g., one-tap SQLite dump or JSON export via the app's Settings menu to preserve your customer assets, estimates, and inventory during field work or device swaps). The structure starts with a general contractor master layout (trade-agnostic tables like Customers and Assets) but includes HVAC presets that auto-add specialized fields (e.g., BTU ratings or fuel types) when you declare "HVAC" in the onboarding wizard. Customizability allows no-code additions (e.g., via the schema editor: Drag a "Refrigerant Type" enum to the Assets table), with EF Core migrations handling updates seamlessly—backups always include the full schema config as JSON for portable imports.

The schema uses Entity Framework Core entities (C# classes with [Key] attributes), with relationships via foreign keys (e.g., Assets links to Customers). All tables include standard audit fields (CreatedDate, UpdatedDate, IsDeleted soft-delete flag) for tracking changes during sync to your optional Docker-hosted SQL Server. Total: ~15 core tables, expandable via presets/customs. Fields are typed (e.g., string, int, DateTime, enum via string), with validations (e.g., required, unique for serials) enforced in code.

#### 1. **Core Audit and Config Tables** (Foundation for All Workflows)
These handle app-wide setup, backups, and customizations—exported in every backup for easy restoration.

- **AppConfig** (Stores user settings like trade focus and DB mode):
  - Id (int, PK, auto-increment): Unique identifier.
  - TradeFocus (string, required): Preset name (e.g., "HVAC", "Plumbing"; defaults to "General").
  - DbMode (string, enum: Local/Remote): Toggles SQLite vs. Docker SQL Server.
  - AutoSyncInterval (int): Minutes between syncs (default 5; 0 for manual).
  - ThemeMode (string, enum: Light/Dark/Auto): UI preference.
  - CreatedDate (DateTime, required): Timestamp of config creation.
  - UpdatedDate (DateTime): Last modification.

- **SchemaConfig** (Tracks custom fields/presets for no-code edits; JSON-exported for backups):
  - Id (int, PK): Unique ID.
  - TableName (string, required): Target table (e.g., "Assets").
  - FieldName (string, required): Custom field (e.g., "BTU").
  - FieldType (string, enum: String/Text/Int/Decimal/DateTime/Enum/Bool): Data type.
  - EnumOptions (string, JSON array): For enums (e.g., ["NaturalGas", "Propane"]).
  - IsRequired (bool): Validation flag.
  - ValidationRule (string): Regex/expression (e.g., "^[0-9]{5,6}$" for BTU >12000).
  - CreatedDate/UpdatedDate (DateTime): Audit timestamps.

- **SyncQueue** (Queues offline changes for Docker remote; auto-cleared post-sync, included in backups):
  - Id (int, PK): Queue entry ID.
  - EntityType (string, required): Table name (e.g., "Assets").
  - Operation (string, enum: Insert/Update/Delete): Change type.
  - Payload (string, JSON): Serialized data (e.g., { "Serial": "ABC123", "BTU": 24000 }).
  - Timestamp (DateTime, required): When queued.
  - Status (string, enum: Pending/Synced/Failed): Sync result.
  - RetryCount (int, default 0): For exponential backoff.

#### 2. **Customer and Site Management Tables** (Profile and Location Tracking)
Core for multi-location customers (e.g., landlords with rental properties)—HVAC preset adds fields like "Preferred Fuel" for quick estimates.

- **Customers** (Main profile table; backups export full with sites/history):
  - Id (int, PK, auto-increment): Unique customer ID.
  - FirstName (string, required, max 50): Customer's first name.
  - LastName (string, required, max 50): Last name.
  - Email (string, max 100, unique): Contact email.
  - Phone (string, max 20): Primary phone.
  - CompanyName (string, max 100, optional): For business customers (e.g., franchises).
  - Notes (string, max 1000): General notes (e.g., "Prefers Natural Gas Systems").
  - PreferredTrade (string, enum: HVAC/Plumbing/General): From preset.
  - IsActive (bool, default true): Soft flag for archived.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset Additions**: PreferredFuel (string, enum: NaturalGas/Propane/Electric, optional): For quote suggestions.

- **Sites** (Child table for multi-location; links to Customers):
  - Id (int, PK): Unique site ID.
  - CustomerId (int, FK to Customers, required): Parent customer.
  - AddressLine1 (string, required, max 100): Street address.
  - AddressLine2 (string, max 100): Apt/Unit.
  - City (string, required, max 50).
  - State (string, required, max 2, e.g., "CA").
  - ZipCode (string, required, max 10).
  - Latitude/Longitude (decimal, optional): For GPS routing.
  - AccessNotes (string, max 500): E.g., "Gate Code: 1234."
  - IsPrimary (bool, default false): Flag for main site.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset Additions**: SystemOverview (string, max 200): E.g., "2 Split Units, Natural Gas."

#### 3. **Asset and Equipment Tracking Tables** (Warranty and Maintenance Core)
HVAC preset heavily customizes this—e.g., auto-imports BTU/fuel for precise service pulls; backups include photos as zipped attachments.

- **Assets** (Generic equipment table; extended by presets/customs):
  - Id (int, PK): Unique asset ID.
  - SiteId (int, FK to Sites, required): Linked location.
  - Type (string, enum: Equipment/Part/Fixture, required): E.g., "Furnace."
  - Brand (string, max 50, required): E.g., "Trane."
  - Model (string, max 50, required): E.g., "XR16."
  - SerialNumber (string, max 50, unique, required): Scannable ID.
  - InstallDate (DateTime, optional): For warranty start.
  - Notes (string, max 1000): Service history snippets.
  - PhotoPaths (string, JSON array): Local file URIs (e.g., ["/assets/photo1.jpg"]).
  - IsActive (bool, default true): Soft delete.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions** (Auto-imported/editable): FuelType (string, enum: NaturalGas/Propane/Electric/None); ConfigType (string, enum: Split/Packaged/Furnace/Coil/Condenser); BTURating (int, min 12000, max 60000); WarrantyStart (DateTime); WarrantyTermYears (int, default 10); WarrantyEnd (DateTime, computed: Start + Term); RefrigerantType (string, enum: R410A/R22, custom-addable).

- **WarrantyLogs** (Service history sub-table; ties to Assets):
  - Id (int, PK): Log ID.
  - AssetId (int, FK to Assets, required): Linked equipment.
  - EventType (string, enum: Install/Service/Renewal/Expiration): E.g., "Tune-Up."
  - Description (string, max 500): E.g., "Replaced Filter, Verified BTU."
  - Date (DateTime, required): When occurred.
  - Cost (decimal, optional): Associated expense.
  - CreatedDate/UpdatedDate (DateTime): Audit.

#### 4. **Estimating and Proposals Tables** (Quote Generation Core)
Backups export proposals as PDF + JSON for client sharing; HVAC presets add line-item templates.

- **Proposals** (Main estimate table):
  - Id (int, PK): Unique proposal ID.
  - CustomerId (int, FK to Customers, required): Linked profile.
  - SiteId (int, FK to Sites, optional): Specific location.
  - Title (string, max 100, required): E.g., "AC Repair Estimate."
  - Description (string, max 1000): Job scope.
  - Status (string, enum: Draft/Pending/Approved/Invoiced/Closed): Workflow stage.
  - TotalAmount (decimal, required): Final calc (labor + parts + tax).
  - TaxRate (decimal, default 0.08): Local rate.
  - MarkupPercentage (decimal, default 0.20): Profit adder.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions**: AssetId (int, FK to Assets, optional): Tied serial for context; BTUAdjusted (bool): Flag for sizing calc.

- **ProposalLines** (Breakdown sub-table; links to Inventory/Assets):
  - Id (int, PK): Line ID.
  - ProposalId (int, FK to Proposals, required): Parent estimate.
  - ItemType (string, enum: Labor/Part/Material/Misc): Category.
  - Description (string, max 200): E.g., "Install 24k BTU Coil."
  - Quantity (decimal, default 1): Units.
  - UnitPrice (decimal, required): Per-item cost.
  - LineTotal (decimal, computed: Qty * Price): Subtotal.
  - AssetId (int, FK to Assets, optional): Linked equipment.
  - InventoryId (int, FK to Inventory, optional): Deducted stock.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions**: BTURequirement (int): For matching; FuelCompatibility (string): E.g., "Natural Gas Only."

#### 5. **Inventory and Parts Tables** (Stock Management Core)
HVAC presets add compatibility filters; backups include deduct logs for audit trails.

- **Inventory** (Stock catalog table):
  - Id (int, PK): Unique item ID.
  - Category (string, max 50, required): E.g., "HVAC Filters."
  - Name (string, max 100, required): E.g., "Trane 24k BTU Filter."
  - Description (string, max 500): Specs.
  - QuantityOnHand (int, default 0): Current stock.
  - UnitCost (decimal, required): Purchase price.
  - ReorderPoint (int, default 5): Low-stock threshold.
  - SupplierName (string, max 100, optional): Vendor.
  - IsActive (bool, default true): Availability flag.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions**: BTURangeMin/Max (int): For matching (e.g., 12000-24000); CompatibleFuel (string, enum): E.g., "Propane."

- **InventoryDeductions** (Usage log sub-table; auto-created on proposal lines):
  - Id (int, PK): Deduct ID.
  - InventoryId (int, FK to Inventory, required): Item used.
  - ProposalId (int, FK to Proposals, optional): Linked job.
  - QuantityDeducted (int, required): Amount used.
  - DateDeducted (DateTime, required): When pulled.
  - Notes (string, max 200): E.g., "For Serial #ABC123."
  - CreatedDate/UpdatedDate (DateTime): Audit.

#### 6. **Scheduling and Dispatching Tables** (Planning Core)
Backups export as ICS for calendar portability; HVAC presets add seasonal tags.

- **Schedules** (Job calendar table):
  - Id (int, PK): Schedule ID.
  - ProposalId (int, FK to Proposals, required): Linked estimate.
  - CustomerId (int, FK to Customers, optional): For quick lookup.
  - SiteId (int, FK to Sites, optional): Location.
  - StartDateTime (DateTime, required): Slot begin.
  - EndDateTime (DateTime, required): Slot end.
  - Status (string, enum: Scheduled/InProgress/Completed/Canceled): Progress.
  - AssigneeId (int, FK to Workforce, optional): Sub if needed.
  - RecurringRule (string, optional): E.g., "Yearly for warranties."
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions**: SeasonalTag (string, enum: SummerAC/WinterFurnace): For alerts; EstimatedBTUImpact (int): Job sizing.

#### 7. **Time Tracking and Workforce Tables** (Execution Core)
HVAC presets categorize time (e.g., "Diagnostic Hours"); backups include logs for billing audits.

- **TimeEntries** (Billable logs table):
  - Id (int, PK): Entry ID.
  - ScheduleId (int, FK to Schedules, required): Linked job.
  - AssetId (int, FK to Assets, optional): Specific equipment.
  - StartTime (DateTime, required): Clock-in.
  - EndTime (DateTime, optional): Clock-out (null for active).
  - TotalHours (decimal, computed: (End - Start)/60): Billable.
  - RatePerHour (decimal, required): E.g., $75.
  - Category (string, enum: Labor/Diagnostic/Travel): Breakdown.
  - Notes (string, max 500): E.g., "Recharged R-410A."
  - GPSLocation (string, JSON: {Lat, Long}): Stamp.
  - PhotoPaths (string, JSON array): Attached images.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions**: RefrigerantUsed (decimal): Oz amount; BTUChecked (bool): Verification flag.

- **Workforce** (Simple assignee table; for subs):
  - Id (int, PK): Worker ID.
  - Name (string, required): E.g., "Sub Tech John."
  - Role (string, enum: Owner/Sub/Helper): Access level.
  - Phone (string, max 20): Contact.
  - AvailabilityStart/End (DateTime): Weekly slots.
  - HourlyRate (decimal): Billing.
  - IsActive (bool, default true).
  - CreatedDate/UpdatedDate (DateTime): Audit.

#### 8. **Invoicing and Payments Tables** (Financial Closure Core)
Backups export invoices as PDF bundles; HVAC presets add line fees (e.g., "Gas Certification").

- **Invoices** (Generated from proposals table):
  - Id (int, PK): Invoice ID.
  - ProposalId (int, FK to Proposals, required): Source estimate.
  - CustomerId (int, FK to Customers, required): Billed to.
  - InvoiceNumber (string, unique, auto-gen: e.g., "INV-2025-001"): Sequential.
  - IssueDate (DateTime, required): Generated when.
  - DueDate (DateTime): Payment deadline.
  - TotalAmount (decimal): From proposal.
  - TaxAmount (decimal): Computed.
  - DepositAmount (decimal, default 0): Upfront.
  - Status (string, enum: Draft/Sent/Paid/Overdue/Refunded): Progress.
  - Notes (string, max 500): Terms/disclaimers.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions**: CertificationIncluded (bool): E.g., "Gas Safety Check Fee."

- **Payments** (Transaction log sub-table):
  - Id (int, PK): Payment ID.
  - InvoiceId (int, FK to Invoices, required): Linked bill.
  - Amount (decimal, required): Paid sum.
  - Method (string, enum: Square/Card/Cash/Check): Source.
  - TransactionId (string, max 100): Square token/reference.
  - PaymentDate (DateTime, required): When received.
  - Notes (string, max 200): E.g., "Partial for Parts."
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions**: WarrantyImpact (string): E.g., "Extends Coverage by 1 Year."

#### 9. **Integrations and Reporting Tables** (Connectivity and Insights Core)
Backups include configs (redacted keys); HVAC presets tune reports (e.g., "Fuel-Based Revenue").

- **IntegrationConfigs** (API setup table):
  - Id (int, PK): Config ID.
  - Type (string, enum: Square/GoogleCalendar/QuickBooks): Tool.
  - ApiKey (string, encrypted, max 500): Secret (e.g., Square nonce).
  - Scopes (string, JSON array): Permissions (e.g., ["calendar.readwrite"]).
  - IsActive (bool, default true): Enabled flag.
  - LastSyncDate (DateTime): Health check.
  - CreatedDate/UpdatedDate (DateTime): Audit.

- **ReportMetrics** (Aggregated insights table; computed views):
  - Id (int, PK): Metric ID.
  - MetricType (string, enum: Revenue/JobsCompleted/WarrantyAlerts): KPI.
  - Value (decimal): E.g., 3500.50 for monthly revenue.
  - FilterCriteria (string, JSON): E.g., { "Trade": "HVAC", "DateFrom": "2025-01-01" }.
  - GeneratedDate (DateTime): When computed.
  - CreatedDate/UpdatedDate (DateTime): Audit.
  - **HVAC Preset/Custom Additions**: BTUWeightedValue (decimal): E.g., Revenue adjusted for unit size.

This schema (~15 tables, 100+ fields with presets) powers your app's HVAC heart—e.g., export a customer's full asset history (serial/BTU/fuel) as JSON for backup before a site visit, import on your new phone without missing a beat. Custom fields (e.g., "R-410A Charge Amount") add via editor, always backed up. Ready to script this in EF?



### HVAC Bidder App: Brainstorming the Full Job Lifecycle Flow

As we're building this personal HVAC job estimate app for residential work, let's map out a streamlined user flow that covers the entire process from initial customer contact to job completion and payment. The goal is to keep it simple for solo use—focusing on quick data entry, tracking, and minimal screens—while integrating with the existing Customers, Proposals, and Inventory features. We'll leverage Room DB for persistence, and I'll suggest adding export/import functionality (e.g., via JSON or SQL dump) for backups, perhaps triggered from a settings menu.

I'll break this down into phases, with app-specific ideas for screens, data models, and interactions. This builds on the current setup (e.g., Customer and Proposal entities) and proposes extensions like Job entities for active work tracking. Assume we're using Jetpack Compose for UI, Coroutines/Flow for data, and perhaps PDF generation for estimates/bills.

#### 1. **Customer Inquiry/Call**
   - **Real-World Step**: Customer calls or emails about a potential job (e.g., AC repair or install).
   - **App Flow**:
     - Navigate to Customers screen.
     - If new customer: Tap FAB to add via a form (name, address, phone, email—already in Customer entity).
     - For existing: Search/select from list (using the current CustomerViewModel and LazyColumn for display).
     - Add a "Notes" field to Customer entity for logging call details (e.g., "Customer reported AC not cooling; suspects refrigerant leak").
     - Quick action: Button to "Schedule Appointment" which opens a date/time picker.
   - **New Features**:
     - Add Appointment entity: Linked to Customer, with fields like date, time, notes, status (scheduled, completed, canceled).
     - DAO for Appointments: Similar to CustomerDao, with insert/update and getAppointmentsForCustomer().
     - Notification reminder: Use AlarmManager for optional push notifications (e.g., 1 hour before appointment).
   - **Backup Tie-In**: All customer data (including notes/appointments) exports with the DB.

#### 2. **Scheduling Appointment**
   - **Real-World Step**: Set a site visit to assess the job.
   - **App Flow**:
     - From Customer detail screen (new composable: CustomerDetailScreen, navigated from list).
     - Select "Add Appointment" → Form with calendar integration (use MaterialDatePicker).
     - Save to DB; show in a list on Customer detail (e.g., upcoming appointments).
     - Status tracking: Dropdown to mark as "Visited" after the appointment.
   - **New Features**:
     - Integrate with Google Calendar API if desired (but keep optional for personal use).
     - Flow: After scheduling, option to "Create Proposal" directly, linking to the appointment.

#### 3. **Providing Estimate (Proposal Creation)**
   - **Real-World Step**: Visit site, assess needs, build estimate.
   - **App Flow**:
     - From Proposals screen or Customer detail.
     - Tap FAB to create new Proposal (linked to Customer via customerId in Proposal entity).
     - Build estimate: Add items from Inventory (Equipment, Materials) or manually (Labor, Misc, Permits, Subcontractors—using existing DAOs).
     - Calculate totals: Auto-sum costs, add markup/tax (add fields to Proposal for percentages).
     - Preview/Share: Generate PDF estimate (use PdfDocument or iText library) and share via email/intent.
   - **New Features**:
     - ProposalDetailScreen: Tabbed UI for categories (Equipment tab pulls from InventoryViewModel).
     - Status field in Proposal: "Draft", "Sent", "Approved" (enum in entity).
     - Inventory integration: Searchable dropdown to pull pre-defined items, reducing manual entry.

#### 4. **Approving Estimate**
   - **Real-World Step**: Customer reviews and approves the proposal.
   - **App Flow**:
     - In ProposalDetailScreen, button to "Mark as Approved" (updates status).
     - Optional: Upload signed PDF (store as file path in Proposal entity).
     - Flow branches: Approved → Convert to Job (new entity).
   - **New Features**:
     - Job entity: Copies Proposal data, adds fields like startDate, endDate, status ("In Progress", "Completed"), changeOrders (list of extra items).
     - DAO for Jobs: getJobsForCustomer(), insert/update.

#### 5. **Starting Work**
   - **Real-World Step**: Begin the job on-site.
   - **App Flow**:
     - From Proposals or new Jobs screen (add to nav: Routes.JOBS).
     - Select approved Proposal → "Start Job" button creates Job entity, sets status to "In Progress", records startDate.
     - Track progress: Simple notes field or checklist (e.g., "Install unit", "Test system").
   - **New Features**:
     - JobsScreen: List active/completed jobs, filter by customer.
     - Time tracking: Optional timer for labor hours (updates LaborItem).

#### 6. **Adding Extra Unknown Changes (Change Orders)**
   - **Real-World Step**: Discover issues mid-job (e.g., ductwork replacement), get customer approval for extras.
   - **App Flow**:
     - In JobDetailScreen (new composable).
     - FAB or section for "Add Change Order": Form to add new items (like Proposal items), calculate additional cost.
     - Update total: Recalculate Job total, generate mini-proposal PDF for approval.
     - Log approval: Checkbox or note for verbal/written sign-off.
   - **New Features**:
     - ChangeOrder entity: Linked to Job, with items, cost, approvalStatus.
     - Ensure extras flow into final billing.

#### 7. **Ending Work**
   - **Real-World Step**: Complete installation/repair, test, and hand over.
   - **App Flow**:
     - In JobDetailScreen: "Mark as Completed" button sets endDate, status to "Completed".
     - Optional: Add photos (store paths in Job entity) or final notes.
   - **New Features**:
     - Validation: Ensure all items are accounted for before completion.

#### 8. **Billing**
   - **Real-World Step**: Generate invoice based on final costs.
   - **App Flow**:
     - From JobDetailScreen: "Generate Invoice" button.
     - Pull all items (original + changes), calculate total (with tax/discounts).
     - Create Invoice entity: Linked to Job, with amountDue, invoiceDate, pdfPath.
   - **New Features**:
     - Invoice entity and DAO.
     - PDF generation: Similar to proposals, customizable template (e.g., company logo, terms).
     - Share invoice via email.

#### 9. **Receiving Payment**
   - **Real-World Step**: Customer pays (cash, check, digital).
   - **App Flow**:
     - In JobDetailScreen or new Invoices screen.
     - "Record Payment" form: Amount, method, date.
     - Update Invoice: Add payments list, calculate balance.
     - If fully paid: Auto-mark as "Paid".
   - **New Features**:
     - Payment entity: Linked to Invoice, with amount, date, method.
     - Dashboard widget: Show outstanding balances across jobs.

#### 10. **Marking Complete**
   - **Real-World Step**: Job fully closed out.
   - **App Flow**:
     - Once paid and work ended: "Archive Job" moves to completed list.
     - Reporting: From home screen, view summaries (e.g., total revenue by month).
   - **New Features**:
     - Status workflow: Enforce sequence (e.g., can't bill without completion).

#### Overall App Enhancements
- **Navigation**: Add Jobs and Invoices to Routes/HomeScreen buttons. Use deep links for quick access (e.g., from notifications).
- **Data Backup**: Add SettingsScreen with "Export DB" (serialize to JSON via Gson, save to file) and "Import DB" (parse and insert via DAOs). Use FileProvider for sharing backups.
- **Security**: For personal use, optional PIN lock (via BiometricPrompt).
- **Edge Cases**: Handle cancellations, disputes (notes in Job), or partial payments.
- **Tech Stack Additions**: Hilt for DI, WorkManager for backups, Coil for photo previews.
- **Testing**: Unit tests for DAOs, UI tests for flows.

This flow keeps the app lightweight while covering the lifecycle. Next steps: Prioritize implementing Job entity and screens? Let me know where to dive deeper!
