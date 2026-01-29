using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Admin;

    public bool Active { get; set; } = true;

    public string? CustomerNumber { get; set; }

    public string? CustomerId { get; set; }

    public string? RmId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Checklist> CreatedChecklists { get; set; } = new List<Checklist>();
    public ICollection<Checklist> AssignedAsRM { get; set; } = new List<Checklist>();
    public ICollection<Checklist> AssignedAsCoChecker { get; set; } = new List<Checklist>();
    public ICollection<UserLog> TargetUserLogs { get; set; } = new List<UserLog>();
    public ICollection<UserLog> PerformedByLogs { get; set; } = new List<UserLog>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Deferral> CreatedDeferrals { get; set; } = new List<Deferral>();
}

public enum UserRole
{
    Admin,
    RM,
    Approver,
    CoCreator,
    CoChecker,
    Customer
}
