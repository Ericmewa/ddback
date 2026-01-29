using System.Threading.Tasks;
using NCBA.DCL.DTOs;

namespace NCBA.DCL.Services
{
    public interface IAdminService
    {
        Task<(int StatusCode, object Body)> RegisterAdminAsync(RegisterAdminDto dto);
        Task<(int StatusCode, object Body)> LoginAdminAsync(LoginAdminDto dto);
        Task<(int StatusCode, object Body)> CreateUserAsync(CreateUserDto dto);
        Task<(int StatusCode, object Body)> ToggleActiveAsync(string id);
        Task<(int StatusCode, object Body)> ArchiveUserAsync(string id);
        Task<(int StatusCode, object Body)> TransferRoleAsync(string id, TransferRoleDto dto);
        Task<(int StatusCode, object Body)> ReassignTasksAsync(string id, ReassignTasksDto dto);
    }
}
