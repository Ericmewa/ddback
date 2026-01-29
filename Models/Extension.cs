using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

public class Extension
{
    public Guid Id { get; set; }

    [Required]
    public string ExtensionNumber { get; set; } = string.Empty;

    public string? CustomerNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? LoanType { get; set; }

    public ExtensionStatus Status { get; set; } = ExtensionStatus.Pending;

    public string? RejectionReason { get; set; }

    public int CurrentApproverIndex { get; set; } = 0;

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Approver> Approvers { get; set; } = new List<Approver>();
}

public enum ExtensionStatus
{
    Pending,
    Approved,
    Rejected
}
