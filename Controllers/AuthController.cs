using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Helpers;
using NCBA.DCL.Models;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        JwtTokenGenerator tokenGenerator,
        ILogger<AuthController> logger)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request)
    {
        try
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
            {
                return BadRequest(new { message = "Admin already exists" });
            }

            var admin = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email,
                Password = PasswordHasher.HashPassword(request.Password),
                Role = UserRole.Admin,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Admin registered",
                admin = new
                {
                    id = admin.Id,
                    name = admin.Name,
                    email = admin.Email,
                    role = admin.Role.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering admin");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!user.Active)
            {
                return StatusCode(403, new { message = "Account deactivated" });
            }

            if (!PasswordHasher.VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var token = _tokenGenerator.GenerateToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                User = new UserResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
