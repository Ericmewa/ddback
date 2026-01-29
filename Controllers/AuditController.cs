using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NCBA.DCL.DTOs;
using NCBA.DCL.Services;
using System;
using System.Threading.Tasks;

namespace NCBA.DCL.Controllers
{
    [ApiController]
    [Route("api/audit")]
    [Authorize(Roles = "admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        public AuditController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string action = null, [FromQuery] Guid? userId = null, [FromQuery] string resource = null, [FromQuery] string status = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] string search = null)
        {
            var result = await _auditLogService.GetLogsAsync(page, limit, action, userId, resource, status, startDate, endDate, search);
            return StatusCode(result.StatusCode, result.Body);
        }

        [HttpGet("logs/{id}")]
        public async Task<IActionResult> GetLogById(Guid id)
        {
            var result = await _auditLogService.GetLogByIdAsync(id);
            return StatusCode(result.StatusCode, result.Body);
        }

        [HttpPost("logs")]
        public async Task<IActionResult> CreateLog([FromBody] AuditLogCreateDto dto)
        {
            var result = await _auditLogService.CreateLogAsync(dto);
            return StatusCode(result.StatusCode, result.Body);
        }

        [HttpGet("logs/export")]
        public async Task<IActionResult> ExportLogs([FromQuery] string action = null, [FromQuery] Guid? userId = null, [FromQuery] string resource = null, [FromQuery] string status = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] string search = null)
        {
            var result = await _auditLogService.ExportLogsAsync(action, userId, resource, status, startDate, endDate, search);
            // CSV export logic (convert result.Body to CSV and return as file)
            // For now, return as JSON
            return StatusCode(result.StatusCode, result.Body);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _auditLogService.GetStatsAsync();
            return StatusCode(result.StatusCode, result.Body);
        }

        [HttpGet("online-users")]
        public async Task<IActionResult> GetOnlineUsersWithActivity()
        {
            var result = await _auditLogService.GetOnlineUsersWithActivityAsync();
            return StatusCode(result.StatusCode, result.Body);
        }
    }
}
