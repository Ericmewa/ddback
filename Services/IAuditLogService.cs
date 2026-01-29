using System;
using System.Threading.Tasks;
using NCBA.DCL.DTOs;

namespace NCBA.DCL.Services
{
    public interface IAuditLogService
    {
        Task<(int StatusCode, object Body)> GetLogsAsync(int page, int limit, string? action, Guid? userId, string? resource, string? status, DateTime? startDate, DateTime? endDate, string? search);
        Task<(int StatusCode, object Body)> GetLogByIdAsync(Guid id);
        Task<(int StatusCode, object Body)> CreateLogAsync(AuditLogCreateDto dto);
        Task<(int StatusCode, object Body)> ExportLogsAsync(string? action, Guid? userId, string? resource, string? status, DateTime? startDate, DateTime? endDate, string? search);
        Task<(int StatusCode, object Body)> GetStatsAsync();
        Task<(int StatusCode, object Body)> GetOnlineUsersWithActivityAsync();
    }
}