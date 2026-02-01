namespace OneManVan.Shared.Models.Enums;

/// <summary>
/// Type of employee or contractor.
/// </summary>
public enum EmployeeType
{
    /// <summary>
    /// Full-time employee (W-2, benefits eligible).
    /// </summary>
    FullTime = 0,

    /// <summary>
    /// Part-time employee (W-2, limited hours).
    /// </summary>
    PartTime = 1,

    /// <summary>
    /// Independent contractor (1099, self-employed).
    /// </summary>
    Contractor1099 = 2,

    /// <summary>
    /// Subcontractor company (invoices, has own EIN).
    /// </summary>
    SubcontractorCompany = 3
}

/// <summary>
/// Employment status.
/// </summary>
public enum EmployeeStatus
{
    /// <summary>
    /// Currently active and working.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Temporarily inactive (not scheduled).
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Employment has ended.
    /// </summary>
    Terminated = 2,

    /// <summary>
    /// On leave (vacation, medical, etc.).
    /// </summary>
    OnLeave = 3
}

/// <summary>
/// How the worker is compensated.
/// </summary>
public enum PayRateType
{
    /// <summary>
    /// Paid per hour worked.
    /// </summary>
    Hourly = 0,

    /// <summary>
    /// Flat rate per job/project.
    /// </summary>
    Flat = 1,

    /// <summary>
    /// Annual salary.
    /// </summary>
    Salary = 2
}

// Note: PaymentMethod is defined in InvoiceStatus.cs with values:
// Cash, Check, CreditCard, DebitCard, BankTransfer, Digital, Financing, Other, DirectDeposit, ACH, Invoice

/// <summary>
/// Category of performance note.
/// </summary>
public enum PerformanceCategory
{
    /// <summary>
    /// Quality of work performed.
    /// </summary>
    Quality = 0,

    /// <summary>
    /// Reliability and punctuality.
    /// </summary>
    Reliability = 1,

    /// <summary>
    /// Speed and efficiency.
    /// </summary>
    Speed = 2,

    /// <summary>
    /// Attitude and professionalism.
    /// </summary>
    Attitude = 3,

    /// <summary>
    /// Specific job performance.
    /// </summary>
    JobPerformance = 4,

    /// <summary>
    /// Issue or problem.
    /// </summary>
    Issue = 5,

    /// <summary>
    /// Positive feedback.
    /// </summary>
    Positive = 6,

    /// <summary>
    /// General note.
    /// </summary>
    General = 7
}
