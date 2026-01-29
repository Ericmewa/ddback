using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

public class UserLog
{
    public Guid Id { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    public Guid? TargetUserId { get; set; }
    public User? TargetUser { get; set; }

    public string? TargetEmail { get; set; }

    public Guid? PerformedById { get; set; }
    public User? PerformedBy { get; set; }

    public string? PerformedByEmail { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
