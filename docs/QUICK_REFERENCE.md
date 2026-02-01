# Quick Reference - Database & Features

## ?? Quick Start (Right Now)

**Your app is now in SQLite mode and ready to run:**

```powershell
cd OneManVan.Web
dotnet run
```

Then open: **https://localhost:5001**

---

## ?? Switch Database Modes

**Local Development (SQLite):**
```powershell
.\SwitchDatabaseMode.ps1 -Mode SQLite
```

**Production (SQL Server):**
```powershell
.\SwitchDatabaseMode.ps1 -Mode SQLServer
docker-compose up -d
```

---

## ?? Feature Roadmap Priority

**Top 5 to implement next:**

| # | Feature | Time | ROI |
|---|---------|------|-----|
| 1 | Customer Portal | 2 weeks | ????? |
| 2 | Payment Processing | 1 week | ????? |
| 3 | Email Automation | 3 days | ???? |
| 4 | Digital Signatures | 1 week | ???? |
| 5 | Dashboard KPIs | 1 week | ???? |

**See `FEATURE_ROADMAP.md` for all 26 features**

---

## ??? Troubleshooting

**App won't start?**
```powershell
# 1. Switch to SQLite
.\SwitchDatabaseMode.ps1 -Mode SQLite

# 2. Clean and rebuild
dotnet clean
dotnet build

# 3. Run
cd OneManVan.Web
dotnet run
```

**SQL Server error?**
? Read: `SQL_SERVER_ERROR_FIX.md`

---

## ?? Important Files

| File | What It Is |
|------|------------|
| `FEATURE_ROADMAP.md` | All feature suggestions |
| `SQL_SERVER_ERROR_FIX.md` | Database connection troubleshooting |
| `SESSION_SUMMARY_FEATURES_AND_FIX.md` | Today's work summary |
| `SwitchDatabaseMode.ps1` | Database mode switcher |

---

## ? What Was Fixed Today

1. **SQL Server connection crash** ? Now falls back to SQLite gracefully
2. **No feature plan** ? Comprehensive 26-feature roadmap created
3. **Manual config edits** ? One-command mode switcher
4. **Unclear errors** ? Detailed logging and troubleshooting guide

---

## ?? Remember

- **SQLite** = Fast local development, no Docker needed
- **SQL Server** = Production-ready, requires Docker
- **Switch anytime** with one PowerShell command
- **App is resilient** now - won't crash on DB issues

---

**Your app is ready to run! ??**
