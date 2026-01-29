using System.Threading.Tasks;
using NCBA.DCL.DTOs;

namespace NCBA.DCL.Services
{
    public interface IAuthService
    {
        Task<(int StatusCode, object Body)> RegisterAdminAsync(RegisterAuthDto dto);
        Task<(int StatusCode, object Body)> LoginAsync(LoginAuthDto dto);
    }
}