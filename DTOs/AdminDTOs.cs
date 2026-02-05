namespace NCBA.DCL.DTOs
{
    public class RegisterAdminDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginAdminDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class CreateUserDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class TransferRoleDto
    {
        public string NewRole { get; set; }
    }

    public class ReassignTasksDto
    {
        public string ToUserId { get; set; }
    }
}
