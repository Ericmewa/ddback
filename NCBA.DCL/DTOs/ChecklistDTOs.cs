
// using NCBA.DCL.Models;
// using System.ComponentModel.DataAnnotations;
// using System.Text.Json.Serialization;

// namespace NCBA.DCL.DTOs;

// // ===============================
// // Checklist Document DTO
// // ===============================

// public class CreateChecklistRequest
// {
//     [Required]
//     [JsonPropertyName("customerId")]
//     public Guid? CustomerId { get; set; }

//     [JsonPropertyName("customerNumber")]
//     public string CustomerNumber { get; set; } = string.Empty;

//     [JsonPropertyName("customerName")]
//     public string? CustomerName { get; set; }

//     [JsonPropertyName("loanType")]
//     public string? LoanType { get; set; }

//     // [JsonPropertyName("assignedToRM")]
//     // public string? AssignedToRM { get; set; }
//     [JsonPropertyName("assignedToRMId")]
//     public Guid AssignedToRMId { get; set; }



//     // ✅ THIS MUST MATCH FRONTEND JSON
//     [Required]
//     [MinLength(1)]
//     [JsonPropertyName("documents")]
//     public List<ChecklistCategoryDto> Documents { get; set; } = new();
// }

// public class SubmitToCoCheckerRequest
// {
//     public List<ChecklistDocumentDto> Documents { get; set; } = new();
//     public string FinalComment { get; set; } = string.Empty;
//     public List<AttachmentDto> Attachments { get; set; } = new();
// }

// public class AttachmentDto
// {
//     public string FileName { get; set; } = string.Empty;
//     public string FileUrl { get; set; } = string.Empty;
// }
// public class ChecklistDocumentDto
// {
//     [Required]
//     [JsonPropertyName("name")]
//     public string Name { get; set; } = string.Empty;

//     [JsonPropertyName("status")]

//     // 🔥 STRING, NOT enum
//     public string Status { get; set; } = "PendingRM";

//     [JsonPropertyName("fileUrl")]
//     public string? FileUrl { get; set; }

//     // ✅ Add comment fields
//     [JsonPropertyName("creatorComment")]
//     public string? CreatorComment { get; set; }

//     [JsonPropertyName("checkerComment")]
//     public string? CheckerComment { get; set; }

//     [JsonPropertyName("rmComment")]
//     public string? RmComment { get; set; }
//     [JsonPropertyName("comment")]
//     public string? Comment { get; set; }
// }




// // }

// public class ChecklistCategoryDto
// {
//     [Required]
//     [JsonPropertyName("category")]
//     public string Category { get; set; } = string.Empty;

//     [Required]
//     [MinLength(1)]
//     [JsonPropertyName("documents")] // must match frontend payload
//     public List<ChecklistDocumentDto> Documents { get; set; } = new();
// }


// // ===============================
// // Update Checklist
// // ===============================
// public class UpdateChecklistRequest
// {
//     [JsonPropertyName("customerName")]
//     public string? CustomerName { get; set; }

//     [JsonPropertyName("loanType")]
//     public string? LoanType { get; set; }

//     [JsonPropertyName("assignedToRMId")]
//     public Guid? AssignedToRMId { get; set; }

//     [JsonPropertyName("assignedToCoCheckerId")]
//     public Guid? AssignedToCoCheckerId { get; set; }
// }

// public class UpdateStatusRequest
// {
//     [JsonPropertyName("status")]
//     public ChecklistStatus Status { get; set; }
// }

// // ===============================
// // Document DTOs
// // ===============================
// public class AddDocumentRequest
// {
//     [Required]
//     [JsonPropertyName("name")]
//     public string Name { get; set; } = string.Empty;

//     [Required]
//     [JsonPropertyName("category")]
//     public string Category { get; set; } = string.Empty;
// }

// public class UpdateDocumentRequest
// {
//     [JsonPropertyName("status")]
//     public DocumentStatus? Status { get; set; }

//     [JsonPropertyName("checkerComment")]
//     public string? CheckerComment { get; set; }

//     [JsonPropertyName("creatorComment")]
//     public string? CreatorComment { get; set; }

//     [JsonPropertyName("rmComment")]
//     public string? RmComment { get; set; }

//     [JsonPropertyName("fileUrl")]
//     public string? FileUrl { get; set; }
//     [JsonPropertyName("comment")]
//     public string? Comment { get; set; }
// }

// // ===============================
// // CoCreator DTOs
// // ===============================
// public class CoCreatorReviewRequest
// {
//     [JsonPropertyName("approved")]
//     public bool Approved { get; set; }

//     [JsonPropertyName("comment")]
//     public string? Comment { get; set; }
// }

// public class CoCheckerApprovalRequest
// {
//     [JsonPropertyName("approved")]
//     public bool Approved { get; set; }

//     [JsonPropertyName("comment")]
//     public string? Comment { get; set; }
// }

// public class AdminUpdateDocumentRequest
// {
//     [Required]
//     [JsonPropertyName("documentId")]
//     public Guid DocumentId { get; set; }

//     [JsonPropertyName("status")]
//     public DocumentStatus? Status { get; set; }

//     [JsonPropertyName("comment")]
//     public string? Comment { get; set; }
// }

// public class UpdateChecklistStatusRequest
// {
//     [Required]
//     [JsonPropertyName("checklistId")]
//     public Guid ChecklistId { get; set; }

//     [JsonPropertyName("status")]
//     public ChecklistStatus Status { get; set; }
// }

// // ===============================
// // Checker DTOs
// // ===============================
// public class UpdateCheckerDCLRequest
// {
//     [JsonPropertyName("status")]
//     public ChecklistStatus Status { get; set; }
// }

// public class RejectDCLRequest
// {
//     [Required]
//     [JsonPropertyName("reason")]
//     public string Reason { get; set; } = string.Empty;
// }

// // ===============================
// // RM DTOs
// // ===============================
// public class SubmitToCoCreatorRequest
// {
//     [Required]
//     [JsonPropertyName("checklistId")]
//     public Guid ChecklistId { get; set; }
// }

// // ===============================
// // Create Checklist DTO
// // ===============================
using NCBA.DCL.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NCBA.DCL.DTOs;

// ===============================
// Checklist Document DTO
// ===============================

public class CreateChecklistRequest
{
    [Required]
    [JsonPropertyName("customerId")]
    public Guid? CustomerId { get; set; }

    [JsonPropertyName("customerNumber")]
    public string CustomerNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("loanType")]
    public string? LoanType { get; set; }

    [JsonPropertyName("assignedToRMId")]
    public Guid AssignedToRMId { get; set; }

    [Required]
    [MinLength(1)]
    [JsonPropertyName("documents")]
    public List<ChecklistCategoryDto> Documents { get; set; } = new();
}

// ===============================
// Submit to Co-Checker DTO
// ===============================
// public class SubmitToCoCheckerRequest
// {
//     [Required]
//     [JsonPropertyName("checklistId")]
//     public Guid ChecklistId { get; set; }   // 🔥 Added

//     [JsonPropertyName("status")]
//     public ChecklistStatus Status { get; set; } = ChecklistStatus.CoCheckerReview; // 🔥 Added

//     [Required]
//     [JsonPropertyName("documents")]
//     public List<ChecklistDocumentDto> Documents { get; set; } = new();

//     [JsonPropertyName("finalComment")]
//     public string FinalComment { get; set; } = string.Empty;

//     [JsonPropertyName("attachments")]
//     public List<AttachmentDto> Attachments { get; set; } = new();
// }

public class SubmitToCoCheckerRequest
{
    [Required]
    [JsonPropertyName("checklistId")]
    public Guid ChecklistId { get; set; }

    [JsonPropertyName("status")]
    public ChecklistStatus Status { get; set; } = ChecklistStatus.CoCheckerReview;

    [Required]
    [JsonPropertyName("documents")]
    public List<ChecklistCategoryDto> Documents { get; set; } = new();

    [JsonPropertyName("finalComment")]
    public string FinalComment { get; set; } = string.Empty;

    [JsonPropertyName("attachments")]
    public List<AttachmentDto> Attachments { get; set; } = new();
}


// ===============================
// Attachment DTO
// ===============================
public class AttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
}

// ===============================
// Checklist Document DTO
// ===============================
public class ChecklistDocumentDto
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "PendingRM";

    [JsonPropertyName("action")]
    public string? Action { get; set; }  // 🔥 Added

    [JsonPropertyName("fileUrl")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("creatorComment")]
    public string? CreatorComment { get; set; }

    [JsonPropertyName("checkerComment")]
    public string? CheckerComment { get; set; }

    [JsonPropertyName("rmComment")]
    public string? RmComment { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

// ===============================
// Checklist Category DTO
// ===============================
public class ChecklistCategoryDto
{
    [Required]
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [JsonPropertyName("documents")]
    public List<ChecklistDocumentDto> Documents { get; set; } = new();
}

// ===============================
// Update Checklist
// ===============================
public class UpdateChecklistRequest
{
    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("loanType")]
    public string? LoanType { get; set; }

    [JsonPropertyName("assignedToRMId")]
    public Guid? AssignedToRMId { get; set; }

    [JsonPropertyName("assignedToCoCheckerId")]
    public Guid? AssignedToCoCheckerId { get; set; }
}

public class UpdateStatusRequest
{
    [JsonPropertyName("status")]
    public ChecklistStatus Status { get; set; }
}

// ===============================
// Document DTOs
// ===============================
public class AddDocumentRequest
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}

public class UpdateDocumentRequest
{
    [JsonPropertyName("status")]
    public DocumentStatus? Status { get; set; }

    [JsonPropertyName("checkerComment")]
    public string? CheckerComment { get; set; }

    [JsonPropertyName("creatorComment")]
    public string? CreatorComment { get; set; }

    [JsonPropertyName("rmComment")]
    public string? RmComment { get; set; }

    [JsonPropertyName("fileUrl")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }  // 🔥 Added
}

// ===============================
// CoCreator DTOs
// ===============================
public class CoCreatorReviewRequest
{
    [JsonPropertyName("approved")]
    public bool Approved { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class CoCheckerApprovalRequest
{
    [JsonPropertyName("approved")]
    public bool Approved { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class AdminUpdateDocumentRequest
{
    [Required]
    [JsonPropertyName("documentId")]
    public Guid DocumentId { get; set; }

    [JsonPropertyName("status")]
    public DocumentStatus? Status { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class UpdateChecklistStatusRequest
{
    [Required]
    [JsonPropertyName("checklistId")]
    public Guid ChecklistId { get; set; }

    [JsonPropertyName("status")]
    public ChecklistStatus Status { get; set; }
}

// ===============================
// Checker DTOs
// ===============================
public class UpdateCheckerDCLRequest
{
    [JsonPropertyName("status")]
    public ChecklistStatus Status { get; set; }
}

public class RejectDCLRequest
{
    [Required]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

// ===============================
// RM DTOs
// ===============================
public class SubmitToCoCreatorRequest
{
    [Required]
    [JsonPropertyName("checklistId")]
    public Guid ChecklistId { get; set; }
}
