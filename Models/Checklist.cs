using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

public class Checklist
{
    // Draft support
    public string? DraftDataJson { get; set; }
    public bool IsDraft { get; set; } = false;
    public DateTime? DraftExpiresAt { get; set; }
    public DateTime? DraftLastSaved { get; set; }

    public Guid Id { get; set; }

    [Required]
    public string DclNo { get; set; } = string.Empty;

    // Customer details
    public Guid? CustomerId { get; set; }
    public User? Customer { get; set; }

    [Required]
    public string CustomerNumber { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    // Loan info
    public string? LoanType { get; set; }

    // Assignments
    public Guid? AssignedToRMId { get; set; }
    public User? AssignedToRM { get; set; }

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public Guid? AssignedToCoCheckerId { get; set; }
    public User? AssignedToCoChecker { get; set; }

    // Main Status
    public ChecklistStatus Status { get; set; } = ChecklistStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<DocumentCategory> Documents { get; set; } = new List<DocumentCategory>();
    public ICollection<ChecklistLog> Logs { get; set; } = new List<ChecklistLog>();
}

public class ChecklistLog
{
    public Guid Id { get; set; }

    public string? Message { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;
}

public enum ChecklistStatus
{
    CoCreatorReview,
    RMReview,
    CoCheckerReview,
    Approved,
    Rejected,
    Active,
    Completed,
    Pending
}
