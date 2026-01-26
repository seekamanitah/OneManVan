# ?? **BATCH COLOR FIX SCRIPT**

## Quick Find & Replace Patterns

Use Visual Studio's Find & Replace (Ctrl+H) with "Use Regular Expressions" enabled:

---

### **Pattern 1: Section Headers (#1976D2)**
**Find:**
```
TextColor="#1976D2"
```

**Replace:**
```
TextColor="{AppThemeBinding Light={StaticResource SectionPrimary}, Dark={StaticResource SectionPrimaryDark}}"
```

---

### **Pattern 2: Success Green Headers (#4CAF50)**
**Find:**
```
TextColor="#4CAF50"
```

**Replace:**
```
TextColor="{AppThemeBinding Light={StaticResource SectionSuccess}, Dark={StaticResource SectionSuccessDark}}"
```

---

### **Pattern 3: Secondary Labels (#757575)**
**Find:**
```
TextColor="#757575"
```

**Replace:**
```
TextColor="{AppThemeBinding Light={StaticResource LightTextSecondary}, Dark={StaticResource DarkTextSecondary}}"
```

---

### **Pattern 4: Tertiary Text (#9E9E9E)**
**Find:**
```
TextColor="#9E9E9E"
```

**Replace:**
```
TextColor="{AppThemeBinding Light={StaticResource LightTextTertiary}, Dark={StaticResource DarkTextTertiary}}"
```

---

### **Pattern 5: Primary Text (#333, #333333)**
**Find:**
```
TextColor="#333"
```

**Replace:**
```
TextColor="{AppThemeBinding Light={StaticResource LightTextPrimary}, Dark={StaticResource DarkTextPrimary}}"
```

---

### **Pattern 6: White Backgrounds**
**Find:**
```
BackgroundColor="White"
```

**Replace:**
```
BackgroundColor="{AppThemeBinding Light={StaticResource LightCardBackground}, Dark={StaticResource DarkCardBackground}}"
```

---

### **Pattern 7: Light Gray Backgrounds (#F5F5F5)**
**Find:**
```
BackgroundColor="#F5F5F5"
```

**Replace:**
```
BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"
```

---

### **Pattern 8: Primary Surface (#E3F2FD)**
**Find:**
```
BackgroundColor="#E3F2FD"
```

**Replace:**
```
BackgroundColor="{AppThemeBinding Light={StaticResource PrimarySurface}, Dark={StaticResource DarkCardBackground}}"
```

---

### **Pattern 9: Frame Black Shadow**
**Find:**
```
<Shadow Brush="Black" Offset="0,2" Radius="4" Opacity="0.1"/>
```

**Replace:**
```
<Shadow Brush="{AppThemeBinding Light=Black, Dark={StaticResource Gray900}}" Offset="0,2" Radius="4" Opacity="0.1"/>
```

---

## Frame to Border Conversion (Manual)

1. Change `<Frame` to `<Border`
2. Change `CornerRadius="12"` to `StrokeShape="RoundRectangle 12"`
3. Change `BorderColor="Transparent"` to `Stroke="Transparent"`
4. Replace `HasShadow="True"` with explicit Shadow element
5. Change `</Frame>` to `</Border>`

---

## Pages Still Needing Work

### **High Priority (Detail Pages):**
- ? AssetDetailPage.xaml - COMPLETE!
- ?? CustomerDetailPage.xaml - Partially done, continue
- ?? JobDetailPage.xaml
- ?? InvoiceDetailPage.xaml
- ?? ProductDetailPage.xaml
- ?? EstimateDetailPage.xaml

### **Medium Priority (Add Pages):**
- ?? AddAssetPage.xaml
- ?? AddJobPage.xaml
- ?? AddEstimatePage.xaml
- ?? AddInvoicePage.xaml
- ?? AddInventoryItemPage.xaml
- ?? AddSitePage.xaml

### **Medium Priority (Edit Pages):**
- ?? EditCustomerPage.xaml
- ?? EditAssetPage.xaml
- ?? EditJobPage.xaml
- ?? EditEstimatePage.xaml
- ?? EditInvoicePage.xaml
- ?? EditProductPage.xaml
- ?? EditInventoryItemPage.xaml

---

## Quick Workflow

For each page:
1. Open in Visual Studio
2. Ctrl+H (Find & Replace)
3. Run patterns 1-9 in order
4. Manually convert Frames to Borders if needed
5. Build to verify
6. Test in light and dark mode
7. Move to next page

**Time per page:** ~5-10 minutes

---

## Completed So Far

? Colors.xaml - Theme colors added
? AddServiceAgreementPage.xaml - Complete
? AddProductPage.xaml - Complete
? SchemaEditorPage.xaml - Complete
? AssetDetailPage.xaml - Complete (29 fixes!)
?? CustomerDetailPage.xaml - 50% complete (13 fixes done)

**Total Progress:** 18% (5.5 of 30 pages)
