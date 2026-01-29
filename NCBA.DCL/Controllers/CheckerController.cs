using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Middleware;
using NCBA.DCL.Models;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/checkerChecklist")]
[Authorize]
public class CheckerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckerController> _logger;

    public CheckerController(ApplicationDbContext context, ILogger<CheckerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /api/checkerChecklist/active-dcls
    [HttpGet("active-dcls")]
    public async Task<IActionResult> GetActiveDCLs()
    {
        try
        {
            var activeDcls = await _context.Checklists
                .Where(c => c.Status == ChecklistStatus.CoCreatorReview)
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
                .Select(c => new
                {
                    id = c.Id,
                    dclNo = c.DclNo,
                    customerNumber = c.CustomerNumber,
                    customerName = c.CustomerName,
                    loanType = c.LoanType,
                    status = c.Status.ToString(),
                    createdBy = c.CreatedBy != null ? new { id = c.CreatedBy.Id, name = c.CreatedBy.Name } : null,
                    assignedToRM = c.AssignedToRM != null ? new { id = c.AssignedToRM.Id, name = c.AssignedToRM.Name } : null,
                    createdAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(activeDcls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active DCLs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/checkerChecklist/my-queue/:checkerId
    [HttpGet("my-queue/{checkerId}")]
    public async Task<IActionResult> GetMyQueue(Guid checkerId)
    {
        try
        {
            var myQueue = await _context.Checklists
                .Where(c => c.AssignedToCoCheckerId == checkerId &&
                           c.Status == ChecklistStatus.CoCheckerReview)
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(myQueue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching checker queue");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/checkerChecklist/completed/:checkerId
    [HttpGet("completed/{checkerId}")]
    public async Task<IActionResult> GetCompletedDCLs(Guid checkerId)
    {
        try
        {
            var completed = await _context.Checklists
                .Where(c => c.AssignedToCoCheckerId == checkerId &&
                           c.Status == ChecklistStatus.Approved)
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
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
                    assignedToRM = c.AssignedToRM != null ? new { id = c.AssignedToRM.Id, name = c.AssignedToRM.Name } : null,
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

    // GET /api/checkerChecklist/dcl/:id
    [HttpGet("dcl/{id}")]
    public async Task<IActionResult> GetDCLById(Guid id)
    {
        try
        {
            var dcl = await _context.Checklists
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                        .ThenInclude(d => d.CoCreatorFiles)
                .Include(c => c.Logs)
                    .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (dcl == null)
            {
                return NotFound(new { message = "DCL not found" });
            }

            return Ok(dcl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching DCL");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/checkerChecklist/dcl/:id
    [HttpPut("dcl/{id}")]
    [RoleAuthorize(UserRole.CoChecker)]
    public async Task<IActionResult> UpdateDCLStatus(Guid id, [FromBody] UpdateCheckerDCLRequest request)
    {
        try
        {
            var dcl = await _context.Checklists.FindAsync(id);
            if (dcl == null)
            {
                return NotFound(new { message = "DCL not found" });
            }

            dcl.Status = request.Status;

            // Add log entry
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var log = new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = $"Status updated to {request.Status} by checker",
                UserId = userId,
                ChecklistId = id,
                Timestamp = DateTime.UtcNow
            };
            _context.ChecklistLogs.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new { message = "DCL status updated successfully", status = dcl.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DCL status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/checkerChecklist/my-queue-auto/:checkerId
    [HttpGet("my-queue-auto/{checkerId}")]
    public async Task<IActionResult> GetAutoMovedQueue(Guid checkerId)
    {
        try
        {
            // Auto-move approved items from queue to completed
            var approvedItems = await _context.Checklists
                .Where(c => c.AssignedToCoCheckerId == checkerId &&
                           c.Status == ChecklistStatus.Approved)
                .ToListAsync();

            var myQueue = await _context.Checklists
                .Where(c => c.AssignedToCoCheckerId == checkerId &&
                           c.Status == ChecklistStatus.CoCheckerReview)
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                queue = myQueue,
                movedToCompleted = approvedItems.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching auto-moved queue");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PATCH /api/checkerChecklist/update-status
    [HttpPatch("update-status")]
    [RoleAuthorize(UserRole.CoChecker)]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateChecklistStatusRequest request)
    {
        try
        {
            var checklist = await _context.Checklists.FindAsync(request.ChecklistId);
            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            checklist.Status = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated successfully", status = checklist.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/checkerChecklist/reports/:checkerId
    [HttpGet("reports/{checkerId}")]
    public async Task<IActionResult> GetReports(Guid checkerId)
    {
        try
        {
            var totalAssigned = await _context.Checklists
                .CountAsync(c => c.AssignedToCoCheckerId == checkerId);

            var completed = await _context.Checklists
                .CountAsync(c => c.AssignedToCoCheckerId == checkerId &&
                               c.Status == ChecklistStatus.Approved);

            var pending = await _context.Checklists
                .CountAsync(c => c.AssignedToCoCheckerId == checkerId &&
                               c.Status == ChecklistStatus.CoCheckerReview);

            var rejected = await _context.Checklists
                .CountAsync(c => c.AssignedToCoCheckerId == checkerId &&
                               c.Status == ChecklistStatus.Rejected);

            return Ok(new
            {
                totalAssigned,
                completed,
                pending,
                rejected,
                completionRate = totalAssigned > 0 ? (double)completed / totalAssigned * 100 : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reports");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PATCH /api/checkerChecklist/approve/:id
    [HttpPatch("approve/{id}")]
    [RoleAuthorize(UserRole.CoChecker)]
    public async Task<IActionResult> ApproveDCL(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var dcl = await _context.Checklists
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (dcl == null)
            {
                return NotFound(new { message = "DCL not found" });
            }

            dcl.Status = ChecklistStatus.Approved;

            // Add log
            var log = new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = "DCL approved by checker",
                UserId = userId,
                ChecklistId = id,
                Timestamp = DateTime.UtcNow
            };
            _context.ChecklistLogs.Add(log);

            // Create notification for creator
            if (dcl.CreatedById.HasValue)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = dcl.CreatedById.Value,
                    Message = $"Your DCL {dcl.DclNo} has been approved",
                    Read = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "DCL approved successfully", status = dcl.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving DCL");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PATCH /api/checkerChecklist/reject/:id
    [HttpPatch("reject/{id}")]
    [RoleAuthorize(UserRole.CoChecker)]
    public async Task<IActionResult> RejectDCL(Guid id, [FromBody] RejectDCLRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var dcl = await _context.Checklists
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (dcl == null)
            {
                return NotFound(new { message = "DCL not found" });
            }

            dcl.Status = ChecklistStatus.Rejected;

            // Add log
            var log = new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = $"DCL rejected by checker: {request.Reason}",
                UserId = userId,
                ChecklistId = id,
                Timestamp = DateTime.UtcNow
            };
            _context.ChecklistLogs.Add(log);

            // Create notification for creator
            if (dcl.CreatedById.HasValue)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = dcl.CreatedById.Value,
                    Message = $"Your DCL {dcl.DclNo} has been rejected: {request.Reason}",
                    Read = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "DCL rejected successfully", status = dcl.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting DCL");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
