using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.Models;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/deferrals")]
[Authorize]
public class DeferralController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeferralController> _logger;

    public DeferralController(ApplicationDbContext context, ILogger<DeferralController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ============================================
    // CREATE & BASIC OPERATIONS
    // ============================================

    [HttpPost]
    public async Task<IActionResult> CreateDeferral([FromBody] CreateDeferralRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            // Generate deferral number (DEF-YY-XXXX format)
            var deferralNumber = await GenerateDeferralNumber();

            var deferral = new Deferral
            {
                Id = Guid.NewGuid(),
                DeferralNumber = deferralNumber,
                CustomerNumber = request.CustomerNumber,
                CustomerName = request.CustomerName,
                BusinessName = request.BusinessName,
                LoanType = request.LoanType,
                Status = DeferralStatus.Pending,
                CreatedById = userId,
                CurrentApproverIndex = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Deferrals.Add(deferral);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Deferral created: {deferralNumber}");

            return StatusCode(201, deferral);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating deferral");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // FETCH OPERATIONS
    // ============================================

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingDeferrals()
    {
        try
        {
            var deferrals = await _context.Deferrals
                .Where(d => d.Status == DeferralStatus.Pending)
                .Include(d => d.CreatedBy)
                .Include(d => d.Facilities)
                .Include(d => d.Documents)
                    .ThenInclude(doc => doc.UploadedBy)
                .Include(d => d.Approvers)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            _logger.LogInformation($"📊 Fetched {deferrals.Count} pending deferrals");
            return Ok(deferrals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error fetching pending deferrals");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("approved")]
    public async Task<IActionResult> GetApprovedDeferrals()
    {
        try
        {
            var deferrals = await _context.Deferrals
                .Where(d => d.Status == DeferralStatus.Approved)
                .Include(d => d.CreatedBy)
                .Include(d => d.Facilities)
                .Include(d => d.Documents)
                    .ThenInclude(doc => doc.UploadedBy)
                .Include(d => d.Approvers)
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();

            _logger.LogInformation($"✅ Fetched {deferrals.Count} approved deferrals");
            return Ok(deferrals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error fetching approved deferrals");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyDeferrals()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var deferrals = await _context.Deferrals
                .Where(d => d.CreatedById == userId)
                .Include(d => d.CreatedBy)
                .Include(d => d.Facilities)
                .Include(d => d.Documents)
                    .ThenInclude(doc => doc.UploadedBy)
                .Include(d => d.Approvers)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(deferrals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error fetching my deferrals");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("approver-queue")]
    public async Task<IActionResult> GetApproverQueue()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var deferrals = await _context.Deferrals
                .Where(d => d.Status == DeferralStatus.Pending &&
                           d.Approvers.Any(a => a.UserId == userId && !a.Approved))
                .Include(d => d.CreatedBy)
                .Include(d => d.Facilities)
                .Include(d => d.Documents)
                .Include(d => d.Approvers)
                    .ThenInclude(a => a.User)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(deferrals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error fetching approver queue");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("actioned")]
    public async Task<IActionResult> GetActionedDeferrals()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var deferrals = await _context.Deferrals
                .Where(d => d.Approvers.Any(a => a.UserId == userId && a.Approved))
                .Include(d => d.CreatedBy)
                .Include(d => d.Facilities)
                .Include(d => d.Documents)
                .Include(d => d.Approvers)
                    .ThenInclude(a => a.User)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(deferrals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error fetching actioned deferrals");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDeferral(Guid id)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.CreatedBy)
                .Include(d => d.Facilities)
                .Include(d => d.Documents)
                    .ThenInclude(doc => doc.UploadedBy)
                .Include(d => d.Approvers)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            return Ok(deferral);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error fetching deferral");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // DEFERRAL NUMBER GENERATION
    // ============================================

    [HttpGet("next-number")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNextDeferralNumber()
    {
        try
        {
            var deferralNumber = await GenerateDeferralNumber();
            return Ok(new { deferralNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error generating deferral number");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // FACILITIES OPERATIONS
    // ============================================

    [HttpPut("{id}/facilities")]
    public async Task<IActionResult> UpdateFacilities(Guid id, [FromBody] List<FacilityRequest> facilities)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.Facilities)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            _context.Facilities.RemoveRange(deferral.Facilities);

            foreach (var facilityReq in facilities)
            {
                var facility = new Facility
                {
                    Id = Guid.NewGuid(),
                    Type = facilityReq.Type,
                    Sanctioned = facilityReq.Sanctioned,
                    Balance = facilityReq.Balance,
                    Headroom = facilityReq.Headroom,
                    DeferralId = id
                };
                _context.Facilities.Add(facility);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Facilities updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error updating facilities");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // DEFERRAL UPDATE (Resubmit)
    // ============================================

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDeferral(Guid id, [FromBody] UpdateDeferralRequest request)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.Approvers)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            // Update allowed fields
            if (!string.IsNullOrEmpty(request.DeferralDescription))
                deferral.DeferralDescription = request.DeferralDescription;

            if (request.Facilities != null && request.Facilities.Count > 0)
            {
                _context.Facilities.RemoveRange(deferral.Facilities);
                foreach (var fac in request.Facilities)
                {
                    _context.Facilities.Add(new Facility
                    {
                        Id = Guid.NewGuid(),
                        Type = fac.Type,
                        Sanctioned = fac.Sanctioned,
                        Balance = fac.Balance,
                        Headroom = fac.Headroom,
                        DeferralId = id
                    });
                }
            }

            // Reset approval statuses on resubmission
            if (request.Status == DeferralStatus.Pending)
            {
                deferral.Status = DeferralStatus.Pending;
                deferral.CurrentApproverIndex = 0;

                foreach (var approver in deferral.Approvers)
                {
                    approver.Approved = false;
                    approver.ApprovedAt = null;
                }
            }

            deferral.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, deferral });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error updating deferral");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // DOCUMENT OPERATIONS
    // ============================================

    [HttpPost("{id}/documents")]
    public async Task<IActionResult> AddDocument(Guid id, [FromBody] AddDeferralDocumentRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var document = new DeferralDocument
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Url = request.Url,
                UploadedById = userId,
                DeferralId = id
            };

            _context.DeferralDocuments.Add(document);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Document added", document });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error adding document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/documents/{docId}")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid docId)
    {
        try
        {
            var document = await _context.DeferralDocuments
                .FirstOrDefaultAsync(d => d.Id == docId && d.DeferralId == id);

            if (document == null)
                return NotFound(new { error = "Document not found" });

            _context.DeferralDocuments.Remove(document);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Document deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error deleting document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // APPROVERS OPERATIONS
    // ============================================

    [HttpPut("{id}/approvers")]
    public async Task<IActionResult> SetApprovers(Guid id, [FromBody] List<ApproverRequest> approvers)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.Approvers)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            _context.Approvers.RemoveRange(deferral.Approvers);

            foreach (var approverReq in approvers)
            {
                var approver = new Approver
                {
                    Id = Guid.NewGuid(),
                    Name = approverReq.Name,
                    Approved = false,
                    DeferralId = id
                };
                _context.Approvers.Add(approver);
            }

            deferral.CurrentApproverIndex = 0;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Approvers set" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error setting approvers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/approvers/{index}")]
    public async Task<IActionResult> RemoveApprover(Guid id, int index)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.Approvers)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            if (index < 0 || index >= deferral.Approvers.Count)
                return BadRequest(new { error = "Invalid approver index" });

            var approverToRemove = deferral.Approvers.ElementAt(index);
            _context.Approvers.Remove(approverToRemove);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Approver removed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error removing approver");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // APPROVAL OPERATIONS
    // ============================================

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveDeferral(Guid id, [FromBody] ApprovalRequest? request = null)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var deferral = await _context.Deferrals
                .Include(d => d.Approvers)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            var currentApprover = deferral.Approvers.ElementAtOrDefault(deferral.CurrentApproverIndex);
            if (currentApprover == null || currentApprover.UserId != userId)
                return StatusCode(403, new { error = "Only current approver can take this action" });

            currentApprover.Approved = true;
            currentApprover.ApprovedAt = DateTime.UtcNow;

            if (deferral.CurrentApproverIndex + 1 < deferral.Approvers.Count)
            {
                deferral.CurrentApproverIndex++;
                deferral.Status = DeferralStatus.InReview;
            }
            else
            {
                deferral.Status = DeferralStatus.Approved;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Deferral {deferral.DeferralNumber} approved by {userId}");

            return Ok(new { message = "Approved successfully", status = deferral.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error approving deferral");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/approve-creator")]
    public async Task<IActionResult> ApproveByCreator(Guid id, [FromBody] ApprovalRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            var deferral = await _context.Deferrals
                .Include(d => d.CreatedBy)
                .Include(d => d.Approvers)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            if (deferral.CreatedById != userId && deferral.CreatedById != null)
                return StatusCode(403, new { error = "Only creator can approve" });

            deferral.CreatedById = userId;
            deferral.Status = DeferralStatus.Approved;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Approved by creator", deferral });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error in creator approval");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // REJECTION & RETURN OPERATIONS
    // ============================================

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectDeferral(Guid id, [FromBody] RejectDeferralRequest request)
    {
        try
        {
            var deferral = await _context.Deferrals.FindAsync(id);
            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            deferral.Status = DeferralStatus.Rejected;
            deferral.RejectionReason = request.Reason;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"❌ Deferral {deferral.DeferralNumber} rejected");

            return Ok(new { message = "Rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error rejecting deferral");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/return-for-rework")]
    public async Task<IActionResult> ReturnForRework(Guid id, [FromBody] ReturnForReworkRequest request)
    {
        try
        {
            var deferral = await _context.Deferrals.FindAsync(id);
            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            deferral.Status = DeferralStatus.ReturnedForRework;
            deferral.ReworkComments = request.ReworkComment;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"🔄 Deferral {deferral.DeferralNumber} returned for rework");

            return Ok(new { message = "Returned for rework" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error returning for rework");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> CloseDeferral(Guid id, [FromBody] CloseDeferralRequest request)
    {
        try
        {
            var deferral = await _context.Deferrals.FindAsync(id);
            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            deferral.Status = DeferralStatus.Rejected; // Mark as withdrawn/closed
            deferral.ClosedReason = request.Reason;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"🔒 Deferral {deferral.DeferralNumber} closed");

            return Ok(new { message = "Closed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error closing deferral");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // PDF & EXPORT OPERATIONS
    // ============================================

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GeneratePDF(Guid id)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.CreatedBy)
                .Include(d => d.Facilities)
                .Include(d => d.Documents)
                .Include(d => d.Approvers)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
                return NotFound(new { error = "Deferral not found" });

            // TODO: Implement PDF generation
            return Ok(new { message = "PDF generation not yet implemented", deferral });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error generating PDF");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============================================
    // HELPER METHODS
    // ============================================

    private async Task<string> GenerateDeferralNumber()
    {
        var yy = DateTime.UtcNow.Year.ToString().Substring(2);
        var prefix = $"DEF-{yy}-";
        var lastDeferral = await _context.Deferrals
            .Where(d => d.DeferralNumber.StartsWith(prefix))
            .OrderByDescending(d => d.DeferralNumber)
            .FirstOrDefaultAsync();

        int seq = 1;
        if (lastDeferral != null)
        {
            var match = System.Text.RegularExpressions.Regex.Match(lastDeferral.DeferralNumber, @"DEF-\d{2}-(\d{4})");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int lastSeq))
                seq = lastSeq + 1;
        }

        return $"{prefix}{seq:D4}";
    }
}

// ============================================
// REQUEST/RESPONSE MODELS
// ============================================

public class CreateDeferralRequest
{
    public string? CustomerNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? BusinessName { get; set; }
    public string? LoanType { get; set; }
}

public class UpdateDeferralRequest
{
    public string? DeferralDescription { get; set; }
    public List<FacilityRequest>? Facilities { get; set; }
    public DeferralStatus? Status { get; set; }
}

public class FacilityRequest
{
    public string? Type { get; set; }
    public decimal Sanctioned { get; set; }
    public decimal Balance { get; set; }
    public decimal Headroom { get; set; }
}

public class AddDeferralDocumentRequest
{
    public string? Name { get; set; }
    public string? Url { get; set; }
}

public class ApproverRequest
{
    public string? Name { get; set; }
}

public class ApprovalRequest
{
    public string? Comment { get; set; }
}

public class RejectDeferralRequest
{
    public string? Reason { get; set; }
}

public class ReturnForReworkRequest
{
    public string? ReworkComment { get; set; }
}

public class CloseDeferralRequest
{
    public string? Reason { get; set; }
}
