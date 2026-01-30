using System.ComponentModel.DataAnnotations;

namespace OneManVan.Shared.Models;

/// <summary>
/// Junction table for customer-company relationships.
/// Enables multiple customers to be associated with one company in different roles.
/// </summary>
public class CustomerCompanyRole
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Contact"; // Owner, Employee, Contact, Manager, Technician

    [MaxLength(100)]
    public string? Title { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public bool IsPrimaryContact { get; set; } = false;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Company Company { get; set; } = null!;
}
