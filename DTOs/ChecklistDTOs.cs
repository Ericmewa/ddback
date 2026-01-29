using NCBA.DCL.Models;

namespace NCBA.DCL.DTOs;

public class SaveChecklistDraftRequest
{
    public Guid ChecklistId { get; set; }
    public string? DraftDataJson { get; set; }
    public bool? IsDraft { get; set; }
    public DateTime? DraftExpiresAt { get; set; }
}

// Checklist DTOs
public class CreateChecklistRequest
{
    public string CustomerNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? LoanType { get; set; }
}

public class UpdateChecklistRequest
{
    public string? CustomerName { get; set; }
    public string? LoanType { get; set; }
    public Guid? AssignedToRMId { get; set; }
    public Guid? AssignedToCoCheckerId { get; set; }
}

public class UpdateStatusRequest
{
    public ChecklistStatus Status { get; set; }
}

// Document DTOs
public class AddDocumentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class UpdateDocumentRequest
{
    public DocumentStatus? Status { get; set; }
    public string? CheckerComment { get; set; }
    public string? CreatorComment { get; set; }
    public string? RmComment { get; set; }
    public string? FileUrl { get; set; }
}

// CoCreator DTOs
public class CoCreatorReviewRequest
{
    public bool Approved { get; set; }
    public string? Comment { get; set; }
}

public class CoCheckerApprovalRequest
{
    public bool Approved { get; set; }
    public string? Comment { get; set; }
}

public class AdminUpdateDocumentRequest
{
    public Guid DocumentId { get; set; }
    public DocumentStatus? Status { get; set; }
    public string? Comment { get; set; }
}

public class UpdateChecklistStatusRequest
{
    public Guid ChecklistId { get; set; }
    public ChecklistStatus Status { get; set; }
}

// Checker DTOs
public class UpdateCheckerDCLRequest
{
    public ChecklistStatus Status { get; set; }
    public List<DocumentUpdateDto>? DocumentUpdates { get; set; }
}

public class DocumentUpdateDto
{
    public Guid DocumentId { get; set; }
    public CheckerStatus? Status { get; set; }
    public string? CheckerComment { get; set; }
}

public class RejectDCLRequest
{
    public string Reason { get; set; } = string.Empty;
}

// RM DTOs
public class SubmitToCoCreatorRequest
{
    public Guid ChecklistId { get; set; }
}
