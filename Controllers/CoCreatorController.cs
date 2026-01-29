using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Helpers;
using NCBA.DCL.Middleware;
using NCBA.DCL.Models;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/cocreatorChecklist")]
[Authorize]



public class CoCreatorController : ControllerBase
{

    // GET /api/cocreatorChecklist/{id}/draft
    [HttpGet("{id}/draft")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> GetChecklistDraft(Guid id)
    {
        try
        {
            var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == id);
            if (checklist == null)
                return NotFound(new { message = "Checklist not found" });

            return Ok(new
            {
                draftData = checklist.DraftDataJson,
                isDraft = checklist.IsDraft,
                expiresAt = checklist.DraftExpiresAt,
                lastSaved = checklist.DraftLastSaved
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading draft");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST /api/cocreatorChecklist/save-draft
    [HttpPost("save-draft")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> SaveChecklistDraft([FromBody] SaveChecklistDraftRequest request)
    {
        try
        {
            if (request.ChecklistId == null || request.ChecklistId == Guid.Empty)
                return BadRequest(new { message = "ChecklistId is required in the request body." });

            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId);
            if (checklist == null)
                return NotFound(new { message = "Checklist not found" });

            checklist.DraftDataJson = request.DraftDataJson;
            checklist.IsDraft = request.IsDraft ?? true;
            checklist.DraftExpiresAt = request.DraftExpiresAt ?? DateTime.UtcNow.AddDays(1);
            checklist.DraftLastSaved = DateTime.UtcNow;
            checklist.UpdatedAt = DateTime.UtcNow;

            checklist.Logs.Add(new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = "Draft saved",
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                ChecklistId = checklist.Id
            });

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Draft saved successfully",
                checklist = new
                {
                    id = checklist.Id,
                    dclNo = checklist.DclNo,
                    status = checklist.Status.ToString(),
                    lastSaved = checklist.DraftLastSaved,
                    expiresAt = checklist.DraftExpiresAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving draft");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PATCH /api/cocreatorChecklist/{id}/update-status-with-docs
    [HttpPatch("{id}/update-status-with-docs")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> UpdateChecklistStatusWithDocs(Guid id, [FromBody] UpdateChecklistWithDocsRequest request)
    {
        try
        {
            var checklist = await _context.Checklists
                .Include(c => c.Documents).ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (checklist == null)
                return NotFound(new { message = "Checklist not found" });

            // Only allow update if status is CoCreatorReview
            if (checklist.Status != ChecklistStatus.CoCreatorReview)
                return StatusCode(403, new { message = "You can only update this checklist after RM sends it back for correction." });

            // Update documents
            if (request.Documents != null)
            {
                foreach (var updatedCat in request.Documents)
                {
                    var cat = checklist.Documents.FirstOrDefault(c => c.Category == updatedCat.Category);
                    if (cat == null) continue;
                    foreach (var docUpdate in updatedCat.DocList)
                    {
                        var doc = cat.DocList.FirstOrDefault(d => d.Id == docUpdate.Id);
                        if (doc == null) continue;
                        if (docUpdate.FileUrl != null) doc.FileUrl = docUpdate.FileUrl;
                        if (docUpdate.Comment != null) doc.Comment = docUpdate.Comment;
                        if (docUpdate.Status.HasValue) doc.Status = docUpdate.Status.Value;
                        if (docUpdate.DeferralReason != null) doc.DeferralReason = docUpdate.DeferralReason;
                        if (docUpdate.DeferralNumber != null) doc.DeferralNumber = docUpdate.DeferralNumber;
                    }
                }
            }
            if (request.Status.HasValue) checklist.Status = request.Status.Value;
            if (!string.IsNullOrEmpty(request.GeneralComment))
            {
                checklist.Logs.Add(new ChecklistLog
                {
                    Id = Guid.NewGuid(),
                    Message = $"Co-Creator comment: {request.GeneralComment}",
                    UserId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty),
                    Timestamp = DateTime.UtcNow
                });
            }
            checklist.Logs.Add(new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = "Checklist updated by Co-Creator",
                UserId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty),
                Timestamp = DateTime.UtcNow
            });
            checklist.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Checklist updated successfully", checklist });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating checklist");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CoCreatorController> _logger;
    private readonly IWebHostEnvironment _environment;

    public CoCreatorController(
        ApplicationDbContext context,
        ILogger<CoCreatorController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    // POST /api/cocreatorChecklist/{id}/revive
    [HttpPost("{id}/revive")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> ReviveChecklist(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var original = await _context.Checklists
                .Include(c => c.Documents).ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (original == null)
                return NotFound(new { message = "Checklist not found" });

            // Only allow revival of approved or completed checklists
            if (!(original.Status == ChecklistStatus.Approved || original.Status == ChecklistStatus.Completed))
                return BadRequest(new { message = "Only approved or completed checklists can be revived", currentStatus = original.Status.ToString() });

            // Generate new DCL number with copy suffix
            var baseDclNo = original.DclNo.Split(" copy ")[0];
            var existingCopies = await _context.Checklists.Where(c => c.DclNo.StartsWith(baseDclNo + " copy ")).OrderByDescending(c => c.CreatedAt).ToListAsync();
            int copyNumber = 1;
            if (existingCopies.Count > 0)
            {
                var last = existingCopies[0].DclNo;
                var match = System.Text.RegularExpressions.Regex.Match(last, @" copy (\d+)$");
                if (match.Success) copyNumber = int.Parse(match.Groups[1].Value) + 1;
            }
            var newDclNo = $"{baseDclNo} copy {copyNumber}";

            // Clone checklist and documents
            var revived = new Checklist
            {
                Id = Guid.NewGuid(),
                DclNo = newDclNo,
                CustomerId = original.CustomerId,
                CustomerNumber = original.CustomerNumber,
                CustomerName = original.CustomerName,
                LoanType = original.LoanType,
                AssignedToRMId = original.AssignedToRMId,
                CreatedById = userId,
                Status = ChecklistStatus.CoCreatorReview,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Documents = original.Documents.Select(cat => new DocumentCategory
                {
                    Id = Guid.NewGuid(),
                    Category = cat.Category,
                    ChecklistId = cat.ChecklistId,
                    DocList = cat.DocList.Select(doc => new Document
                    {
                        Id = Guid.NewGuid(),
                        Name = doc.Name,
                        Status = doc.Status,
                        FileUrl = doc.FileUrl,
                        ExpiryDate = doc.ExpiryDate,
                        Comment = doc.Comment,
                        DeferralReason = doc.DeferralReason,
                        DeferralNumber = doc.DeferralNumber,
                        CheckerStatus = doc.CheckerStatus,

                        RmStatus = doc.RmStatus,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList()
                }).ToList(),
                Logs = new List<ChecklistLog> {
                        new ChecklistLog {
                            Id = Guid.NewGuid(),
                            Message = $"Revived from {original.DclNo}",
                            UserId = userId,
                            Timestamp = DateTime.UtcNow
                        }
                    }
            };
            _context.Checklists.Add(revived);
            await _context.SaveChangesAsync();
            // TODO: Audit log (implement as needed)
            return StatusCode(201, new { message = $"Checklist revived as {newDclNo}", checklist = revived });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviving checklist");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST /api/cocreatorChecklist
    [HttpPost]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            // Generate DCL number
            var lastChecklist = await _context.Checklists
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            var dclNo = lastChecklist == null
                ? "DCL-000001"
                : $"DCL-{DateTime.Now.Ticks.ToString().Substring(DateTime.Now.Ticks.ToString().Length - 6)}";

            var checklist = new Checklist
            {
                Id = Guid.NewGuid(),
                DclNo = dclNo,
                CustomerNumber = request.CustomerNumber,
                CustomerName = request.CustomerName,
                LoanType = request.LoanType,
                CreatedById = userId,
                Status = ChecklistStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Checklists.Add(checklist);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Checklist created successfully",
                checklist = new
                {
                    id = checklist.Id,
                    dclNo = checklist.DclNo,
                    customerNumber = checklist.CustomerNumber,
                    customerName = checklist.CustomerName,
                    status = checklist.Status.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checklist");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/cocreatorChecklist
    [HttpGet]
    public async Task<IActionResult> GetAllChecklists()
    {
        try
        {
            var checklists = await _context.Checklists
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
                .OrderByDescending(c => c.CreatedAt)
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
                    assignedToCoChecker = c.AssignedToCoChecker != null ? new { id = c.AssignedToCoChecker.Id, name = c.AssignedToCoChecker.Name } : null,
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(checklists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching checklists");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/cocreatorChecklist/:id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetChecklistById(Guid id)
    {
        try
        {
            var checklist = await _context.Checklists
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
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

    // GET /api/cocreatorChecklist/dcl/:dclNo
    [HttpGet("dcl/{dclNo}")]
    public async Task<IActionResult> GetChecklistByDclNo(string dclNo)
    {
        try
        {
            var checklist = await _context.Checklists
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.DclNo == dclNo);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            return Ok(checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching checklist by DCL number");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/cocreatorChecklist/:id
    [HttpPut("{id}")]
    [RoleAuthorize(UserRole.CoCreator, UserRole.RM, UserRole.CoChecker)]
    public async Task<IActionResult> UpdateChecklist(Guid id, [FromBody] UpdateChecklistRequest request)
    {
        try
        {
            var checklist = await _context.Checklists.FindAsync(id);
            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            if (request.CustomerName != null)
                checklist.CustomerName = request.CustomerName;
            if (request.LoanType != null)
                checklist.LoanType = request.LoanType;
            if (request.AssignedToRMId.HasValue)
                checklist.AssignedToRMId = request.AssignedToRMId;
            if (request.AssignedToCoCheckerId.HasValue)
                checklist.AssignedToCoCheckerId = request.AssignedToCoCheckerId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Checklist updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating checklist");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/cocreatorChecklist/:checklistId/comments
    [HttpGet("{checklistId}/comments")]
    public async Task<IActionResult> GetChecklistComments(Guid checklistId)
    {
        try
        {
            var logs = await _context.ChecklistLogs
                .Where(l => l.ChecklistId == checklistId)
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new
                {
                    id = l.Id,
                    message = l.Message,
                    user = l.User != null ? new { id = l.User.Id, name = l.User.Name } : null,
                    timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/cocreatorChecklist/search/customer
    [HttpGet("search/customer")]
    public async Task<IActionResult> SearchCustomer([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Search query is required" });
            }

            var customers = await _context.Users
                .Where(u => u.Role == UserRole.Customer &&
                           (u.CustomerNumber!.Contains(q) ||
                            u.Name.Contains(q) ||
                            u.CustomerId!.Contains(q) ||
                            u.Email.Contains(q)))
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    customerNumber = u.CustomerNumber,
                    customerId = u.CustomerId,
                    email = u.Email
                })
                .Take(10)
                .ToListAsync();

            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/cocreatorChecklist/creator/:creatorId
    [HttpGet("creator/{creatorId}")]
    public async Task<IActionResult> GetChecklistsByCreator(Guid creatorId)
    {
        try
        {
            var checklists = await _context.Checklists
                .Where(c => c.CreatedById == creatorId)
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(checklists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching checklists by creator");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/cocreatorChecklist/:id/co-create
    [HttpPut("{id}/co-create")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> CoCreatorReview(Guid id, [FromBody] CoCreatorReviewRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var checklist = await _context.Checklists.FindAsync(id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            // Update status based on review
            checklist.Status = request.Approved ? ChecklistStatus.RMReview : ChecklistStatus.Pending;

            // Add log
            var log = new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = request.Approved ? "Approved by Co-Creator" : $"Returned by Co-Creator: {request.Comment}",
                UserId = userId,
                ChecklistId = id,
                Timestamp = DateTime.UtcNow
            };
            _context.ChecklistLogs.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = request.Approved ? "Checklist approved" : "Checklist returned for revision",
                status = checklist.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in co-creator review");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/cocreatorChecklist/:id/co-check
    [HttpPut("{id}/co-check")]
    [RoleAuthorize(UserRole.CoChecker)]
    public async Task<IActionResult> CoCheckerApproval(Guid id, [FromBody] CoCheckerApprovalRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var checklist = await _context.Checklists.FindAsync(id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            checklist.Status = request.Approved ? ChecklistStatus.Approved : ChecklistStatus.Rejected;

            var log = new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = request.Approved ? "Approved by Co-Checker" : $"Rejected by Co-Checker: {request.Comment}",
                UserId = userId,
                ChecklistId = id,
                Timestamp = DateTime.UtcNow
            };
            _context.ChecklistLogs.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = request.Approved ? "Checklist approved" : "Checklist rejected",
                status = checklist.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in co-checker approval");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/cocreatorChecklist/update-document
    [HttpPut("update-document")]
    [RoleAuthorize(UserRole.Admin)]
    public async Task<IActionResult> UpdateDocumentAdmin([FromBody] AdminUpdateDocumentRequest request)
    {
        try
        {
            var document = await _context.Documents.FindAsync(request.DocumentId);
            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            if (request.Status.HasValue)
                document.Status = request.Status.Value;
            if (request.Comment != null)
                document.Comment = request.Comment;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Document updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PATCH /api/cocreatorChecklist/update-status
    [HttpPatch("update-status")]
    public async Task<IActionResult> UpdateChecklistStatus([FromBody] UpdateChecklistStatusRequest request)
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

    // POST /api/cocreatorChecklist/:id/submit-to-rm
    [HttpPost("{id}/submit-to-rm")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> SubmitToRM(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var checklist = await _context.Checklists
                .Include(c => c.AssignedToRM)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            checklist.Status = ChecklistStatus.RMReview;

            var log = new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = "Submitted to RM for review",
                UserId = userId,
                ChecklistId = id,
                Timestamp = DateTime.UtcNow
            };
            _context.ChecklistLogs.Add(log);

            // Create notification for RM
            if (checklist.AssignedToRMId.HasValue)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = checklist.AssignedToRMId.Value,
                    Message = $"DCL {checklist.DclNo} has been submitted for your review",
                    Read = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Checklist submitted to RM successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting to RM");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST /api/cocreatorChecklist/:id/submit-to-cochecker
    [HttpPost("{id}/submit-to-cochecker")]
    [RoleAuthorize(UserRole.CoCreator, UserRole.RM)]
    public async Task<IActionResult> SubmitToCoChecker(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var checklist = await _context.Checklists
                .Include(c => c.AssignedToCoChecker)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            checklist.Status = ChecklistStatus.CoCheckerReview;

            var log = new ChecklistLog
            {
                Id = Guid.NewGuid(),
                Message = "Submitted to Co-Checker for final approval",
                UserId = userId,
                ChecklistId = id,
                Timestamp = DateTime.UtcNow
            };
            _context.ChecklistLogs.Add(log);

            // Create notification for CoChecker
            if (checklist.AssignedToCoCheckerId.HasValue)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = checklist.AssignedToCoCheckerId.Value,
                    Message = $"DCL {checklist.DclNo} has been submitted for your approval",
                    Read = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Checklist submitted to Co-Checker successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting to co-checker");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PATCH /api/cocreatorChecklist/:checklistId/checklist-status
    [HttpPatch("{checklistId}/checklist-status")]
    public async Task<IActionResult> UpdateStatus(Guid checklistId, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var checklist = await _context.Checklists.FindAsync(checklistId);
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

    // POST /api/cocreatorChecklist/:id/documents
    [HttpPost("{id}/documents")]
    [RoleAuthorize(UserRole.CoCreator, UserRole.RM)]
    public async Task<IActionResult> AddDocument(Guid id, [FromBody] AddDocumentRequest request)
    {
        try
        {
            var checklist = await _context.Checklists
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            var category = checklist.Documents.FirstOrDefault(dc => dc.Category == request.Category);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Category = request.Category,
                Status = DocumentStatus.PendingRM,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (category == null)
            {
                category = new DocumentCategory
                {
                    Id = Guid.NewGuid(),
                    Category = request.Category,
                    ChecklistId = id
                };
                _context.DocumentCategories.Add(category);
                await _context.SaveChangesAsync();
            }

            document.CategoryId = category.Id;
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Document added successfully",
                document = new
                {
                    id = document.Id,
                    name = document.Name,
                    category = document.Category,
                    status = document.Status.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PATCH /api/cocreatorChecklist/:id/documents/:docId
    [HttpPatch("{id}/documents/{docId}")]
    [RoleAuthorize(UserRole.CoCreator, UserRole.RM)]
    public async Task<IActionResult> UpdateDocument(Guid id, Guid docId, [FromBody] UpdateDocumentRequest request)
    {
        try
        {
            var checklist = await _context.Checklists
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            var document = checklist.Documents
                .SelectMany(dc => dc.DocList)
                .FirstOrDefault(d => d.Id == docId);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            if (request.Status.HasValue)
                document.Status = request.Status.Value;
            if (request.CheckerComment != null)
                document.CheckerComment = request.CheckerComment;
            if (request.CreatorComment != null)
                document.CreatorComment = request.CreatorComment;
            if (request.RmComment != null)
                document.RmComment = request.RmComment;
            if (request.FileUrl != null)
                document.FileUrl = request.FileUrl;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Document updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // DELETE /api/cocreatorChecklist/:id/documents/:docId
    [HttpDelete("{id}/documents/{docId}")]
    [RoleAuthorize(UserRole.RM, UserRole.CoCreator)]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid docId)
    {
        try
        {
            var checklist = await _context.Checklists
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            var document = checklist.Documents
                .SelectMany(dc => dc.DocList)
                .FirstOrDefault(d => d.Id == docId);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Document deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST /api/cocreatorChecklist/:id/documents/:docId/upload
    [HttpPost("{id}/documents/{docId}/upload")]
    [RoleAuthorize(UserRole.CoCreator, UserRole.RM)]
    public async Task<IActionResult> UploadDocumentFile(Guid id, Guid docId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var checklist = await _context.Checklists
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            var document = checklist.Documents
                .SelectMany(dc => dc.DocList)
                .FirstOrDefault(d => d.Id == docId);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            // Save file
            var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", id.ToString());
            var fileName = await FileUploadHelper.SaveFileAsync(file, uploadsPath);

            // Update document with file URL
            document.FileUrl = $"/uploads/{id}/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "File uploaded successfully",
                fileUrl = document.FileUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // POST /api/cocreatorChecklist/:id/upload
    [HttpPost("{id}/upload")]
    [RoleAuthorize(UserRole.CoCreator, UserRole.RM)]
    public async Task<IActionResult> UploadSupportingDocs(Guid id, List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files provided" });
            }

            var checklist = await _context.Checklists.FindAsync(id);
            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", id.ToString());
            var uploadedFiles = new List<object>();

            foreach (var file in files)
            {
                var fileName = await FileUploadHelper.SaveFileAsync(file, uploadsPath);
                uploadedFiles.Add(new
                {
                    name = file.FileName,
                    url = $"/uploads/{id}/{fileName}"
                });
            }

            return Ok(new
            {
                message = "Files uploaded successfully",
                files = uploadedFiles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading files");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/cocreatorChecklist/:id/download
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadChecklist(Guid id)
    {
        try
        {
            var checklist = await _context.Checklists
                .Include(c => c.Documents)
                    .ThenInclude(dc => dc.DocList)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
            {
                return NotFound(new { message = "Checklist not found" });
            }

            // TODO: Implement archive/zip creation of all checklist documents
            return Ok(new
            {
                message = "Download functionality not yet implemented",
                checklistId = id,
                dclNo = checklist.DclNo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading checklist");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/cocreatorChecklist/cocreator/active
    [HttpGet("cocreator/active")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> GetActiveChecklists()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var activeChecklists = await _context.Checklists
                .Where(c => c.CreatedById == userId &&
                           (c.Status == ChecklistStatus.Pending ||
                            c.Status == ChecklistStatus.CoCreatorReview ||
                            c.Status == ChecklistStatus.RMReview))
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(activeChecklists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active checklists");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


}

