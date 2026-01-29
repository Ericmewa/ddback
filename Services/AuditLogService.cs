using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Models;

namespace NCBA.DCL.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _db;
        public AuditLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(int StatusCode, object Body)> GetLogsAsync(int page, int limit, string? action, Guid? userId, string? resource, string? status, DateTime? startDate, DateTime? endDate, string? search)
        {
            var query = _db.AuditLogs.AsQueryable();
            if (!string.IsNullOrEmpty(action)) query = query.Where(x => x.Action == action);
            if (userId.HasValue) query = query.Where(x => x.PerformedById == userId);
            if (!string.IsNullOrEmpty(resource)) query = query.Where(x => x.Resource == resource);
            if (!string.IsNullOrEmpty(status)) query = query.Where(x => x.Status == status);
            if (startDate.HasValue) query = query.Where(x => x.CreatedAt >= startDate);
            if (endDate.HasValue) query = query.Where(x => x.CreatedAt <= endDate);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x =>
                    x.Action.Contains(search) ||
                    (x.Resource != null && x.Resource.Contains(search)) ||
                    (x.Details != null && x.Details.Contains(search)));
            }
            var total = await query.CountAsync();
            var logs = await query
                .Include(x => x.PerformedBy)
                .Include(x => x.TargetUser)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();
            return (200, new { logs, total, page, limit });
        }

        public async Task<(int StatusCode, object Body)> GetLogByIdAsync(Guid id)
        {
            var log = await _db.AuditLogs
                .Include(x => x.PerformedBy)
                .Include(x => x.TargetUser)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (log == null) return (404, new { message = "Audit log not found" });
            return (200, log);
        }

        public async Task<(int StatusCode, object Body)> CreateLogAsync(AuditLogCreateDto dto)
        {
            var log = new AuditLog
            {
                Action = dto.Action,
                Resource = dto.Resource,
                Status = dto.Status,
                Details = dto.Details,
                ErrorMessage = dto.ErrorMessage,
                PerformedById = dto.PerformedById,
                TargetUserId = dto.TargetUserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
            return (201, log);
        }

        public async Task<(int StatusCode, object Body)> ExportLogsAsync(string? action, Guid? userId, string? resource, string? status, DateTime? startDate, DateTime? endDate, string? search)
        {
            var query = _db.AuditLogs.AsQueryable();
            if (!string.IsNullOrEmpty(action)) query = query.Where(x => x.Action == action);
            if (userId.HasValue) query = query.Where(x => x.PerformedById == userId);
            if (!string.IsNullOrEmpty(resource)) query = query.Where(x => x.Resource == resource);
            if (!string.IsNullOrEmpty(status)) query = query.Where(x => x.Status == status);
            if (startDate.HasValue) query = query.Where(x => x.CreatedAt >= startDate);
            if (endDate.HasValue) query = query.Where(x => x.CreatedAt <= endDate);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x =>
                    x.Action.Contains(search) ||
                    (x.Resource != null && x.Resource.Contains(search)) ||
                    (x.Details != null && x.Details.Contains(search)));
            }
            var logs = await query
                .Include(x => x.PerformedBy)
                .Include(x => x.TargetUser)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            // CSV export logic to be implemented in controller
            return (200, logs);
        }

        public async Task<(int StatusCode, object Body)> GetStatsAsync()
        {
            var totalLogs = await _db.AuditLogs.CountAsync();
            var todayStart = DateTime.UtcNow.Date;
            var todayLogs = await _db.AuditLogs.CountAsync(x => x.CreatedAt >= todayStart);
            var successLogs = await _db.AuditLogs.CountAsync(x => x.Status == "success");
            var failureLogs = await _db.AuditLogs.CountAsync(x => x.Status == "failure");
            return (200, new { totalLogs, todayLogs, successLogs, failureLogs });
        }

        public Task<(int StatusCode, object Body)> GetOnlineUsersWithActivityAsync()
        {
            // Placeholder: online users tracking is not implemented in backend
            return Task.FromResult<(int, object)>((200, Array.Empty<object>()));
        }
    }
}