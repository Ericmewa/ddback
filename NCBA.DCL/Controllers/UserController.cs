
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

    // ---------------------------------
    // CREATE USER
    // ---------------------------------
    [HttpPost]
    [RoleAuthorize(UserRole.Admin)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // 1️⃣ Validate role string
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                return BadRequest(new { message = "Invalid role" });
            }

            // 2️⃣ Check if email already exists
            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
            {
                return BadRequest(new { message = "User already exists" });
            }

            // 3️⃣ Generate CustomerNumber for Customers only
            string? customerNumber = null;
            if (role == UserRole.Customer)
            {
                customerNumber = await GenerateUniqueCustomerNumber();
            }

            // 4️⃣ Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email,
                Password = PasswordHasher.HashPassword(request.Password),
                Role = role,
                CustomerNumber = customerNumber ?? request.CustomerNumber,
                CustomerId = request.CustomerId?.ToString(),
                RmId = request.RmId?.ToString(),
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // 5️⃣ Log the action
            Guid performedById = Guid.Empty;
            var claim = User.FindFirst("id")?.Value;
            if (!string.IsNullOrEmpty(claim))
            {
                Guid.TryParse(claim, out performedById);
            }

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

            // 6️⃣ Save changes
            await _context.SaveChangesAsync();

            // 7️⃣ Return success
            return StatusCode(201, new
            {
                message = "User created successfully",
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role.ToString(),
                    customerNumber = user.CustomerNumber
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // ---------------------------------
    // GET ALL USERS
    // ---------------------------------
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _context.Users
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

    // ---------------------------------
    // GET USERS STATS
    // ---------------------------------
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

    // ---------------------------------
    // TOGGLE ACTIVE STATUS
    // ---------------------------------
    [HttpPut("{id}/active")]
    [RoleAuthorize(UserRole.Admin)]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

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

    // ---------------------------------
    // CHANGE ROLE
    // ---------------------------------
    [HttpPut("{id}/role")]
    [RoleAuthorize(UserRole.Admin)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        try
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                return BadRequest(new { message = "Invalid role" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            user.Role = role;
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

    // ---------------------------------
    // HELPER: Generate unique customer number
    // ---------------------------------
    private async Task<string> GenerateUniqueCustomerNumber()
    {
        string number;
        do
        {
            number = GenerateCustomerNumber();
        } while (await _context.Users.AnyAsync(u => u.CustomerNumber == number && u.Role == UserRole.Customer));

        return number;
    }

    private string GenerateCustomerNumber()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = new Random().Next(1000, 9999); // 4-digit random
        return $"CUST-{datePart}-{randomPart}";
    }
}

// ---------------------------------
// DTOs
// ---------------------------------
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // string for parsing
    public string CustomerNumber { get; set; } = string.Empty; // optional, for non-customers
    public Guid? CustomerId { get; set; }
    public Guid? RmId { get; set; }
}

public class ChangeRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
