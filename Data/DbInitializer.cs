using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.Helpers;
using NCBA.DCL.Models;

namespace NCBA.DCL.Data
{
    public static class DbInitializer
    {
        public static async Task SeedData(ApplicationDbContext context)
        {
            await context.Database.MigrateAsync();

            if (!await context.Users.AnyAsync(u => u.Email == "admin@ncba.com"))
            {
                var admin = new User
                {
                    Id = Guid.NewGuid(),
                    Name = "Super Admin",
                    Email = "admin@ncba.com",
                    Password = PasswordHasher.HashPassword("Password@123"),
                    Role = UserRole.Admin,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(admin);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Admin user seeded: admin@ncba.com / Password@123");
            }
        }
    }
}
