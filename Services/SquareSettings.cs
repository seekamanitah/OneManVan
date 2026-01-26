namespace OneManVan.Services;

/// <summary>
/// Settings for Square payment integration.
/// </summary>
public class SquareSettings
{
    public bool IsEnabled { get; set; }
    public bool UseSandbox { get; set; } = true;
    public string ApplicationId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApplicationId) &&
        !string.IsNullOrWhiteSpace(AccessToken) &&
        !string.IsNullOrWhiteSpace(LocationId);
}
