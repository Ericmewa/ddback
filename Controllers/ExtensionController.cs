using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Middleware;
using NCBA.DCL.Models;
using NCBA.DCL.Services;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/extensions")]
[Authorize]
public class ExtensionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExtensionController> _logger;
    private readonly IEmailService _emailService;

    public ExtensionController(
        ApplicationDbContext context,
        ILogger<ExtensionController> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    // ================================
    // RM ROUTES
    // ================================

    // POST /api/extensions
    [HttpPost]
    [RoleAuthorize(UserRole.RM)]
    public async Task<IActionResult> CreateExtension([FromBody] CreateExtensionRequest request)
    {
        try
        {
            // Log incoming request for debugging
            try
            {
                var reqJson = JsonSerializer.Serialize(new { request.DeferralId, request.RequestedDaysSought, request.ExtensionReason, additionalFiles = request.AdditionalFiles?.Count ?? 0 });
                _logger.LogInformation("Incoming CreateExtension request: {req}", reqJson);
            }
            catch { }
            var idClaim = User.FindFirst("id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return StatusCode(401, new { message = "Invalid or missing user id claim" });
            var userName = User.FindFirst("name")?.Value ?? "User";

            var deferral = await _context.Deferrals.FindAsync(request.DeferralId);
            if (deferral == null)
                return NotFound(new { message = "Deferral not found" });

            if (deferral.Status != DeferralStatus.Approved)
                return BadRequest(new { message = "Can only apply for extension on approved deferrals" });

            if (deferral.DaysSought < 1)
                return BadRequest(new { message = $"Deferral must have valid daysSought field (current value: {deferral.DaysSought})" });

            if (request.RequestedDaysSought <= deferral.DaysSought)
                return BadRequest(new { message = $"Requested days ({request.RequestedDaysSought}) must be greater than current days ({deferral.DaysSought})" });

            // Create extension
            var extension = new Extension
            {
                DeferralId = request.DeferralId,
                DeferralNumber = deferral.DeferralNumber,
                CustomerName = deferral.CustomerName,
                CustomerNumber = deferral.CustomerNumber,
                DclNumber = deferral.DclNumber,
                NextDueDate = deferral.NextDueDate,
                NextDocumentDueDate = deferral.NextDocumentDueDate,
                SlaExpiry = deferral.SlaExpiry,
                CurrentDaysSought = deferral.DaysSought,
                RequestedDaysSought = request.RequestedDaysSought,
                ExtensionReason = request.ExtensionReason,
                RequestedById = userId,
                RequestedByName = userName,
                Status = ExtensionStatus.PendingApproval,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Handle Additional Files
            if (request.AdditionalFiles != null && request.AdditionalFiles.Any())
            {
                foreach (var fileDto in request.AdditionalFiles)
                {
                    extension.AdditionalFiles.Add(new ExtensionFile
                    {
                        Name = fileDto.Name,
                        Url = fileDto.Url,
                        Size = fileDto.Size,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }

            // Copy approvers from deferral
            var deferralApprovers = await _context.Approvers
                .Where(a => a.DeferralId == deferral.Id)
                .Include(a => a.User)
                .ToListAsync();

            if (!deferralApprovers.Any())
                return BadRequest(new { message = "Deferral must have approvers to create extension" });

            // Copy approvers from deferral preserving order and assign explicit sequence
            int seq = 0;
            foreach (var approver in deferralApprovers)
            {
                extension.Approvers.Add(new ExtensionApprover
                {
                    UserId = approver.UserId,
                    // Do not assign the tracked User entity here; only set UserId to avoid EF tracking/insert conflicts
                    Role = approver.Role,
                    ApprovalStatus = ApproverApprovalStatus.Pending,
                    Sequence = seq,
                    IsCurrent = seq == 0
                });

                seq++;
            }

            extension.History.Add(new ExtensionHistory
            {
                Action = "extension_requested",
                UserId = userId,
                UserName = userName,
                UserRole = User.FindFirst(ClaimTypes.Role)?.Value,
                Notes = $"Extension requested: {deferral.DaysSought} days -> {request.RequestedDaysSought} days"
            });

            _context.Extensions.Add(extension);
            await _context.SaveChangesAsync();

            // Send email notification to first approver
            var currentApprover = extension.Approvers.FirstOrDefault(a => a.IsCurrent);
            if (currentApprover != null && currentApprover.UserId.HasValue)
            {
                var approverUser = await _context.Users.FindAsync(currentApprover.UserId);
                if (approverUser != null && !string.IsNullOrEmpty(approverUser.Email))
                {
                    try
                    {
                        await _emailService.SendExtensionApprovalRequestAsync(
                            approverUser.Email,
                            approverUser.Name,
                            extension.DeferralNumber ?? "Unknown",
                            userName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send extension notification email");
                    }
                }
            }

            // Notify RM (requester) that extension application was created
            try
            {
                var rmUser = deferral != null ? await _context.Users.FindAsync(deferral.CreatedById) : null;
                // Fallback: the requester is the current authenticated RM
                if ((rmUser == null || string.IsNullOrWhiteSpace(rmUser.Email)) && !string.IsNullOrWhiteSpace(userName))
                {
                    // nothing to do if no RM email
                }
                else if (rmUser != null && !string.IsNullOrWhiteSpace(rmUser.Email))
                {
                    try
                    {
                        await _emailService.SendExtensionApprovalRequestAsync(
                            rmUser.Email,
                            rmUser.Name,
                            extension.DeferralNumber ?? "Unknown",
                            userName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to notify RM about extension application {ExtensionId}", extension.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to perform RM notification after extension creation");
            }

            return StatusCode(201, extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating extension");
            var inner = ex.InnerException?.Message;
            return StatusCode(500, new { message = "Internal server error", error = ex.Message, detail = inner });
        }
    }

    // GET /api/extensions/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyExtensions()
    {
        try
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return StatusCode(401, new { message = "Invalid or missing user id claim" });

            var extensions = await _context.Extensions
                .Include(e => e.Deferral)
                .Include(e => e.Approvers).ThenInclude(a => a.User)
                .Include(e => e.RequestedBy)
                .Include(e => e.History).ThenInclude(h => h.User)
                .Include(e => e.AdditionalFiles)
                .Where(e => e.RequestedById == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            try
            {
                // Project to a lightweight shape to avoid serialization cycles and reduce payload
                var result = extensions.Select(e => new
                {
                id = e.Id,
                deferral = e.Deferral == null ? null : new
                {
                    _id = e.Deferral.Id,
                    deferralNumber = e.Deferral.DeferralNumber,
                    dclNumber = e.Deferral.DclNumber,
                    customerName = e.Deferral.CustomerName,
                    customerNumber = e.Deferral.CustomerNumber
                },
                deferralId = e.DeferralId,
                deferralNumber = e.DeferralNumber,
                customerName = e.CustomerName,
                customerNumber = e.CustomerNumber,
                requestedDaysSought = e.RequestedDaysSought,
                currentDaysSought = e.CurrentDaysSought,
                extensionReason = e.ExtensionReason,
                status = e.Status.ToString(),
                requestedById = e.RequestedById,
                requestedByName = e.RequestedByName,
                createdAt = e.CreatedAt,
                updatedAt = e.UpdatedAt,
                approvers = e.Approvers.Select(a => new
                {
                    id = a.Id,
                    userId = a.UserId,
                    role = a.Role,
                    approvalStatus = a.ApprovalStatus.ToString(),
                    isCurrent = a.IsCurrent,
                    user = a.User == null ? null : new { _id = a.User.Id, name = a.User.Name, email = a.User.Email }
                }).ToList(),
                history = e.History.Select(h => new
                {
                    id = h.Id,
                    action = h.Action,
                    userId = h.UserId,
                    userName = h.UserName,
                    userRole = h.UserRole,
                    date = h.Date,
                    notes = h.Notes,
                    comment = h.Comment
                }).ToList(),
                additionalFiles = e.AdditionalFiles.Select(f => new { id = f.Id, name = f.Name, url = f.Url, size = f.Size }).ToList(),
                extensionStatus = e.Status.ToString(),
                creatorApprovalStatus = e.CreatorApprovalStatus.ToString(),
                checkerApprovalStatus = e.CheckerApprovalStatus.ToString(),
                allApproversApproved = e.AllApproversApproved
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log and return an empty list as a safe fallback to avoid breaking the UI
                _logger.LogError(ex, "Error projecting extensions for response, returning empty list as fallback");
                return Ok(new object[0]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my extensions");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    // ================================
    // APPROVER ROUTES
    // ================================

    // GET /api/extensions/approver/queue
    [HttpGet("approver/queue")]
    public async Task<IActionResult> GetApproverQueue()
    {
        try
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return StatusCode(401, new { message = "Invalid or missing user id claim" });

            var extensions = await _context.Extensions
                .Include(e => e.Deferral)
                .Include(e => e.Approvers).ThenInclude(a => a.User)
                .Include(e => e.RequestedBy)
                .Where(e => e.Approvers.Any(a =>
                    a.UserId == userId &&
                    a.IsCurrent == true &&
                    a.ApprovalStatus == ApproverApprovalStatus.Pending))
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Ok(extensions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approver queue");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET /api/extensions/approver/actioned
    [HttpGet("approver/actioned")]
    public async Task<IActionResult> GetApproverActioned()
    {
        try
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return StatusCode(401, new { message = "Invalid or missing user id claim" });

            var extensions = await _context.Extensions
                .Include(e => e.Deferral)
                .Include(e => e.Approvers).ThenInclude(a => a.User)
                .Include(e => e.RequestedBy)
                .Where(e => e.Approvers.Any(a =>
                    a.UserId == userId &&
                    a.ApprovalStatus != ApproverApprovalStatus.Pending))
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Ok(extensions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting actioned extensions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/extensions/{id}/approve
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveExtension(Guid id, [FromBody] ApproveExtensionRequest request)
    {
        try
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return StatusCode(401, new { message = "Invalid or missing user id claim" });

            var extension = await _context.Extensions
                .Include(e => e.Approvers)
                .Include(e => e.History)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (extension == null)
                return NotFound(new { message = "Extension not found" });

            var currentApprover = extension.Approvers.FirstOrDefault(a => a.IsCurrent);
            if (currentApprover == null || currentApprover.UserId != userId)
                return StatusCode(403, new { message = "Only current approver can approve" });

            // Mark as approved
            currentApprover.ApprovalStatus = ApproverApprovalStatus.Approved;
            currentApprover.ApprovalDate = DateTime.UtcNow;
            currentApprover.ApprovalComment = request.Comment;
            currentApprover.IsCurrent = false;

            // Add to history
            extension.History.Add(new ExtensionHistory
            {
                Action = "approved_by_approver",
                UserId = userId,
                UserName = User.FindFirst("name")?.Value,
                UserRole = User.FindFirst(ClaimTypes.Role)?.Value,
                Date = DateTime.UtcNow,
                Comment = request.Comment
            });

            // Move to next approver based on explicit Sequence
            var nextApprover = extension.Approvers
                .OrderBy(a => a.Sequence)
                .FirstOrDefault(a => a.Sequence > currentApprover.Sequence && a.ApprovalStatus == ApproverApprovalStatus.Pending);

            if (nextApprover != null)
            {
                nextApprover.IsCurrent = true;
                extension.Status = ExtensionStatus.InReview;
                // Notify next approver by email if available
                if (nextApprover.UserId.HasValue)
                {
                    try
                    {
                        var approverUser = await _context.Users.FindAsync(nextApprover.UserId.Value);
                        if (approverUser != null && !string.IsNullOrEmpty(approverUser.Email))
                        {
                            await _emailService.SendExtensionApprovalRequestAsync(
                                approverUser.Email,
                                approverUser.Name,
                                extension.DeferralNumber ?? "Unknown",
                                User.FindFirst("name")?.Value ?? "Requester");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send extension notification to next approver");
                    }
                }
            }
            else
            {
                extension.AllApproversApproved = true;
                // Move to creator (CoCreator) for finalization rather than marking fully approved
                extension.Status = ExtensionStatus.InReview;

                // Add history entry indicating all approvers have approved
                extension.History.Add(new ExtensionHistory
                {
                    Action = "all_approvers_approved",
                    UserId = userId,
                    UserName = User.FindFirst("name")?.Value,
                    UserRole = User.FindFirst(ClaimTypes.Role)?.Value,
                    Date = DateTime.UtcNow,
                    Notes = "All approvers have approved the extension; awaiting CoCreator approval"
                });

                // Notify CoCreators (all users with role CoCreator)
                // Notify RM that an approver approved and the extension moved to next approver
                try
                {
                    var rm = await _context.Users.FindAsync(extension.DeferralId != null ? extension.DeferralId : (Guid?)null);
                    // Better fetch deferral's creator if available
                    var def = await _context.Deferrals.FindAsync(extension.DeferralId);
                    var rmUser = def != null ? await _context.Users.FindAsync(def.CreatedById) : null;
                    if (rmUser != null && !string.IsNullOrWhiteSpace(rmUser.Email))
                    {
                        var rmName = string.IsNullOrWhiteSpace(rmUser.Name) ? "Relationship Manager" : rmUser.Name;
                        await _emailService.SendExtensionApprovalRequestAsync(
                            rmUser.Email,
                            rmName,
                            extension.DeferralNumber ?? "Unknown",
                            User.FindFirst("name")?.Value ?? "Approver");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify RM after approver approved extension {ExtensionId}", extension.Id);
                }
                try
                {
                    var coCreators = await _context.Users.Where(u => u.Role == UserRole.CoCreator && u.Active).ToListAsync();
                    foreach (var cc in coCreators)
                    {
                        if (!string.IsNullOrEmpty(cc.Email))
                        {
                            try
                            {
                                await _emailService.SendExtensionApprovalRequestAsync(
                                    cc.Email,
                                    cc.Name,
                                    extension.DeferralNumber ?? "Unknown",
                                    User.FindFirst("name")?.Value ?? "Requester");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send extension notification to cocreator {email}", cc.Email);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to notify cocreators after all approvers approved");
                }

                // Notify RM that all approvers have approved the extension
                try
                {
                    var def = await _context.Deferrals.FindAsync(extension.DeferralId);
                    var rmUser = def != null ? await _context.Users.FindAsync(def.CreatedById) : null;
                    if (rmUser != null && !string.IsNullOrWhiteSpace(rmUser.Email))
                    {
                        var rmName = string.IsNullOrWhiteSpace(rmUser.Name) ? "Relationship Manager" : rmUser.Name;
                        await _emailService.SendExtensionApprovalRequestAsync(
                            rmUser.Email,
                            rmName,
                            extension.DeferralNumber ?? "Unknown",
                            User.FindFirst("name")?.Value ?? "Approver");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify RM after all approvers approved extension {ExtensionId}", extension.Id);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Extension approved", extension });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving extension");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/extensions/{id}/reject
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectExtension(Guid id, [FromBody] RejectExtensionRequest request)
    {
        try
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return StatusCode(401, new { message = "Invalid or missing user id claim" });

            var extension = await _context.Extensions
                .Include(e => e.Approvers)
                .Include(e => e.History)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (extension == null)
                return NotFound(new { message = "Extension not found" });

            var currentApprover = extension.Approvers.FirstOrDefault(a => a.IsCurrent);
            if (currentApprover == null || currentApprover.UserId != userId)
                return StatusCode(403, new { message = "Only current approver can reject" });

            // Mark as rejected
            currentApprover.ApprovalStatus = ApproverApprovalStatus.Rejected;
            currentApprover.ApprovalDate = DateTime.UtcNow;
            currentApprover.ApprovalComment = request.Reason;
            currentApprover.IsCurrent = false;

            extension.Status = ExtensionStatus.Rejected;
            extension.RejectionReason = request.Reason;
            extension.RejectedById = userId;
            extension.RejectedDate = DateTime.UtcNow;

            // Add to history
            extension.History.Add(new ExtensionHistory
            {
                Action = "rejected_by_approver",
                UserId = userId,
                UserName = User.FindFirst("name")?.Value,
                UserRole = User.FindFirst(ClaimTypes.Role)?.Value,
                Date = DateTime.UtcNow,
                Comment = request.Reason
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Extension rejected", extension });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting extension");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // ================================
    // CREATOR ROUTES
    // ================================

    // GET /api/extensions/creator/pending
    [HttpGet("creator/pending")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> GetCreatorPending()
    {
        try
        {
            var extensions = await _context.Extensions
                .Include(e => e.Deferral)
                .Include(e => e.Approvers).ThenInclude(a => a.User)
                .Include(e => e.RequestedBy)
                .Where(e => e.CreatorApprovalStatus == CreatorApprovalStatus.Pending)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Ok(extensions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting creator pending");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/extensions/{id}/approve-creator
    [HttpPut("{id}/approve-creator")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> ApproveAsCreator(Guid id, [FromBody] ApproveExtensionRequest request)
    {
        try
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
                return StatusCode(401, new { message = "Invalid or missing user id claim" });

            var extension = await _context.Extensions
                .Include(e => e.History)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (extension == null)
                return NotFound(new { message = "Extension not found" });

            extension.CreatorApprovalStatus = CreatorApprovalStatus.Approved;
            extension.CreatorApprovedById = userId;
            extension.CreatorApprovalDate = DateTime.UtcNow;
            extension.CreatorApprovalComment = request.Comment;

            extension.History.Add(new ExtensionHistory
            {
                Action = "approved_by_creator",
                UserId = userId,
                UserName = User.FindFirst("name")?.Value,
                UserRole = "Creator",
                Date = DateTime.UtcNow,
                Comment = request.Comment
            });

            await _context.SaveChangesAsync();

            // Notify RM that CoCreator approved the extension
            try
            {
                var def = await _context.Deferrals.FindAsync(extension.DeferralId);
                var rmUser = def != null ? await _context.Users.FindAsync(def.CreatedById) : null;
                if (rmUser != null && !string.IsNullOrWhiteSpace(rmUser.Email))
                {
                    var rmName = string.IsNullOrWhiteSpace(rmUser.Name) ? "Relationship Manager" : rmUser.Name;
                    await _emailService.SendExtensionApprovalRequestAsync(
                        rmUser.Email,
                        rmName,
                        extension.DeferralNumber ?? "Unknown",
                        User.FindFirst("name")?.Value ?? "CoCreator");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify RM after cocreator approved extension {ExtensionId}", extension.Id);
            }

            return Ok(new { message = "Extension approved by creator", extension });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving as creator");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/extensions/{id}/reject-creator
    [HttpPut("{id}/reject-creator")]
    [RoleAuthorize(UserRole.CoCreator)]
    public async Task<IActionResult> RejectAsCreator(Guid id, [FromBody] RejectExtensionRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var extension = await _context.Extensions
                .Include(e => e.History)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (extension == null)
                return NotFound(new { message = "Extension not found" });

            extension.CreatorApprovalStatus = CreatorApprovalStatus.Rejected;
            extension.CreatorApprovedById = userId;
            extension.CreatorApprovalDate = DateTime.UtcNow;
            extension.CreatorApprovalComment = request.Reason;
            extension.Status = ExtensionStatus.Rejected;

            extension.History.Add(new ExtensionHistory
            {
                Action = "rejected_by_creator",
                UserId = userId,
                UserName = User.FindFirst("name")?.Value,
                UserRole = "Creator",
                Date = DateTime.UtcNow,
                Comment = request.Reason
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Extension rejected by creator", extension });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting as creator");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // ================================
    // CHECKER ROUTES
    // ================================

    // GET /api/extensions/checker/pending
    [HttpGet("checker/pending")]
    [RoleAuthorize(UserRole.CoChecker)]
    public async Task<IActionResult> GetCheckerPending()
    {
        try
        {
            var extensions = await _context.Extensions
                .Include(e => e.Deferral)
                .Include(e => e.Approvers).ThenInclude(a => a.User)
                .Include(e => e.RequestedBy)
                .Where(e => e.CheckerApprovalStatus == CheckerApprovalStatus.Pending)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Ok(extensions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting checker pending");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/extensions/{id}/approve-checker
    [HttpPut("{id}/approve-checker")]
    [RoleAuthorize(UserRole.CoChecker)]
    public async Task<IActionResult> ApproveAsChecker(Guid id, [FromBody] ApproveExtensionRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var extension = await _context.Extensions
                .Include(e => e.History)
                .Include(e => e.Deferral)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (extension == null)
                return NotFound(new { message = "Extension not found" });

            extension.CheckerApprovalStatus = CheckerApprovalStatus.Approved;
            extension.CheckerApprovedById = userId;
            extension.CheckerApprovalDate = DateTime.UtcNow;
            extension.CheckerApprovalComment = request.Comment;
            extension.Status = ExtensionStatus.Approved;

            extension.History.Add(new ExtensionHistory
            {
                Action = "approved_by_checker",
                UserId = userId,
                UserName = User.FindFirst("name")?.Value,
                UserRole = "Checker",
                Date = DateTime.UtcNow,
                Comment = request.Comment
            });

            // Update original deferral
            if (extension.Deferral != null)
            {
                extension.Deferral.DaysSought = extension.RequestedDaysSought;
                // Potentially update deferral status or add note
            }

            await _context.SaveChangesAsync();

            // Notify RM that CoChecker approved and extension finalized
            try
            {
                var def = await _context.Deferrals.FindAsync(extension.DeferralId);
                var rmUser = def != null ? await _context.Users.FindAsync(def.CreatedById) : null;
                if (rmUser != null && !string.IsNullOrWhiteSpace(rmUser.Email))
                {
                    var rmName = string.IsNullOrWhiteSpace(rmUser.Name) ? "Relationship Manager" : rmUser.Name;
                    await _emailService.SendExtensionApprovalRequestAsync(
                        rmUser.Email,
                        rmName,
                        extension.DeferralNumber ?? "Unknown",
                        User.FindFirst("name")?.Value ?? "Checker");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify RM after cochecker approved extension {ExtensionId}", extension.Id);
            }

            return Ok(new { message = "Extension approved by checker", extension });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving as checker");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // PUT /api/extensions/{id}/reject-checker
    [HttpPut("{id}/reject-checker")]
    [RoleAuthorize(UserRole.CoChecker)]
    public async Task<IActionResult> RejectAsChecker(Guid id, [FromBody] RejectExtensionRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var extension = await _context.Extensions
                .Include(e => e.History)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (extension == null)
                return NotFound(new { message = "Extension not found" });

            extension.CheckerApprovalStatus = CheckerApprovalStatus.Rejected;
            extension.CheckerApprovedById = userId;
            extension.CheckerApprovalDate = DateTime.UtcNow;
            extension.CheckerApprovalComment = request.Reason;
            extension.Status = ExtensionStatus.Rejected;

            extension.History.Add(new ExtensionHistory
            {
                Action = "rejected_by_checker",
                UserId = userId,
                UserName = User.FindFirst("name")?.Value,
                UserRole = "Checker",
                Date = DateTime.UtcNow,
                Comment = request.Reason
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Extension rejected by checker", extension });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting as checker");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // ================================
    // GENERIC ROUTES
    // ================================

    // GET /api/extensions/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetExtensionById(Guid id)
    {
        try
        {
            var extension = await _context.Extensions
                .Include(e => e.Deferral)
                .Include(e => e.RequestedBy)
                .Include(e => e.Approvers).ThenInclude(a => a.User)
                .Include(e => e.History).ThenInclude(h => h.User)
                .Include(e => e.Comments).ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (extension == null)
                return NotFound(new { message = "Extension not found" });

            return Ok(extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting extension by id");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
