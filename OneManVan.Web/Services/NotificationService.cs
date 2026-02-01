using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Services;

namespace OneManVan.Web.Services;

/// <summary>
/// Automated notification service for sending emails on key business events.
/// Emails only send when AutoNotificationsEnabled is true in settings.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;
    private readonly CompanySettingsService _companySettings;
    private readonly ISettingsStorage _settingsStorage;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Setting key for enabling/disabling automatic notifications.
    /// Default is false (disabled) - must be explicitly enabled in Settings.
    /// </summary>
    private const string AutoNotificationsEnabledKey = "AutoNotificationsEnabled";

    public NotificationService(
        IEmailService emailService,
        IDbContextFactory<OneManVanDbContext> contextFactory,
        CompanySettingsService companySettings,
        ISettingsStorage settingsStorage,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _contextFactory = contextFactory;
        _companySettings = companySettings;
        _settingsStorage = settingsStorage;
        _logger = logger;
    }

    /// <summary>
    /// Checks if automatic notifications are enabled.
    /// </summary>
    private bool IsAutoNotificationsEnabled => _settingsStorage.GetBool(AutoNotificationsEnabledKey, false);

    #region Invoice Notifications

    public async Task SendInvoiceCreatedAsync(Invoice invoice)
    {
        if (!IsAutoNotificationsEnabled)
        {
            _logger.LogDebug("Auto-notifications disabled - skipping invoice notification");
            return;
        }

        if (!_emailService.IsConfigured)
        {
            _logger.LogWarning("Email not configured - skipping invoice notification");
            return;
        }

        var customer = await GetCustomerAsync(invoice.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email))
        {
            _logger.LogWarning("No email for customer {CustomerId} - skipping notification", invoice.CustomerId);
            return;
        }

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Invoice #{invoice.InvoiceNumber} from {settings.CompanyName}";
        var body = GenerateInvoiceEmailBody(invoice, customer, settings);

        try
        {
            await _emailService.SendEmailAsync(customer.Email, subject, body);
            _logger.LogInformation("Sent invoice email to {Email} for invoice {InvoiceNumber}", 
                customer.Email, invoice.InvoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice email for {InvoiceNumber}", invoice.InvoiceNumber);
        }
    }

    public async Task SendInvoiceReminderAsync(Invoice invoice)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(invoice.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Payment Reminder: Invoice #{invoice.InvoiceNumber}";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Payment Reminder</h2>
    <p>Dear {customer.Name},</p>
    <p>This is a friendly reminder that invoice <strong>#{invoice.InvoiceNumber}</strong> is due for payment.</p>
    <table style='margin: 20px 0;'>
        <tr><td>Invoice Number:</td><td><strong>{invoice.InvoiceNumber}</strong></td></tr>
        <tr><td>Amount Due:</td><td><strong>${invoice.Total:N2}</strong></td></tr>
        <tr><td>Due Date:</td><td><strong>{invoice.DueDate:MMMM dd, yyyy}</strong></td></tr>
    </table>
    <p>Please remit payment at your earliest convenience.</p>
    <p>Thank you for your business!</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
    }

    public async Task SendPaymentReceivedAsync(Invoice invoice, decimal amount)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(invoice.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Payment Received - Thank You!";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #28a745;'>Payment Received</h2>
    <p>Dear {customer.Name},</p>
    <p>Thank you for your payment of <strong>${amount:N2}</strong> for invoice #{invoice.InvoiceNumber}.</p>
    <p>We appreciate your business and look forward to serving you again!</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
        _logger.LogInformation("Sent payment confirmation to {Email}", customer.Email);
    }

    #endregion

    #region Estimate Notifications

    public async Task SendEstimateCreatedAsync(Estimate estimate)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(estimate.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Estimate from {settings.CompanyName}: {estimate.Title}";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Estimate for Your Review</h2>
    <p>Dear {customer.Name},</p>
    <p>Thank you for considering {settings.CompanyName}. Please review the attached estimate for:</p>
    <h3>{estimate.Title}</h3>
    <table style='margin: 20px 0;'>
        <tr><td>Estimated Total:</td><td><strong>${estimate.Total:N2}</strong></td></tr>
        <tr><td>Valid Until:</td><td>{estimate.ExpiresAt:MMMM dd, yyyy}</td></tr>
    </table>
    <p>Please reply to this email or call us to approve this estimate and schedule your service.</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
        _logger.LogInformation("Sent estimate email to {Email} for estimate {Title}", customer.Email, estimate.Title);
    }

    public async Task SendEstimateApprovedAsync(Estimate estimate)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(estimate.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Estimate Approved - Thank You!";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #28a745;'>Estimate Approved</h2>
    <p>Dear {customer.Name},</p>
    <p>Thank you for approving estimate: <strong>{estimate.Title}</strong></p>
    <p>We will be in touch shortly to schedule your service.</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
    }

    public async Task SendEstimateRejectedAsync(Estimate estimate)
    {
        // Usually internal notification only
        _logger.LogInformation("Estimate {Title} was declined", estimate.Title);
    }

    #endregion

    #region Job Notifications

    public async Task SendJobScheduledAsync(Job job)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(job.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Service Scheduled: {job.Title}";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Your Service is Scheduled</h2>
    <p>Dear {customer.Name},</p>
    <p>Your service has been scheduled:</p>
    <table style='margin: 20px 0; border-collapse: collapse;'>
        <tr><td style='padding: 5px 20px 5px 0;'>Service:</td><td><strong>{job.Title}</strong></td></tr>
        <tr><td style='padding: 5px 20px 5px 0;'>Date:</td><td><strong>{job.ScheduledDate:dddd, MMMM dd, yyyy}</strong></td></tr>
        <tr><td style='padding: 5px 20px 5px 0;'>Time:</td><td><strong>{FormatArrivalWindow(job)}</strong></td></tr>
    </table>
    <p>Please ensure someone is available at the service location. If you need to reschedule, please contact us as soon as possible.</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
        _logger.LogInformation("Sent job scheduled email to {Email} for job {Title}", customer.Email, job.Title);
    }

    public async Task SendJobReminderAsync(Job job, TimeSpan before)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(job.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var timeDescription = before.TotalHours >= 24 ? $"{before.TotalDays:0} day" : $"{before.TotalHours:0} hour";
        var subject = $"Reminder: Service Tomorrow - {job.Title}";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Service Reminder</h2>
    <p>Dear {customer.Name},</p>
    <p>This is a friendly reminder that your service is scheduled for <strong>tomorrow</strong>:</p>
    <table style='margin: 20px 0;'>
        <tr><td>Service:</td><td><strong>{job.Title}</strong></td></tr>
        <tr><td>Date:</td><td><strong>{job.ScheduledDate:dddd, MMMM dd, yyyy}</strong></td></tr>
        <tr><td>Time:</td><td><strong>{FormatArrivalWindow(job)}</strong></td></tr>
    </table>
    <p>Please ensure someone is available at the service location.</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
    }

    public async Task SendJobCompletedAsync(Job job)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(job.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Service Complete - {job.Title}";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #28a745;'>Service Complete</h2>
    <p>Dear {customer.Name},</p>
    <p>We have completed the following service:</p>
    <h3>{job.Title}</h3>
    <p>Thank you for choosing {settings.CompanyName}! If you have any questions or concerns about the work performed, please don't hesitate to contact us.</p>
    <p>We would appreciate a review of our service!</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
        _logger.LogInformation("Sent job completion email to {Email} for job {Title}", customer.Email, job.Title);
    }

    #endregion

    #region Service Agreement Notifications

    public async Task SendAgreementCreatedAsync(ServiceAgreement agreement)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(agreement.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Service Agreement Confirmation - {settings.CompanyName}";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Service Agreement Confirmed</h2>
    <p>Dear {customer.Name},</p>
    <p>Thank you for enrolling in our service agreement program!</p>
    <table style='margin: 20px 0;'>
        <tr><td>Agreement Type:</td><td><strong>{agreement.Name}</strong></td></tr>
        <tr><td>Start Date:</td><td>{agreement.StartDate:MMMM dd, yyyy}</td></tr>
        <tr><td>End Date:</td><td>{agreement.EndDate:MMMM dd, yyyy}</td></tr>
    </table>
    <p>We look forward to providing you with excellent service!</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
    }

    public async Task SendAgreementRenewalReminderAsync(ServiceAgreement agreement, int daysUntilExpiry)
    {
        if (!IsAutoNotificationsEnabled || !_emailService.IsConfigured) return;

        var customer = await GetCustomerAsync(agreement.CustomerId);
        if (customer == null || string.IsNullOrEmpty(customer.Email)) return;

        var settings = await _companySettings.GetCompanySettingsAsync();
        var subject = $"Service Agreement Renewal Reminder";
        
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Service Agreement Expiring Soon</h2>
    <p>Dear {customer.Name},</p>
    <p>Your service agreement with {settings.CompanyName} will expire in <strong>{daysUntilExpiry} days</strong> on {agreement.EndDate:MMMM dd, yyyy}.</p>
    <p>To continue enjoying the benefits of your service agreement, please contact us to renew.</p>
    <h3>Your Current Plan: {agreement.Name}</h3>
    <p>Renewing ensures you continue to receive priority service, discounts, and peace of mind.</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";

        await _emailService.SendEmailAsync(customer.Email, subject, body);
        _logger.LogInformation("Sent agreement renewal reminder to {Email}", customer.Email);
    }

    #endregion

    #region General

    public async Task SendCustomEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (!_emailService.IsConfigured)
        {
            _logger.LogWarning("Email not configured - cannot send custom email");
            return;
        }

        await _emailService.SendEmailAsync(toEmail, subject, htmlBody);
    }

    #endregion

    #region Helpers

    private async Task<Customer?> GetCustomerAsync(int customerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Customers.FindAsync(customerId);
    }

    private static string FormatArrivalWindow(Job job)
    {
        if (job.ArrivalWindowStart.HasValue && job.ArrivalWindowEnd.HasValue)
        {
            var start = DateTime.Today.Add(job.ArrivalWindowStart.Value);
            var end = DateTime.Today.Add(job.ArrivalWindowEnd.Value);
            return $"{start:h:mm tt} - {end:h:mm tt}";
        }
        if (job.ArrivalWindowStart.HasValue)
        {
            var start = DateTime.Today.Add(job.ArrivalWindowStart.Value);
            return start.ToString("h:mm tt");
        }
        return "TBD";
    }

    private string GenerateInvoiceEmailBody(Invoice invoice, Customer customer, CompanySettings settings)
    {
        return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Invoice #{invoice.InvoiceNumber}</h2>
    <p>Dear {customer.Name},</p>
    <p>Please find your invoice details below:</p>
    <table style='margin: 20px 0; border-collapse: collapse;'>
        <tr><td style='padding: 5px 20px 5px 0;'>Invoice Number:</td><td><strong>{invoice.InvoiceNumber}</strong></td></tr>
        <tr><td style='padding: 5px 20px 5px 0;'>Invoice Date:</td><td>{invoice.InvoiceDate:MMMM dd, yyyy}</td></tr>
        <tr><td style='padding: 5px 20px 5px 0;'>Due Date:</td><td><strong>{invoice.DueDate:MMMM dd, yyyy}</strong></td></tr>
        <tr><td style='padding: 5px 20px 5px 0;'>Amount Due:</td><td style='font-size: 18px; color: #007bff;'><strong>${invoice.Total:N2}</strong></td></tr>
    </table>
    <p>{settings.PaymentTerms}</p>
    <hr style='margin-top: 30px;' />
    <p style='color: #666; font-size: 12px;'>
        {settings.CompanyName}<br/>
        {settings.Phone} | {settings.Email}
    </p>
</body>
</html>";
    }

    #endregion
}
