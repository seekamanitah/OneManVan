using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Shared.Models;

/// <summary>
/// Job entity representing scheduled work derived from an estimate.
/// </summary>
public class Job
{
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated job number (e.g., J-2024-0001).
    /// </summary>
    [MaxLength(20)]
    public string? JobNumber { get; set; }

    // === Relationships ===

    public int? EstimateId { get; set; }

    [Required]
    public int CustomerId { get; set; }

    public int? SiteId { get; set; }

    public int? AssetId { get; set; }

    /// <summary>
    /// Service agreement this job is associated with.
    /// </summary>
    public int? ServiceAgreementId { get; set; }

    /// <summary>
    /// Parent job ID if this is a recurring job.
    /// </summary>
    public int? RecurringJobId { get; set; }

    /// <summary>
    /// Follow-up from this previous job.
    /// </summary>
    public int? FollowUpFromJobId { get; set; }

    // === Job Info ===

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public JobType JobType { get; set; } = JobType.ServiceCall;

    public JobPriority Priority { get; set; } = JobPriority.Normal;

    public JobStatus Status { get; set; } = JobStatus.Draft;

    // === Scheduling ===

    /// <summary>
    /// Date/time customer requested.
    /// </summary>
    public DateTime? RequestedDate { get; set; }

    public DateTime? ScheduledDate { get; set; }

    public DateTime? ScheduledEndDate { get; set; }

    /// <summary>
    /// Arrival window start (e.g., 8:00 AM).
    /// </summary>
    public TimeSpan? ArrivalWindowStart { get; set; }

    /// <summary>
    /// Arrival window end (e.g., 12:00 PM).
    /// </summary>
    public TimeSpan? ArrivalWindowEnd { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal EstimatedHours { get; set; } = 1;

    public bool IsFlexibleSchedule { get; set; } = false;

    // === Execution Timestamps ===

    public DateTime? DispatchedAt { get; set; }

    public DateTime? EnRouteAt { get; set; }

    public DateTime? ArrivedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    // === Work Details ===

    /// <summary>
    /// Customer-reported problem/issue.
    /// </summary>
    [MaxLength(2000)]
    public string? ProblemDescription { get; set; }

    /// <summary>
    /// Technician diagnosis.
    /// </summary>
    [MaxLength(2000)]
    public string? Diagnosis { get; set; }

    /// <summary>
    /// Work that was performed.
    /// </summary>
    [MaxLength(2000)]
    public string? WorkPerformed { get; set; }

    /// <summary>
    /// Recommended future work (upsell opportunities).
    /// </summary>
    [MaxLength(2000)]
    public string? RecommendedWork { get; set; }

    /// <summary>
    /// Internal notes (not visible to customer).
    /// </summary>
    [MaxLength(2000)]
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Notes visible on invoice/to customer.
    /// </summary>
    [MaxLength(2000)]
    public string? CustomerNotes { get; set; }

    /// <summary>
    /// Legacy notes field.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    // === Checklists (JSON) ===

    /// <summary>
    /// Pre-job checklist items (JSON array).
    /// </summary>
    [MaxLength(4000)]
    public string? PreJobChecklist { get; set; }

    /// <summary>
    /// Post-job checklist items (JSON array).
    /// </summary>
    [MaxLength(4000)]
    public string? PostJobChecklist { get; set; }

    // === Financial ===

    [Column(TypeName = "decimal(10,2)")]
    public decimal LaborTotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PartsTotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MaterialTotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TripCharge { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    [MaxLength(200)]
    public string? DiscountReason { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(5,3)")]
    public decimal TaxRate { get; set; } = 0.07m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// When true, the price already includes tax (no additional tax calculation needed).
    /// </summary>
    public bool TaxIncluded { get; set; } = false;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    // === Signature & Approval ===

    /// <summary>
    /// Customer signature (Base64 or file path).
    /// </summary>
    [MaxLength(500)]
    public string? CustomerSignature { get; set; }

    [MaxLength(100)]
    public string? SignedByName { get; set; }

    public DateTime? SignedAt { get; set; }

    public bool CustomerApproved { get; set; } = false;

    // === Follow-up ===

    public bool RequiresFollowUp { get; set; } = false;

    [MaxLength(500)]
    public string? FollowUpReason { get; set; }

    public DateTime? FollowUpDate { get; set; }

    public int? FollowUpJobId { get; set; }

    // === Calendar Integration ===

    [MaxLength(100)]
    public string? GoogleCalendarEventId { get; set; }

    [MaxLength(100)]
    public string? OutlookEventId { get; set; }

    [MaxLength(100)]
    public string? CalendarEventId { get; set; }

    // === Metadata ===

    [MaxLength(500)]
    public string? Tags { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [MaxLength(100)]
    public string? LastModifiedBy { get; set; }

    // === Soft Delete ===

    /// <summary>
    /// Indicates if the job has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When the job was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the job.
    /// </summary>
    [MaxLength(200)]
    public string? DeletedBy { get; set; }

    // === Navigation Properties ===

    public Estimate? Estimate { get; set; }

    public Customer Customer { get; set; } = null!;

    public Site? Site { get; set; }

    public Asset? Asset { get; set; }

    public Job? FollowUpJob { get; set; }

    public Job? FollowUpFromJob { get; set; }

    public ICollection<TimeEntry> TimeEntries { get; set; } = [];

    public ICollection<Invoice> Invoices { get; set; } = [];

    /// <summary>
    /// Workers assigned to this job.
    /// </summary>
    public ICollection<JobWorker> Workers { get; set; } = [];

    // === Computed Properties ===

    [NotMapped]
    public decimal ActualHours => TimeEntries
        .Where(t => t.EndTime.HasValue)
        .Sum(t => (decimal)(t.EndTime!.Value - t.StartTime).TotalHours);

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        JobStatus.Draft => "Draft",
        JobStatus.Scheduled => "Scheduled",
        JobStatus.EnRoute => "En Route",
        JobStatus.InProgress => "In Progress",
        JobStatus.Completed => "Completed",
        JobStatus.Closed => "Closed",
        JobStatus.Cancelled => "Cancelled",
        JobStatus.OnHold => "On Hold",
        _ => "Unknown"
    };

    [NotMapped]
    public string JobTypeDisplay => JobType switch
    {
        Enums.JobType.ServiceCall => "Service Call",
        Enums.JobType.Repair => "Repair",
        Enums.JobType.Maintenance => "Maintenance",
        Enums.JobType.Installation => "Installation",
        Enums.JobType.Replacement => "Replacement",
        Enums.JobType.Inspection => "Inspection",
        Enums.JobType.Emergency => "Emergency",
        Enums.JobType.Warranty => "Warranty",
        Enums.JobType.Callback => "Callback",
        Enums.JobType.Estimate => "Estimate Only",
        Enums.JobType.Ductwork => "Ductwork",
        Enums.JobType.StartUp => "Start-Up",
        _ => "Other"
    };

    [NotMapped]
    public string PriorityDisplay => Priority switch
    {
        JobPriority.Low => "Low",
        JobPriority.Normal => "Normal",
        JobPriority.High => "High",
        JobPriority.Urgent => "Urgent",
        JobPriority.Emergency => "?? Emergency",
        _ => "Normal"
    };

    [NotMapped]
    public bool IsEmergency => Priority == JobPriority.Emergency || JobType == JobType.Emergency;

    [NotMapped]
    public bool CanStart => Status == JobStatus.Scheduled || Status == JobStatus.EnRoute;

    [NotMapped]
    public bool CanComplete => Status == JobStatus.InProgress;

    [NotMapped]
    public bool CanEdit => Status != JobStatus.Closed && Status != JobStatus.Cancelled;

    [NotMapped]
    public bool CanInvoice => Status == JobStatus.Completed;

    [NotMapped]
    public bool HasSignature => !string.IsNullOrWhiteSpace(CustomerSignature);

    [NotMapped]
    public string ArrivalWindowDisplay
    {
        get
        {
            if (!ArrivalWindowStart.HasValue || !ArrivalWindowEnd.HasValue)
                return "Any time";

            var start = DateTime.Today.Add(ArrivalWindowStart.Value);
            var end = DateTime.Today.Add(ArrivalWindowEnd.Value);
            return $"{start:h:mm tt} - {end:h:mm tt}";
        }
    }

    [NotMapped]
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    [NotMapped]
    public List<string> TagList => string.IsNullOrWhiteSpace(Tags)
        ? []
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    [NotMapped]
    public bool IsScheduledToday => ScheduledDate.HasValue && ScheduledDate.Value.Date == DateTime.Today;

    [NotMapped]
    public bool IsOverdue => ScheduledDate.HasValue && 
        ScheduledDate.Value.Date < DateTime.Today && 
        Status != JobStatus.Completed && 
        Status != JobStatus.Closed && 
        Status != JobStatus.Cancelled;

    // === Helper Methods ===

    public static Job FromEstimate(Estimate estimate)
    {
        return new Job
        {
            EstimateId = estimate.Id,
            CustomerId = estimate.CustomerId,
            SiteId = estimate.SiteId,
            AssetId = estimate.AssetId,
            Title = estimate.Title,
            Description = estimate.Description,
            LaborTotal = estimate.Lines.Where(l => l.Type == LineItemType.Labor).Sum(l => l.Total),
            PartsTotal = estimate.Lines.Where(l => l.Type != LineItemType.Labor && l.Type != LineItemType.Discount).Sum(l => l.Total),
            TaxAmount = estimate.TaxAmount,
            Total = estimate.Total,
            EstimatedHours = estimate.Lines.Where(l => l.Type == LineItemType.Labor).Sum(l => l.Quantity),
            Status = JobStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecalculateTotals(decimal laborRate = 85m, decimal taxRate = 0.07m)
    {
        LaborTotal = ActualHours * laborRate;
        SubTotal = LaborTotal + PartsTotal + MaterialTotal + TripCharge - DiscountAmount;
        TaxRate = taxRate;
        TaxAmount = SubTotal * taxRate;
        Total = SubTotal + TaxAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(JobStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        switch (newStatus)
        {
            case JobStatus.EnRoute:
                EnRouteAt ??= DateTime.UtcNow;
                break;
            case JobStatus.InProgress:
                ArrivedAt ??= DateTime.UtcNow;
                StartedAt ??= DateTime.UtcNow;
                break;
            case JobStatus.Completed:
                CompletedAt ??= DateTime.UtcNow;
                break;
            case JobStatus.Closed:
                ClosedAt ??= DateTime.UtcNow;
                break;
        }
    }
}
