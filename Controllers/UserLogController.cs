using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/user-logs")]
[Authorize]
public class UserLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserLogController> _logger;

    public UserLogController(ApplicationDbContext context, ILogger<UserLogController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        try
        {
            var logs = await _context.UserLogs
                .Include(l => l.TargetUser)
                .Include(l => l.PerformedBy)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new
                {
                    id = l.Id,
                    action = l.Action,
                    targetUser = l.TargetUser != null ? new { id = l.TargetUser.Id, email = l.TargetUser.Email } : null,
                    targetEmail = l.TargetEmail,
                    performedBy = l.PerformedBy != null ? new { id = l.PerformedBy.Id, email = l.PerformedBy.Email } : null,
                    performedByEmail = l.PerformedByEmail,
                    timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user logs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
