using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using OneManVan.Shared.Models;
using OneManVan.Shared.Services;

namespace OneManVan.Web.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, byte[]? attachment = null, string? attachmentName = null);
    Task<bool> SendInvoiceEmailAsync(string customerEmail, string invoiceNumber, byte[] pdfContent);
    Task<bool> SendEstimateEmailAsync(string customerEmail, string estimateTitle, byte[] pdfContent);
    Task<bool> SendTestEmailAsync(string toEmail);
    EmailSettings GetSettings();
    void SaveSettings(EmailSettings settings);
    bool IsConfigured { get; }
}

public class EmailService : IEmailService
{
    private readonly ISettingsStorage _settingsStorage;
    private readonly ILogger<EmailService> _logger;

    public EmailService(ISettingsStorage settingsStorage, ILogger<EmailService> logger)
    {
        _settingsStorage = settingsStorage;
        _logger = logger;
    }

    public bool IsConfigured
    {
        get
        {
            var settings = GetSettings();
            return settings.IsConfigured;
        }
    }

    public EmailSettings GetSettings()
    {
        return new EmailSettings
        {
            SmtpHost = _settingsStorage.GetString("Email_SmtpHost", ""),
            SmtpPort = _settingsStorage.GetInt("Email_SmtpPort", 587),
            SmtpUser = _settingsStorage.GetString("Email_SmtpUser", ""),
            SmtpPassword = _settingsStorage.GetString("Email_SmtpPassword", ""),
            FromEmail = _settingsStorage.GetString("Email_FromEmail", ""),
            FromName = _settingsStorage.GetString("Email_FromName", "OneManVan"),
            EnableBcc = _settingsStorage.GetBool("Email_EnableBcc", false),
            BccEmail = _settingsStorage.GetString("Email_BccEmail", "")
        };
    }

    public void SaveSettings(EmailSettings settings)
    {
        _settingsStorage.SetString("Email_SmtpHost", settings.SmtpHost);
        _settingsStorage.SetInt("Email_SmtpPort", settings.SmtpPort);
        _settingsStorage.SetString("Email_SmtpUser", settings.SmtpUser);
        _settingsStorage.SetString("Email_SmtpPassword", settings.SmtpPassword);
        _settingsStorage.SetString("Email_FromEmail", settings.FromEmail);
        _settingsStorage.SetString("Email_FromName", settings.FromName);
        _settingsStorage.SetBool("Email_EnableBcc", settings.EnableBcc);
        _settingsStorage.SetString("Email_BccEmail", settings.BccEmail);
    }

    /// <summary>
    /// Validates an email address format.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        try
        {
            // Use MailAddress for validation
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, byte[]? attachment = null, string? attachmentName = null)
    {
        // Validate recipient email
        if (!IsValidEmail(to))
        {
            _logger.LogWarning("Invalid recipient email address: {Email}", to);
            return false;
        }
        
        var settings = GetSettings();
        
        if (!settings.IsConfigured)
        {
            _logger.LogWarning("Email service is not configured. Please configure SMTP settings in Settings.");
            return false;
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(settings.FromEmail, settings.FromName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            // Add BCC if enabled and valid
            if (settings.EnableBcc && IsValidEmail(settings.BccEmail))
            {
                message.Bcc.Add(new MailAddress(settings.BccEmail));
                _logger.LogInformation("BCC copy will be sent to configured address");
            }

            if (attachment != null && !string.IsNullOrEmpty(attachmentName))
            {
                var stream = new MemoryStream(attachment);
                message.Attachments.Add(new Attachment(stream, attachmentName, "application/pdf"));
            }

            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort);
            
            if (!string.IsNullOrEmpty(settings.SmtpUser) && !string.IsNullOrEmpty(settings.SmtpPassword))
            {
                client.Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword);
            }
            
            client.EnableSsl = true;
            
            await client.SendMailAsync(message);
            
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendTestEmailAsync(string toEmail)
    {
        var settings = GetSettings();
        var subject = "OneManVan Email Test";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Email Configuration Test</h2>
                <p>This is a test email from OneManVan.</p>
                <p>If you received this email, your SMTP settings are configured correctly!</p>
                <hr/>
                <p style='font-size: 12px; color: #666;'>
                    <strong>Settings:</strong><br/>
                    SMTP Server: {settings.SmtpHost}:{settings.SmtpPort}<br/>
                    From: {settings.FromName} &lt;{settings.FromEmail}&gt;<br/>
                    BCC Enabled: {(settings.EnableBcc ? "Yes - " + settings.BccEmail : "No")}
                </p>
                <p style='color: #666;'>Sent at: {DateTime.Now:g}</p>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendInvoiceEmailAsync(string customerEmail, string invoiceNumber, byte[] pdfContent)
    {
        var settings = GetSettings();
        var subject = $"Invoice {invoiceNumber} from {settings.FromName}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Invoice {invoiceNumber}</h2>
                <p>Please find your invoice attached to this email.</p>
                <p>If you have any questions, please don't hesitate to contact us.</p>
                <p>Thank you for your business!</p>
                <br/>
                <p style='color: #666; font-size: 12px;'>
                    {settings.FromName}<br/>
                    {settings.FromEmail}
                </p>
            </body>
            </html>";

        return await SendEmailAsync(customerEmail, subject, body, pdfContent, $"Invoice_{invoiceNumber}.pdf");
    }

    public async Task<bool> SendEstimateEmailAsync(string customerEmail, string estimateTitle, byte[] pdfContent)
    {
        var settings = GetSettings();
        var subject = $"Estimate {estimateTitle} from {settings.FromName}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Estimate {estimateTitle}</h2>
                <p>Please find your estimate attached to this email.</p>
                <p>This estimate is valid for 30 days from the date shown on the document.</p>
                <p>If you have any questions or would like to proceed, please contact us.</p>
                <p>Thank you for considering our services!</p>
                <br/>
                <p style='color: #666; font-size: 12px;'>
                    {settings.FromName}<br/>
                    {settings.FromEmail}
                </p>
            </body>
            </html>";

        return await SendEmailAsync(customerEmail, subject, body, pdfContent, $"Estimate_{estimateTitle}.pdf");
    }
}
