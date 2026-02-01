namespace OneManVan.Shared.Models;

/// <summary>
/// Email configuration settings for SMTP.
/// </summary>
public class EmailSettings
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "OneManVan";
    
    /// <summary>
    /// Whether to send BCC copies of all outgoing emails.
    /// </summary>
    public bool EnableBcc { get; set; } = false;
    
    /// <summary>
    /// Email address to receive BCC copies for records.
    /// </summary>
    public string BccEmail { get; set; } = "";
    
    /// <summary>
    /// Whether the email service is properly configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(SmtpHost) && !string.IsNullOrEmpty(FromEmail);
}
