# OneManVan Mobile UI Design System
**Based on Web UI Design - Modern, Professional, Responsive**

---

## ?? Color Palette (Matching Web UI)

### Primary Colors
```xml
<Color x:Key="Primary">#3b82f6</Color>          <!-- Blue-500 - Main brand -->
<Color x:Key="PrimaryDark">#2563eb</Color>      <!-- Blue-600 - Hover/Active -->
<Color x:Key="PrimaryLight">#60a5fa</Color>     <!-- Blue-400 - Subtle -->
<Color x:Key="PrimaryLighter">#dbeafe</Color>   <!-- Blue-100 - Backgrounds -->
```

### Semantic Colors
```xml
<Color x:Key="Success">#10b981</Color>          <!-- Green-500 -->
<Color x:Key="Warning">#f59e0b</Color>          <!-- Amber-500 -->
<Color x:Key="Danger">#ef4444</Color>           <!-- Red-500 -->
<Color x:Key="Info">#06b6d4</Color>             <!-- Cyan-500 -->
```

### Neutral Colors
```xml
<Color x:Key="Gray50">#f9fafb</Color>
<Color x:Key="Gray100">#f3f4f6</Color>
<Color x:Key="Gray200">#e5e7eb</Color>
<Color x:Key="Gray300">#d1d5db</Color>
<Color x:Key="Gray400">#9ca3af</Color>
<Color x:Key="Gray500">#6b7280</Color>
<Color x:Key="Gray600">#4b5563</Color>
<Color x:Key="Gray700">#374151</Color>
<Color x:Key="Gray800">#1f2937</Color>
<Color x:Key="Gray900">#111827</Color>
```

### Surface Colors
```xml
<!-- Light Theme -->
<Color x:Key="LightBackground">#ffffff</Color>
<Color x:Key="LightSurface">#f9fafb</Color>
<Color x:Key="LightCard">#ffffff</Color>
<Color x:Key="LightBorder">#e5e7eb</Color>

<!-- Dark Theme -->
<Color x:Key="DarkBackground">#0f172a</Color>
<Color x:Key="DarkSurface">#1e293b</Color>
<Color x:Key="DarkCard">#334155</Color>
<Color x:Key="DarkBorder">#475569</Color>
```

---

## ?? Spacing System (8px Base)

```csharp
// Constants/Spacing.cs - Already exists, update these values
public static class Spacing
{
    public const double XSmall = 4;
    public const double Small = 8;
    public const double Medium = 16;
    public const double Large = 24;
    public const double XLarge = 32;
    public const double XXLarge = 48;
    
    public const double CardPadding = 16;
    public const double PagePadding = 16;
    public const double SectionGap = 24;
}
```

---

## ?? Typography

### Font Sizes
```xml
<x:Double x:Key="FontSizeH1">32</x:Double>
<x:Double x:Key="FontSizeH2">24</x:Double>
<x:Double x:Key="FontSizeH3">20</x:Double>
<x:Double x:Key="FontSizeH4">18</x:Double>
<x:Double x:Key="FontSizeBody">16</x:Double>
<x:Double x:Key="FontSizeBodySmall">14</x:Double>
<x:Double x:Key="FontSizeCaption">12</x:Double>
<x:Double x:Key="FontSizeSmall">10</x:Double>
```

### Font Weights
```csharp
FontAttributes.Bold      // Headings, emphasis
FontAttributes.None      // Body text
```

---

## ?? Card Design (Matching Web)

```xml
<Frame Padding="16"
       Margin="0,0,0,12"
       BackgroundColor="{AppThemeBinding Light={StaticResource LightCard}, Dark={StaticResource DarkCard}}"
       BorderColor="{AppThemeBinding Light={StaticResource LightBorder}, Dark={StaticResource DarkBorder}}"
       CornerRadius="12"
       HasShadow="True">
    <!-- Card content -->
</Frame>
```

### Card Elevation
- **Level 1:** Small shadow for list items
- **Level 2:** Medium shadow for interactive cards
- **Level 3:** Large shadow for modals/dialogs

---

## ?? Responsive Layouts

### Breakpoints
```csharp
public const double PhoneMaxWidth = 600;
public const double TabletMinWidth = 601;
public const double TabletMaxWidth = 1024;
public const double DesktopMinWidth = 1025;
```

### Grid Patterns

#### Phone (Portrait)
```xml
<Grid RowDefinitions="Auto,*">
    <Label Text="Title" Grid.Row="0"/>
    <CollectionView Grid.Row="1"/>
</Grid>
```

#### Tablet (Landscape) - Master-Detail
```xml
<Grid ColumnDefinitions="320,*">
    <CollectionView Grid.Column="0"/> <!-- Master -->
    <ContentView Grid.Column="1"/>    <!-- Detail -->
</Grid>
```

---

## ?? Component Patterns

### List Item (Matching Web Cards)
```xml
<Frame Padding="16" Margin="0,0,0,8" CornerRadius="12" HasShadow="True">
    <Grid RowDefinitions="Auto,Auto,Auto" ColumnDefinitions="*,Auto">
        <!-- Title -->
        <Label Grid.Row="0" Grid.Column="0"
               Text="{Binding Title}"
               FontSize="18"
               FontAttributes="Bold"/>
        
        <!-- Badge/Status -->
        <Frame Grid.Row="0" Grid.Column="1"
               Padding="8,4"
               CornerRadius="12"
               BackgroundColor="{StaticResource Success}">
            <Label Text="Active" TextColor="White" FontSize="12"/>
        </Frame>
        
        <!-- Subtitle -->
        <Label Grid.Row="1" Grid.ColumnSpan="2"
               Text="{Binding Subtitle}"
               FontSize="14"
               TextColor="{StaticResource Gray600}"/>
        
        <!-- Footer Meta -->
        <HorizontalStackLayout Grid.Row="2" Grid.ColumnSpan="2" Spacing="16">
            <Label Text="{Binding Date, StringFormat='{0:MMM dd}'}"
                   FontSize="12"
                   TextColor="{StaticResource Gray500}"/>
            <Label Text="{Binding Amount, StringFormat='${0:N2}'}"
                   FontSize="14"
                   FontAttributes="Bold"
                   TextColor="{StaticResource Primary}"/>
        </HorizontalStackLayout>
    </Grid>
</Frame>
```

### Button Styles
```xml
<!-- Primary Button -->
<Button Text="Save"
        BackgroundColor="{StaticResource Primary}"
        TextColor="White"
        CornerRadius="8"
        HeightRequest="44"
        FontSize="16"
        FontAttributes="Bold"/>

<!-- Secondary Button -->
<Button Text="Cancel"
        BackgroundColor="{StaticResource Gray200}"
        TextColor="{StaticResource Gray700}"
        CornerRadius="8"
        HeightRequest="44"/>

<!-- Text Button -->
<Button Text="Learn More"
        BackgroundColor="Transparent"
        TextColor="{StaticResource Primary}"
        BorderWidth="0"/>
```

---

## ?? Dashboard Metrics Card (Matching Web)

```xml
<Frame Padding="20" CornerRadius="12" HasShadow="True">
    <VerticalStackLayout Spacing="8">
        <!-- Icon + Value -->
        <HorizontalStackLayout Spacing="12">
            <Label Text="??" FontSize="24"/>
            <Label Text="$12,450"
                   FontSize="28"
                   FontAttributes="Bold"
                   TextColor="{StaticResource Primary}"/>
        </HorizontalStackLayout>
        
        <!-- Label -->
        <Label Text="Total Revenue"
               FontSize="14"
               TextColor="{StaticResource Gray600}"/>
        
        <!-- Change Indicator -->
        <HorizontalStackLayout Spacing="4">
            <Label Text="?" TextColor="{StaticResource Success}"/>
            <Label Text="+12.5%"
                   FontSize="12"
                   TextColor="{StaticResource Success}"/>
            <Label Text="vs last month"
                   FontSize="12"
                   TextColor="{StaticResource Gray500}"/>
        </HorizontalStackLayout>
    </VerticalStackLayout>
</Frame>
```

---

## ?? Animation Guidelines

### Subtle Transitions
```csharp
// Fade in new content
await element.FadeTo(1, 250, Easing.CubicOut);

// Scale on tap
await element.ScaleTo(0.95, 100);
await element.ScaleTo(1, 100);
```

### Loading States
Use `ActivityIndicator` or skeleton screens (already have SkeletonView control)

---

## ? Accessibility

### Touch Targets
- Minimum 44x44 points for all interactive elements
- Adequate spacing between tappable items (12px minimum)

### Contrast
- Text on background: 4.5:1 minimum
- Large text (18pt+): 3:1 minimum

---

## ?? Dark Mode Support

Use `AppThemeBinding` for all colors:
```xml
<Label TextColor="{AppThemeBinding Light={StaticResource Gray900}, 
                                    Dark={StaticResource Gray50}}"/>
```

---

## ?? Platform-Specific Considerations

### iOS
- Use native navigation bar style
- SwipeView for delete actions
- Pull-to-refresh

### Android
- Material Design elevation
- Ripple effects on tap
- Floating Action Button where appropriate

---

## ?? Implementation Priority

**Phase 1: Foundation**
1. Update `Colors.xaml` with new palette
2. Update `Styles.xaml` with component styles
3. Create `ResponsiveGrid.cs` helper

**Phase 2: Core Pages**
1. Dashboard (MainPage) - Card layout
2. Customer List - Master-detail pattern
3. Customer Detail - Polished form

**Phase 3: Expand**
4. Job List
5. Invoice List
6. Settings polish
