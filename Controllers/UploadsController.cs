using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NCBA.DCL.Data;
using NCBA.DCL.Models;
using Microsoft.EntityFrameworkCore;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize]
public class UploadsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<UploadsController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Test() => Ok(new { message = "Upload API is working!" });

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload([FromForm] NCBA.DCL.DTOs.FileUploadDto dto)
    {
        try
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { success = false, error = "No file uploaded" });

            var checklistIdStr = dto.ChecklistId;
            var documentIdStr = dto.DocumentId;
            var documentName = dto.DocumentName ?? dto.File.FileName;
            var category = dto.Category;

            // Read file as binary
            byte[] fileData;
            using (var stream = new MemoryStream())
            {
                await dto.File.CopyToAsync(stream);
                fileData = stream.ToArray();
            }

            var upload = new Upload
            {
                Id = Guid.NewGuid(),
                ChecklistId = Guid.TryParse(checklistIdStr, out var cId) ? cId : (Guid?)null,
                DocumentId = Guid.TryParse(documentIdStr, out var dId) ? dId : (Guid?)null,
                DocumentName = documentName,
                Category = category,
                FileName = dto.File.FileName,
                FileData = fileData, // Store binary data in database
                FilePath = null, // No longer using filesystem
                FileUrl = null, // Will be set after saving
                FileSize = dto.File.Length,
                FileType = dto.File.ContentType,
                UploadedBy = User?.Identity?.Name ?? "RM",
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Uploads.Add(upload);
            await _context.SaveChangesAsync();

            // Set the fileUrl with the actual upload ID
            upload.FileUrl = $"/api/uploads/{upload.Id}";
            _context.Uploads.Update(upload);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                success = true,
                data = new
                {
                    _id = upload.Id,
                    checklistId = upload.ChecklistId,
                    documentId = upload.DocumentId,
                    documentName = upload.DocumentName,
                    category = upload.Category,
                    fileName = upload.FileName,
                    fileUrl = upload.FileUrl,
                    fileSize = upload.FileSize,
                    fileType = upload.FileType,
                    uploadedBy = upload.UploadedBy,
                    status = upload.Status,
                    createdAt = upload.CreatedAt,
                    updatedAt = upload.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload error");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var upload = await _context.Uploads.FindAsync(id);
            if (upload == null) return NotFound(new { success = false, error = "File not found" });

            if (upload.FileData == null || upload.FileData.Length == 0)
                return NotFound(new { success = false, error = "File data not found" });

            // Return file as binary with appropriate content type
            return File(upload.FileData, upload.FileType ?? "application/octet-stream", upload.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get file error");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("checklist/{checklistId}")]
    public async Task<IActionResult> GetByChecklist(Guid checklistId)
    {
        var uploads = await _context.Uploads
            .Where(u => u.ChecklistId == checklistId && u.Status == "active")
            .ToListAsync();

        return Ok(new { success = true, data = uploads });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var upload = await _context.Uploads.FindAsync(id);
            if (upload == null) return NotFound(new { success = false, error = "Upload not found" });

            if (!string.IsNullOrEmpty(upload.FilePath))
            {
                var fullPath = Path.Combine(_env.ContentRootPath, upload.FilePath.TrimStart('/', '\\'));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }

            _context.Uploads.Remove(upload);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "File deleted successfully", data = new { id = upload.Id, fileName = upload.FileName } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete error");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
