using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Middleware;
using NCBA.DCL.Models;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/rmChecklist")]
[Authorize]
public class RMController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RMController> _logger;

    public RMController(ApplicationDbContext context, ILogger<RMController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /api/rmChecklist/:rmId/myqueue
    [HttpGet("{rmId}/myqueue")]
    [RoleAuthorize(UserRole.RM)]
    public async Task<IActionResult> GetMyQueue(Guid rmId)
    {
        try
        {
            var myQueue = await _context.Checklists
                .Where(c => c.AssignedToRMId == rmId &&
                           (c.Status == ChecklistStatus.RMReview ||
                            c.Status == ChecklistStatus.Pending))
                .Include(c => c.CreatedBy)
                .Include(c => c.Customer)
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    id = c.Id,
                    dclNo = c.DclNo,
                    customerNumber = c.CustomerNumber,
                    customerName = c.CustomerName,
                    loanType = c.LoanType,
                    status = c.Status.ToString(),
                    createdBy = c.CreatedBy != null ? new { id = c.CreatedBy.Id, name = c.CreatedBy.Name, email = c.CreatedBy.Email } : null,
                    documentCount = c.Documents.Sum(dc => dc.DocList.Count),
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(myQueue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching RM queue");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST /api/rmChecklist/rm-submit-to-co-creator
    [HttpPost("rm-submit-to-co-creator")]
    [RoleAuthorize(UserRole.RM)]
    public async Task<IActionResult> SubmitToCoCreator([FromBody] SubmitToCoCreatorRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var checklist = await _context.Checklists
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == request.ChecklistId);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            // Verify current user is the assigned RM
            if (checklist.AssignedToRMId != userId)
            {
                return Forbid();
            }

            // Update status to co-creator review
            checklist.Status = ChecklistStatus.CoCreatorReview;

            // Add log entry
            var log = new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = "Submitted to Co-Creator for review by RM",
                UserId = userId,
                ChecklistId = request.ChecklistId,
                Timestamp = DateTime.UtcNow
            };
            _context.ChecklistLogs.Add(log);

            // Create notification for co-creator
            if (checklist.CreatedById.HasValue)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = checklist.CreatedById.Value,
                    Message = $"DCL {checklist.DclNo} has been submitted for your review by RM",
                    Read = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Checklist submitted to Co-Creator successfully",
                status = checklist.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting to co-creator");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/rmChecklist/completed/rm/:rmId
    [HttpGet("completed/rm/{rmId}")]
    [RoleAuthorize(UserRole.RM)]
    public async Task<IActionResult> GetCompletedDCLs(Guid rmId)
    {
        try
        {
            var completed = await _context.Checklists
                .Where(c => c.AssignedToRMId == rmId &&
                           (c.Status == ChecklistStatus.Approved ||
                            c.Status == ChecklistStatus.Completed))
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToCoChecker)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new
                {
                    id = c.Id,
                    dclNo = c.DclNo,
                    customerNumber = c.CustomerNumber,
                    customerName = c.CustomerName,
                    loanType = c.LoanType,
                    status = c.Status.ToString(),
                    createdBy = c.CreatedBy != null ? new { id = c.CreatedBy.Id, name = c.CreatedBy.Name } : null,
                    assignedToCoChecker = c.AssignedToCoChecker != null ? new { id = c.AssignedToCoChecker.Id, name = c.AssignedToCoChecker.Name } : null,
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(completed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching completed DCLs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // DELETE /api/rmChecklist/:id
    [HttpDelete("{id}")]
    [RoleAuthorize(UserRole.RM, UserRole.Admin)]
    public async Task<IActionResult> DeleteDCL(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var checklist = await _context.Checklists.FindAsync(id);

            if (checklist == null)
            {
                return NotFound(new { message = "DCL not found" });
            }

            // Verify user is authorized (assigned RM or admin)
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && checklist.AssignedToRMId != userId)
            {
                return Forbid();
            }

            _context.Checklists.Remove(checklist);
            await _context.SaveChangesAsync();

            return Ok(new { message = "DCL deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting DCL");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/rmChecklist/:id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetChecklistById(Guid id)
    {
        try
        {
            var checklist = await _context.Checklists
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
                .Include(c => c.Customer)
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                        .ThenInclude(d => d.CoCreatorFiles)
                .Include(c => c.Logs)
                    .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            return Ok(checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching checklist");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // DELETE /api/rmChecklist/:checklistId/document/:documentId
    [HttpDelete("{checklistId}/document/{documentId}")]
    [RoleAuthorize(UserRole.RM)]
    public async Task<IActionResult> DeleteDocumentFile(Guid checklistId, Guid documentId)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var checklist = await _context.Checklists
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.Id == checklistId);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            // Verify user is the assigned RM
            if (checklist.AssignedToRMId != userId)
            {
                return Forbid();
            }

            var document = checklist.Documents
                .SelectMany(dc => dc.DocList)
                .FirstOrDefault(d => d.Id == documentId);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            // Clear the file URL
            document.FileUrl = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Document file removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document file");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/rmChecklist/notifications/rm
    [HttpGet("notifications/rm")]
    [RoleAuthorize(UserRole.RM)]
    public async Task<IActionResult> GetNotifications([FromQuery] Guid userId)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    id = n.Id,
                    message = n.Message,
                    read = n.Read,
                    createdAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/rmChecklist/notifications/rm/:notificationId
    [HttpPut("notifications/rm/{notificationId}")]
    [RoleAuthorize(UserRole.RM)]
    public async Task<IActionResult> MarkNotificationAsRead(Guid notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return NotFound(new { message = "Notification not found" });
            }

            notification.Read = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
