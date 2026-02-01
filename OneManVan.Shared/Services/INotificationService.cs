using OneManVan.Shared.Models;

namespace OneManVan.Shared.Services;

/// <summary>
/// Interface for automated notification services (email, SMS, etc).
/// </summary>
public interface INotificationService
{
    // Invoice notifications
    Task SendInvoiceCreatedAsync(Invoice invoice);
    Task SendInvoiceReminderAsync(Invoice invoice);
    Task SendPaymentReceivedAsync(Invoice invoice, decimal amount);
    
    // Estimate notifications
    Task SendEstimateCreatedAsync(Estimate estimate);
    Task SendEstimateApprovedAsync(Estimate estimate);
    Task SendEstimateRejectedAsync(Estimate estimate);
    
    // Job notifications
    Task SendJobScheduledAsync(Job job);
    Task SendJobReminderAsync(Job job, TimeSpan before);
    Task SendJobCompletedAsync(Job job);
    
    // Service Agreement notifications
    Task SendAgreementCreatedAsync(ServiceAgreement agreement);
    Task SendAgreementRenewalReminderAsync(ServiceAgreement agreement, int daysUntilExpiry);
    
    // General notifications
    Task SendCustomEmailAsync(string toEmail, string subject, string htmlBody);
}

/// <summary>
/// Notification result tracking.
/// </summary>
public class NotificationResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string NotificationType { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
}
