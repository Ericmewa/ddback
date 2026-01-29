using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Helpers;
using NCBA.DCL.Middleware;
using NCBA.DCL.Models;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(ApplicationDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [RoleAuthorize(UserRole.Admin)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
            {
                return BadRequest(new { message = "User already exists" });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email,
                Password = PasswordHasher.HashPassword(request.Password),
                Role = request.Role,
                CustomerNumber = request.CustomerNumber,
                CustomerId = request.CustomerId,
                RmId = request.RmId,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // Log the action
            var performedById = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
            var log = new UserLog
            {
                Id = Guid.NewGuid(),
                Action = "CREATE_USER",
                TargetUserId = user.Id,
                TargetEmail = user.Email,
                PerformedById = performedById,
                Timestamp = DateTime.UtcNow
            };
            _context.UserLogs.Add(log);

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "User created successfully",
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.Active)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    role = u.Role.ToString(),
                    active = u.Active,
                    customerNumber = u.CustomerNumber,
                    customerId = u.CustomerId,
                    rmId = u.RmId,
                    createdAt = u.CreatedAt,
                    updatedAt = u.UpdatedAt
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.Active);
            var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
            var rmCount = await _context.Users.CountAsync(u => u.Role == UserRole.RM);
            var coCreatorCount = await _context.Users.CountAsync(u => u.Role == UserRole.CoCreator);
            var coCheckerCount = await _context.Users.CountAsync(u => u.Role == UserRole.CoChecker);
            var customerCount = await _context.Users.CountAsync(u => u.Role == UserRole.Customer);

            return Ok(new
            {
                totalUsers,
                activeUsers,
                inactiveUsers = totalUsers - activeUsers,
                adminCount,
                rmCount,
                coCreatorCount,
                coCheckerCount,
                customerCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stats");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/active")]
    [RoleAuthorize(UserRole.Admin)]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.Active = !user.Active;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"User {(user.Active ? "activated" : "deactivated")} successfully",
                active = user.Active
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user active status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.Role = request.Role;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Role updated successfully",
                role = user.Role.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user role");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class ChangeRoleRequest
{
    public UserRole Role { get; set; }
}
