Fantasy Football

### Application Overview
Fantasy Football Dashboard: Web app for large TVs displaying real-time stats, calculated points, rankings. Features auto-refresh/polling, auto-scroll, custom scoring, multi-API support (Yahoo default). Passive mode: minimal interaction. Future: Mobile via shared assets (e.g., Blazor to MAUI).

### Tech Stack
- **IDE**: Visual Studio 2026 (Community/Professional).
- **Backend**: ASP.NET Core 8+ (Web API for endpoints, SignalR for WebSockets/real-time).
- **Frontend**: Blazor WebAssembly (for offline-capable, shareable with MAUI mobile).
- **Database**: None initially; use in-memory/cache (Redis optional for sessions).
- **Auth/Integrations**: OAuth1.0 for Yahoo; API keys for others.
- **Libs**: HttpClient for APIs; Cron.NET for polling; Newtonsoft.Json/XML for parsing; MudBlazor for UI components (material design, responsive).
- **Build**: .NET SDK 8+; npm for any JS interop if needed.
- **Deployment**: Azure/IIS for web; future MAUI for iOS/Android/Windows.

Create solution: File > New > Project > Blazor Web App (.NET 8). Select WebAssembly interactive render mode. Add ASP.NET Core Web API project for backend.

### Project Structure
- **Solution**: FantasyDashboard.sln
  - **FantasyDashboard.Web** (Blazor WASM project): UI components, pages, shared assets (CSS, images, models).
  - **FantasyDashboard.API** (ASP.NET Core Web API): Controllers, services for API polling, calculations.
  - **FantasyDashboard.Shared** (Class Library): Shared models, formulas, configs (for web/mobile reuse).
  - **FantasyDashboard.Tests** (xUnit): Unit tests for formulas, API mocks.

Shared assets: Place CSS/JS/images in Shared project; reference in Web/API. For mobile: Add MAUI project later, reference Shared.

### Data Models (in Shared)
Define classes for stats/players (JSON/XML serializable).

```csharp
public class Player
{
    public string Name { get; set; }
    public string Position { get; set; } // QB, RB, etc.
    public string Team { get; set; }
    public Dictionary<string, double> Stats { get; set; } // e.g., "PassYds": 250
    public double FantasyPoints { get; set; }
}

public class ScoringConfig
{
    // Defaults from Yahoo (expandable)
    public double PassYdsPerPt { get; set; } = 0.04; // 1/25
    public double PassTdPt { get; set; } = 4;
    // ... all from earlier formulas (QB, RB, WR/TE, K, DST, IDP)
    public Dictionary<string, double> DstPaTiers { get; set; } = new() { { "0", 10 }, { "1-6", 7 } /* etc. */ };
}
```

API response models: e.g., YahooScoreboardResponse (XML deserialization).

### Backend (API Project)
- **Services**:
  - ApiService: Abstract base for providers (Yahoo, Sleeper, etc.).
    - YahooApiService: Implements OAuth1.0 (use Yahoo.OAuth NuGet or custom HttpClient with HMAC-SHA1 signing).
      - Methods: GetRequestTokenAsync(), GetAccessTokenAsync(verifier), SignedGetAsync(endpoint).
    - Other: Sleeper (no auth, HttpClient.GetAsync), ESPN (wrapper via NuGet if avail, else custom), etc.
  - PollingService: Use Cron.NET to schedule (e.g., every 60s).
    ```csharp
    public class PollingService : BackgroundService
    {
        private readonly IHubContext<DashboardHub> _hub;
        private readonly ApiService _api;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cron = new CronDaemon();
            cron.Add("*/60 * * * * *", async () => await PollAndBroadcast());
            cron.Start();
            await Task.Delay(-1, stoppingToken);
        }

        private async Task PollAndBroadcast()
        {
            var data = await _api.FetchData(); // e.g., scoreboard, players/stats
            var calculated = CalculatePoints(data); // Apply formulas
            await _hub.Clients.All.SendAsync("UpdateDashboard", calculated);
        }
    }
    ```
  - PointCalculatorService: Apply formulas from history.
    ```csharp
    public double CalculateQbPoints(Player player, ScoringConfig config)
    {
        return (player.Stats.GetValueOrDefault("PassYds", 0) * config.PassYdsPerPt) +
               (player.Stats.GetValueOrDefault("PassTd", 0) * config.PassTdPt) +
               // ... full QB formula + rush components
               (player.Stats.GetValueOrDefault("FumLost", 0) * -2);
    }
    // Similar for RB, WR/TE, K, DST, IDP
    ```
- **Controllers**: SetupController for wizard (POST /setup/provider), DataController for manual fetches.
- **SignalR**: Add DashboardHub.
  ```csharp
  public class DashboardHub : Hub { }
  ```
- Startup.cs: Add services, CORS for Blazor, SignalR mapping.

### Frontend (Blazor Web Project)
- **Pages**:
  - Index.razor: Main dashboard.
  - Setup.razor: Wizard (use MudBlazor Stepper).
- **Components** (in Shared for reuse):
  - TopBanner.razor: League name, week, ticker (use MudCarousel for scrolling? or CSS marquee).
  - PositionColumn.razor: MudTable for each (QB, etc.), sortable, color-coded (MudTd style="@GetColor(stat)").
    ```razor
    <MudTable Items="@Players" Dense="true" Hover="true">
        <HeaderContent>
            <MudTh>Player</MudTh> <!-- etc. -->
        </HeaderContent>
        <RowTemplate>
            <MudTd>@context.Name</MudTd>
            <!-- Stats with colors: green/red -->
        </RowTemplate>
    </MudTable>
    ```
    Auto-scroll: JS interop (InvokeAsync("scrollToTop")) on timer.
  - OverallColumn.razor: Ranked MudList, larger FPts font.
  - BottomStrip.razor: Refresh countdown (Timer component), scoring badge.
  - CustomScoringModal.razor: MudDialog with sliders/inputs for config, save to localStorage/IndexedDB.
- **JS Interop**: For auto-refresh (setInterval), scrolling.
- **State Management**: Fluxor or singleton service for data, config.
- **Real-time**: Connect to SignalR on load.
  ```razor
  @inject IJSRuntime JS
  protected override async Task OnInitializedAsync()
  {
      var dotNetRef = DotNetObjectReference.Create(this);
      await JS.InvokeVoidAsync("initSignalR", dotNetRef);
  }

  [JSInvokable]
  public void UpdateFromHub(object data) { /* Update state */ }
  ```
- **Responsive**: MudBlazor breakpoints; for TV/large: full columns; mobile future: stack/accordion.

### Setup Wizard
In Setup.razor: MudStepper.
- Step 1: Provider dropdown (Yahoo default).
- Step 2: Provider auth (e.g., Yahoo: Navigate to OAuth URL, callback handler).
- Step 3: League ID/season select (fetch via API).
- Step 4: Custom scoring checkbox ? open modal.
- Save config: LocalStorageService (Blazored.LocalStorage NuGet).

### Testing/Expansion
- Tests: Mock ApiService, assert formulas.
- Mobile: Add MAUI project, reference Shared/Web (host BlazorWebView), adapt layout (MudBreakpoint).
- Security: Store tokens encrypted (ProtectedLocalStorage).
- Performance: Cache API responses, throttle polls.






### Researched APIs
- **Yahoo (Default)**: OAuth1.0 auth. Register app at https://developer.yahoo.com/apps for keys. 3-legged flow: Get request token, user auth, access token. Endpoints: /fantasy/v2/league/{key}/scoreboard, /players/stats.
- **Sleeper**: No auth needed. Public read-only. Start with username to get user_id via GET /v1/user/{username}. Endpoints: /v1/leagues/nfl/{season}, /v1/players/nfl. Docs: https://docs.sleeper.com/.
- **ESPN**: Use espn-api wrapper (pip install). No auth; need league_id/year. Init: League(league_id=XXX, year=2026). Access stats via league objects. GitHub: https://github.com/cwendt94/espn-api.
- **API-Football**: Register at https://dashboard.api-football.com for API key. Use in headers (x-apisports-key). Endpoints: /players, /fixtures/statistics, /predictions. Docs: https://www.api-football.com/documentation-v3.
- **SportsDataIO**: Subscribe at https://sportsdata.io for API key. Use in query/header. Endpoints: /v3/nfl/stats/json/PlayerSeasonStats/{season}, /projections/json/PlayerGameProjectionStatsByWeek/{season}/{week}. Docs: https://sportsdata.io/developers/api-documentation/nfl.

### Setup Screen Design (Wizard for Non-Tech Users)
Simple, full-screen wizard with large buttons, progress bar, plain English. No jargon; use images/tooltips. Auto-advance on success.

1. **Welcome**: "Let's set up your fantasy football dashboard. Choose provider (Yahoo default)." Dropdown: Yahoo, Sleeper, ESPN, API-Football, SportsDataIO. Next button.
2. **Provider-Specific Setup**:
   - **Yahoo**: "Sign in to Yahoo." Button: "Login with Yahoo" (triggers OAuth redirect). Success: "Connected! Enter league ID (from Yahoo URL)." Input field with example. Validate/fetch data.
   - **Sleeper**: "Enter your Sleeper username." Input field. Button: "Connect." Fetches user_id/leagues. Dropdown: Select league.
   - **ESPN**: "Enter your ESPN league ID (from league URL)." Input. Optional: Year (default current). Button: "Connect." Uses wrapper to fetch.
   - **API-Football**: "Sign up at api-football.com for free key." Link button. "Paste your API key here." Input. Button: "Test Connection" (call /status).
   - **SportsDataIO**: "Subscribe at sportsdata.io for key." Link. "Paste key here." Input. Button: "Connect." Test with /CurrentSeason.
3. **Common Steps**: "Select league/season." Dropdowns auto-populated from API. "Customize scoring?" Checkbox, defaults to provider's (e.g., Yahoo standard).
4. **Finish**: "Setup complete! Dashboard ready." Save config (localStorage/cookies for simplicity). Error handling: "Oops, try again" with retry.

### Development Plan
1. **Backend (Node.js/Express)**: API router for providers. Implement auth/polling per API (OAuth for Yahoo, keys for others). Calculate points using formulas. WebSockets for real-time. Store user tokens securely.
2. **Frontend (React)**: Wizard component (react-wizardry lib). Dashboard: Columns as visualized, auto-refresh (useSWR). Custom scoring form with inputs/sliders.
3. **Integration**: Abstract API calls (e.g., service classes per provider). Default Yahoo. Polling: Cron every 60s, emit updates.
4. **Testing/Deploy**: Unit tests for calcs. Deploy to Vercel. Accessibility: Large text, voice prompts for non-literate.
5. **Timeline**: Week 1: Research/setup APIs. Week 2: Backend integrations. Week 3: Wizard UI. Week 4: Dashboard + polling. Week 5: Test/customize.



**Expanded Custom Scoring Formulas (Customizable in App)**

Use configurable variables (e.g., via UI sliders/inputs) to override defaults. Formulas calculate fantasy points (FPts) from stats.

- **Passing**: FPts = (PassYds / YdsPerPt) + (PassTD * TD_Pt) + (PassINT * INT_Pt) + (Completions * Comp_Pt) + (2PtPass * 2Pt_Pt)  
  Defaults: YdsPerPt=25 (0.04/pt), TD_Pt=4, INT_Pt=-1, Comp_Pt=0, 2Pt_Pt=2.

- **Rushing**: FPts = (RushYds / YdsPerPt) + (RushTD * TD_Pt) + (2PtRush * 2Pt_Pt) + (FumLost * Fum_Pt)  
  Defaults: YdsPerPt=10 (0.1/pt), TD_Pt=6, 2Pt_Pt=2, Fum_Pt=-2.

- **Receiving**: FPts = (RecYds / YdsPerPt) + (RecTD * TD_Pt) + (Receptions * PPR_Pt) + (2PtRec * 2Pt_Pt)  
  Defaults: YdsPerPt=10 (0.1/pt), TD_Pt=6, PPR_Pt=0.5 (half-PPR), 2Pt_Pt=2.

- **Kicking**: FPts = Tiered FG (0-39:3, 40-49:4, 50+:5) + (PAT * PAT_Pt) + (FG_Miss * Miss_Pt)  
  Defaults: PAT_Pt=1, Miss_Pt=0 (customizable tiers).

- **DST**: FPts = Sacks *1 + INT *2 + FumRec *2 + DefTD *6 + ST_TD *6 + Safety *2 + Block *2 + XPR *2 + PA_Tier (0:10,1-6:7,7-13:4,14-20:1,21-27:0,28-34:-1,35+:-4)  
  All values customizable.

App Implementation: Store as JSON config, apply formulas in backend on stat fetch, recalculate live on UI changes.


**QB Formulas:**
FPts = (PassYds / 25) + (PassTD * 4) + (INT * -1) + (RushYds / 10) + (RushTD * 6) + (FumLost * -2) + (OffFumRetTD * 6)

**RB Formulas:**
FPts = (RushYds / 10) + (RushTD * 6) + (Rec * 0.5) + (RecYds / 10) + (RecTD * 6) + (RetTD * 6) + (2PtConv * 2) + (FumLost * -2) + (OffFumRetTD * 6)

**WR/TE Formulas:**
FPts = (Rec * 0.5) + (RecYds / 10) + (RecTD * 6) + (RetTD * 6) + (2PtConv * 2) + (FumLost * -2) + (OffFumRetTD * 6)

**K Formulas:**
FPts = FG(0-39: 3 each) + FG(40-49: 4 each) + FG(50+: 5 each) + (PAT * 1)

**DST Formulas:**
FPts = (Sack * 1) + (INT * 2) + (FumRec * 2) + (TD * 6) + (Safety * 2) + (BlkKick * 2) + (RetTD * 6) + PA(0:10, 1-6:7, 7-13:4, 14-20:1, 21-27:0, 28-34:-1, 35+:-4) + (XPR * 2)


**IDP (Individual Defensive Player) Formulas** (Yahoo default/common settings):

- **Tackle Solo**: 1.5 pts  
- **Tackle Assist**: 0.75 pts  
- **Tackle Total**: 1 pt (alternative to solo/assist split)  
- **Sack**: 4 pts  
- **Interception**: 5 pts  
- **Fumble Forced**: 3 pts  
- **Fumble Recovered**: 4 pts  
- **Defensive TD**: 6 pts  
- **Safety**: 2 pts  
- **Pass Defended**: 1.5 pts  
- **Blocked Kick**: 3 pts  
- **Extra Point Returned**: 2 pts  

Most leagues combine these into LB, DB, DL scoring groups with slight variations (e.g., higher tackle value for LB, higher sack value for DL). All values customizable in the app.


**UI Layout Specification for Designer – Large-Screen Fantasy Football Dashboard**

**Target display**  
- 55–85" 4K TV landscape orientation  
- Viewing distance: 8–15 ft  
- Zero to minimal user interaction (kiosk / passive mode)  
- Always full-screen, no browser chrome

**Overall structure** (100% viewport)

1. **Top Banner** (height 8–10%)  
   - Background: dark gradient (#0f172a ? #1e293b)  
   - Left: League name + logo (large, bold white text, 48–72px)  
   - Center: “Week X • YYYY Season” + live game clock / next slate countdown (red accent when <15 min)  
   - Right: Current total fantasy points scored league-wide this week (huge number, 96px, green)  
   - Bottom edge: thin horizontal scrolling news ticker (injuries, big plays, Yahoo headlines, 24px white on semi-transparent black bar, speed 8–12 sec full cycle)

2. **Main Content Area** (height 82–84%)  
   - 5 equal-width columns (20% each) with 8–12 px gutter  
   - All columns scroll independently if content overflows (smooth, slow auto-scroll every 20–30 s when idle)  
   - Column header: position abbreviation (QB / RB / WR/TE / K/DST / OVERALL) – 48px bold, centered, team-color accent bar underneath  
   - Row height: 80–100 px per player entry  
   - Font: sans-serif (Inter / Roboto), 28–36 px player name, 20–24 px stats  
   - Background per cell: very dark (#0a0e17), hover/active subtle glow

   **Column 1 – QB**  
   Player | Team | Pass Yd | TD | INT | Rush Yd | FPts  
   Color: green text for positive stat contribution, red for negative

   **Column 2 – RB**  
   Player | Team | Rush Yd | TD | Rec | Rec Yd | FPts

   **Column 3 – WR/TE**  
   Player | Team | Rec | Rec Yd | TD | Rush Yd | FPts

   **Column 4 – K + DST**  
   Player | Team | FG | PAT | Miss | (DST: Sack/INT/PA) | FPts

   **Column 5 – Overall Top Performers** (wider visual weight, 22–25% if needed)  
   Rank | Player | Pos | Team | Key stat line | FPts (largest text, 48–60 px)  
   Top 15–20, descending FPts, background highlight intensity increases with rank

3. **Bottom Control Strip** (height 6–8%)  
   - Semi-transparent black bar (#00000080)  
   - Left: Current refresh countdown (“Next update in 42 s”)  
   - Center: Selected scoring mode badge (“Yahoo Default 0.5 PPR” / “Custom”) – clickable only in admin mode  
   - Right: Small icons (gear = settings hidden, refresh = manual trigger hidden)  
   - Optional: league standings mini-bar (top 3 teams + their points)

**Visual styling rules**  
- Primary accent: #10b981 (emerald green) for positive / leaders  
- Negative: #ef4444 (red)  
- Neutral stat: #94a3b8 (gray)  
- Highlight hot players: yellow (#fbbf24) border or background pulse  
- Row zebra: very subtle (#111827 vs #0f172a)  
- All numbers right-aligned in cells  
- Player headshots: 64×64 px circle left of name (fallback silhouette)  
- Auto-cycle: every 45–60 s scroll each column to top if stopped

**Admin / Setup overlay (rare interaction)**  
- Large centered modal, high-contrast white-on-dark  
- Stepper wizard UI (progress dots)  
- Minimum 48 px touch targets  
- Large readable text (32–48 px)

Deliverable expectation: high-fidelity mockup matching this spec at 3840×2160 resolution.


### Development Plan for Missing Features

#### 1. Detailed Error Handling
- Implement global exception handler in ASP.NET Core (Middleware: UseExceptionHandler).
- Log errors with Serilog/ELK.
- Frontend: Use try-catch in Blazor components, display user-friendly MudAlert messages.
- API: Return standardized ErrorResponse (HTTP 4xx/5xx with codes/messages).
- Timeline: Week 2, integrate with services/controllers.

#### 2. Full Authentication Flows (e.g., Token Refresh)
- Yahoo OAuth1: Use OAuth1 library, store access token/secret in secure storage (Azure Key Vault or Encrypted DB).
- Refresh: For APIs with refresh (e.g., ESPN if added), implement token refresh logic in ApiService (check expiry, auto-refresh on 401).
- General: Add JWT for app auth (IdentityServer or ASP.NET Identity), secure endpoints.
- Handle callbacks: Blazor NavigationManager for OAuth redirects.
- Timeline: Week 3, test flows end-to-end.

#### 3. Database for Persistent Configs/Leagues
- Use Entity Framework Core with SQL Server/Azure SQL (or SQLite for dev).
- Models: ConfigEntity (scoring JSON), LeagueEntity (ID, provider, tokens encrypted).
- Repository pattern: ConfigRepo for CRUD.
- Migration: Add-Migration/Update-Database in VS.
- Seed defaults on startup.
- Timeline: Week 1, setup DB project/reference in API.

#### 4. User Roles (Admin/Viewer)
- Integrate ASP.NET Identity: Add roles ("Admin", "Viewer").
- Auth: JWT bearer for API, Blazor AuthenticationStateProvider.
- UI: [Authorize(Roles="Admin")] for custom scoring modal/setup.
- Viewer: Read-only dashboard.
- Admin: Full access, user management page.
- Timeline: Week 4, after auth flows.

#### 5. Accessibility Features
- MudBlazor: Use ARIA attributes (aria-label on components).
- Keyboard nav: Focusable elements, tab order.
- Screen reader: Alt text for images, semantic HTML (headings, lists).
- Contrast: WCAG 2.1 AA (tools: WAVE extension in VS).
- Test: Lighthouse audit in browser.
- Timeline: Week 5, apply to all components/pages.

#### 6. Deployment Scripts
- CI/CD: Azure DevOps/GitHub Actions YAML (build, test, deploy).
- Script: PowerShell/Dockerfile for containerization (docker build/push).
- Web: Publish to Azure App Service (VS Publish profile).
- API: Separate App Service or Functions.
- Env vars: For keys/secrets.
- Timeline: Week 6, post-testing.

#### 7. Integration Tests
- xUnit with WebApplicationFactory for API (test controllers/services).
- Blazor: bUnit for component tests (render/simulate events).
- End-to-end: Playwright/Selenium for UI flows (setup wizard, polling).
- Mocks: Moq for ApiService/DB.
- Coverage: dotnet test --collect:"XPlat Code Coverage".
- Timeline: Week 5-6, parallel with features.

Overall Timeline: 6 weeks, iterative; start with DB/auth, end with tests/deploy.


**Plan for Improvements**

**1. Enhance Mobile Responsiveness Now**  
- Use MudBlazor built-in breakpoints (xs, sm, md, lg, xl).  
- Wrap main layout in MudContainer + MudGrid.  
- Desktop (lg+): 5-column grid as specified.  
- Tablet (md): 3-column (stack QB/RB, WR/TE, K/DST+Overall).  
- Mobile (sm/xs): Single-column stack with collapsible MudExpansionPanels per position.  
- Hide news ticker on xs; show only on demand.  
- Apply MudHidden + MudShow based on breakpoint.  
- Test: Resize browser, use device toolbar in VS.  
- Timeline: Week 1–2.

**2. Add Logging/Monitoring**  
- Install Serilog + Serilog.Sinks.Console + Serilog.Sinks.File.  
- Configure in Program.cs: minimum level Information, enrich with context.  
- Add Serilog.AspNetCore for request logging.  
- Optional: Serilog.Sinks.Seq or Application Insights for production monitoring.  
- Log key events: API calls, polling success/failure, auth errors, point calc exceptions.  
- Timeline: Week 2.

**3. Optimize Polling for Rate Limits**  
- Store last API call timestamp per provider per league.  
- Implement exponential backoff on 429 responses (wait 1s ? 2s ? 4s ? cap at 60s).  
- Configurable interval: default 60s, admin can set 30–300s.  
- Cache responses 30–60s to avoid redundant calls.  
- Add circuit breaker (Polly): break after 3 failures, half-open after 2 min.  
- Log rate limit headers if present.  
- Timeline: Week 3.

**4. Modularize Formulas for Easier Extension**  
- Create interface IScoringCalculator.  
- Implement concrete classes: QbCalculator, RbCalculator, WrTeCalculator, KickerCalculator, DstCalculator, IdpCalculator.  
- Each class has method: double Calculate(Player player, ScoringConfig config).  
- Register all in DI as keyed services or factory pattern.  
- ScoringConfig becomes extensible (Dictionary<string, double> for custom stats).  
- Add factory: IScoringCalculator GetCalculator(string position).  
- Future: Load custom calculators from plugins/assembly scan.  
- Timeline: Week 2–3.

All improvements integrated iteratively during core development. No separate phase needed.



**Complete Condensed Guide: Yahoo Fantasy Football TV Dashboard Application**

**Project Goal**  
Build a large-screen (TV) passive dashboard displaying real-time fantasy football stats, calculated points, and rankings. Auto-refresh, auto-scroll, minimal interaction. Yahoo default, extensible to other APIs. Future mobile version. Developed in Visual Studio 2026 with .NET 8+.

**Core Features**
- 5-column layout: QB | RB | WR/TE | K/DST | Overall Top Performers
- Auto-refresh every 60 s via polling + SignalR broadcast
- Customizable scoring (defaults to Yahoo)
- Wizard setup for non-technical users (provider, league, auth)
- Passive: auto-scroll, news ticker, color highlights (green positive, red negative)
- Multi-API support (Yahoo default; Sleeper, ESPN, API-Football, SportsDataIO)

**UI Layout (4K Landscape TV)**
- **Top Banner** (8–10%): League name/logo, week/season, live clock/countdown, total league points, scrolling news ticker
- **Main Area** (82–84%): 5 equal columns (20% each), independent auto-scroll if overflow
  - Headers: large bold position text
  - Rows: 80–100 px, player name (headshot 64×64), team, key stats, FPts (largest font in Overall)
  - Colors: green/red for contribution, yellow for hot players
- **Bottom Strip** (6–8%): Refresh countdown, scoring mode badge, small controls (hidden unless admin)
- Responsive: MudBlazor breakpoints (desktop 5-col ? tablet 3-col ? mobile stack/accordion)

**Yahoo Fantasy Default Scoring (2025–2026)**
**Offense**
- Passing: 1 pt / 25 yds, 4 pts / TD, -1 pt / INT
- Rushing/Receiving: 1 pt / 10 yds, 6 pts / TD, 0.5 pt / reception (half-PPR common)
- 2-pt conversions: 2 pts

**Kicking**
- FG 0–39 yds: 3 pts
- FG 40–49 yds: 4 pts
- FG 50+ yds: 5 pts
- PAT: 1 pt

**DST**
- Sack: 1 pt
- INT: 2 pts
- Fumble Recovery: 2 pts
- Def TD: 6 pts
- Safety: 2 pts
- Blocked Kick: 2 pts
- Points Allowed tiers: 0=10, 1–6=7, 7–13=4, 14–20=1, 21–27=0, 28–34=-1, 35+=-4

**IDP (common, not always default)**
- Solo Tackle: 1.5 pts
- Assist: 0.75 pts
- Sack: 4 pts
- INT: 5 pts
- Forced Fumble: 3 pts
- Fumble Rec: 4 pts
- Def TD: 6 pts
- Safety: 2 pts
- Pass Defended: 1.5 pts
- Blocked Kick: 3 pts

**Yahoo API**
- OAuth 1.0 (3-legged), XML only
- Key endpoints: /fantasy/v2/league/{key}/scoreboard, /players;sort=PTS, /roster;week=XX
- Register app: https://developer.yahoo.com/apps

**Other APIs**
- Sleeper: No auth, /v1/user/{username}, /v1/league/{id}
- ESPN: espn-api wrapper, league_id + year
- API-Football: API key, /players, /fixtures
- SportsDataIO: API key, projections/stats

**Tech Stack**
- Backend: ASP.NET Core Web API + SignalR
- Frontend: Blazor WebAssembly + MudBlazor
- Shared: Class library (models, calculators)
- Database: EF Core (SQL Server/SQLite) for configs/tokens
- Auth: ASP.NET Identity + roles (Admin/Viewer)
- Logging: Serilog (console/file/Seq)
- Polling: Cron.NET + Polly circuit-breaker/backoff
- Testing: xUnit, bUnit, Playwright

**Modular Scoring**
Interface IScoringCalculator per position (QB, RB, WRTE, K, DST, IDP).  
Factory returns calculator by position.  
Config: ScoringConfig class with all variables (extensible Dictionary).

**Setup Wizard (MudStepper)**
1. Choose provider (Yahoo default)
2. Provider-specific: OAuth redirect / username / league ID / API key
3. Select league/season
4. Custom scoring? (modal with sliders/inputs)
5. Save config ? dashboard

**Polling & Real-Time**
- BackgroundService + Cron every 60 s
- Fetch ? calculate points ? SignalR broadcast
- Rate-limit handling: exponential backoff, cache, configurable interval

**Missing/Implemented Features**
- Error handling: global middleware, MudAlert, standardized responses
- Token refresh/secure storage
- DB persistence for configs/leagues
- Roles: Admin (edit), Viewer (read-only)
- Accessibility: ARIA, contrast, keyboard nav
- Deployment: Azure App Service, GitHub Actions/Docker
- Integration tests: API, components, E2E

**Development Order (6–8 weeks)**
1. Shared models + formulas
2. DB + EF setup
3. API services (Yahoo OAuth first)
4. Polling + SignalR + point calculator
5. Blazor dashboard + columns
6. Setup wizard
7. Auth/roles + custom scoring modal
8. Logging, error handling, responsiveness
9. Tests + deployment scripts

**Improvements Applied**
- Mobile-ready layout from start
- Optimized polling (backoff, cache)
- Modular/extensible formulas
- Comprehensive logging

This guide contains all requirements, specs, scoring, APIs, architecture, and plans discussed.



```markdown
# Fantasy Football TV Dashboard Specification

**Project Name**  
Fantasy Football Live Dashboard (TV Kiosk Mode)

**Target Platform**  
Large-screen TVs (55–85" 4K landscape) – passive viewing, minimal/no interaction  
Future: mobile-responsive version

**Core Purpose**  
Display real-time fantasy football stats, calculated fantasy points, and rankings  
Auto-refresh, auto-scroll, color-coded highlights, news ticker  
Yahoo Fantasy default, extensible to Sleeper/ESPN/API-Football/SportsDataIO

## 1. UI Layout (3840×2160 target)

**Top Banner** (8–10% height)  
- League name + logo (left, 48–72px)  
- Week X • Season YYYY + live clock/next slate countdown (center)  
- Total league fantasy points this week (right, 96px green)  
- Scrolling news ticker (bottom edge, 24px, injuries/big plays/Yahoo headlines)

**Main Content** (82–84% height)  
5 equal columns (20% each), independent vertical auto-scroll (slow, 20–30s cycle)  
- Headers: 48px bold position text + accent bar  
- Row height: 80–100px  
- Player: 64×64 headshot circle + name (36px) + team (24px)  
- Stats: 20–28px, right-aligned  
- Colors: green (positive contrib), red (negative), yellow border/pulse (hot)  
- Zebra rows: #111827 / #0f172a

**Columns**  
1. **QB** ? Player | Team | Pass Yd | TD | INT | Rush Yd | FPts  
2. **RB** ? Player | Team | Rush Yd | TD | Rec | Rec Yd | FPts  
3. **WR/TE** ? Player | Team | Rec | Rec Yd | TD | Rush Yd | FPts  
4. **K + DST** ? Player | Team | FG | PAT | Miss | (DST: Sack/INT/PA) | FPts  
5. **Overall Top** (wider visual weight) ? Rank | Player | Pos | Team | Key stat | FPts (48–60px)

**Bottom Strip** (6–8% height)  
- Refresh countdown (“Next update in Xs”)  
- Scoring mode badge (“Yahoo Default 0.5 PPR” / “Custom”)  
- Small icons (gear/settings – admin only)

**Responsive Rules** (MudBlazor breakpoints)  
- lg+ (desktop/TV): 5 columns  
- md (tablet): 3 columns (QB/RB | WR/TE | K/DST+Overall)  
- sm/xs (mobile): single column, collapsible MudExpansionPanels

## 2. Scoring Rules (Yahoo Default)

**Offense**  
- Pass: 1 pt / 25 yds, 4 pt / TD, –1 pt / INT  
- Rush/Rec: 1 pt / 10 yds, 6 pt / TD, 0.5 pt / rec (half-PPR)  
- 2-pt conv: 2 pts  
- Fum lost: –2 pts

**Kicking**  
- FG 0–39: 3 pts  
- FG 40–49: 4 pts  
- FG 50+: 5 pts  
- PAT: 1 pt

**DST**  
- Sack: 1  
- INT: 2  
- Fum Rec: 2  
- Def TD: 6  
- Safety: 2  
- Blocked Kick: 2  
- PA tiers: 0=10, 1–6=7, 7–13=4, 14–20=1, 21–27=0, 28–34=–1, 35+=–4

**IDP (common extension)**  
- Solo Tackle: 1.5  
- Assist: 0.75  
- Sack: 4  
- INT: 5  
- Forced Fum: 3  
- Fum Rec: 4  
- Def TD: 6  
- Safety: 2  
- Pass Def: 1.5  
- Blocked Kick: 3

## 3. Tech Stack

- IDE: Visual Studio 2026  
- Backend: ASP.NET Core 8+ Web API + SignalR  
- Frontend: Blazor WebAssembly + MudBlazor  
- Shared: Class library (models, calculators, config)  
- DB: EF Core (SQL Server / SQLite)  
- Auth: ASP.NET Identity + JWT + roles (Admin / Viewer)  
- Logging: Serilog (console/file/Seq)  
- Polling: Cron.NET + Polly (backoff, circuit breaker)  
- Testing: xUnit, bUnit, Playwright

## 4. Data Flow

1. Setup wizard ? save provider, league, tokens/config to DB  
2. Polling service (every 60s default)  
   - Fetch via provider-specific ApiService  
   - Calculate points using modular IScoringCalculator  
   - Broadcast via SignalR  
3. Blazor components subscribe ? update UI reactively  
4. Rate-limit handling: exponential backoff, cache, configurable interval

## 5. Modular Scoring System

```csharp
interface IScoringCalculator {
    double Calculate(Player player, ScoringConfig config);
}

class QbCalculator : IScoringCalculator { … }
class RbCalculator : IScoringCalculator { … }
// etc. for WRTE, K, DST, IDP

ScoringConfig {
    double PassYdsPerPt = 0.04;
    double PassTdPt = 4;
    // … all variables
    Dictionary<string,double> DstPaTiers { get; set; }
}
```

## 6. Setup Wizard Flow (MudStepper)

1. Select provider (Yahoo default)  
2. Provider auth/input  
   - Yahoo: OAuth 1.0 redirect ? callback ? league ID  
   - Sleeper: username ? league dropdown  
   - ESPN: league ID + year  
   - API-Football / SportsDataIO: paste API key  
3. Select league/season  
4. Custom scoring? ? modal with inputs/sliders  
5. Finish ? save config ? redirect to dashboard

## 7. Security & Reliability

- Encrypted token storage (ProtectedLocalStorage or DB encryption)  
- Role-based access: Admin edits, Viewer read-only  
- Global exception handling + user-friendly alerts  
- Serilog logging of API calls, errors, polling  
- Polly circuit breaker & backoff on rate limits / failures

## 8. Deployment

- Azure App Service (Web + API)  
- GitHub Actions / Azure DevOps YAML pipeline  
- Docker support optional  
- Env vars for secrets/keys

## 9. Future Extensions

- Multi-league support  
- Live game tracking / play-by-play  
- Projections (SportsDataIO)  
- Trade analyzer / AI insights  
- Push/email notifications  
- MAUI mobile app reusing Shared + BlazorWebView

**Last Updated**  
January 2026 – compiled from full conversation history
```




