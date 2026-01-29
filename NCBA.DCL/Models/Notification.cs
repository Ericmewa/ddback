namespace NCBA.DCL.Models;

public class Notification
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string? Message { get; set; }

    public bool Read { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
