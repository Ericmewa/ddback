using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Models;
using NCBA.DCL.Helpers;

namespace NCBA.DCL.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        public AuthService(ApplicationDbContext db, JwtTokenGenerator jwtTokenGenerator)
        {
            _db = db;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<(int StatusCode, object Body)> RegisterAdminAsync(RegisterAuthDto dto)
        {
            var exists = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (exists != null)
                return (400, new { message = "Admin already exists" });
            var admin = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = PasswordHasher.HashPassword(dto.Password),
                Role = UserRole.Admin,
                Active = true
            };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();
            return (201, new { message = "Admin registered", admin });
        }

        public async Task<(int StatusCode, object Body)> LoginAsync(LoginAuthDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return (404, new { message = "User not found" });
            if (!user.Active)
                return (403, new { message = "Account deactivated" });
            if (!PasswordHasher.VerifyPassword(dto.Password, user.Password))
                return (401, new { message = "Invalid credentials" });
            var token = _jwtTokenGenerator.GenerateToken(user);
            return (200, new { token, user = new { id = user.Id, name = user.Name, email = user.Email, role = user.Role } });
        }
    }
}