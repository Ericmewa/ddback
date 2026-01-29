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

    [HttpPost]
    public async Task<IActionResult> CreateDeferral([FromBody] CreateDeferralRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);

            // Generate deferral number
            var lastDeferral = await _context.Deferrals
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            var deferralNumber = lastDeferral == null
                ? "DEF-000001"
                : $"DEF-{(int.Parse(lastDeferral.DeferralNumber.Split('-')[1]) + 1):D6}";

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

            return StatusCode(201, new
            {
                message = "Deferral created successfully",
                deferral = new
                {
                    id = deferral.Id,
                    deferralNumber = deferral.DeferralNumber,
                    status = deferral.Status.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deferral");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

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
                .Include(d => d.Approvers)
                .ToListAsync();

            return Ok(deferrals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending deferrals");
            return StatusCode(500, new { message = "Internal server error" });
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
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
            {
                return NotFound(new { message = "Deferral not found" });
            }

            return Ok(deferral);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deferral");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/facilities")]
    public async Task<IActionResult> UpdateFacilities(Guid id, [FromBody] List<FacilityRequest> facilities)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.Facilities)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
            {
                return NotFound(new { message = "Deferral not found" });
            }

            // Remove existing facilities
            _context.Facilities.RemoveRange(deferral.Facilities);

            // Add new facilities
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

            return Ok(new { message = "Facilities updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating facilities");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

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

            return StatusCode(201, new
            {
                message = "Document added successfully",
                document = new
                {
                    id = document.Id,
                    name = document.Name,
                    url = document.Url
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document");
            return StatusCode(500, new { message = "Internal server error" });
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
            {
                return NotFound(new { message = "Document not found" });
            }

            _context.DeferralDocuments.Remove(document);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Document deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/approvers")]
    public async Task<IActionResult> SetApprovers(Guid id, [FromBody] List<ApproverRequest> approvers)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.Approvers)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
            {
                return NotFound(new { message = "Deferral not found" });
            }

            // Remove existing approvers
            _context.Approvers.RemoveRange(deferral.Approvers);

            // Add new approvers
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

            await _context.SaveChangesAsync();

            return Ok(new { message = "Approvers set successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting approvers");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveDeferral(Guid id)
    {
        try
        {
            var deferral = await _context.Deferrals
                .Include(d => d.Approvers)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deferral == null)
            {
                return NotFound(new { message = "Deferral not found" });
            }

            var currentApprover = deferral.Approvers.ElementAtOrDefault(deferral.CurrentApproverIndex);
            if (currentApprover != null)
            {
                currentApprover.Approved = true;
                currentApprover.ApprovedAt = DateTime.UtcNow;
            }

            deferral.CurrentApproverIndex++;

            // If all approvers have approved
            if (deferral.CurrentApproverIndex >= deferral.Approvers.Count)
            {
                deferral.Status = DeferralStatus.Approved;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Deferral approved successfully",
                status = deferral.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving deferral");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectDeferral(Guid id, [FromBody] RejectDeferralRequest request)
    {
        try
        {
            var deferral = await _context.Deferrals.FindAsync(id);
            if (deferral == null)
            {
                return NotFound(new { message = "Deferral not found" });
            }

            deferral.Status = DeferralStatus.Rejected;
            deferral.RejectionReason = request.Reason;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Deferral rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting deferral");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

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
            {
                return NotFound(new { message = "Deferral not found" });
            }

            // TODO: Implement PDF generation using a library like iTextSharp or PdfSharpCore
            // For now, return the data as JSON
            return Ok(new
            {
                message = "PDF generation not implemented yet",
                deferral
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CreateDeferralRequest
{
    public string? CustomerNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? BusinessName { get; set; }
    public string? LoanType { get; set; }
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
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class ApproverRequest
{
    public string Name { get; set; } = string.Empty;
}

public class RejectDeferralRequest
{
    public string Reason { get; set; } = string.Empty;
}
