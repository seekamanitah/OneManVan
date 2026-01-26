# Mobile App UI Design System Specification

**Version**: 1.0  
**Last Updated**: 2025-12-12  
**Target Platform**: .NET MAUI (iOS, Android, Windows)

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Color System](#color-system)
3. [Typography](#typography)
4. [Spacing & Layout](#spacing--layout)
5. [Navigation Structure](#navigation-structure)
6. [Component Library](#component-library)
7. [Page Templates](#page-templates)
8. [Animations & Feedback](#animations--feedback)
9. [Responsive Patterns](#responsive-patterns)

---

## Design Philosophy

### Core Principles

1. **Mobile-First**: Designed for one-handed use with thumb-reachable primary actions
2. **Information Hierarchy**: Most important information visible without scrolling
3. **Minimal Taps**: Critical actions accessible within 1-2 taps from any screen
4. **Offline-Ready**: Visual indicators for sync status, graceful degradation
5. **Field-Friendly**: High contrast, large touch targets, glanceable information

### Visual Style

- **Clean & Professional**: Minimal decoration, focus on content
- **Card-Based Layout**: Information grouped in elevated cards
- **Color-Coded Status**: Consistent colors for states across the app
- **Text-Only Labels**: No emoji/unicode icons (cross-platform reliability)

---

## Color System

### Primary Palette

| Name | Hex Code | Usage |
|------|----------|-------|
| Primary | `#1976D2` | Main actions, links, active states, branding |
| Primary Light | `#E3F2FD` | Backgrounds, inactive chips, subtle highlights |
| Primary Dark | `#1565C0` | Pressed states, emphasis |

### Semantic Colors

| Name | Hex Code | Usage |
|------|----------|-------|
| Success | `#4CAF50` | Completed, paid, confirmed, positive actions |
| Success Light | `#E8F5E9` | Success backgrounds |
| Warning | `#FF9800` | Pending, in progress, attention needed |
| Warning Light | `#FFF3E0` | Warning backgrounds |
| Error | `#F44336` | Overdue, cancelled, critical alerts |
| Error Light | `#FFEBEE` | Error backgrounds |
| Info | `#2196F3` | Informational, tips, secondary actions |
| Info Light | `#E3F2FD` | Info backgrounds |

### Neutral Colors

| Name | Hex Code | Usage |
|------|----------|-------|
| Text Primary | `#333333` | Main text, headings |
| Text Secondary | `#757575` | Subtitles, labels, descriptions |
| Text Tertiary | `#9E9E9E` | Hints, disabled text, timestamps |
| Background | `#F5F5F5` | Page backgrounds |
| Surface | `#FFFFFF` | Cards, elevated surfaces |
| Border | `#E0E0E0` | Dividers, borders |

### Status Color Mapping

```
Draft/New       ? Primary (#1976D2)
Scheduled       ? Primary (#1976D2)
In Progress     ? Warning (#FF9800)
Completed/Paid  ? Success (#4CAF50)
Overdue/Error   ? Error (#F44336)
Cancelled       ? Text Tertiary (#9E9E9E)
```

---

## Typography

### Font Stack

```
Primary: OpenSans-Regular
Bold: OpenSans-SemiBold
System Fallback: Platform default
```

### Scale

| Style | Size | Weight | Usage |
|-------|------|--------|-------|
| Display | 28sp | Bold | Page titles, hero numbers |
| Headline | 18sp | Bold | Section headers |
| Title | 16sp | Bold | Card titles, list item primary |
| Body | 14sp | Regular | Body text, descriptions |
| Caption | 12sp | Regular | Labels, metadata |
| Micro | 10sp | Regular | Badges, timestamps |

### Line Heights

- Single line text: 1.2x font size
- Multi-line body: 1.5x font size

---

## Spacing & Layout

### Base Unit

All spacing derived from **4dp base unit**.

### Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4dp | Tight spacing, inline elements |
| sm | 8dp | Related elements, chip spacing |
| md | 12dp | Card padding, list item spacing |
| lg | 16dp | Section spacing, page padding |
| xl | 24dp | Major section breaks |
| xxl | 32dp | Page section separation |

### Standard Dimensions

| Element | Value |
|---------|-------|
| Page Padding | 16dp |
| Card Padding | 12-16dp |
| Card Corner Radius | 12dp |
| Button Corner Radius | 8dp |
| Chip Corner Radius | 16dp (pill) |
| Touch Target Minimum | 44x44dp |
| FAB Size | 60x60dp |
| Icon Size (inline) | 20dp |
| Status Dot | 8dp |

---

## Navigation Structure

### Shell Navigation (Flyout Menu)

```
???????????????????????????????????
? [Hamburger]  App Title  [Toggle]?  ? Top Bar
???????????????????????????????????
?                                 ?
?         Page Content            ?
?                                 ?
???????????????????????????????????

Flyout Menu (from hamburger):
???????????????????????????????????
?    App Logo / Title             ?
?    Subtitle                     ?
???????????????????????????????????
? ? Dashboard                     ?
? ? List Page 1                   ?
? ? List Page 2                   ?
? ? List Page 3                   ?
? ? List Page 4                   ?
? ? Settings                      ?
???????????????????????????????????
```

### Top Bar Configuration

| Position | Element | Purpose |
|----------|---------|---------|
| Left | Hamburger Menu | Opens flyout navigation |
| Center | Page Title | Current page name |
| Right | Toggle/Action | Context-specific (view toggle, add, etc.) |

### Page Navigation Flow

```
List Page ? Detail Page ? Sub-pages (modal/push)
         ? Quick Add (modal)
```

---

## Component Library

### 1. Filter Chip Bar

Horizontal scrolling filter chips at top of list pages.

```xaml
<ScrollView Orientation="Horizontal" 
            HorizontalScrollBarVisibility="Never"
            Padding="16,12">
    <HorizontalStackLayout Spacing="8">
        <!-- Active Chip -->
        <Button Text="Label"
                BackgroundColor="#1976D2"
                TextColor="White"
                FontSize="12"
                HeightRequest="32"
                CornerRadius="16"
                Padding="16,0"/>
        
        <!-- Inactive Chip -->
        <Button Text="Label"
                BackgroundColor="#E3F2FD"
                TextColor="#1976D2"
                FontSize="12"
                HeightRequest="32"
                CornerRadius="16"
                Padding="16,0"/>
        
        <!-- Warning Chip -->
        <Button Text="Label"
                BackgroundColor="#FFF3E0"
                TextColor="#FF9800"
                FontSize="12"
                HeightRequest="32"
                CornerRadius="16"
                Padding="16,0"/>
    </HorizontalStackLayout>
</ScrollView>
```

### 2. Card Component

Standard elevated card for list items and content blocks.

```xaml
<Border BackgroundColor="White"
        StrokeShape="RoundRectangle 12"
        Stroke="Transparent"
        Padding="16"
        Margin="16,4">
    <Border.Shadow>
        <Shadow Brush="Black" Offset="0,2" Radius="4" Opacity="0.1"/>
    </Border.Shadow>
    <!-- Content here -->
</Border>
```

### 3. KPI Card

Compact metric display card.

```xaml
<Border BackgroundColor="White" 
        StrokeShape="RoundRectangle 12"
        Stroke="Transparent"
        Padding="16">
    <Border.Shadow>
        <Shadow Brush="Black" Offset="0,2" Radius="4" Opacity="0.1"/>
    </Border.Shadow>
    <VerticalStackLayout>
        <Label Text="Label" 
               FontSize="12" 
               TextColor="#757575"/>
        <Label Text="0" 
               FontSize="32" 
               FontAttributes="Bold"
               TextColor="#1976D2"/>
    </VerticalStackLayout>
</Border>
```

### 4. Status Badge

Small colored badge for status indication.

```xaml
<Border BackgroundColor="{StatusColor}"
        StrokeShape="RoundRectangle 12"
        Stroke="Transparent"
        Padding="8,4">
    <Label Text="Status"
           FontSize="10"
           FontAttributes="Bold"
           TextColor="White"/>
</Border>
```

### 5. List Item with Swipe Actions

```xaml
<SwipeView>
    <SwipeView.LeftItems>
        <SwipeItems Mode="Reveal">
            <SwipeItem Text="Action"
                       BackgroundColor="#4CAF50"/>
        </SwipeItems>
    </SwipeView.LeftItems>
    <SwipeView.RightItems>
        <SwipeItems Mode="Execute">
            <SwipeItem Text="Delete"
                       BackgroundColor="#F44336"/>
        </SwipeItems>
    </SwipeView.RightItems>
    
    <!-- Card content -->
</SwipeView>
```

### 6. Floating Action Button (FAB)

```xaml
<Button Text="+"
        FontSize="28"
        WidthRequest="60"
        HeightRequest="60"
        CornerRadius="30"
        BackgroundColor="#FF9800"
        TextColor="White"
        FontAttributes="Bold"
        HorizontalOptions="End"
        VerticalOptions="End"
        Margin="20">
    <Button.Shadow>
        <Shadow Brush="Black" Offset="2,4" Radius="8" Opacity="0.25"/>
    </Button.Shadow>
</Button>
```

### 7. Stats Bar

Bottom-anchored statistics bar.

```xaml
<Border BackgroundColor="#1976D2"
        StrokeShape="RoundRectangle 0"
        Stroke="Transparent"
        Padding="16,8">
    <Grid ColumnDefinitions="*,*,*">
        <VerticalStackLayout HorizontalOptions="Center">
            <Label Text="0"
                   FontSize="18"
                   FontAttributes="Bold"
                   TextColor="White"
                   HorizontalTextAlignment="Center"/>
            <Label Text="Label"
                   FontSize="10"
                   TextColor="#BBDEFB"
                   HorizontalTextAlignment="Center"/>
        </VerticalStackLayout>
        <!-- Repeat for other columns -->
    </Grid>
</Border>
```

### 8. Section Header

```xaml
<Label Text="Section Title"
       FontSize="18"
       FontAttributes="Bold"
       TextColor="#333"
       Margin="0,8,0,0"/>
```

### 9. Empty State

```xaml
<Border BackgroundColor="#F5F5F5"
        StrokeShape="RoundRectangle 8"
        Stroke="Transparent"
        Padding="24">
    <VerticalStackLayout HorizontalOptions="Center" Spacing="8">
        <Label Text="No Items"
               FontSize="16"
               FontAttributes="Bold"
               TextColor="#757575"
               HorizontalTextAlignment="Center"/>
        <Label Text="Description of empty state"
               FontSize="12"
               TextColor="#9E9E9E"
               HorizontalTextAlignment="Center"/>
    </VerticalStackLayout>
</Border>
```

### 10. Action Button (Full Width)

```xaml
<Button Text="Action Label"
        BackgroundColor="#1976D2"
        TextColor="White"
        FontAttributes="Bold"
        CornerRadius="8"
        HeightRequest="50"/>
```

---

## Page Templates

### Template 1: List Page with Filters

```
???????????????????????????????????
? [?]      Page Title      [+]   ? Top Bar
???????????????????????????????????
? [All] [Filter1] [Filter2] ?    ? Filter Chips (scroll)
???????????????????????????????????
? ????????????????????????????????
? ? Item Card 1                 ??
? ? Primary Text                ??
? ? Secondary • Metadata        ??
? ?                    [Badge]  ??
? ????????????????????????????????
? ????????????????????????????????
? ? Item Card 2                 ??
? ????????????????????????????????
?           ...                   ?
???????????????????????????????????
?  Stat1    ?   Stat2   ? Stat3  ? Stats Bar
???????????????????????????????????
         [+] ? FAB (floating)
```

**Grid Structure:**
```xaml
<Grid RowDefinitions="Auto,Auto,*,Auto">
    <!-- Row 0: Search (optional) -->
    <!-- Row 1: Filter Chips -->
    <!-- Row 2: RefreshView + CollectionView -->
    <!-- Row 3: Stats Bar -->
    <!-- FAB overlaid on Row 2 -->
</Grid>
```

### Template 2: Dashboard with Toggle

```
???????????????????????????????????
? [?]      Dashboard     [Week]  ? Top Bar + Toggle
???????????????????????????????????
?                                 ?
?  DASHBOARD VIEW 1 (KPIs)        ?
?  ????????? ?????????           ?
?  ? KPI 1 ? ? KPI 2 ?           ? KPI Cards Grid
?  ????????? ?????????           ?
?  ????????? ?????????           ?
?  ? KPI 3 ? ? KPI 4 ?           ?
?  ????????? ?????????           ?
?                                 ?
?  Quick Actions                  ? Section Header
?  [Button 1]                     ?
?  [Button 2]                     ? Action Buttons
?  [Button 3]                     ?
?                                 ?
?  [Sync Status Banner]           ?
?                                 ?
???????????????????????????????????

???????????????????????????????????
? [?]      Dashboard     [KPIs]  ? Toggle changed
???????????????????????????????????
?                                 ?
?  DASHBOARD VIEW 2 (Week)        ?
?  [<]    Dec 9 - Dec 15    [>]  ? Week Nav
?         2024                    ?
?  ????????????????????????????????
?  ? M  T  W  T  F  S  S        ?? Week Calendar
?  ? 9  10 11 12 13 14 15       ??
?  ? •     •     •              ?? Job Dots
?  ????????????????????????????????
?  ??????? ??????? ???????       ?
?  ?Jobs ? ?Done ? ? Rev ?       ? Week Stats
?  ??????? ??????? ???????       ?
?                                 ?
?  Need To Do                     ? Section
?  ????????????????????????????????
?  ?? Task Title        [Badge] ?? To-Do Cards
?  ?  Subtitle                   ??
?  ????????????????????????????????
?                                 ?
???????????????????????????????????
```

### Template 3: Detail Page

```
???????????????????????????????????
? [?]      Detail Title    [···] ? Back + More
???????????????????????????????????
? ????????????????????????????????
? ?      STATUS BANNER          ?? Tappable Status
? ?   Status Text • Subtext     ??
? ????????????????????????????????
?                                 ?
? Section 1                       ?
? ????????????????????????????????
? ? Info Card                   ??
? ? Label: Value                ??
? ? Label: Value                ??
? ????????????????????????????????
?                                 ?
? Section 2                       ?
? ????????????????????????????????
? ? Content Card                ??
? ????????????????????????????????
?                                 ?
???????????????????????????????????
? [    Primary Action Button    ] ? Fixed Bottom
???????????????????????????????????
```

### Template 4: Form Page

```
???????????????????????????????????
? [?]       Add/Edit       [Save]?
???????????????????????????????????
?                                 ?
? ????????????????????????????????
? ? Field Group 1               ??
? ? Label *                     ?? Required indicator
? ? [Input Field          ]     ??
? ?                             ??
? ? Label                       ??
? ? [Picker/Dropdown      ?]    ??
? ????????????????????????????????
?                                 ?
? ????????????????????????????????
? ? Field Group 2               ??
? ? [Half]      [Half]          ?? Split fields
? ????????????????????????????????
?                                 ?
???????????????????????????????????
? [      Save Button            ] ? Fixed Bottom
???????????????????????????????????
```

---

## Animations & Feedback

### Haptic Feedback

Trigger on:
- Button taps
- Swipe actions completed
- Toggle changes
- Successful saves
- Error states

```csharp
try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
```

### Loading States

```xaml
<!-- Overlay Loading -->
<Grid IsVisible="{Binding IsLoading}">
    <BoxView BackgroundColor="#80000000"/>
    <ActivityIndicator IsRunning="True" Color="White"/>
    <Label Text="Loading..." TextColor="White"/>
</Grid>
```

### Pull-to-Refresh

All list pages should support pull-to-refresh:

```xaml
<RefreshView IsRefreshing="{Binding IsRefreshing}"
             Refreshing="OnRefreshing">
    <CollectionView ... />
</RefreshView>
```

### Transitions

- Page push: Slide from right
- Modal: Slide from bottom
- Dismiss: Reverse of entry

---

## Responsive Patterns

### Breakpoints

| Size | Width | Behavior |
|------|-------|----------|
| Compact | < 600dp | Single column, full-width cards |
| Medium | 600-840dp | 2-column KPIs, wider cards |
| Expanded | > 840dp | Master-detail, multi-column |

### Adaptive Layouts

**KPI Grid:**
```xaml
<!-- Phone: 2 columns -->
<Grid ColumnDefinitions="*,*" ColumnSpacing="12">

<!-- Tablet: 4 columns -->
<Grid ColumnDefinitions="*,*,*,*" ColumnSpacing="12">
```

**List Items:**
- Phone: Full card with stacked info
- Tablet: Card with horizontal info layout

---

## Quick Add Pattern

For creating new items without leaving current page:

1. Show action sheet with options
2. Use sequential `DisplayPromptAsync` for each field
3. Validate and create
4. Return to list with new item visible

```csharp
// Example flow
var name = await DisplayPromptAsync("Title", "Enter name:", "Next", "Cancel");
if (string.IsNullOrWhiteSpace(name)) return;

var detail = await DisplayPromptAsync("Title", "Enter detail:", "Create", "Cancel");
// ... create item
```

---

## Accessibility

### Requirements

1. **Minimum touch target**: 44x44dp
2. **Color contrast**: 4.5:1 for text, 3:1 for UI elements
3. **Semantic properties**: Set `SemanticProperties.Description` on interactive elements
4. **No color-only indicators**: Always pair color with text/icon

### Implementation

```xaml
<Button SemanticProperties.Description="Add new item"
        SemanticProperties.Hint="Double tap to create"/>
```

---

## File Organization

```
/App.xaml                    ? Global resources, converters
/AppShell.xaml               ? Navigation structure
/MainPage.xaml               ? Dashboard/Home
/Pages/
    /[Entity]ListPage.xaml   ? List views
    /[Entity]DetailPage.xaml ? Detail views  
    /Add[Entity]Page.xaml    ? Create forms
    /SettingsPage.xaml       ? Settings
/Services/
    /QuickAdd[Entity]Service.cs ? Quick-add helpers
/Converters/
    /StatusColorConverter.cs
    /BoolToVisibilityConverter.cs
/Resources/
    /Styles/
        /Colors.xaml
        /Styles.xaml
```

---

## Implementation Checklist

### New App Setup

- [ ] Define color palette in App.xaml
- [ ] Configure AppShell with flyout menu
- [ ] Create base page templates
- [ ] Implement common converters
- [ ] Set up navigation routes
- [ ] Create list page with filters
- [ ] Create detail page template
- [ ] Add FAB for primary action
- [ ] Implement pull-to-refresh
- [ ] Add swipe actions
- [ ] Create dashboard with KPIs
- [ ] Add dashboard toggle for alternate view
- [ ] Implement quick-add pattern
- [ ] Add haptic feedback
- [ ] Test on multiple screen sizes

---

## Appendix: XAML Resource Dictionary Template

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    
    <!-- Colors -->
    <Color x:Key="Primary">#1976D2</Color>
    <Color x:Key="PrimaryLight">#E3F2FD</Color>
    <Color x:Key="Success">#4CAF50</Color>
    <Color x:Key="SuccessLight">#E8F5E9</Color>
    <Color x:Key="Warning">#FF9800</Color>
    <Color x:Key="WarningLight">#FFF3E0</Color>
    <Color x:Key="Error">#F44336</Color>
    <Color x:Key="ErrorLight">#FFEBEE</Color>
    <Color x:Key="TextPrimary">#333333</Color>
    <Color x:Key="TextSecondary">#757575</Color>
    <Color x:Key="TextTertiary">#9E9E9E</Color>
    <Color x:Key="Background">#F5F5F5</Color>
    <Color x:Key="Surface">#FFFFFF</Color>
    
    <!-- Styles -->
    <Style x:Key="CardStyle" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource Surface}"/>
        <Setter Property="StrokeShape" Value="RoundRectangle 12"/>
        <Setter Property="Stroke" Value="Transparent"/>
        <Setter Property="Padding" Value="16"/>
    </Style>
    
    <Style x:Key="FilterChipStyle" TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource PrimaryLight}"/>
        <Setter Property="TextColor" Value="{StaticResource Primary}"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="HeightRequest" Value="32"/>
        <Setter Property="CornerRadius" Value="16"/>
        <Setter Property="Padding" Value="16,0"/>
    </Style>
    
    <Style x:Key="FilterChipActiveStyle" TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource Primary}"/>
        <Setter Property="TextColor" Value="White"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="HeightRequest" Value="32"/>
        <Setter Property="CornerRadius" Value="16"/>
        <Setter Property="Padding" Value="16,0"/>
    </Style>
    
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource Primary}"/>
        <Setter Property="TextColor" Value="White"/>
        <Setter Property="FontAttributes" Value="Bold"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="HeightRequest" Value="50"/>
    </Style>
    
    <Style x:Key="SectionHeaderStyle" TargetType="Label">
        <Setter Property="FontSize" Value="18"/>
        <Setter Property="FontAttributes" Value="Bold"/>
        <Setter Property="TextColor" Value="{StaticResource TextPrimary}"/>
        <Setter Property="Margin" Value="0,8,0,0"/>
    </Style>
    
</ResourceDictionary>
```

---

*This design system provides a consistent, professional foundation for building mobile field service and business applications with .NET MAUI.*
