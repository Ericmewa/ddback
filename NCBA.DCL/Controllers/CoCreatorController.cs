using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Helpers;
using NCBA.DCL.Middleware;
using NCBA.DCL.Models;
using NCBA.DCL.Constants;
using System.Text.Json;
using NCBA.DCL.Migrations;




namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/cocreatorChecklist")]
[Authorize]
public class CoCreatorController : ControllerBase
{
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



    [HttpPost]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var userIdClaim = User.FindFirst("id")?.Value;
            _logger.LogInformation("User ID claim: {userIdClaim}", userIdClaim);

            // 🔥 VALIDATE DOCUMENT STATUSES (ADD THIS BLOCK)


            foreach (var category in request.Documents) // list of ChecklistCategoryDto
            {
                foreach (var doc in category.Documents) // list of ChecklistDocumentDto
                {
                    if (string.IsNullOrWhiteSpace(doc.Status))
                        return BadRequest(new { message = "Document status is required" });

                    doc.Status = doc.Status.Trim().ToLowerInvariant();

                    if (!DocumentStatusStrings.Allowed.Contains(doc.Status))
                        return BadRequest(new { message = $"Invalid document status: {doc.Status}" });

                    // ✅ Normalize comments if null
                    doc.CreatorComment ??= string.Empty;
                    doc.Comment ??= string.Empty;
                    doc.RmComment ??= string.Empty;
                }


            }


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
                AssignedToRMId = request.AssignedToRMId,
                Status = ChecklistStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,



                // ✅ JSON now safely contains STRING statuses
                DocumentsJson = JsonSerializer.Serialize(
                    request.Documents,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }
                )
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
            return StatusCode(500, new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }


    [HttpGet]
    public async Task<IActionResult> GetAllChecklists()
    {
        try
        {
            var checklistsFromDb = await _context.Checklists
                .Include(c => c.CreatedBy)
                .Include(c => c.AssignedToRM)
                .Include(c => c.AssignedToCoChecker)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var result = checklistsFromDb.Select(c =>
            {
                List<ChecklistCategoryDto> docs = new();

                if (!string.IsNullOrEmpty(c.DocumentsJson))
                {
                    try
                    {
                        docs = JsonSerializer.Deserialize<List<ChecklistCategoryDto>>(
                            c.DocumentsJson,
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                AllowTrailingCommas = true
                            }
                        ) ?? new List<ChecklistCategoryDto>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize documents for checklist {ChecklistId}", c.Id);
                        docs = new List<ChecklistCategoryDto>();
                    }
                }

                return new
                {
                    id = c.Id,
                    dclNo = c.DclNo,
                    customerNumber = c.CustomerNumber,
                    customerName = c.CustomerName,
                    loanType = c.LoanType,
                    comment = c.Comment,
                    status = c.Status.ToString(),
                    createdBy = c.CreatedBy != null ? new { id = c.CreatedBy.Id, name = c.CreatedBy.Name } : null,
                    assignedToRM = c.AssignedToRM != null ? new { id = c.AssignedToRM.Id, name = c.AssignedToRM.Name } : null,
                    assignedToCoChecker = c.AssignedToCoChecker != null ? new { id = c.AssignedToCoChecker.Id, name = c.AssignedToCoChecker.Name } : null,

                    // ✅ Make sure to return the full documents array
                    documents = docs.Select(cat => new
                    {
                        category = cat.Category,
                        documents = cat.Documents.Select(d => new
                        {
                            name = d.Name,
                            status = d.Status,
                            fileUrl = d.FileUrl
                        }).ToList()
                    }).ToList(),

                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt
                };
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching checklists");
            return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
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
    public async Task<IActionResult> SubmitToRM(
    Guid id,
    [FromBody] SubmitToRMRequest request
)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var checklist = await _context.Checklists
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
                return NotFound(new { message = "Checklist not found" });

            // ✅ Validate documents
            foreach (var category in request.Documents)
            {
                foreach (var doc in category.Documents)
                {
                    if (string.IsNullOrWhiteSpace(doc.Status))
                        return BadRequest(new { message = "Document status is required" });

                    doc.Status = doc.Status.Trim().ToLowerInvariant();

                    if (!DocumentStatusStrings.Allowed.Contains(doc.Status))
                        return BadRequest(new { message = $"Invalid status: {doc.Status}" });

                    doc.CreatorComment ??= string.Empty;
                    doc.RmComment ??= string.Empty;
                }
            }

            // ✅ Save updated documents
            checklist.DocumentsJson = JsonSerializer.Serialize(
                request.Documents,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );

            checklist.Status = ChecklistStatus.RMReview;
            checklist.UpdatedAt = DateTime.UtcNow;

            // ✅ Log
            _context.ChecklistLogs.Add(new ChecklistLog
            {
                Id = Guid.NewGuid(),
                ChecklistId = id,
                UserId = userId,
                Message = "Submitted to RM for review",
                Timestamp = DateTime.UtcNow
            });

            // ✅ Notify RM
            if (checklist.AssignedToRMId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = checklist.AssignedToRMId.Value,
                    Message = $"DCL {checklist.DclNo} submitted for review",
                    Read = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Checklist submitted to RM successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitToRM failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }


    // POST /api/cocreatorChecklist/:id/submit-to-cochecker
    [HttpPost("{id}/submit-to-cochecker")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> SubmitToCoChecker(
        Guid id,
        [FromBody] SubmitToCoCheckerRequest request
    )
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var checklist = await _context.Checklists
                .FirstOrDefaultAsync(c => c.Id == id);

            if (checklist == null)
                return NotFound(new { message = "Checklist not found" });

            // ✅ Validate documents
            if (request.Documents != null)
            {
                foreach (var category in request.Documents)
                {
                    if (category.Documents == null) continue;

                    foreach (var doc in category.Documents)
                    {
                        if (string.IsNullOrWhiteSpace(doc.Status))
                            return BadRequest(new { message = "Document status is required" });

                        doc.Status = doc.Status.Trim().ToLowerInvariant();

                        if (!DocumentStatusStrings.Allowed.Contains(doc.Status))
                            return BadRequest(new { message = $"Invalid status: {doc.Status}" });

                        doc.CreatorComment ??= string.Empty;
                        doc.RmComment ??= string.Empty;
                    }
                }

                // ✅ Save updated documents (store as JSON)
                checklist.DocumentsJson = JsonSerializer.Serialize(
                    request.Documents,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }
                );
            }

            checklist.Status = ChecklistStatus.CoCheckerReview;
            checklist.UpdatedAt = DateTime.UtcNow;

            // ✅ Log
            _context.ChecklistLogs.Add(new ChecklistLog
            {
                Id = Guid.NewGuid(),
                ChecklistId = id,
                UserId = userId,
                Message = "Submitted to Co-Checker for review",
                Timestamp = DateTime.UtcNow
            });

            // ✅ Notify Co-Checker
            if (checklist.AssignedToCoCheckerId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = checklist.AssignedToCoCheckerId.Value,
                    Message = $"DCL {checklist.DclNo} submitted for Co-Checker review",
                    Read = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Checklist submitted to Co-Checker successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitToCoChecker failed");
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
