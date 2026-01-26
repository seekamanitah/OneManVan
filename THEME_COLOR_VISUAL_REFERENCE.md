# ?? **THEME COLOR QUICK REFERENCE**

**Visual Guide for Testing Theme Colors**

---

## **?? COLOR VALUES REFERENCE**

### **Section Headers (What You Should See):**

| Section Type | Light Mode Color | Dark Mode Color | Hex Values |
|--------------|-----------------|-----------------|------------|
| **Primary (Blue)** | Dark Blue | Light Blue | #1976D2 ? #64B5F6 |
| **Success (Green)** | Dark Green | Light Green | #388E3C ? #81C784 |
| **Warning (Orange)** | Dark Orange | Light Orange | #F57C00 ? #FFB74D |
| **Info (Teal)** | Dark Teal | Light Teal | #0097A7 ? #4DD0E1 |
| **Purple** | Dark Purple | Light Purple | #7B1FA2 ? #CE93D8 |

### **Text Colors:**

| Text Type | Light Mode | Dark Mode | Usage |
|-----------|------------|-----------|-------|
| **Primary Text** | #333333 (Dark Gray) | #FFFFFF (White) | Main content |
| **Secondary Text** | #757575 (Gray) | #B0B0B0 (Light Gray) | Labels, hints |
| **Tertiary Text** | #9E9E9E (Light Gray) | #808080 (Gray) | Subtle text |

### **Background Colors:**

| Background Type | Light Mode | Dark Mode | Usage |
|----------------|------------|-----------|-------|
| **Page** | #F5F5F5 (Very Light Gray) | #121212 (Almost Black) | Page background |
| **Card** | #FFFFFF (White) | #2D2D2D (Dark Gray) | Card containers |
| **Input** | #F5F5F5 (Light Gray) | #3D3D3D (Medium Dark) | Text inputs |
| **Primary Surface** | #E3F2FD (Light Blue) | #2D2D2D (Dark Gray) | Info panels |

---

## **?? WHERE EACH COLOR IS USED**

### **AddServiceAgreementPage:**
```
Header: "Customer" ? Primary Blue
Header: "Agreement Details" ? Success Green  
Header: "Pricing" ? Warning Orange
Header: "Included Services" ? Primary Blue
Header: "Scheduling" ? Purple
```

### **AddProductPage:**
```
Header: "Basic Information" ? Primary Blue
Header: "Specifications" ? Success Green
Header: "Pricing" ? Warning Orange
Header: "Warranty" ? Primary Blue
```

### **AssetDetailPage:**
```
Header: "Equipment Information" ? Primary Blue
Header: "HVAC Configuration" ? Primary Blue
Header: "Warranty Information" ? Primary Blue
Header: "Photos" ? Primary Blue
Header: "Notes" ? Primary Blue
```

### **CustomerDetailPage:**
```
Header: "Basic Information" ? Primary Blue
Header: "Contact Information" ? Primary Blue
Header: "Billing" ? Primary Blue
Header: "Service Notes" ? Primary Blue
Header: "Primary Address" ? Primary Blue
```

### **EditCustomerPage:**
```
Header: "Basic Information" ? Primary Blue
Header: "Contact" ? Primary Blue
Header: "Classification" ? Primary Blue
Header: "Additional Information" ? Primary Blue
```

---

## **? VISUAL TESTING QUICK CHECKS**

### **Light Mode - Look For:**
? **Section Headers:** Should be colorful (blue/green/orange/purple)  
? **Labels:** Should be medium gray (#757575 ? theme-aware)  
? **Cards:** Should be white with subtle shadows  
? **Inputs:** Should have light gray backgrounds  
? **Text:** Should be dark gray for good readability  

### **Dark Mode - Look For:**
? **Section Headers:** Should be LIGHT versions (pastel colors)  
? **Labels:** Should be light gray (#B0B0B0)  
? **Cards:** Should be dark gray (#2D2D2D)  
? **Inputs:** Should be darker gray (#3D3D3D)  
? **Text:** Should be white for good contrast  

---

## **? COMMON PROBLEMS TO SPOT**

### **Problem 1: Invisible Text in Dark Mode**
**What it looks like:** Section headers or labels disappear  
**Cause:** Using #1976D2 (dark blue) on dark background  
**Should be:** Using #64B5F6 (light blue) on dark background  

### **Problem 2: Harsh White in Dark Mode**
**What it looks like:** Bright white cards that hurt eyes  
**Cause:** BackgroundColor="White" hardcoded  
**Should be:** Dark gray (#2D2D2D) cards  

### **Problem 3: Text Overlapping**
**What it looks like:** Text in grids runs into each other  
**Cause:** Missing Grid.Column attributes  
**Should be:** Clean column layout with proper spacing  

### **Problem 4: Wrong Color Shade**
**What it looks like:** Dark blue header in Dark mode (barely visible)  
**Cause:** Not using AppThemeBinding  
**Should be:** Light blue header in Dark mode  

---

## **?? SIDE-BY-SIDE COMPARISON**

### **Section Header "Customer" - Primary Blue:**
```
Light Mode: ????? #1976D2 (Dark Blue) - Clear on white
Dark Mode:  ????? #64B5F6 (Light Blue) - Clear on dark
```

### **Section Header "Pricing" - Warning Orange:**
```
Light Mode: ????? #F57C00 (Dark Orange) - Clear on white
Dark Mode:  ????? #FFB74D (Light Orange) - Clear on dark
```

### **Label Text:**
```
Light Mode: ???? #757575 (Medium Gray) - Clear on white
Dark Mode:  ???? #B0B0B0 (Light Gray) - Clear on dark
```

### **Card Background:**
```
Light Mode: ???? #FFFFFF (White) - Clean and bright
Dark Mode:  ???? #2D2D2D (Dark Gray) - Comfortable and modern
```

---

## **?? SCREENSHOT COMPARISON GUIDE**

### **What to Capture:**

**Light Mode Screenshots:**
1. Full page view of AddServiceAgreementPage
2. Close-up of section headers showing colors
3. Form inputs showing backgrounds
4. Buttons showing primary colors

**Dark Mode Screenshots:**
1. Same full page view
2. Same section headers (should be lighter)
3. Same form inputs (should be darker)
4. Same buttons (should adapt)

**Side-by-Side:**
1. Place Light and Dark screenshots next to each other
2. Verify all text is readable in both
3. Verify colors adapt properly
4. Verify professional appearance in both

---

## **?? 5-MINUTE VISUAL CHECK**

**Quick Visual Verification (No Navigation Needed):**

1. **Open AddServiceAgreementPage**
   - Count section headers (should see 5)
   - Check all are colorful (not all same color)
   - Check all text readable

2. **Switch to Dark Mode**
   - Headers should become lighter shades
   - Background should become dark
   - All text should still be readable

3. **Open CustomerDetailPage**
   - Check all sections visible
   - Check cards have proper backgrounds
   - Check text has good contrast

4. **Open MainPage**
   - Check dashboard cards visible
   - Check all metrics readable
   - Check quick actions visible

**If all 4 checks pass ? 95% likely everything is working!**

---

## **? EXPECTED APPEARANCE**

### **Light Mode - Professional Modern Look:**
- Clean white backgrounds
- Colorful section headers
- Good spacing and shadows
- Clear hierarchy
- Easy to read
- Familiar Material Design

### **Dark Mode - Comfortable Modern Look:**
- Dark gray backgrounds (not black)
- Pastel section headers (not bright)
- Good contrast without harshness
- Comfortable for eyes
- Professional appearance
- Modern dark theme standards

---

## **?? TESTING PRIORITY ORDER**

### **Priority 1 (Must Test):**
1. AddServiceAgreementPage - Has all color types
2. CustomerDetailPage - Complex detail page
3. MainPage - Dashboard users see first

### **Priority 2 (Should Test):**
4. AddProductPage - Another complex form
5. AssetDetailPage - Detail page with many sections
6. EditCustomerPage - Edit form example

### **Priority 3 (Nice to Test):**
7. SchemaEditorPage - Form with many inputs
8. JobDetailPage - Another detail page
9. SettingsPage - Settings UI

---

**Use this as a quick reference while testing!**  
**Keep it open next to the running app for easy verification.**
