// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using NCBA.DCL.Data;
// using NCBA.DCL.DTOs;
// using NCBA.DCL.Middleware;
// using NCBA.DCL.Models;

// namespace NCBA.DCL.Controllers;

// [ApiController]
// [Route("api/checklist")]
// [Authorize]
// public class ChecklistController : ControllerBase
// {
//     private readonly ApplicationDbContext _context;
//     private readonly ILogger<ChecklistController> _logger;

//     public ChecklistController(ApplicationDbContext context, ILogger<ChecklistController> logger)
//     {
//         _context = context;
//         _logger = logger;
//     }

//     [HttpPost]
//     [RoleAuthorize(UserRole.CoCreator)]
//     public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistRequest request)
//     {
//         try
//         {
//             var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

//             // Generate DCL number
//             var lastChecklist = await _context.Checklists
//                 .OrderByDescending(c => c.CreatedAt)
//                 .FirstOrDefaultAsync();

//             var dclNo = lastChecklist == null
//                 ? "DCL-000001"
//                 : $"DCL-{DateTime.Now.Ticks.ToString().Substring(DateTime.Now.Ticks.ToString().Length - 6)}";

//             var checklist = new Checklist
//             {
//                 Id = Guid.NewGuid(),
//                 DclNo = dclNo,
//                 CustomerNumber = request.CustomerNumber,
//                 CustomerName = request.CustomerName,
//                 LoanType = request.LoanType,
//                 CreatedById = userId,
//                 Status = ChecklistStatus.Pending,
//                 CreatedAt = DateTime.UtcNow,
//                 UpdatedAt = DateTime.UtcNow
//             };

//             _context.Checklists.Add(checklist);
//             await _context.SaveChangesAsync();

//             return StatusCode(201, new
//             {
//                 message = "Checklist created successfully",
//                 checklist = new
//                 {
//                     id = checklist.Id,
//                     dclNo = checklist.DclNo,
//                     customerNumber = checklist.CustomerNumber,
//                     customerName = checklist.CustomerName,
//                     status = checklist.Status.ToString()
//                 }
//             });
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error creating checklist");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpGet]
//     public async Task<IActionResult> GetAllChecklists()
//     {
//         try
//         {
//             var checklists = await _context.Checklists
//                 .Include(c => c.CreatedBy)
//                 .Include(c => c.AssignedToRM)
//                 .Include(c => c.AssignedToCoChecker)
//                 .Select(c => new
//                 {
//                     id = c.Id,
//                     dclNo = c.DclNo,
//                     customerNumber = c.CustomerNumber,
//                     customerName = c.CustomerName,
//                     loanType = c.LoanType,
//                     status = c.Status.ToString(),
//                     createdBy = c.CreatedBy != null ? new { id = c.CreatedBy.Id, name = c.CreatedBy.Name } : null,
//                     assignedToRM = c.AssignedToRM != null ? new { id = c.AssignedToRM.Id, name = c.AssignedToRM.Name } : null,
//                     assignedToCoChecker = c.AssignedToCoChecker != null ? new { id = c.AssignedToCoChecker.Id, name = c.AssignedToCoChecker.Name } : null,
//                     createdAt = c.CreatedAt,
//                     updatedAt = c.UpdatedAt
//                 })
//                 .ToListAsync();

//             return Ok(checklists);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error fetching checklists");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpGet("{id}")]
//     public async Task<IActionResult> GetChecklistById(Guid id)
//     {
//         try
//         {
//             var checklist = await _context.Checklists
//                 .Include(c => c.CreatedBy)
//                 .Include(c => c.AssignedToRM)
//                 .Include(c => c.AssignedToCoChecker)
//                 .Include(c => c.Documents)
//                     .ThenInclude(dc => dc.DocList)
//                         .ThenInclude(d => d.CoCreatorFiles)
//                 .Include(c => c.Logs)
//                     .ThenInclude(l => l.User)
//                 .FirstOrDefaultAsync(c => c.Id == id);

//             if (checklist == null)
//             {
//                 return NotFound(new { message = "Checklist not found" });
//             }

//             return Ok(checklist);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error fetching checklist");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpGet("dcl/{dclNo}")]
//     public async Task<IActionResult> GetChecklistByDclNo(string dclNo)
//     {
//         try
//         {
//             var checklist = await _context.Checklists
//                 .Include(c => c.CreatedBy)
//                 .Include(c => c.AssignedToRM)
//                 .Include(c => c.AssignedToCoChecker)
//                 .Include(c => c.Documents)
//                     .ThenInclude(dc => dc.DocList)
//                 .FirstOrDefaultAsync(c => c.DclNo == dclNo);

//             if (checklist == null)
//             {
//                 return NotFound(new { message = "Checklist not found" });
//             }

//             return Ok(checklist);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error fetching checklist by DCL number");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpPut("{id}")]
//     [RoleAuthorize(UserRole.CoCreator, UserRole.RM, UserRole.CoChecker)]
//     public async Task<IActionResult> UpdateChecklist(Guid id, [FromBody] UpdateChecklistRequest request)
//     {
//         try
//         {
//             var checklist = await _context.Checklists.FindAsync(id);
//             if (checklist == null)
//             {
//                 return NotFound(new { message = "Checklist not found" });
//             }

//             if (request.CustomerName != null)
//                 checklist.CustomerName = request.CustomerName;
//             if (request.LoanType != null)
//                 checklist.LoanType = request.LoanType;
//             if (request.AssignedToRMId.HasValue)
//                 checklist.AssignedToRMId = request.AssignedToRMId;
//             if (request.AssignedToCoCheckerId.HasValue)
//                 checklist.AssignedToCoCheckerId = request.AssignedToCoCheckerId;

//             await _context.SaveChangesAsync();

//             return Ok(new { message = "Checklist updated successfully" });
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error updating checklist");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpPost("{id}/documents")]
//     [RoleAuthorize(UserRole.CoCreator, UserRole.RM)]
//     public async Task<IActionResult> AddDocument(Guid id, [FromBody] AddDocumentRequest request)
//     {
//         try
//         {
//             var checklist = await _context.Checklists
//                 .Include(c => c.Documents)
//                     .ThenInclude(dc => dc.DocList)
//                 .FirstOrDefaultAsync(c => c.Id == id);

//             if (checklist == null)
//             {
//                 return NotFound(new { message = "Checklist not found" });
//             }

//             var category = checklist.Documents.FirstOrDefault(dc => dc.Category == request.Category);

//             var document = new Document
//             {
//                 Id = Guid.NewGuid(),
//                 Name = request.Name,
//                 Category = request.Category,
//                 Status = DocumentStatus.PendingRM,
//                 CreatedAt = DateTime.UtcNow,
//                 UpdatedAt = DateTime.UtcNow
//             };

//             if (category == null)
//             {
//                 category = new DocumentCategory
//                 {
//                     Id = Guid.NewGuid(),
//                     Category = request.Category,
//                     ChecklistId = id
//                 };
//                 _context.DocumentCategories.Add(category);
//                 await _context.SaveChangesAsync();
//             }

//             document.CategoryId = category.Id;
//             _context.Documents.Add(document);
//             await _context.SaveChangesAsync();

//             return StatusCode(201, new
//             {
//                 message = "Document added successfully",
//                 document = new
//                 {
//                     id = document.Id,
//                     name = document.Name,
//                     category = document.Category,
//                     status = document.Status.ToString()
//                 }
//             });
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error adding document");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpPatch("{id}/documents/{docId}")]
//     [RoleAuthorize(UserRole.CoCreator, UserRole.RM)]
//     public async Task<IActionResult> UpdateDocument(Guid id, Guid docId, [FromBody] UpdateDocumentRequest request)
//     {
//         try
//         {
//             var checklist = await _context.Checklists
//                 .Include(c => c.Documents)
//                     .ThenInclude(dc => dc.DocList)
//                 .FirstOrDefaultAsync(c => c.Id == id);

//             if (checklist == null)
//             {
//                 return NotFound(new { message = "Checklist not found" });
//             }

//             var document = checklist.Documents
//                 .SelectMany(dc => dc.DocList)
//                 .FirstOrDefault(d => d.Id == docId);

//             if (document == null)
//             {
//                 return NotFound(new { message = "Document not found" });
//             }

//             if (request.Status.HasValue)
//                 document.Status = request.Status.Value;
//             if (request.CheckerComment != null)
//                 document.CheckerComment = request.CheckerComment;
//             if (request.CreatorComment != null)
//                 document.CreatorComment = request.CreatorComment;
//             if (request.RmComment != null)
//                 document.RmComment = request.RmComment;
//             if (request.FileUrl != null)
//                 document.FileUrl = request.FileUrl;

//             await _context.SaveChangesAsync();

//             return Ok(new { message = "Document updated successfully" });
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error updating document");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpDelete("{id}/documents/{docId}")]
//     [RoleAuthorize(UserRole.RM, UserRole.CoCreator)]
//     public async Task<IActionResult> DeleteDocument(Guid id, Guid docId)
//     {
//         try
//         {
//             var checklist = await _context.Checklists
//                 .Include(c => c.Documents)
//                     .ThenInclude(dc => dc.DocList)
//                 .FirstOrDefaultAsync(c => c.Id == id);

//             if (checklist == null)
//             {
//                 return NotFound(new { message = "Checklist not found" });
//             }

//             var document = checklist.Documents
//                 .SelectMany(dc => dc.DocList)
//                 .FirstOrDefault(d => d.Id == docId);

//             if (document == null)
//             {
//                 return NotFound(new { message = "Document not found" });
//             }

//             _context.Documents.Remove(document);
//             await _context.SaveChangesAsync();

//             return Ok(new { message = "Document deleted successfully" });
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error deleting document");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpPatch("{id}/checklist-status")]
//     public async Task<IActionResult> UpdateChecklistStatus(Guid id, [FromBody] UpdateStatusRequest request)
//     {
//         try
//         {
//             var checklist = await _context.Checklists.FindAsync(id);
//             if (checklist == null)
//             {
//                 return NotFound(new { message = "Checklist not found" });
//             }

//             checklist.Status = request.Status;
//             await _context.SaveChangesAsync();

//             return Ok(new
//             {
//                 message = "Status updated successfully",
//                 status = checklist.Status.ToString()
//             });
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error updating checklist status");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }

//     [HttpGet("{checklistId}/comments")]
//     public async Task<IActionResult> GetChecklistComments(Guid checklistId)
//     {
//         try
//         {
//             var logs = await _context.ChecklistLogs
//                 .Where(l => l.ChecklistId == checklistId)
//                 .Include(l => l.User)
//                 .OrderByDescending(l => l.Timestamp)
//                 .Select(l => new
//                 {
//                     id = l.Id,
//                     message = l.Message,
//                     user = l.User != null ? new { id = l.User.Id, name = l.User.Name } : null,
//                     timestamp = l.Timestamp
//                 })
//                 .ToListAsync();

//             return Ok(logs);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error fetching checklist comments");
//             return StatusCode(500, new { message = "Internal server error" });
//         }
//     }
// }
