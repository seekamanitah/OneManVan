using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Controls;

/// <summary>
/// Alert card for dashboard notifications.
/// </summary>
public partial class AlertCard : ContentView
{
    public AlertCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Set alert data and style the card.
    /// </summary>
    public void SetAlert(DashboardAlert alert)
    {
        TitleLabel.Text = alert.Title;
        DescriptionLabel.Text = alert.Description;
        CountLabel.Text = alert.Count.ToString();

        // Set colors based on alert type
        switch (alert.Type)
        {
            case AlertType.Danger:
                IconBorder.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#3D1F1F")
                    : Color.FromArgb("#FFEBEE");
                IconLabel.Text = "?";
                IconLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#EF9A9A")
                    : Color.FromArgb("#D32F2F");
                CountBadge.BackgroundColor = Color.FromArgb("#F44336");
                break;

            case AlertType.Warning:
                IconBorder.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#3D2D1F")
                    : Color.FromArgb("#FFF3E0");
                IconLabel.Text = "?";
                IconLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#FFB74D")
                    : Color.FromArgb("#F57C00");
                CountBadge.BackgroundColor = Color.FromArgb("#FF9800");
                break;

            case AlertType.Info:
                IconBorder.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#1E3A5F")
                    : Color.FromArgb("#E3F2FD");
                IconLabel.Text = "?";
                IconLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#64B5F6")
                    : Color.FromArgb("#1976D2");
                CountBadge.BackgroundColor = Color.FromArgb("#2196F3");
                break;

            case AlertType.Success:
                IconBorder.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#1B3D1F")
                    : Color.FromArgb("#E8F5E9");
                IconLabel.Text = "?";
                IconLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#81C784")
                    : Color.FromArgb("#388E3C");
                CountBadge.BackgroundColor = Color.FromArgb("#4CAF50");
                break;
        }
    }
}
