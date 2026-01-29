using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

public class AuditLog
{
    public Guid Id { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    public string? Resource { get; set; }
    public string? Status { get; set; }
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }

    public Guid? PerformedById { get; set; }
    public User? PerformedBy { get; set; }

    public Guid? TargetUserId { get; set; }
    public User? TargetUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}