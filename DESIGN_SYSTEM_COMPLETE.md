# ?? **OneManVan Mobile - Design System**

**Version:** 1.0  
**Status:** Ready to Implement

---

## **?? DESIGN PRINCIPLES**

1. **Clarity** - Information is easy to find and understand
2. **Efficiency** - Tasks are quick to complete
3. **Consistency** - Predictable patterns throughout
4. **Feedback** - User always knows what's happening
5. **Accessibility** - Usable by everyone

---

## **?? COLOR PALETTE**

### **Primary Colors**
```
Primary Blue:    #1976D2 (Main brand color)
Primary Dark:    #1565C0 (Pressed state)
Primary Light:   #42A5F5 (Hover state)
```

### **Semantic Colors**
```
Success Green:   #4CAF50 (Completed, Active, Paid)
Warning Orange:  #FF9800 (In Progress, Pending)
Error Red:       #F44336 (Overdue, Cancelled, Failed)
Info Blue:       #2196F3 (Scheduled, Sent, Info)
```

### **Neutral Colors**
```
Text Primary:    #212121 (Light mode)
Text Secondary:  #757575 (Light mode)
Text Tertiary:   #9E9E9E (Light mode)

Text Primary:    #FFFFFF (Dark mode)
Text Secondary:  #BDBDBD (Dark mode)
Text Tertiary:   #757575 (Dark mode)

Background:      #FFFFFF / #121212
Surface:         #FFFFFF / #1E1E1E
Border:          #E0E0E0 / #424242
```

---

## **?? SPACING SYSTEM**

### **Standard Spacing**
```csharp
public static class Spacing
{
    public const int Tiny = 4;      // 4px  - Tight spacing
    public const int Small = 8;     // 8px  - List items, compact elements
    public const int Medium = 12;   // 12px - General spacing
    public const int Large = 16;    // 16px - Card padding, section spacing
    public const int XLarge = 24;   // 24px - Page margins
    public const int XXLarge = 32;  // 32px - Large separations
}
```

### **Usage Examples**
```xaml
<!-- Page Padding -->
<ContentPage Padding="16">

<!-- Section Spacing -->
<VerticalStackLayout Spacing="12">

<!-- Card Margin -->
<Border Margin="0,0,0,12">

<!-- Button Padding -->
<Button Padding="16,8">
```

---

## **?? TYPOGRAPHY**

### **Font Sizes**
```csharp
public static class FontSizes
{
    public const int Tiny = 10;      // Captions, helper text
    public const int Small = 12;     // Secondary info
    public const int Body = 14;      // Body text, labels
    public const int Medium = 16;    // List items, inputs
    public const int Large = 18;     // Subheadings
    public const int Title = 20;     // Card titles
    public const int Heading = 24;   // Page headings
    public const int Display = 32;   // Hero text
}
```

### **Font Weights**
```
Regular:     400 (Body text)
SemiBold:    600 (Emphasis)
Bold:        700 (Headings, buttons)
```

### **Usage**
```xaml
<!-- Page Title -->
<Label Text="Customers"
       FontSize="24"
       FontAttributes="Bold"/>

<!-- Section Heading -->
<Label Text="Contact Information"
       FontSize="16"
       FontAttributes="Bold"/>

<!-- Body Text -->
<Label Text="Details here"
       FontSize="14"/>

<!-- Secondary Text -->
<Label Text="Last updated: 2 hours ago"
       FontSize="12"
       TextColor="Gray"/>
```

---

## **?? CARD SYSTEM**

### **Standard Card**
```xaml
<Border BackgroundColor="{AppThemeBinding Light=White, Dark=#1E1E1E}"
        StrokeShape="RoundRectangle 12"
        Stroke="Transparent"
        Padding="16"
        Margin="0,0,0,12">
    <Border.Shadow>
        <Shadow Brush="Black" Offset="0,2" Radius="4" Opacity="0.1"/>
    </Border.Shadow>
    
    <!-- Content -->
    <VerticalStackLayout Spacing="8">
        <Label Text="Title" FontSize="16" FontAttributes="Bold"/>
        <Label Text="Content" FontSize="14"/>
    </VerticalStackLayout>
</Border>
```

### **Card Variants**
1. **Standard Card** - Default for most content
2. **Clickable Card** - Has hover/press state
3. **Elevated Card** - Stronger shadow for emphasis
4. **Outlined Card** - Border instead of shadow

---

## **?? BUTTON SYSTEM**

### **Primary Button**
```xaml
<Button Text="Save"
        BackgroundColor="{StaticResource Primary}"
        TextColor="White"
        CornerRadius="8"
        Padding="16,12"
        FontSize="16"
        FontAttributes="Bold"/>
```

### **Secondary Button**
```xaml
<Button Text="Cancel"
        BackgroundColor="Transparent"
        TextColor="{StaticResource Primary}"
        BorderColor="{StaticResource Primary}"
        BorderWidth="2"
        CornerRadius="8"
        Padding="16,12"
        FontSize="16"/>
```

### **Button Sizes**
```
Small:     Padding="12,8"  FontSize="14"
Medium:    Padding="16,12" FontSize="16" (Default)
Large:     Padding="20,16" FontSize="18"
```

### **Button States**
- Normal: Full color
- Pressed: 90% opacity + scale 0.98
- Disabled: 40% opacity + gray color

---

## **?? FORM INPUTS**

### **Text Entry**
```xaml
<Entry Placeholder="Enter name"
       PlaceholderColor="#9E9E9E"
       TextColor="{AppThemeBinding Light=#212121, Dark=White}"
       BackgroundColor="{AppThemeBinding Light=#F5F5F5, Dark=#2C2C2C}"
       HeightRequest="48"
       FontSize="16"
       Margin="0,0,0,12"/>
```

### **Picker**
```xaml
<Picker Title="Select option"
        TitleColor="#9E9E9E"
        TextColor="{AppThemeBinding Light=#212121, Dark=White}"
        BackgroundColor="{AppThemeBinding Light=#F5F5F5, Dark=#2C2C2C}"
        HeightRequest="48"
        FontSize="16"/>
```

### **Search Bar**
```xaml
<SearchBar Placeholder="Search..."
           PlaceholderColor="#9E9E9E"
           TextColor="{AppThemeBinding Light=#212121, Dark=White}"
           BackgroundColor="{AppThemeBinding Light=#F5F5F5, Dark=#2C2C2C}"
           HeightRequest="44"
           FontSize="16"/>
```

---

## **?? STATUS INDICATORS**

### **Status Badge**
```xaml
<!-- Success -->
<Border BackgroundColor="#4CAF50"
        StrokeShape="RoundRectangle 12"
        Padding="8,4">
    <Label Text="Completed" TextColor="White"
           FontSize="12" FontAttributes="Bold"/>
</Border>

<!-- Warning -->
<Border BackgroundColor="#FF9800"
        StrokeShape="RoundRectangle 12"
        Padding="8,4">
    <Label Text="Pending" TextColor="White"
           FontSize="12" FontAttributes="Bold"/>
</Border>

<!-- Error -->
<Border BackgroundColor="#F44336"
        StrokeShape="RoundRectangle 12"
        Padding="8,4">
    <Label Text="Overdue" TextColor="White"
           FontSize="12" FontAttributes="Bold"/>
</Border>
```

### **Progress Indicator**
```xaml
<ProgressBar Progress="0.75"
             ProgressColor="{StaticResource Primary}"
             HeightRequest="4"/>
```

---

## **?? LIST ITEMS**

### **Standard List Item**
```xaml
<Border Style="{StaticResource ClickableCardStyle}">
    <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
        
        <!-- Icon/Avatar -->
        <Border Grid.Column="0"
                WidthRequest="40" HeightRequest="40"
                BackgroundColor="#E3F2FD"
                StrokeShape="RoundRectangle 20">
            <Label Text="JD" HorizontalOptions="Center"
                   VerticalOptions="Center" FontSize="16"
                   FontAttributes="Bold" TextColor="#1976D2"/>
        </Border>
        
        <!-- Content -->
        <VerticalStackLayout Grid.Column="1" Spacing="4">
            <Label Text="John Doe" FontSize="16" FontAttributes="Bold"/>
            <Label Text="555-1234" FontSize="14" TextColor="Gray"/>
        </VerticalStackLayout>
        
        <!-- Action/Status -->
        <Label Grid.Column="2" Text=">" FontSize="20"
               TextColor="Gray" VerticalOptions="Center"/>
    </Grid>
</Border>
```

---

## **?? ICONS**

### **System Icons**
Use consistent emoji or icon font:
```
Home:         ??
Customers:    ??
Jobs:         ??
Calendar:     ??
Estimates:    ??
Invoices:     ??
Assets:       ??
Inventory:    ??
Settings:     ??

Add:          ?
Edit:         ??
Delete:       ???
Save:         ??
Cancel:       ?
Complete:     ?
Phone:        ??
Email:        ??
Location:     ??
Camera:       ??
```

### **Icon Sizes**
```
Small:     16x16
Medium:    24x24 (Default)
Large:     32x32
XLarge:    48x48
```

---

## **? LOADING STATES**

### **Loading Indicator**
```xaml
<ActivityIndicator IsRunning="True"
                   Color="{StaticResource Primary}"
                   HeightRequest="40"
                   WidthRequest="40"/>
```

### **Skeleton Screen**
```xaml
<VerticalStackLayout Spacing="12">
    <BoxView HeightRequest="24" WidthRequest="200"
             BackgroundColor="#E0E0E0" CornerRadius="4"/>
    <BoxView HeightRequest="16" WidthRequest="300"
             BackgroundColor="#E0E0E0" CornerRadius="4"/>
    <BoxView HeightRequest="16" WidthRequest="250"
             BackgroundColor="#E0E0E0" CornerRadius="4"/>
</VerticalStackLayout>
```

---

## **?? EMPTY STATES**

### **Standard Empty State**
```xaml
<VerticalStackLayout Padding="40" Spacing="16"
                     VerticalOptions="Center">
    <Label Text="??" FontSize="64"
           HorizontalOptions="Center"/>
    <Label Text="No items yet" FontSize="20"
           FontAttributes="Bold"
           HorizontalOptions="Center"/>
    <Label Text="Get started by adding your first item"
           FontSize="14" TextColor="Gray"
           HorizontalOptions="Center"
           HorizontalTextAlignment="Center"/>
    <Button Text="Add Item"
            HorizontalOptions="Center"
            Margin="0,16,0,0"/>
</VerticalStackLayout>
```

---

## **?? ANIMATIONS**

### **Button Press**
```xaml
<Button.Triggers>
    <EventTrigger Event="Pressed">
        <ScaleToAnimation Scale="0.95" Duration="100"/>
    </EventTrigger>
    <EventTrigger Event="Released">
        <ScaleToAnimation Scale="1.0" Duration="100"/>
    </EventTrigger>
</Button.Triggers>
```

### **Fade In**
```csharp
await view.FadeTo(1, 300);
```

### **Slide In**
```csharp
await view.TranslateTo(0, 0, 300, Easing.CubicOut);
```

---

## **?? RESPONSIVE DESIGN**

### **Breakpoints**
```
Phone Portrait:   < 600px
Phone Landscape:  600-900px
Tablet:           > 900px
```

### **Adaptive Layout**
```xaml
<OnPlatform x:TypeArguments="x:Double">
    <On Platform="Android, iOS" Value="16"/>
    <On Platform="WinUI" Value="24"/>
</OnPlatform>
```

---

## **? ACCESSIBILITY**

### **Semantic Properties**
```xaml
<Button Text="Save"
        SemanticProperties.Description="Save customer information"
        SemanticProperties.Hint="Double tap to save"/>
```

### **Contrast Ratios**
- Body text: 4.5:1 minimum
- Large text: 3:1 minimum
- Interactive elements: 3:1 minimum

---

## **? IMPLEMENTATION CHECKLIST**

- [ ] Create `Spacing.cs` constants
- [ ] Create `FontSizes.cs` constants
- [ ] Create `CardStyles.xaml` resource dictionary
- [ ] Create `ButtonStyles.xaml` resource dictionary
- [ ] Create reusable controls (SkeletonView, EmptyStateView)
- [ ] Apply to all pages systematically
- [ ] Test in light and dark mode
- [ ] Test with large text enabled
- [ ] Test on multiple devices

---

**This design system ensures consistency across the entire app!**
