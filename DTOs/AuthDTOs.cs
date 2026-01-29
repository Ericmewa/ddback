using System.ComponentModel.DataAnnotations;
using NCBA.DCL.Models;

namespace NCBA.DCL.DTOs;

public class RegisterAdminRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserResponse User { get; set; } = null!;
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public string? CustomerNumber { get; set; }
    public string? CustomerId { get; set; }
    public string? RmId { get; set; }
}

public class RegisterAuthDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginAuthDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}
