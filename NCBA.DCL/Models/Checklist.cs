
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;


namespace NCBA.DCL.Models;


public class Checklist
{
    public Guid Id { get; set; }

    [Required]
    public string DclNo { get; set; } = string.Empty;

    // Customer details
    public Guid? CustomerId { get; set; }

    [JsonIgnore] // Ignore navigation properties for JSON
    public User? Customer { get; set; }

    [Required]
    public string CustomerNumber { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    // Loan info
    public string? LoanType { get; set; }
    public string? Comment { get; set; }

    // Assignments
    // Foreign key for RM (nullable)
    public Guid? AssignedToRMId { get; set; }

    [ForeignKey("AssignedToRMId")]
    public User? AssignedToRM { get; set; } // EF will handle FK




    public Guid? CreatedById { get; set; }

    [JsonIgnore]
    public User? CreatedBy { get; set; }

    public Guid? AssignedToCoCheckerId { get; set; }

    [JsonIgnore]
    public User? AssignedToCoChecker { get; set; }

    // Status
    public ChecklistStatus Status { get; set; } = ChecklistStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Documents
    public ICollection<DocumentCategory> Documents { get; set; } = new List<DocumentCategory>();

    [JsonIgnore] // Internal JSON storage, EF Core uses this
    public string? DocumentsJson { get; set; }

    // Logs
    public ICollection<ChecklistLog> Logs { get; set; } = new List<ChecklistLog>();

    // Helper methods for serialization (optional)
    public void SetDocumentsJson()
    {
        if (Documents != null)
        {
            DocumentsJson = System.Text.Json.JsonSerializer.Serialize(Documents);
        }
    }

    public void LoadDocumentsFromJson()
    {
        if (!string.IsNullOrEmpty(DocumentsJson))
        {
            try
            {
                Documents = System.Text.Json.JsonSerializer.Deserialize<ICollection<DocumentCategory>>(DocumentsJson)
                            ?? new List<DocumentCategory>();
            }
            catch
            {
                Documents = new List<DocumentCategory>();
            }
        }
    }

    // public static implicit operator Checklist(Checklist v)
    // {
    //     throw new NotImplementedException();
    // }
}

// Checklist log 

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



// Checklist status
public enum ChecklistStatus
{
    [EnumMember(Value = "co_creator_review")]
    CoCreatorReview,

    [EnumMember(Value = "rm_review")]
    RMReview,

    [EnumMember(Value = "co_checker_review")]
    CoCheckerReview,

    [EnumMember(Value = "approved")]
    Approved,

    [EnumMember(Value = "rejected")]
    Rejected,

    [EnumMember(Value = "active")]
    Active,

    [EnumMember(Value = "completed")]
    Completed,

    [EnumMember(Value = "pending")]
    Pending
}

