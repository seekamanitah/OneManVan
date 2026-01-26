namespace OneManVan.Mobile.Theme;

/// <summary>
/// Centralized color constants for consistent theming across the app.
/// Replaces magic color strings throughout the codebase.
/// </summary>
public static class AppColors
{
    #region Primary Colors
    
    /// <summary>Primary brand color - Blue 500 (lighter)</summary>
    public static readonly Color Primary = Color.FromArgb("#2196F3");
    
    /// <summary>Primary dark variant - Blue 600</summary>
    public static readonly Color PrimaryDark = Color.FromArgb("#1E88E5");
    
    /// <summary>Primary darker variant - Blue 700 (for contrast)</summary>
    public static readonly Color PrimaryDarker = Color.FromArgb("#1976D2");
    
    /// <summary>Primary light variant</summary>
    public static readonly Color PrimaryLight = Color.FromArgb("#BBDEFB");
    
    /// <summary>Primary surface/background</summary>
    public static readonly Color PrimarySurface = Color.FromArgb("#E3F2FD");
    
    #endregion

    #region Semantic Colors
    
    /// <summary>Success/positive actions - Green</summary>
    public static readonly Color Success = Color.FromArgb("#4CAF50");
    
    /// <summary>Success light surface</summary>
    public static readonly Color SuccessSurface = Color.FromArgb("#E8F5E9");
    
    /// <summary>Warning/attention - Orange</summary>
    public static readonly Color Warning = Color.FromArgb("#FF9800");
    
    /// <summary>Warning light surface</summary>
    public static readonly Color WarningSurface = Color.FromArgb("#FFF3E0");
    
    /// <summary>Error/danger - Red</summary>
    public static readonly Color Error = Color.FromArgb("#F44336");
    
    /// <summary>Error light surface</summary>
    public static readonly Color ErrorSurface = Color.FromArgb("#FFEBEE");
    
    /// <summary>Info/neutral - Teal</summary>
    public static readonly Color Info = Color.FromArgb("#009688");
    
    /// <summary>Info light surface</summary>
    public static readonly Color InfoSurface = Color.FromArgb("#E0F2F1");
    
    #endregion

    #region Accent Colors
    
    /// <summary>Purple accent (estimates, schema)</summary>
    public static readonly Color Purple = Color.FromArgb("#9C27B0");
    
    /// <summary>Purple light surface</summary>
    public static readonly Color PurpleSurface = Color.FromArgb("#F3E5F5");
    
    /// <summary>Brown accent (general contractor)</summary>
    public static readonly Color Brown = Color.FromArgb("#795548");
    
    /// <summary>Blue-grey accent (locksmith)</summary>
    public static readonly Color BlueGrey = Color.FromArgb("#455A64");
    
    /// <summary>Deep purple accent (appliance)</summary>
    public static readonly Color DeepPurple = Color.FromArgb("#7B1FA2");
    
    /// <summary>Amber/yellow accent (electrical)</summary>
    public static readonly Color Amber = Color.FromArgb("#FFC107");
    
    /// <summary>Light blue accent (plumbing)</summary>
    public static readonly Color LightBlue = Color.FromArgb("#0288D1");
    
    #endregion

    #region Text Colors
    
    /// <summary>Primary text - Dark</summary>
    public static readonly Color TextPrimary = Color.FromArgb("#333333");
    
    /// <summary>Secondary text - Medium grey</summary>
    public static readonly Color TextSecondary = Color.FromArgb("#757575");
    
    /// <summary>Tertiary/hint text - Light grey</summary>
    public static readonly Color TextTertiary = Color.FromArgb("#9E9E9E");
    
    /// <summary>Disabled text</summary>
    public static readonly Color TextDisabled = Color.FromArgb("#BDBDBD");
    
    /// <summary>Text on dark backgrounds</summary>
    public static readonly Color TextOnDark = Colors.White;
    
    #endregion

    #region Background Colors
    
    /// <summary>Page background - Light grey</summary>
    public static readonly Color Background = Color.FromArgb("#F5F5F5");
    
    /// <summary>Card/surface background</summary>
    public static readonly Color Surface = Colors.White;
    
    /// <summary>Divider/border color</summary>
    public static readonly Color Divider = Color.FromArgb("#E0E0E0");
    
    /// <summary>Input field background</summary>
    public static readonly Color InputBackground = Color.FromArgb("#F5F5F5");
    
    #endregion

    #region Filter Button States
    
    /// <summary>Inactive filter - grey background</summary>
    public static readonly Color FilterInactive = Color.FromArgb("#E0E0E0");
    
    /// <summary>Inactive filter text</summary>
    public static readonly Color FilterInactiveText = Color.FromArgb("#616161");
    
    #endregion

    #region Hex String Helpers (for XAML or dynamic use)
    
    public static class Hex
    {
        public const string Primary = "#2196F3";
        public const string PrimaryDark = "#1E88E5";
        public const string PrimaryDarker = "#1976D2";
        public const string PrimaryLight = "#BBDEFB";
        public const string PrimarySurface = "#E3F2FD";
        
        public const string Success = "#4CAF50";
        public const string SuccessSurface = "#E8F5E9";
        public const string Warning = "#FF9800";
        public const string WarningSurface = "#FFF3E0";
        public const string Error = "#F44336";
        public const string ErrorSurface = "#FFEBEE";
        public const string Info = "#009688";
        public const string InfoSurface = "#E0F2F1";
        
        public const string Purple = "#9C27B0";
        public const string PurpleSurface = "#F3E5F5";
        
        public const string TextPrimary = "#333333";
        public const string TextSecondary = "#757575";
        public const string TextTertiary = "#9E9E9E";
        
        public const string Background = "#F5F5F5";
        public const string Divider = "#E0E0E0";
    }
    
    #endregion
}
