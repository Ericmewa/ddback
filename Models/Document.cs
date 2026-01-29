using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

public class Document
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Category { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.PendingRM;

    public string? CheckerComment { get; set; }

    public string? CreatorComment { get; set; }

    public string? RmComment { get; set; }

    public string? FileUrl { get; set; }

    public string? Comment { get; set; }

    public CreatorStatus? CreatorStatus { get; set; }

    public CheckerStatus CheckerStatus { get; set; } = CheckerStatus.Pending;

    public RmStatus RmStatus { get; set; } = RmStatus.PendingFromCustomer;

    public string? DeferralReason { get; set; }
    public string? DeferralNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public Guid CategoryId { get; set; }
    public DocumentCategory DocumentCategory { get; set; } = null!;

    // Supporting files
    public ICollection<CoCreatorFile> CoCreatorFiles { get; set; } = new List<CoCreatorFile>();
}

public class CoCreatorFile
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }

    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
}

public class DocumentCategory
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;

    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    public ICollection<Document> DocList { get; set; } = new List<Document>();
}

public enum DocumentStatus
{
    Pending,
    Submitted,
    PendingRM,
    PendingCo,
    SubmittedForReview,
    Sighted,
    Waived,
    Deferred,
    TBO,
    Approved,
    Incomplete,
    ReturnedByChecker,
    DeferralRequested,
    PendingFromCustomer
}

public enum CreatorStatus
{
    Submitted,
    PendingRM,
    PendingCo,
    Deferred,
    TBO,
    Waived,
    Sighted
}

public enum CheckerStatus
{
    Approved,
    Rejected,
    Pending
}

public enum RmStatus
{
    DeferralRequested,
    SubmittedForReview,
    PendingFromCustomer
}
